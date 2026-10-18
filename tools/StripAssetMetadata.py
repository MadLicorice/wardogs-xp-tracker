"""Remove optional image metadata without changing compressed pixel data."""
from pathlib import Path
import argparse
import hashlib
import json
import struct

PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
PNG_REQUIRED = {b"IHDR", b"PLTE", b"tRNS", b"IDAT", b"IEND"}

def strip_png(data):
    if not data.startswith(PNG_SIGNATURE):
        raise ValueError("Invalid PNG signature")
    output = bytearray(PNG_SIGNATURE)
    pixel_data = bytearray()
    removed = 0
    offset = 8
    while offset < len(data):
        length = struct.unpack_from(">I", data, offset)[0]
        end = offset + length + 12
        if end > len(data):
            raise ValueError("Invalid PNG chunk length")
        chunk_type = data[offset + 4:offset + 8]
        chunk = data[offset:end]
        if chunk_type in PNG_REQUIRED:
            output.extend(chunk)
            pixel_data.extend(chunk)
        else:
            removed += 1
        offset = end
        if chunk_type == b"IEND":
            if offset != len(data):
                removed += 1
            break
    return bytes(output), removed, hashlib.sha256(pixel_data).hexdigest()

def strip_jpeg(data):
    if not data.startswith(b"\xff\xd8"):
        raise ValueError("Invalid JPEG signature")
    output = bytearray(data[:2])
    offset = 2
    removed = 0
    while offset < len(data):
        if data[offset] != 255:
            raise ValueError("Invalid JPEG marker")
        marker = data[offset + 1]
        if marker in (0xDA, 0xD9):
            output.extend(data[offset:])
            break
        length = struct.unpack_from(">H", data, offset + 2)[0]
        end = offset + 2 + length
        if length < 2 or end > len(data):
            raise ValueError("Invalid JPEG segment length")
        if 0xE0 <= marker <= 0xEF or marker == 0xFE:
            removed += 1
        else:
            output.extend(data[offset:end])
        offset = end
    clean = bytes(output)
    return clean, removed, hashlib.sha256(clean).hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("paths", nargs="+", type=Path)
    args = parser.parse_args()
    files = []
    for entry in args.paths:
        files.extend(sorted(entry.rglob("*")) if entry.is_dir() else [entry])
    report = {"ImagesChecked": 0, "ImagesChanged": 0, "OptionalBlocksRemoved": 0}
    for path in files:
        transform = strip_png if path.suffix.lower() == ".png" else strip_jpeg if path.suffix.lower() in {".jpg", ".jpeg", ".jfif"} else None
        if transform is None or not path.is_file():
            continue
        original = path.read_bytes()
        clean, removed, fingerprint = transform(original)
        repeated, more_removed, repeated_fingerprint = transform(clean)
        if repeated != clean or more_removed or repeated_fingerprint != fingerprint:
            raise ValueError("Image cleanup must be idempotent and retain its pixel stream")
        report["ImagesChecked"] += 1
        if clean != original:
            path.write_bytes(clean)
            report["ImagesChanged"] += 1
        report["OptionalBlocksRemoved"] += removed
    print(json.dumps(report, indent=2))

if __name__ == "__main__":
    main()
