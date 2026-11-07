"""Prepare reviewed, isolated runtime frames from the supplied PNG drawings.
Requires Pillow, numpy and scipy. Does not modify the PNG originals.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image
from scipy.ndimage import label, find_objects

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'src/assets/game'
OUT = ROOT / 'src/assets/match'
OUT.mkdir(exist_ok=True)
manifest = {}

def regions(image, count):
    alpha = np.asarray(image)[:,:,3]
    active = (alpha > 160).sum(axis=0) > 2
    edges = np.diff(np.r_[False, active, False].astype(int))
    runs = list(zip(np.where(edges == 1)[0], np.where(edges == -1)[0]))
    # Discard isolated compression/antialias specks, never divide into equal cells.
    runs = [(int(a),int(b)) for a,b in runs if b-a > 35]
    if len(runs) != count:
        raise ValueError(f'Expected {count} distinct silhouettes, got {runs}')
    cuts = [0] + [(runs[i-1][1]+runs[i][0])//2 for i in range(1,count)] + [image.width]
    return [(cuts[i],0,cuts[i+1],image.height) for i in range(count)]

def write(name, frames, size, radius=None, heading='down'):
    sheet = Image.new('RGBA',(size*len(frames),size))
    shadow = Image.new('RGBA',(size*len(frames),size))
    for i,frame in enumerate(frames):
        sheet.paste(frame,(i*size,0))
        black = Image.new('RGBA',frame.size,(0,0,0,0));black.putalpha(frame.getchannel('A'))
        shadow.paste(black,(i*size,0))
    file = name.replace('/','-')
    sheet.save(OUT/(file+'.png'),optimize=True)
    if name.startswith('hawk'):
        shadow.save(OUT/(file+'-shadow.png'),optimize=True)
    manifest[name] = dict(file=file+'.png',size=size,count=len(frames),bodyRadius=radius or size/2,heading=heading)

def place(image, box, anchor, scale, size):
    crop=image.crop(box)
    result=Image.new('RGBA',(size,size))
    resized=crop.resize((round(crop.width*scale),round(crop.height*scale)),Image.Resampling.LANCZOS)
    x=round(size/2+(box[0]-anchor[0])*scale);y=round(size/2+(box[1]-anchor[1])*scale)
    if x < 0 or y < 0 or x+resized.width > size or y+resized.height > size:
        # Transparent source padding is excluded before this operation.
        raise ValueError(f'Frame would clip: {box}, {anchor}, {scale}, {size}')
    result.paste(resized,(x,y))
    return result

for anim,count in [('idle',8),('jump',6),('swim',8),('attack',2)]:
    master=Image.open(SOURCE/f'frogs/common-frog/{anim}.png').convert('RGBA')
    boxes=regions(master,count)
    transforms=[]
    for box in boxes:
        crop=master.crop(box);a=np.asarray(crop).astype(float)
        skin=(a[:,:,1]>a[:,:,0]*1.05)&(a[:,:,1]>a[:,:,2]*1.3)&(a[:,:,3]>180)
        ids,n=label(skin);counts=np.bincount(ids.ravel());counts[0]=0
        body=find_objects(ids)[int(counts.argmax())-1]
        y,x=body
        # Extended rear legs can share the green component with the torso.
        # For swimming, locate the torso from its front/head end, excluding legs.
        if anim == 'swim':
            y=slice(max(y.start,round(y.stop-(x.stop-x.start)*1.15)),y.stop)
        anchor=(box[0]+(x.start+x.stop)/2,(y.start+y.stop)/2)
        radius=((x.stop-x.start)*(y.stop-y.start))**.5/2
        alpha=crop.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
        tight=(box[0]+alpha[0],alpha[1],box[0]+alpha[2],alpha[3])
        transforms.append((tight,anchor,64/radius))
    if anim == 'swim':
        # In frames 3 and 4 the drawn body is smaller, while the extended legs
        # skew the automatic torso measurement. Normalize using eye/body size.
        corrections = (1, 1, 1.25, 1.25, 1, 1, 1, 1)
        transforms = [(box, anchor, scale * correction)
                      for (box, anchor, scale), correction in zip(transforms, corrections)]
    # Size derived from all extremities so jumping/swimming feet never get clipped.
    extent=max(max(abs(box[0]-ax),abs(box[2]-ax),abs(box[1]-ay),abs(box[3]-ay))*scale for box,(ax,ay),scale in transforms)
    size=int(np.ceil((2*extent+8)/32)*32)
    for variant in ['common-frog','heavy-frog','sticky-frog','sniper-frog']:
        image=Image.open(SOURCE/f'frogs/{variant}/{anim}.png').convert('RGBA')
        frames=[place(image,box,anchor,scale,size) for box,anchor,scale in transforms]
        write(f'{variant}/{anim}',frames,size,64)

for name,count,rows in [('fly',8,2),('mosquito',8,1),('firefly',8,1),('hawk-fly',8,1),('hawk-dive',8,1),('hawk-rise',8,1)]:
    image=Image.open(SOURCE/(name+'.png')).convert('RGBA')
    boxes=[]
    for row in range(rows):
        y0=round(row*image.height/rows);y1=round((row+1)*image.height/rows)
        strip=image.crop((0,y0,image.width,y1))
        boxes += [(a,y0,c,y1) for a,_,c,_ in regions(strip,count//rows)]
    anchors=[];tightboxes=[]
    for box in boxes:
        crop=image.crop(box);strong=crop.getchannel('A').point(lambda a:255 if a>160 else 0).getbbox()
        alpha=crop.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
        if name.startswith('hawk'):
            # Wing silhouettes change their bounds while flapping. Anchor on
            # the beak instead, with the same torso offset in all three phases.
            pixels=np.asarray(crop).astype(float)
            yy,xx=np.indices(pixels.shape[:2])
            center_x=(strong[0]+strong[2])/2
            beak=(pixels[:,:,0]>190)&(pixels[:,:,1]>110)&(pixels[:,:,1]<pixels[:,:,0]*.9)&(pixels[:,:,2]<90)&(pixels[:,:,3]>180)
            beak &= (yy>strong[1]+(strong[3]-strong[1])*.55)&(abs(xx-center_x)<max(20,(strong[2]-strong[0])*.2))
            ids,_=label(beak);counts=np.bincount(ids.ravel());counts[0]=0
            if not counts.any():
                raise ValueError(f'Cannot locate hawk beak in {name}: {box}')
            by,bx=np.where(ids==int(counts.argmax()))
            anchors.append((box[0]+float(bx.mean()),box[1]+float(by.mean())-62))
        else:
            anchors.append((box[0]+(strong[0]+strong[2])/2,box[1]+(strong[1]+strong[3])/2))
        tightboxes.append((box[0]+alpha[0],box[1]+alpha[1],box[0]+alpha[2],box[1]+alpha[3]))
    # Use the same scale across all hawk phases; folded wings remain folded.
    scale=.92 if name.startswith('hawk') else .65
    extent=max(max(abs(box[0]-ax),abs(box[2]-ax),abs(box[1]-ay),abs(box[3]-ay))*scale for box,(ax,ay) in zip(tightboxes,anchors))
    size=int(np.ceil((2*extent+8)/32)*32)
    frames=[place(image,box,anchor,scale,size) for box,anchor in zip(tightboxes,anchors)]
    write(name,frames,size,radius=144 if name.startswith('hawk') else None,heading='up' if name=='firefly' else 'down')

for name in ['lily-pad-round-1','lily-pad-round-2','lily-pad-round-3']:
    image=Image.open(SOURCE/(name+'.png')).convert('RGBA')
    image=image.crop(image.getchannel('A').getbbox());image.thumbnail((256,256),Image.Resampling.LANCZOS)
    frame=Image.new('RGBA',(256,256));frame.paste(image,((256-image.width)//2,(256-image.height)//2))
    write(name,[frame],256)
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Prepared',len(manifest),'isolated animation sheets; original PNG files preserved.')
