"""Build a textured FBX-derived patch mesh in the aligned Classroom coordinates."""
import argparse
import json
from pathlib import Path
import shutil
import numpy as np

parser = argparse.ArgumentParser()
parser.add_argument('--reference', type=Path, required=True)
parser.add_argument('--texture', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
reference = np.load(args.reference)
vertices = reference['vertices'].copy()
uv = reference['uv']
triangles = reference['triangles'][:, [0, 2, 1]].astype(np.int32)
# Reference vertices already include FBX-to-Unity X reflection. Reverse winding once.
angle = np.deg2rad(-8)
rotation = np.array([[np.cos(angle), np.sin(angle)], [-np.sin(angle), np.cos(angle)]])
vertices[:, [0, 2]] = vertices[:, [0, 2]] @ rotation
vertices = (vertices + [.05, 0, .4]) * .83
faces = vertices[triangles]
centers = faces.mean(axis=1)
inside = ((np.abs(vertices[:, 0]) < 7) & (np.abs(vertices[:, 2]) < 3.15)
          & (vertices[:, 1] > .015) & (vertices[:, 1] < 2.05))
edge_lengths = np.linalg.norm(faces - np.roll(faces, 1, axis=1), axis=2)
regions = ((centers[:, 0] < -4.2) | (centers[:, 0] > 4.4)
           | (centers[:, 2] > .8) | (centers[:, 2] < -2.6)
           | (centers[:, 1] < .12))
keep = inside[triangles].all(axis=1) & (edge_lengths.max(axis=1) < .65) & regions
selected = triangles[keep]
used, indices = np.unique(selected, return_inverse=True)
args.output.mkdir(parents=True, exist_ok=True)
with (args.output / 'gap-mesh.bytes').open('wb') as output:
    np.array([len(used), selected.size], dtype='<i4').tofile(output)
    vertices[used].astype('<f4').tofile(output)
    uv[used].astype('<f4').tofile(output)
    indices.astype('<i4').tofile(output)
shutil.copyfile(args.texture, args.output / 'ReferenceTexture.jpg')
print(json.dumps({'triangles': len(selected), 'vertices': len(used)}))
