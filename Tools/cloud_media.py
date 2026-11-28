"""Runs only on GitHub-hosted cloud runners; no workstation access."""
from pathlib import Path
from PIL import Image, ImageOps, ImageDraw, ImageFont
import hashlib, json, shutil, subprocess, urllib.request
R=Path.cwd()
O=R/'Publishing_Ready'
A=R/'Assets/Ma3alem/Art'
F=R/'Assets/Ma3alem/Fonts/NotoSans-Regular.ttf'
sizes={'itch.io':{'Cover_630x500.png':(630,500)},'Game_Jolt':{'Thumbnail_1280x720.png':(1280,720),'Header_2000x500.png':(2000,500)},'CrazyGames':{'Landscape_1920x1080.png':(1920,1080),'Portrait_800x1200.png':(800,1200),'Square_800x800.png':(800,800)}}
for platform,images in sizes.items():
    folder=O/platform/'Artwork';folder.mkdir(parents=True,exist_ok=True)
    for name,size in images.items():
        w,h=size
        image=ImageOps.fit(Image.open(A/'Environment_Assiut_Static.png').convert('RGBA'),size)
        sprite=Image.open(A/'Player_Jump_01.png').convert('RGBA');sprite=sprite.crop(sprite.getbbox())
        sprite.thumbnail((int(w*.52),int(h*.63)),Image.Resampling.LANCZOS)
        image.alpha_composite(sprite,(int(w*.53)-sprite.width//2,int(h*.92)-sprite.height))
        ImageDraw.Draw(image).text((w/2,h*.19),'BORG ASYUT',anchor='mm',font=ImageFont.truetype(str(F),max(15,int(min(w*.105,h*.18)))) ,fill='#ffe5a0',stroke_width=max(1,int(h*.007)),stroke_fill='#152b36')
        image.save(folder/name)
    shutil.copy2(R/'Publishing/STORE_COPY.md',O/platform/'Store_Listing.md')
    (O/platform/'STATUS.md').write_text('Private preparation only. Builds, rights, current portal limits and final QA must be verified before submission. Game Jolt artwork sizes are proposed, not independently verified mandatory sizes.\n',encoding='utf8')
shared=O/'Shared';shared.mkdir(exist_ok=True)
shutil.copy2(R/'Media/Weathered Asyut Tower Sign Duo.png',shared/'Owner_Original_Logo.png')
shutil.copy2(R/'CREDITS.md',shared/'CREDITS.md')
for rel in ['PLATFORM_REQUIREMENTS.md','RIGHTS_PRIVACY_REVIEW.md']:
    shutil.copy2(R/'Publishing'/rel,shared/rel)
shutil.copytree(R/'Media/Screenshots',shared/'Screenshots',dirs_exist_ok=True)
card=Image.new('RGB',(1920,1080),'#142b35')
for y,text,sz in [(260,'BORG ASYUT',110),(440,'Mohamad El Hussein',64),(590,'Game Creator | Game Designer | Programmer',36),(660,'Creative Director & Producer',40)]:
    ImageDraw.Draw(card).text((960,y),text,anchor='mm',font=ImageFont.truetype(str(F),sz),fill='#ffe5a0')
card.save(shared/'Trailer_End_Card.png')
trailer=R/'cloud-trailer-source.mp4'
with urllib.request.urlopen('https://github.com/mohamadhussien6/borg-Asyut/releases/download/game/Borg_Asyut_Official_Trailer.mp4',timeout=90) as response:
    trailer.write_bytes(response.read())
def ffmpeg(args,out):
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y',*args,'-map_metadata','-1','-c:v','libx264','-threads','2','-preset','fast','-crf','22','-pix_fmt','yuv420p','-movflags','+faststart',str(out)],check=True)
ffmpeg(['-i',str(trailer),'-loop','1','-framerate','60','-i',str(shared/'Trailer_End_Card.png'),'-filter_complex','[0:v]trim=duration=26,setpts=PTS-STARTPTS,setsar=1[a];[1:v]trim=duration=4,setpts=PTS-STARTPTS,setsar=1[b];[a][b]concat=n=2:v=1:a=0[v]','-map','[v]','-map','0:a?','-c:a','aac','-t','30','-r','60'],shared/'Borg_Asyut_Trailer_30s.mp4')
for name,filter in [('Landscape','scale=1920:1080'),('Portrait','crop=720:1080:600:0,scale=1080:1620')]:
    ffmpeg(['-i',str(trailer),'-t','18','-vf',filter,'-an','-r','60'],O/'CrazyGames'/('Preview_'+name+'_18s.mp4'))
(shared/'PROVENANCE.md').write_text('Original owner logo copied byte-for-byte and README retained. Covers use existing game art, not invented gameplay. Numbered screenshots and trailer gameplay are prior-build captures. Trailer closing card is newly composed with creator credit. Portal previews are silent real-time crops; review framing and matching first-cover frames before submission. These are not fresh patched-build captures.\n',encoding='utf8')
manifest={p.relative_to(O).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in O.rglob('*') if p.is_file()}
(shared/'MEDIA_MANIFEST.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf8')
