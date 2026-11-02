"""Restore the user-owned room scan locally; never upload the private FBX."""
from pathlib import Path
import argparse,hashlib
EXPECTED="245bbdb7284d6b755125b131be681891173ba791be3cf27b429726a048e1b2e8"
def restore(source,project):
    if not (project/"ProjectSettings/ProjectVersion.txt").is_file(): raise ValueError("Not a Unity project")
    data=source.read_bytes()
    if hashlib.sha256(data).hexdigest()!=EXPECTED: raise ValueError("Unexpected room scan; review before replacing the calibration")
    target=project/"Assets/VRFPS/External/RoomScan/Room.fbx"
    if target.exists() and target.read_bytes()!=data: raise ValueError("Preserving modified destination")
    target.parent.mkdir(parents=True,exist_ok=True)
    if not target.exists(): target.write_bytes(data)
    print("Room scan restored/verified:",target)
if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("--source",type=Path,required=True);p.add_argument("--project",type=Path,required=True)
    a=p.parse_args();restore(a.source,a.project)
