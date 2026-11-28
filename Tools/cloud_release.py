"""Assemble on a GitHub runner and upload to an unpublished GitHub Release only."""
from pathlib import Path
import hashlib, json, os, shutil, subprocess, urllib.request, urllib.parse, zipfile
R=Path.cwd(); A=R/'cloud-artifacts'; O=R/'cloud-delivery';O.mkdir(exist_ok=True)
needs=json.loads(os.environ['NEEDS_JSON'])
ready=all(needs.get(name,{}).get('result')=='success' for name in ['source','license','tests','captures','build','windows','browser'])
media=A/'source-and-media/Publishing_Ready'
if media.is_dir(): shutil.copytree(media,O/'Publishing_Ready',dirs_exist_ok=True)
native=A/'native-tests'; browser=A/'browser-tests'
for name in ['source-and-media','unity-tests','explicit-capture-tests','native-tests','browser-tests']:
    folder=A/name
    if folder.is_dir():
        with zipfile.ZipFile(O/(name+'.zip'),'w',zipfile.ZIP_DEFLATED) as z:
            for p in folder.rglob('*'):
                if p.is_file():z.write(p,p.relative_to(folder).as_posix())
if needs.get('build',{}).get('result')=='success':
    import sys
    sys.path.insert(0,str(R/'Tools'))
    from package_store import package
    win=A/'player-windows/Windows';web=A/'player-web/Web'
    assert not any(b'StandaloneVerification' in p.read_bytes() for p in (win/'Borg Asyut_Data/Managed').glob('*.dll'))
    package('windows',win,O/'Borg_Asyut_Windows_x64.zip')
    package('web',web,O/'Borg_Asyut_WebGL.zip')
    files=[p for p in web.rglob('*') if p.is_file()]
    limit={'files':len(files),'uncompressed_bytes':sum(p.stat().st_size for p in files),'largest_file_bytes':max(p.stat().st_size for p in files),'crazygames_total_50MB':sum(p.stat().st_size for p in files)<=50_000_000}
    assert len(files)<=1000 and limit['uncompressed_bytes']<=500_000_000 and limit['largest_file_bytes']<=200_000_000,'itch HTML package limits exceeded'
    (O/'WEB_SIZE_REPORT.json').write_text(json.dumps(limit,indent=2)+'\n')
    for platform in ['itch.io','Game_Jolt','CrazyGames']:
        dest=O/'Publishing_Ready'/platform/'Builds';dest.mkdir(parents=True,exist_ok=True)
        shutil.copy2(O/'Borg_Asyut_WebGL.zip',dest/'Borg_Asyut_WebGL.zip')
        if platform!='CrazyGames':shutil.copy2(O/'Borg_Asyut_Windows_x64.zip',dest/'Borg_Asyut_Windows_x64.zip')
        status='Cloud binaries exist; portal preview, rights review and owner launch approval remain required.'
        if platform=='CrazyGames' and not limit['crazygames_total_50MB']:
            status='BLOCKED: measured total Web size exceeds 50 MB without SDK; optimize before CrazyGames submission.'
        (dest.parent/'STATUS.md').write_text(status+'\n',encoding='utf8')
summary={'commit':os.environ['GITHUB_SHA'],'unity_configured':'6000.3.29f1','run_url':os.environ['GITHUB_SERVER_URL']+'/'+os.environ['GITHUB_REPOSITORY']+'/actions/runs/'+os.environ['GITHUB_RUN_ID'],'jobs':{k:v.get('result') for k,v in needs.items()},'automated_cloud_checks_passed':ready,'public_launch_approved':False,'browser_scope':'Startup and input smoke with screenshot evidence, not human visual/audio approval.','explicit_media_tests':'Separate captures job explicitly selects 13 opt-in capture cases; any genuinely unavailable local-only recording case remains skipped in XML.'}
(O/'CLOUD_STATUS.json').write_text(json.dumps(summary,indent=2)+'\n')
kit=O/'Publishing_Ready'
if kit.is_dir():
    with zipfile.ZipFile(O/'Borg_Asyut_Publishing_Ready.zip','w',zipfile.ZIP_DEFLATED) as z:
        for p in kit.rglob('*'):
            if p.is_file():z.write(p,p.relative_to(kit).as_posix())
hashes={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in O.iterdir() if p.is_file()}
(O/'SHA256.json').write_text(json.dumps(hashes,indent=2)+'\n')
token=os.environ['GH_TOKEN']; repo=os.environ['GITHUB_REPOSITORY']
def api(url,method='GET',data=None,kind='application/json'):
    body=json.dumps(data).encode() if isinstance(data,dict) else data
    req=urllib.request.Request(url,method=method,data=body,headers={'Authorization':'Bearer '+token,'Accept':'application/vnd.github+json','Content-Type':kind,'User-Agent':'Borg-Asyut-Cloud'})
    with urllib.request.urlopen(req,timeout=180) as response:
        raw=response.read();return json.loads(raw) if raw else None
root='https://api.github.com/repos/'+repo
body='Developer: Mohamad El Hussein — محمد الحسين\n\nPRIVATE CLOUD CANDIDATE — NO PUBLIC LAUNCH APPROVAL.\n\n'+json.dumps(summary,indent=2)+'\n\nMissing/failed/skipped checks are not passes. No storefront submission, payment or commercial agreement is performed. Original README logo and approved game art/audio retained.'
draft=api(root+'/releases','POST',{'tag_name':'cloud-'+os.environ['GITHUB_RUN_ID']+'-'+os.environ['GITHUB_RUN_ATTEMPT'],'target_commitish':os.environ['GITHUB_SHA'],'name':'Borg Asyut — cloud '+('candidate' if ready else 'BLOCKED diagnostics'),'body':body,'draft':True,'prerelease':True})
assert draft['draft']
for p in O.iterdir():
    if p.is_file():
        api(draft['upload_url'].split('{')[0]+'?name='+urllib.parse.quote(p.name),'POST',p.read_bytes(),'application/octet-stream')
assert api(root+'/releases/'+str(draft['id']))['draft']
print('Unpublished draft: '+draft['html_url'])
with open(os.environ['GITHUB_STEP_SUMMARY'],'a',encoding='utf8') as f:
    f.write('## Private draft\n'+draft['html_url']+'\n\n'+json.dumps(summary,indent=2)+'\n')
