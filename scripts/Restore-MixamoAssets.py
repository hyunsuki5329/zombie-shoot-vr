"""Restore the three user-downloaded Mixamo files locally, without network access."""
import argparse
import hashlib
from pathlib import Path
import zipfile

EXPECTED = {
    "Yaku J Ignite.fbx": "7c7986bca670897a9d8dda69696613907898035051104886b8afac3ddc1ac0ef",
    "Parasite L Starkie.fbx": "0421651482d7c38dabd116e7980be4b055f67da0bf837a4074a68513d8afb356",
    "Scary Zombie Pack.zip": "39b00f79e526ed2ce71887b1323cedb0b2ac1c1d686b331fd275683c4b4bfba4",
}


def restore(source, project):
    if not (project / "ProjectSettings/ProjectVersion.txt").is_file():
        raise ValueError("Not an existing Unity project")
    root = (project / "Assets/VRFPS/External/Mixamo").resolve()
    pending = []
    # Check all sources and destinations before writing any file.
    for name, digest in EXPECTED.items():
        data = (source / name).read_bytes()
        if hashlib.sha256(data).hexdigest() != digest:
            raise ValueError("Unexpected contents: " + name)
        if name.endswith(".fbx"):
            pending.append((root / name, data))
        else:
            with zipfile.ZipFile(source / name) as archive:
                if archive.testzip():
                    raise ValueError("Corrupt motion ZIP")
                for entry in archive.infolist():
                    path = (root / "Motions" / entry.filename).resolve()
                    if not path.is_relative_to(root / "Motions") or path.suffix.lower() != ".fbx":
                        raise ValueError("Unexpected motion entry: " + entry.filename)
                    pending.append((path, archive.read(entry)))
    for path, data in pending:
        if path.exists() and path.read_bytes() != data:
            raise ValueError("Preserving modified local asset: " + str(path))
    for path, data in pending:
        path.parent.mkdir(parents=True, exist_ok=True)
        if not path.exists():
            path.write_bytes(data)
    return len(pending)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--downloads", type=Path, required=True)
    parser.add_argument("--project", type=Path, required=True)
    args = parser.parse_args()
    print(f"Restored/verified {restore(args.downloads, args.project)} files. "
          "Next run VRFPS.Editor.MixamoVisualSetup.Configure in an isolated Unity batch project.")
