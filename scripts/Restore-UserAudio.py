"""Restore user-supplied originals; Unity UserWeaponAudioSetup creates game clips."""
import argparse
import hashlib
import shutil
from pathlib import Path
SOURCES = {
    'shoot sound.mp3': 'b12aee9041391b0214bfdd67fc50d783ec66fad3d4cc89eae77ac900ea601e88',
    'empty shoot.mp3': 'a15f0ce8251274aa4d33733fe94f408f681ee232c47598df257eba3e6b328ccc',
    'reload.mp3': '78067b1634a13133a218c1826769bfc90ff14c5efeb777d391013cad9b9ce950',
}
def restore(source, project):
    target = project / 'Assets/VRFPS/External/UserAudio'
    for name, expected in SOURCES.items():
        original = source / name
        if hashlib.sha256(original.read_bytes()).hexdigest() != expected:
            raise ValueError('Unexpected original: ' + name)
        dest = target / name
        if dest.exists() and hashlib.sha256(dest.read_bytes()).hexdigest() != expected:
            raise ValueError('Preserving modified destination: ' + str(dest))
    target.mkdir(parents=True, exist_ok=True)
    for name in SOURCES:
        if not (target / name).exists():
            shutil.copy2(source / name, target / name)
    return target
if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--project', type=Path, required=True)
    args = parser.parse_args()
    print(restore(args.source, args.project))
