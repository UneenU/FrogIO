import { countFrames,drawArt } from './art'
export function drawFrogGallery(canvas:HTMLCanvasElement,seconds:number){
  if(canvas.width!==1320){canvas.width=1320;canvas.height=735}
  const ctx=canvas.getContext('2d')!;ctx.setTransform(1.5,0,0,1.5,0,0)
  ctx.fillStyle='#fffbed';ctx.fillRect(0,0,880,490);ctx.font='bold 12px system-ui';ctx.textAlign='center'
  const animations=['idle','jump','attack','swim']
  for(let row=0;row<3;row++)for(let col=0;col<4;col++){
    const key=`${['common-frog','heavy-frog','sticky-frog'][row]}/${animations[col]}`
    const x=170+col*190,y=105+row*148
    ctx.fillStyle='#173a36';if(row===0)ctx.fillText(animations[col].toUpperCase(),x,24)
    if(col===0)ctx.fillText(['Зелёная','Оранжевая','Бирюзовая'][row],40,y)
    drawArt(ctx,key,Math.floor(seconds*8)%countFrames(key),x,y,40)
    ctx.strokeStyle='#e24f5f';ctx.lineWidth=1;ctx.beginPath();ctx.arc(x,y,40,0,Math.PI*2);ctx.stroke()
  }
}
