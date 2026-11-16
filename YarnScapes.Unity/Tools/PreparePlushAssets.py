from pathlib import Path
from PIL import Image, ImageFilter, ImageDraw
import numpy as np
root=Path(__file__).resolve().parents[1]
out=root/'Assets/Resources/YarnMaterialsV2'
out.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(1152)
source=Image.open(root/'aigc/yarn-knit-reference-v1.png').convert('L').resize((512,512))
a=np.array(source,dtype=float)
blur=np.array(source.filter(ImageFilter.GaussianBlur(2)),dtype=float)
# Aligned, gently twisting cotton filaments, with the generated reference's
# irregular grain. U follows the yarn; V wraps around its circumference.
yy,xx=np.mgrid[:512,:512]
filaments=np.zeros((512,512),dtype=float)
for j in range(95):
 phase=float(rng.uniform(0,2*np.pi)); offset=float(rng.uniform(0,512))
 center=offset+xx*.85+np.sin(xx/170+phase)*float(rng.uniform(2,8))
 distance=(yy-center+256)%512-256
 weight=float(rng.uniform(-.8,1))
 filaments += weight*np.exp(-distance*distance/(2*float(rng.uniform(.55,1.6))**2))
fiber=np.clip(128+filaments*47+(a-blur)*.55+rng.normal(0,3,a.shape),0,255).astype('uint8')
Image.fromarray(fiber).save(out/'fiber_roughness.png')
y,x=np.mgrid[:192,:512]
v=(y/191-.5)*2
roundness=np.sqrt(np.maximum(0,1-v*v))
plies=.87+.13*np.cos((x/32+v*1.1)*np.pi*2)
light=(.46+.53*roundness-v*.11)*plies+rng.normal(0,.012,x.shape)
rgba=np.zeros((192,512,4),dtype='uint8')
rgba[:,:,:3]=np.clip(light[:,:,None]*255,0,255).astype('uint8')
rgba[:,:,3]=np.clip((1-abs(v))*7,0,1)*255
strand=Image.fromarray(rgba)
draw=ImageDraw.Draw(strand)
for _ in range(650):
 xx=int(rng.integers(0,512)); yy=int(rng.integers(14,178))
 length=int(rng.integers(3,11)); dy=int(rng.integers(-8,9))
 draw.line((xx,yy,xx+length,yy+dy),fill=(230,230,225,int(rng.integers(35,85))),width=1)
thread_out=root/'Packages/com.weavekit.runtime/Runtime/Resources/WeaveKit'
thread_out.mkdir(parents=True,exist_ok=True)
strand.save(thread_out/'thread_strip.png')
spool=Image.open(root/'Assets/Resources/YarnMaterialsV1/spool_wrap.png').convert('RGBA')
draw=ImageDraw.Draw(spool)
for _ in range(1400):
 xx=int(rng.integers(7,248)); yy=int(rng.integers(3,381))
 value=int(rng.integers(175,245))
 draw.line((xx,yy,xx+int(rng.integers(2,9)),yy+int(rng.integers(-3,4))),fill=(value,value,value,int(rng.integers(40,125))),width=1)
spool.save(out/'spool_wrap.png')
print('Plush fiber roughness and fuzzy strand assets prepared.')
