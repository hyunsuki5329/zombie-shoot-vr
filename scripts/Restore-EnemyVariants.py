"""Restore user-acquired boss model and crawler motions for the variant setup."""
import argparse
import hashlib
from pathlib import Path
SOURCES = {
 'Zombiegirl W Kurniawan.fbx': '30089459520752768335f87ba3cd8db48fa69eb98cbbb8f36c24d624f0d317b1',
 'Skeletonzombie T Avelange.fbx': 'b8a314a0b7babbf000a43f2f81aa58f1de90dc661c4da9faf085c6a630aa0f1a',
 'Skeletonzombie T Avelange@Zombie Crawl.fbx': 'be6211fec144f733e82c75082310761b262d4dbf4aa54ca10d83474b02d4dfa0',
 'Skeletonzombie T Avelange@Zombie Biting.fbx': 'aeee9ed66ea2adf133aaab2bbc2ffb3bc2a1ac1194a5d7a514873c7272e2cde1',
}
def restore(source, project):
 if not (project/'ProjectSettings/ProjectVersion.txt').is_file():
  raise ValueError('Not a Unity project')
 target=project/'Assets/VRFPS/External/Mixamo'
 pending=[]
 for name,digest in SOURCES.items():
  data=(source/name).read_bytes()
  if hashlib.sha256(data).hexdigest()!=digest:raise ValueError('Unexpected original: '+name)
  dest=target/name
  if dest.exists() and dest.read_bytes()!=data:raise ValueError('Preserving modified destination: '+str(dest))
  pending.append((dest,data))
 for dest,data in pending:
  dest.parent.mkdir(parents=True,exist_ok=True)
  if not dest.exists():dest.write_bytes(data)
 return len(pending)
if __name__=='__main__':
 parser=argparse.ArgumentParser(description=__doc__)
 parser.add_argument('--source',type=Path,required=True);parser.add_argument('--project',type=Path,required=True)
 args=parser.parse_args();print('Restored/verified',restore(args.source,args.project),'files')
