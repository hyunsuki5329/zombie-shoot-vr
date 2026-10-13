"""Restore the user-supplied ZIP and stable Unity metadata; no downloads or uploads."""
import argparse
import hashlib
import json
import zipfile
from pathlib import Path

def restore(archive, project, manifest):
    spec = json.loads(manifest.read_text(encoding='utf-8-sig'))
    if hashlib.sha256(archive.read_bytes()).hexdigest() != spec['zipSha256']:
        raise ValueError('Archive differs from the inspected user-supplied model')
    target = (project / 'Assets/VRFPS/External/GrannyHouse').resolve()
    writes = {}
    with zipfile.ZipFile(archive) as z:
        for entry in z.infolist():
            if entry.is_dir():
                continue
            dest = (target / entry.filename).resolve()
            if not dest.is_relative_to(target):
                raise ValueError('Unsafe archive path')
            content = z.read(entry)
            if dest in writes and writes[dest] != content:
                raise ValueError('Conflicting duplicate archive entry')
            writes[dest] = content
    generated = []
    for name, content in spec['metas'].items():
        if name.startswith('Generated/'):
            if name.endswith('.prefab.meta'):
                generated.append({'path': name, 'content': content})
            continue
        dest = target.with_suffix('.meta') if name == '../GrannyHouse.meta' else (target / name).resolve()
        if dest != target.with_suffix('.meta') and not dest.is_relative_to(target):
            raise ValueError('Unsafe metadata path')
        writes[dest] = content.encode('utf-8')
    writes[target / 'GenerationMetadata.json'] = json.dumps({'entries': generated}, ensure_ascii=False).encode('utf-8')
    # Check every destination before writing any file; preserve local modifications.
    for dest, content in writes.items():
        if dest.exists() and dest.read_bytes() != content:
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
    parser.add_argument('--manifest', type=Path, default=Path(__file__).resolve().parents[1] / 'docs/granny-house-manifest.json')
    args = parser.parse_args()
    print(restore(args.archive, args.project, args.manifest))
