"""Check shipping source and publish bytes without printing matched private values."""
from pathlib import Path, PurePosixPath
import argparse
import hashlib
import json
import re
import struct
import subprocess
import zlib
from urllib.parse import unquote


TEXT_SUFFIXES = {".cs", ".csproj", ".props", ".targets", ".ps1", ".py", ".md", ".json", ".xaml", ".config", ".manifest", ".txt", ".rtf", ".yml", ".yaml", ".xml", ".csv", ".sln", ".slnx", ".resx", ".svg", ".toml", ".ini", ".html", ".css", ".js", ".ts"}
TEXT_NAMES = {"license", "notice", ".gitignore", ".gitattributes", ".editorconfig"}
IMAGE_SUFFIXES = {".png", ".ico", ".jpg", ".jpeg", ".jfif"}
PROJECT_DLLS = {"WardogsXpTracker.dll", "WardogsXpTracker.Core.dll", "WardogsXpTracker.Windows.dll"}
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
PNG_ALLOWED = {b"IHDR", b"PLTE", b"tRNS", b"IDAT", b"IEND"}
BUNDLE_SIGNATURE = bytes.fromhex("8b1202b96a612038727b930214d7a03213f5b9e6efae3318ee3b2dce24b36aae")
MAX_ENTRY = 256 * 1024 * 1024
MAX_BUNDLE = 1024 * 1024 * 1024
# Match a path, not a drive label or a C# escape. Public Windows system roots are allowed below.
DRIVE_PATH = re.compile(r"(?<![A-Za-z0-9])([A-Za-z]):[\\/]+([^\s\x00\"<>|]+)")
PROFILE_PATH = re.compile(r"(?:[A-Za-z]:[\\/]+(?:Users|Documents and Settings)[\\/]+(?!Public(?:[\\/]|\b))|(?<![A-Za-z0-9])/(?:Users|home)/[^/\s<>]+/|\\\\[^\\\s<>]+\\(?:Users|home|profiles)\\)", re.I)
UNC_PATH = re.compile(r"(?<![\\])\\\\[A-Za-z0-9][A-Za-z0-9._-]*\\[^\s\\\"<>]+\\")
EMAIL = re.compile(r"(?<![A-Za-z0-9_.+-])[A-Za-z0-9][A-Za-z0-9_.+-]{0,63}@[A-Za-z0-9](?:[A-Za-z0-9.-]*[A-Za-z0-9])?\.[A-Za-z]{2,24}(?![A-Za-z0-9])")
IDENTIFIER = re.compile(r"(?i)\b(?:user_?name|machine_?name|computer_?name|author|creator|last_?modified_?by)[\"']?\s*[=:]\s*[\"']([^\"'\r\n]{2,100})[\"']")
XML_IDENTIFIER = re.compile(r"(?i)<(?:[A-Za-z0-9_.-]+:)?(?:creator|author|lastModifiedBy|userName|computerName)>\s*([^<>\r\n]{2,100})\s*</")
PUBLIC_IDENTIFIERS = {"madlicorice", "madlic gaming", "wardogs", "username", "user", "example", "public", "test", "synthetic", "mock"}
# Required third-party assets are immutable, public build inputs. Their signed/native vendor
# build metadata is not project metadata. A changed file must be reviewed before this list changes.
VENDOR_ASSET_HASHES = {
    "assets/native/win-x64/leptonica-1.82.0.dll": "dfcb3e6ed0b16bc55bfdbcf53543cfe42a354b87c3e35bd3a95eebf005d73e76",
    "assets/native/win-x64/tesseract50.dll": "de4d04ec75095374d98f5dd7a60d14d7e2e0f76589db693eccf7ae658be8cb2b",
    "assets/native/win-x64/msvcp140.dll": "0f885b509a685d2bbfa652fed26b5fb31d88fbdab0a978c641d1c7b8aa460aa9",
    "assets/native/win-x64/vcruntime140.dll": "d5e4d9a3e835fa679450145d6a7d94e36573a509317111904d9b3712c30d9066",
    "assets/native/win-x64/vcruntime140_1.dll": "1f2d41c4aa5db0bc33ebf7b66d72943a817d7ce6cbe880502a9403823633093f",
    "assets/ocr/eng.traineddata": "7d4322bd2a7749724879683fc3912cb542f19906c83bcc1a52132556427170b2",
}


class AuditFormatError(ValueError):
    """The file could not be fully inspected. Do not include payload text in errors."""


def text_views(data, binary=False):
    if binary:
        yield from (m.group().decode("ascii") for m in re.finditer(rb"[\x20-\x7e]{6,}", data))
        yield from (m.group().decode("utf-16-le") for m in re.finditer(rb"(?:[\x20-\x7e]\x00){6,}", data))
    else:
        if data.startswith((b"\xff\xfe", b"\xfe\xff")):
            yield data.decode("utf-16", errors="replace")
        else:
            yield data.decode("utf-8-sig", errors="replace")
        # UTF-16 strings can occur in text containers without a BOM.
        if b"\x00" in data:
            yield data.decode("utf-16-le", errors="ignore")


def private_reasons(data, allowed_emails=(), binary=False, vendor=False):
    reasons = set()
    allowed = {item.lower() for item in allowed_emails}
    for view in text_views(data, binary):
        text = unquote(view)
        normalized = text.replace("\\\\", "\\") if not binary else text
        if any(PROFILE_PATH.search(value) or UNC_PATH.search(value) for value in (text, normalized)):
            reasons.add("Private profile or network path")
        # Vendor SDK build paths are part of required third-party binaries. Consumer profile paths
        # above are still checked; project binaries and source get the broader absolute-path check.
        if not vendor:
            for match in DRIVE_PATH.finditer(normalized):
                tail = match.group(2).replace("\\", "/").lower()
                if "/" in tail and not tail.startswith(("windows/", "program files/", "program files (x86)/", "path/to/")):
                    reasons.add("Non-system absolute drive path")
            if any(m.group().lower() not in allowed and not m.group().lower().endswith(("@example.com", "@example.org", "@example.net")) for m in EMAIL.finditer(text)):
                reasons.add("Email address outside required license notices")
            if any(m.group(1).strip().lower() not in PUBLIC_IDENTIFIERS and not m.group(1).startswith(("{", "$", "%", "<")) for pattern in (IDENTIFIER, XML_IDENTIFIER) for m in pattern.finditer(text)):
                reasons.add("Personal author or machine identifier")
    return sorted(reasons)


def has_private_path(data):
    return any("path" in reason.lower() for reason in private_reasons(data))


def png_info(data, start=0, exact=True):
    if data[start:start + 8] != PNG_SIGNATURE:
        raise AuditFormatError("Invalid PNG signature")
    pos = start + 8
    chunks = []
    seen_data = False
    while pos + 12 <= len(data):
        length = struct.unpack_from(">I", data, pos)[0]
        end = pos + length + 12
        if length > MAX_ENTRY or end > len(data):
            raise AuditFormatError("Truncated PNG chunk")
        kind = data[pos + 4:pos + 8]
        if not re.fullmatch(rb"[A-Za-z]{4}", kind):
            raise AuditFormatError("Invalid PNG chunk type")
        if (zlib.crc32(data[pos + 4:end - 4]) & 0xffffffff) != struct.unpack_from(">I", data, end - 4)[0]:
            raise AuditFormatError("PNG checksum mismatch")
        if not chunks and (kind != b"IHDR" or length != 13):
            raise AuditFormatError("Missing PNG header")
        if kind == b"IHDR" and chunks:
            raise AuditFormatError("Duplicate PNG header")
        chunks.append(kind)
        seen_data |= kind == b"IDAT"
        pos = end
        if kind == b"IEND":
            if length or not seen_data or (exact and pos != len(data)):
                raise AuditFormatError("Invalid PNG end or trailing bytes")
            return pos, chunks
    raise AuditFormatError("Missing PNG end")


def jpeg_info(data, start=0, exact=True):
    if data[start:start + 2] != b"\xff\xd8":
        raise AuditFormatError("Invalid JPEG signature")
    pos = start + 2
    markers = []
    while pos < len(data):
        if data[pos] != 255:
            raise AuditFormatError("Invalid JPEG marker")
        while pos < len(data) and data[pos] == 255:
            pos += 1
        if pos == len(data):
            break
        marker = data[pos]
        pos += 1
        if marker == 0xd9:
            if exact and pos != len(data):
                raise AuditFormatError("JPEG trailing bytes")
            return pos, markers
        if marker in {0xd8, 0x00}:
            raise AuditFormatError("Invalid JPEG marker order")
        if marker == 0x01 or 0xd0 <= marker <= 0xd7:
            continue
        if pos + 2 > len(data):
            break
        length = struct.unpack_from(">H", data, pos)[0]
        end = pos + length
        if length < 2 or end > len(data):
            raise AuditFormatError("Truncated JPEG segment")
        markers.append(marker)
        pos = end
        if marker == 0xda:
            # Scan entropy data, honoring stuffed bytes and restart markers. Progressive JPEGs
            # may contain another scan; resume the marker loop there.
            while pos < len(data):
                found = data.find(b"\xff", pos)
                if found < 0 or found + 1 >= len(data):
                    raise AuditFormatError("Missing JPEG end")
                following = data[found + 1]
                if following == 0 or 0xd0 <= following <= 0xd7:
                    pos = found + 2
                    continue
                pos = found
                break
    raise AuditFormatError("Missing JPEG end")


def image_reasons(data, suffix):
    suffix = suffix.lower()
    if suffix == ".png":
        _, chunks = png_info(data)
        return ["Optional PNG metadata or ancillary chunk"] if any(chunk not in PNG_ALLOWED for chunk in chunks) else []
    if suffix in {".jpg", ".jpeg", ".jfif"}:
        _, markers = jpeg_info(data)
        return ["Optional JPEG APP or comment segment"] if any(0xe0 <= m <= 0xef or m == 0xfe for m in markers) else []
    if suffix == ".ico":
        if len(data) < 6:
            raise AuditFormatError("Truncated icon header")
        reserved, kind, count = struct.unpack_from("<HHH", data)
        if reserved or kind != 1 or not 1 <= count <= 256 or 6 + count * 16 > len(data):
            raise AuditFormatError("Invalid icon directory")
        ranges = [(0, 6 + count * 16)]
        reasons = []
        for index in range(count):
            size, offset = struct.unpack_from("<II", data, 6 + index * 16 + 8)
            if not size or offset < ranges[0][1] or offset + size > len(data):
                raise AuditFormatError("Invalid icon frame bounds")
            ranges.append((offset, offset + size))
            frame = data[offset:offset + size]
            if frame.startswith(PNG_SIGNATURE):
                reasons.extend(image_reasons(frame, ".png"))
            else:
                if len(frame) < 40:
                    raise AuditFormatError("Invalid icon bitmap header")
                header = struct.unpack_from("<I", frame)[0]
                if header not in {40, 52, 56, 108, 124} or header > len(frame):
                    raise AuditFormatError("Unsupported icon bitmap header")
                if header == 124 and (struct.unpack_from("<I", frame, 112)[0] or struct.unpack_from("<I", frame, 116)[0]):
                    reasons.append("Optional icon color profile metadata")
        ranges.sort()
        end = 0
        for left, right in ranges:
            if left < end:
                raise AuditFormatError("Overlapping icon frames")
            if left != end and any(data[end:left]):
                reasons.append("Unreferenced icon container bytes")
            end = right
        if end != len(data) and any(data[end:]):
            reasons.append("Trailing icon container bytes")
        return sorted(set(reasons))
    raise AuditFormatError("Unsupported image format")


def has_custom_image_metadata(path):
    return bool(image_reasons(path.read_bytes(), path.suffix))


def embedded_image_reasons(data):
    reasons = []
    count = 0
    for signature, suffix, reader in ((PNG_SIGNATURE, ".png", png_info), (b"\xff\xd8\xff", ".jpg", jpeg_info)):
        pos = 0
        while True:
            pos = data.find(signature, pos)
            if pos < 0:
                break
            try:
                end, _ = reader(data, pos, exact=False)
                reasons.extend(image_reasons(data[pos:end], suffix))
                count += 1
                pos = end
            except AuditFormatError:
                # Random native instructions may share the JPEG prefix. A PNG signature with
                # an IHDR is an actual asset and must not evade the audit when malformed.
                if suffix == ".png" and data[pos + 12:pos + 16] == b"IHDR":
                    reasons.append("Malformed embedded PNG")
                pos += len(signature)
    return sorted(set(reasons)), count


def pe_debug_types(data):
    if not data.startswith(b"MZ"):
        return []
    try:
        pe = struct.unpack_from("<I", data, 0x3c)[0]
        if data[pe:pe + 4] != b"PE\0\0":
            raise AuditFormatError("Invalid PE header")
        sections, optional_size = struct.unpack_from("<H", data, pe + 6)[0], struct.unpack_from("<H", data, pe + 20)[0]
        opt = pe + 24
        magic = struct.unpack_from("<H", data, opt)[0]
        directory = opt + (112 if magic == 0x20b else 96 if magic == 0x10b else 0)
        if directory == opt or optional_size < directory - opt + 7 * 8:
            raise AuditFormatError("Invalid PE optional header")
        rva, size = struct.unpack_from("<II", data, directory + 6 * 8)
        if not rva and not size:
            return []
        table = opt + optional_size
        for index in range(sections):
            section = table + index * 40
            virtual_size, va, raw_size, raw = struct.unpack_from("<IIII", data, section + 8)
            if va <= rva < va + max(virtual_size, raw_size):
                offset = raw + rva - va
                if size % 28 or offset + size > len(data):
                    raise AuditFormatError("Invalid PE debug directory")
                return [struct.unpack_from("<I", data, offset + x + 12)[0] for x in range(0, size, 28)]
        raise AuditFormatError("Unmapped PE debug directory")
    except struct.error as exc:
        raise AuditFormatError("Truncated PE header") from exc


def read_bundle(data, include_host_length=False):
    """Read the .NET host manifest and bounded raw-deflate entries in memory.

    Format: dotnet/runtime Microsoft.NET.HostModel/Bundle/{Manifest,FileEntry}.cs.
    This reads the published executable itself, including compressed project assemblies.
    """
    signature = data.find(BUNDLE_SIGNATURE)
    if signature < 8:
        raise AuditFormatError("No .NET bundle manifest found")
    header = struct.unpack_from("<q", data, signature - 8)[0]
    if header <= 0 or header + 12 > len(data):
        raise AuditFormatError("Invalid .NET bundle header offset")
    pos = header
    major, minor, count = struct.unpack_from("<IIi", data, pos)
    pos += 12
    if major not in {1, 2, 6} or minor != 0 or not 1 <= count <= 10000:
        raise AuditFormatError("Unsupported .NET bundle format")

    def read_string():
        nonlocal pos
        length = shift = 0
        for _ in range(5):
            if pos >= len(data):
                raise AuditFormatError("Truncated .NET bundle string")
            value = data[pos]
            pos += 1
            length |= (value & 0x7f) << shift
            if value < 128:
                break
            shift += 7
        else:
            raise AuditFormatError("Invalid .NET bundle string length")
        if length > 32768 or pos + length > len(data):
            raise AuditFormatError("Invalid .NET bundle string bounds")
        try:
            result = data[pos:pos + length].decode("utf-8")
        except UnicodeDecodeError as exc:
            raise AuditFormatError("Invalid .NET bundle text") from exc
        pos += length
        return result

    read_string()  # The bundle ID is a functional content hash.
    if major >= 2:
        pos += 40
    entries = []
    host_length = header
    seen = set()
    total = 0
    try:
        for _ in range(count):
            offset, size = struct.unpack_from("<qq", data, pos)
            pos += 16
            compressed = struct.unpack_from("<q", data, pos)[0] if major >= 6 else 0
            pos += 8 if major >= 6 else 0
            file_type = data[pos]
            pos += 1
            name = read_string()
            relative = PurePosixPath(name.replace("\\", "/"))
            if relative.is_absolute() or ".." in relative.parts or ":" in name or name in seen:
                raise AuditFormatError("Unsafe or duplicate .NET bundle entry name")
            seen.add(name)
            stored = compressed or size
            host_length = min(host_length, offset)
            total += size
            if size < 0 or compressed < 0 or max(size, stored) > MAX_ENTRY or total > MAX_BUNDLE or offset < 0 or offset + stored > header:
                raise AuditFormatError("Invalid .NET bundle entry bounds")
            payload = data[offset:offset + stored]
            if compressed:
                engine = zlib.decompressobj(-15)
                payload = engine.decompress(payload, size + 1)
                if not engine.eof or engine.unused_data or len(payload) != size:
                    raise AuditFormatError("Invalid .NET bundle compressed payload")
            entries.append((name, file_type, payload))
    except (struct.error, IndexError, zlib.error) as exc:
        raise AuditFormatError("Truncated or invalid .NET bundle") from exc
    return (entries, host_length) if include_host_length else entries


def legal_file(path):
    name = path.name.lower()
    return "licenses" in {part.lower() for part in path.parts} or name.startswith(("license", "third-party-notices", "notice"))


def text_file(path):
    return path.suffix.lower() in TEXT_SUFFIXES or path.name.lower() in TEXT_NAMES


def inspect_repo(root, publish=None):
    listed = subprocess.run(["git", "-c", "safe.directory=" + str(root), "ls-files", "--cached", "--others", "--exclude-standard", "-z"], cwd=root, capture_output=True)
    if listed.returncode:
        raise AuditFormatError("Git shipping-file enumeration failed")
    names = listed.stdout.decode("utf-8").split("\0")
    files = sorted({root / name for name in names if name and (root / name).is_file()})
    allowed_emails = set()
    for path in files:
        if legal_file(path):
            for text in text_views(path.read_bytes()):
                allowed_emails.update(m.group().lower() for m in EMAIL.finditer(text))
    failures = []
    counts = {"TextFiles": 0, "Images": 0, "ReleaseAssemblies": 0, "PublishFiles": 0, "BundleEntries": 0, "BundledProjectAssemblies": 0, "EmbeddedImages": 0}
    vendor_blobs = []

    def fail(label, reasons):
        if private_reasons(label.encode("utf-8"), allowed_emails):
            label = "Shipping entry (name withheld)"
        for reason in sorted(set(reasons)):
            failures.append({"File": label, "Reason": reason})

    for name, expected in VENDOR_ASSET_HASHES.items():
        path = root / name
        if path.is_file():
            blob = path.read_bytes()
            if hashlib.sha256(blob).hexdigest() != expected:
                fail(name, ["Required vendor asset differs from its reviewed public hash"])
            else:
                vendor_blobs.append(blob)

    def project_binary(data, label):
        project_data = data
        for blob in vendor_blobs:
            if blob in project_data:
                project_data = project_data.replace(blob, b"\0" * len(blob))
        fail(label, private_reasons(project_data, allowed_emails, binary=True))
        if any(kind in {2, 17} for kind in pe_debug_types(data)):
            fail(label, ["Project CodeView or embedded debug symbols"])
        reasons, image_count = embedded_image_reasons(project_data)
        counts["EmbeddedImages"] += image_count
        fail(label, reasons)

    for path in files:
        label = path.relative_to(root).as_posix()
        if not path.resolve().is_relative_to(root):
            fail("Shipping source entry", ["Source symlink leaves the repository"])
            continue
        if path.suffix.lower() in {".pyc", ".pyo"} or "__pycache__" in {part.lower() for part in path.relative_to(root).parts}:
            fail(label, ["Python bytecode or cache in shipping source"])
        if text_file(path):
            counts["TextFiles"] += 1
            fail(label, private_reasons(path.read_bytes(), allowed_emails))
        if path.suffix.lower() in IMAGE_SUFFIXES:
            counts["Images"] += 1
            try:
                fail(label, image_reasons(path.read_bytes(), path.suffix))
            except AuditFormatError as exc:
                fail(label, [str(exc)])
        if path.suffix.lower() == ".pdb":
            fail(label, ["Debug symbols in shipping source"])
    if publish:
        found_projects = set()
        for path in sorted(publish.rglob("*")):
            if not path.is_file():
                continue
            label = "publish/" + path.relative_to(publish).as_posix()
            if not path.resolve().is_relative_to(publish):
                fail("Publish entry", ["Publish symlink leaves the selected folder"])
                continue
            counts["PublishFiles"] += 1
            suffix = path.suffix.lower()
            data = path.read_bytes()
            try:
                if suffix == ".pdb":
                    fail(label, ["Debug symbols in public output"])
                elif suffix in IMAGE_SUFFIXES:
                    fail(label, image_reasons(data, suffix))
                elif text_file(path):
                    fail(label, private_reasons(data, allowed_emails))
                elif suffix == ".exe":
                    entries, host_length = read_bundle(data, include_host_length=True)
                    # Compressed bytes can resemble printable paths by chance. Inspect the host
                    # and each decompressed entry, never compressed payload as text.
                    host = data[:host_length]
                    fail(label, private_reasons(host, allowed_emails, binary=True, vendor=True))
                    reasons, image_count = embedded_image_reasons(host)
                    fail(label, reasons)
                    counts["EmbeddedImages"] += image_count
                    counts["BundleEntries"] += len(entries)
                    for index, (name, _, payload) in enumerate(entries):
                        base = PurePosixPath(name).name
                        entry_label = label + "::entry[" + str(index) + "]"
                        fail(entry_label, private_reasons(name.encode("utf-8"), allowed_emails))
                        if PurePosixPath(name).suffix.lower() == ".pdb":
                            fail(entry_label, ["Debug symbols inside published bundle"])
                        if base in PROJECT_DLLS:
                            project_binary(payload, entry_label + "/" + base)
                            found_projects.add(base)
                            counts["BundledProjectAssemblies"] += 1
                        else:
                            fail(entry_label, private_reasons(payload, allowed_emails, binary=True, vendor=True))
                elif suffix == ".dll":
                    if path.name in PROJECT_DLLS:
                        project_binary(data, label)
                        found_projects.add(path.name)
                    else:
                        fail(label, private_reasons(data, allowed_emails, binary=True, vendor=True))
            except AuditFormatError as exc:
                fail(label, [str(exc)])
        if found_projects != PROJECT_DLLS:
            fail("Published project assemblies", ["Not all three project assemblies were inspected in the public output"])
        # Also check current Release inputs, including symbols added before single-file bundling.
        for path in root.glob("src/**/bin/Release/**/*.dll"):
            if path.name in PROJECT_DLLS:
                counts["ReleaseAssemblies"] += 1
                try:
                    project_binary(path.read_bytes(), path.relative_to(root).as_posix())
                except AuditFormatError as exc:
                    fail(path.relative_to(root).as_posix(), [str(exc)])
    report = dict(counts)
    report.update({"Failures": failures, "Passed": not failures, "Scope": "Eligible Git files exclude ignored fixtures and runtime saves. Publish checks read the actual .NET bundle, project debug directories, and embedded project images. Required third-party binary build metadata and public license contacts are allowed; private profile paths are checked in vendor binaries too."})
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--publish", type=Path)
    args = parser.parse_args()
    root = args.repo.resolve()
    publish = args.publish.resolve() if args.publish else None
    if not root.is_dir() or (publish and not publish.is_dir()):
        parser.error("The selected folder does not exist.")
    try:
        report = inspect_repo(root, publish)
    except (AuditFormatError, OSError, UnicodeError):
        report = {"Passed": False, "Failures": [{"File": "Audit input", "Reason": "Audit could not inspect all selected files"}]}
    print(json.dumps(report, indent=2))
    return 0 if report["Passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
