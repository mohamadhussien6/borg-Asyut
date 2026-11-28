"""Bound retained cloud evidence; raw captures never leave the cloud runner."""
from pathlib import Path
import hashlib, json, shutil
out=Path('capture-evidence');out.mkdir(exist_ok=True)
roots=[Path('cloud-captures'),Path('Documentation')]
files=sorted(p for root in roots if root.exists() for p in root.rglob('*') if p.is_file() and p.suffix.lower() in {'.png','.jpg','.json','.csv','.txt','.mp4'})
retained=[];manifest=[];total=0
for path in files:
    info={'path':str(path),'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'retained':False}
    # Keep reports and a bounded representative set; thousands of raw recorder frames remain ephemeral.
    report=path.suffix.lower() in {'.json','.csv','.txt'}
    if (report or len(retained)<30) and total+path.stat().st_size<=100_000_000:
        target=out/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(path,target)
        info['retained']=True;total+=path.stat().st_size;retained.append(str(path))
    manifest.append(info)
(out/'CAPTURE_MANIFEST.json').write_text(json.dumps({'retained_bytes':total,'files':manifest,'note':'All hashes cover actual generated files; only the bounded subset is retained. Omitted raw frames are not claimed as uploaded.'},indent=2)+'\n',encoding='utf8')
