"""Restore the inspected school archive locally, preserving modified files.

After restoration, run SchoolAssetsSetup.Configure then ClassroomSetup.Configure
in an isolated Unity project to regenerate asset references and navigation.
"""
import argparse
import hashlib
import zipfile
from pathlib import Path

EXPECTED_SHA256 = 'b11955cd8db34dc543afa1858912d3e9cc76cc10caec4cd1292178f89aec6319'


def restore(archive, project):
    if hashlib.sha256(archive.read_bytes()).hexdigest() != EXPECTED_SHA256:
        raise ValueError('Archive differs from the inspected school model')
    target = (project / 'Assets/VRFPS/External/AbandonedSchool').resolve()
    writes = {}
    with zipfile.ZipFile(archive) as source:
        for entry in source.infolist():
            if entry.is_dir():
                continue
            dest = (target / entry.filename).resolve()
            if not dest.is_relative_to(target):
                raise ValueError('Unsafe archive path')
            content = source.read(entry)
            if dest in writes and writes[dest] != content:
                raise ValueError('Conflicting duplicate entry')
            writes[dest] = content
    if target / 'source/model.fbx' not in writes:
        raise ValueError('Missing source/model.fbx')
    for dest, content in writes.items():
        if dest.exists() and (not dest.is_file() or dest.read_bytes() != content):
            raise ValueError('Preserving modified destination: ' + str(dest))
    for dest, content in writes.items():
        if not dest.exists():
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(content)
    return target


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive', type=Path, required=True)
    parser.add_argument('--project', type=Path, required=True)
    args = parser.parse_args()
    print(restore(args.archive, args.project))
