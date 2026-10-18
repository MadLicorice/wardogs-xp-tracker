"""Small, synthetic regressions for the shipping-file privacy audit."""
from pathlib import Path
import importlib.util
import json
import struct
import subprocess
import sys
import tempfile
import unittest
import zlib

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location("audit", Path(__file__).with_name("AuditPublicRelease.py"))
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


def chunk(kind, payload):
    return struct.pack(">I", len(payload)) + kind + payload + struct.pack(">I", zlib.crc32(kind + payload) & 0xffffffff)


def png(extra=b""):
    return audit.PNG_SIGNATURE + chunk(b"IHDR", struct.pack(">IIBBBBB", 1, 1, 8, 6, 0, 0, 0)) + extra + chunk(b"IDAT", zlib.compress(b"\0\0\0\0\xff")) + chunk(b"IEND", b"")


def ico(frames):
    result = bytearray(struct.pack("<HHH", 0, 1, len(frames)))
    offset = 6 + 16 * len(frames)
    for frame in frames:
        result.extend(struct.pack("<BBBBHHII", 1, 1, 0, 0, 1, 32, len(frame), offset))
        offset += len(frame)
    return bytes(result) + b"".join(frames)


def jpeg(marker=None):
    extra = b"" if marker is None else b"\xff" + bytes([marker]) + b"\0\x05abc"
    return b"\xff\xd8" + extra + b"\xff\xc0\0\x02\xff\xda\0\x02pixel\xff\x00stuff\xff\xd9"


def bundle(entries):
    host = bytearray(b"H" * 128)
    host[16:48] = audit.BUNDLE_SIGNATURE
    manifest_entries = []
    for name, data, compressed in entries:
        payload = zlib.compress(data)[2:-4] if compressed else data
        offset = len(host)
        host.extend(payload)
        encoded = name.encode()
        manifest_entries.append(struct.pack("<qqqB", offset, len(data), len(payload) if compressed else 0, 1) + bytes([len(encoded)]) + encoded)
    struct.pack_into("<q", host, 8, len(host))
    host.extend(struct.pack("<IIi", 6, 0, len(entries)) + b"\x01x" + b"\0" * 40 + b"".join(manifest_entries))
    return bytes(host)


def pe(debug_type):
    data = bytearray(1024)
    data[:2] = b"MZ"
    struct.pack_into("<I", data, 0x3c, 128)
    data[128:132] = b"PE\0\0"
    struct.pack_into("<H", data, 134, 1)
    struct.pack_into("<H", data, 148, 240)
    struct.pack_into("<H", data, 152, 0x20b)
    struct.pack_into("<II", data, 152 + 112 + 48, 0x1000, 28)
    struct.pack_into("<IIII", data, 152 + 240 + 8, 512, 0x1000, 512, 512)
    struct.pack_into("<I", data, 512 + 12, debug_type)
    return bytes(data)


class PrivacyTests(unittest.TestCase):
    def test_windows_profile_and_escaped_source_paths(self):
        value = "Q:" + "\\Users\\" + "PrivateTester\\project\\file.cs"
        self.assertTrue(audit.has_private_path(value.encode()))
        self.assertTrue(audit.has_private_path(value.replace("\\", "\\\\").encode()))

    def test_mac_and_linux_profiles(self):
        for base in ("/Us" + "ers/", "/ho" + "me/"):
            self.assertTrue(audit.has_private_path((base + "PrivateTester/project/file").encode()))

    def test_network_path(self):
        value = "\\" * 2 + "PrivateServer\\PrivateShare\\folder"
        self.assertTrue(audit.has_private_path(value.encode()))

    def test_generic_non_system_drive(self):
        self.assertTrue(audit.has_private_path(("Q:" + "\\PersonalFolder\\project\\file.cs").encode()))

    def test_allowed_paths_and_functional_ids(self):
        for value in ("C:" + "\\Windows\\System32\\kernel32.dll", "C:" + "\\path\\to\\dotnet.exe", "C:" + "\\season.md", "MadLicorice", "00000000-0000-0000-0000-000000000001"):
            self.assertEqual([], audit.private_reasons(value.encode()))

    def test_email_and_required_license_allowance(self):
        value = "private.tester" + "@" + "invalid.test"
        self.assertIn("Email address outside required license notices", audit.private_reasons(value.encode()))
        self.assertEqual([], audit.private_reasons(value.encode(), [value]))

    def test_utf16_binary(self):
        value = "Q:" + "\\Users\\PrivateTester\\file.cs"
        self.assertTrue(audit.private_reasons(b"\0" * 8 + value.encode("utf-16-le"), binary=True))

    def test_percent_encoded_path(self):
        value = "Q" + "%3A%5CUsers%5C" + "PrivateTester%5Cfile.cs"
        self.assertTrue(audit.has_private_path(value.encode()))

    def test_identifier_literal(self):
        self.assertIn("Personal author or machine identifier", audit.private_reasons(b'creator="' + b'PrivateTester' + b'"'))
        self.assertEqual([], audit.private_reasons(b'Creator="MadLicorice"'))

    def test_json_and_xml_identifier_literals(self):
        for value in (b'{"cre' + b'ator": "' + b'PrivateTester' + b'"}', b'<dc:' + b'creator>' + b'PrivateTester' + b'</dc:creator>'):
            self.assertIn("Personal author or machine identifier", audit.private_reasons(value))


class ImageTests(unittest.TestCase):
    def test_minimal_png(self):
        self.assertEqual([], audit.image_reasons(png(), ".png"))

    def test_every_optional_png_chunk_is_rejected(self):
        for kind in (b"tEXt", b"iTXt", b"zTXt", b"eXIf", b"pHYs", b"gAMA", b"sRGB", b"tIME", b"iCCP"):
            with self.subTest(kind=kind):
                self.assertTrue(audit.image_reasons(png(chunk(kind, b"payload")), ".png"))

    def test_png_bad_crc(self):
        data = bytearray(png()); data[-1] ^= 1
        with self.assertRaises(audit.AuditFormatError): audit.image_reasons(bytes(data), ".png")

    def test_png_truncated_or_trailing(self):
        for data in (png()[:-1], png() + b"extra"):
            with self.assertRaises(audit.AuditFormatError): audit.image_reasons(data, ".png")

    def test_ico_all_png_frames(self):
        self.assertEqual([], audit.image_reasons(ico([png(), png()]), ".ico"))
        self.assertTrue(audit.image_reasons(ico([png(), png(chunk(b"tEXt", b"payload"))]), ".ico"))

    def test_ico_invalid_bounds(self):
        data = bytearray(ico([png()])); struct.pack_into("<I", data, 18, len(data) + 1)
        with self.assertRaises(audit.AuditFormatError): audit.image_reasons(bytes(data), ".ico")

    def test_ico_v5_profile(self):
        dib = bytearray(124); struct.pack_into("<I", dib, 0, 124); struct.pack_into("<I", dib, 112, 124)
        self.assertTrue(audit.image_reasons(ico([bytes(dib)]), ".ico"))

    def test_ico_unreferenced_data(self):
        self.assertTrue(audit.image_reasons(ico([png()]) + b"metadata", ".ico"))

    def test_bare_jpeg(self):
        self.assertEqual([], audit.image_reasons(jpeg(), ".jpg"))

    def test_jpeg_all_app_segments_and_comment(self):
        for marker in list(range(0xe0, 0xf0)) + [0xfe]:
            with self.subTest(marker=marker): self.assertTrue(audit.image_reasons(jpeg(marker), ".jpg"))

    def test_jpeg_truncated_or_trailing(self):
        for data in (jpeg()[:-1], jpeg() + b"extra"):
            with self.assertRaises(audit.AuditFormatError): audit.image_reasons(data, ".jpg")

    def test_embedded_images(self):
        reasons, count = audit.embedded_image_reasons(b"header" + png(chunk(b"tIME", b"payload")) + b"tail" + jpeg(0xe1))
        self.assertEqual(2, count); self.assertEqual(2, len(reasons))


class CompiledTests(unittest.TestCase):
    def test_bundle_reads_compressed_actual_bytes(self):
        payload = ("Q:" + "\\Users\\PrivateTester\\source.cs").encode()
        data = bundle([("WardogsXpTracker.dll", payload, True), ("Other.dll", b"data", False)])
        entries = audit.read_bundle(data)
        self.assertEqual(payload, entries[0][2]); self.assertTrue(audit.has_private_path(entries[0][2]))

    def test_bundle_rejects_bad_offsets(self):
        data = bytearray(bundle([("A.dll", b"data", False)]))
        header = struct.unpack_from("<q", data, 8)[0]
        struct.pack_into("<q", data, header + 12 + 2 + 40, len(data) + 1)
        with self.assertRaises(audit.AuditFormatError): audit.read_bundle(bytes(data))

    def test_bundle_rejects_unsafe_names(self):
        with self.assertRaises(audit.AuditFormatError): audit.read_bundle(bundle([("../file.dll", b"data", False)]))

    def test_bundle_rejects_duplicate_names(self):
        with self.assertRaises(audit.AuditFormatError): audit.read_bundle(bundle([("A.dll", b"a", False), ("A.dll", b"b", False)]))

    def test_bundle_rejects_expansion_beyond_declared_length(self):
        data = bytearray(bundle([("A.dll", b"x" * 100, True)]))
        header = struct.unpack_from("<q", data, 8)[0]
        struct.pack_into("<q", data, header + 12 + 2 + 40 + 8, 1)
        with self.assertRaises(audit.AuditFormatError): audit.read_bundle(bytes(data))

    def test_pe_codeview_embedded_and_reproducible(self):
        for kind in (2, 17, 16): self.assertEqual([kind], audit.pe_debug_types(pe(kind)))

    def test_non_pe_and_bad_pe(self):
        self.assertEqual([], audit.pe_debug_types(b"not a PE"))
        with self.assertRaises(audit.AuditFormatError): audit.pe_debug_types(b"MZ")

    def test_named_pipe_constant_is_functional(self):
        value = "\\" * 2 + ".\\pipe\\"
        self.assertEqual([], audit.private_reasons(value.encode(), binary=True, vendor=True))


class ScopeTests(unittest.TestCase):
    def test_tracked_bytecode_and_cache_are_rejected_even_when_ignored(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            subprocess.run(["git", "init", "--quiet", str(root)], check=True, capture_output=True)
            (root / ".gitignore").write_text("__pycache__/\n*.pyc\n*.pyo\n", encoding="utf-8")
            cache = root / "__pycache__"
            cache.mkdir()
            for relative in ("module.pyc", "module.pyo", "__pycache__/module.bin"):
                with self.subTest(relative=relative):
                    path = root / relative
                    path.write_bytes(b"synthetic cache")
                    subprocess.run(["git", "add", "--force", "--", relative], cwd=root, check=True, capture_output=True)
                    report = audit.inspect_repo(root)
                    self.assertFalse(report["Passed"])
                    self.assertTrue(any(item["Reason"] == "Python bytecode or cache in shipping source" for item in report["Failures"]))

    def test_git_scope_and_report_redaction(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            subprocess.run(["git", "init", "--quiet", str(root)], check=True, capture_output=True)
            private = ("Q:" + "\\Users\\PrivateTester\\source.cs").encode()
            (root / ".gitignore").write_text("private/\n", encoding="utf-8")
            (root / "private").mkdir(); (root / "private" / "fixture.md").write_bytes(private)
            (root / "README.md").write_text("MadLicorice", encoding="utf-8")
            self.assertTrue(audit.inspect_repo(root)["Passed"])
            (root / "example.md").write_bytes(private)
            report = audit.inspect_repo(root)
            self.assertFalse(report["Passed"])
            self.assertNotIn("PrivateTester", json.dumps(report))

    def test_private_file_name_is_redacted(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            subprocess.run(["git", "init", "--quiet", str(root)], check=True, capture_output=True)
            filename = "private.tester" + "@" + "invalid.test.md"
            (root / filename).write_bytes(("Q:" + "\\Users\\PrivateTester\\source.cs").encode())
            report = audit.inspect_repo(root)
            self.assertFalse(report["Passed"])
            self.assertNotIn(filename, json.dumps(report))

    def test_changed_vendor_asset_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            subprocess.run(["git", "init", "--quiet", str(root)], check=True, capture_output=True)
            asset = root / next(iter(audit.VENDOR_ASSET_HASHES))
            asset.parent.mkdir(parents=True)
            asset.write_bytes(b"changed payload")
            report = audit.inspect_repo(root)
            self.assertFalse(report["Passed"])
            self.assertIn("Required vendor asset differs", json.dumps(report))


if __name__ == "__main__":
    unittest.main()
