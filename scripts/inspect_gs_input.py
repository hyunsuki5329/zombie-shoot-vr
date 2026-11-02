"""Read-only binary PLY header preflight; never converts point clouds into GS."""
import argparse
import hashlib
import json
from pathlib import Path

GAUSSIAN = (["x", "y", "z", "f_dc_0", "f_dc_1", "f_dc_2", "opacity"] +
            [f"scale_{i}" for i in range(3)] + [f"rot_{i}" for i in range(4)])
SIZES = {"char": 1, "uchar": 1, "int8": 1, "uint8": 1,
         "short": 2, "ushort": 2, "int16": 2, "uint16": 2,
         "int": 4, "uint": 4, "int32": 4, "uint32": 4,
         "float": 4, "float32": 4, "double": 8, "float64": 8}


def inspect_ply(path):
    path = Path(path)
    props, comments = {}, []
    count, element, encoding = None, None, None
    with path.open("rb") as stream:
        if stream.readline(16).strip() != b"ply":
            raise ValueError("Not a PLY file")
        for _ in range(1024):
            raw = stream.readline(4097)
            if not raw or len(raw) > 4096:
                raise ValueError("Missing or oversized PLY header")
            parts = raw.decode("ascii").strip().split()
            if not parts:
                continue
            if parts[0] == "end_header":
                break
            if parts[0] == "format":
                encoding = parts[1:]
            elif parts[0] == "comment":
                comments.append(" ".join(parts[1:]))
            elif parts[0] == "element":
                element = parts[1]
                if element == "vertex":
                    if count is not None:
                        raise ValueError("Duplicate vertex element")
                    count = int(parts[2])
            elif parts[0] == "property" and element == "vertex":
                if len(parts) != 3 or parts[1] not in SIZES or parts[2] in props:
                    raise ValueError("Unsupported or duplicate vertex property")
                props[parts[2]] = parts[1]
        else:
            raise ValueError("Oversized PLY header")
        if encoding != ["binary_little_endian", "1.0"]:
            raise ValueError("Preflight supports binary_little_endian PLY 1.0 only")
        if count is None or count <= 0 or not props:
            raise ValueError("PLY must contain vertices")
        if path.stat().st_size - stream.tell() < count * sum(SIZES[x] for x in props.values()):
            raise ValueError("Truncated vertex payload")
        stream.seek(0)
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    missing = [name for name in GAUSSIAN if name not in props]
    gaussian = not missing
    return {"fileName": path.name, "bytes": path.stat().st_size, "sha256": digest,
            "vertexCount": count, "vertexProperties": props, "comments": comments,
            "gaussianHeader": gaussian, "missingGaussianProperties": missing,
            "classification": "gaussian-header-candidate" if gaussian else "point-cloud",
            "rendererImport": "not-tested"}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("paths", nargs="+", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    result = json.dumps([inspect_ply(path) for path in args.paths], indent=2)
    if args.output:
        # Exclusive creation also protects aliases/hardlinks to existing captures.
        with args.output.open("x", encoding="utf-8") as output:
            output.write(result + "\n")
    print(result)
