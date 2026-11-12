# Analytical registration only; never edits the PLY or reference assets.
# Requires numpy, scipy and Pillow. The reference is already in Unity coordinates.
import argparse
parser=argparse.ArgumentParser()
parser.add_argument('--ply',required=True)
parser.add_argument('--reference',required=True)
parser.add_argument('--texture',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args()
from pathlib import Path
out=Path(args.output);out.mkdir(parents=True,exist_ok=True)
from pathlib import Path
import numpy as np
from scipy.spatial import cKDTree
from PIL import Image
import json
b=Path(args.ply).read_bytes();a=np.frombuffer(b,dtype='<f4',offset=b.index(b'end_header\n')+11).reshape(-1,62);v=a[:,:3].copy();v[:,1:]*=-1;c=np.clip(.5+.28209479*a[:,6:9],0,1)
n=np.load(args.reference);r=n['vertices'];uv=n['uv'];im=np.array(Image.open(args.texture).convert('RGB'));rc=im[np.clip(((1-uv[:,1])*(im.shape[0]-1)).astype(int),0,im.shape[0]-1),np.clip((uv[:,0]*(im.shape[1]-1)).astype(int),0,im.shape[1]-1)]/255
th=np.deg2rad(-8);r[:,[0,2]]=r[:,[0,2]]@np.array([[np.cos(th),np.sin(th)],[-np.sin(th),np.cos(th)]])
m=(a[:,54]>1)&(v[:,1]>-.3)&(v[:,1]<2.5)&(np.abs(v[:,0])<8)&(np.abs(v[:,2])<11);s=v[m][::4];sc=c[m][::4];k=(r[:,1]>.1)&(r[:,1]<2.8);t=r[k][::3];tc=rc[k][::3];tree=cKDTree(np.c_[t,tc*1.5]);results=[]
for reflection in [1,-1]:
 for angle in [0,90,180,270]:
  th=np.deg2rad(angle);R=np.array([[np.cos(th),-np.sin(th)],[np.sin(th),np.cos(th)]])@np.diag([reflection,1]);q=s.copy();q[:,[0,2]]=s[:,[0,2]]@R.T;q+=np.median(t,axis=0)-np.median(q,axis=0);M=R.copy();scale=1.;trans=np.median(t,axis=0)-np.median(s@np.array([[R[0,0],0,R[1,0]],[0,1,0],[R[0,1],0,R[1,1]]]),axis=0)
  for it in range(35):
   dist,idx=tree.query(np.c_[q,sc*1.5]);keep=dist<np.quantile(dist,.75);src=q[keep];dst=t[idx[keep]];sx=src[:,[0,2]];dx=dst[:,[0,2]];sm=sx.mean(0);dm=dx.mean(0);u,ss,vt=np.linalg.svd((sx-sm).T@(dx-dm));rot=vt.T@u.T
   if np.linalg.det(rot)<0:vt[-1]*=-1;rot=vt.T@u.T
   fac=np.clip(ss.sum()/np.sum((sx-sm)**2),.98,1.02);q[:,[0,2]]=(q[:,[0,2]]-sm)@rot.T*fac+dm;q[:,1]+=(dst[:,1]-src[:,1]).mean();M=fac*rot@M
  dist,_=tree.query(np.c_[q,sc*1.5]);score=float(np.mean(np.sort(dist)[:int(len(dist)*.8)]));coef=np.linalg.lstsq(np.c_[s,np.ones(len(s))],q,rcond=None)[0];results.append(dict(reflection=reflection,angle=angle,score=score,matrix=coef.tolist()));np.savez(out/f'fit-{reflection}-{angle}.npz',points=q,colors=sc,reference=t,referenceColors=tc)
results.sort(key=lambda x:x['score']);(out/'registration.json').write_text(json.dumps(results,indent=2));print(json.dumps(results[:3],indent=2))
