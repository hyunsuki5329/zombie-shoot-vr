"""Restore the user-acquired GAMGO ZIPs locally; never fetch credentials or upload assets."""
import argparse
import hashlib
import pathlib
import zipfile

EXPECTED = {
    "daewoo-k2.zip": ("K2", "ddb70a33b9049d2f512dc35be7209362c4d5ade0f6b5d02bacdab01f80aaa243"),
    "30-rounds-556mm-stanag-mag.zip": ("Magazine", "10d802588b8f500bd15b40687e4dea6e30467523b96818d8f1e5758388145b16"),
}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archives", required=True, type=pathlib.Path)
    parser.add_argument("--project", required=True, type=pathlib.Path)
    args = parser.parse_args()
    if not (args.project / "ProjectSettings/ProjectVersion.txt").is_file():
        parser.error("Not an existing Unity project")
    # Validate both archives and every destination before writing either asset.
    pending = []
    for name, (folder, digest) in EXPECTED.items():
        source = args.archives / name
        if hashlib.sha256(source.read_bytes()).hexdigest() != digest:
            raise ValueError("Unexpected archive contents: " + name)
        target = (args.project / "Assets/VRFPS/External/GAMGO" / folder).resolve()
        with zipfile.ZipFile(source) as archive:
            if archive.testzip():
                raise ValueError("Corrupt ZIP: " + name)
            for entry in archive.infolist():
                path = (target / entry.filename).resolve()
                if not path.is_relative_to(target) or path.suffix.lower() not in {".fbx", ".png", ".jpg"}:
                    raise ValueError("Unexpected archive entry: " + entry.filename)
                data = archive.read(entry)
                if path.exists() and path.read_bytes() != data:
                    raise ValueError("Preserving changed local asset: " + str(path))
                pending.append((path, data))
    for path, data in pending:
        path.parent.mkdir(parents=True, exist_ok=True)
        if not path.exists():
            path.write_bytes(data)
    print(f"Restored/verified {len(pending)} files. Next run VRFPS.Editor.GamgoVisualSetup.Configure in Unity.")

if __name__ == "__main__":
    main()
