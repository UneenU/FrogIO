import type { Input } from '@/types/contract'
import type { Viewport } from './viewport'
export interface RendererOptions { onInput?:(input:Input)=>void; onZoom?:(zoom:number)=>void }
export class Controls {
  private timer:ReturnType<typeof setInterval>|undefined
  private point:{x:number;y:number}|null=null
  private buttons=0
  private inside=false
  constructor(private canvas:HTMLCanvasElement,private view:Viewport,private options:RendererOptions){}
  get pointerWorld(){return this.inside&&this.point?this.view.world(this.point.x,this.point.y):null}
  attach(){
    this.canvas.addEventListener('pointermove',this.move)
    this.canvas.addEventListener('pointerdown',this.down)
    this.canvas.addEventListener('pointerup',this.move)
    this.canvas.addEventListener('pointercancel',this.release)
    this.canvas.addEventListener('lostpointercapture',this.release)
    this.canvas.addEventListener('pointerleave',this.leave)
    this.canvas.addEventListener('wheel',this.wheel,{passive:false})
    this.canvas.addEventListener('contextmenu',this.context)
    window.addEventListener('blur',this.blur)
    this.timer=setInterval(()=>{if(!document.hidden)this.send()},1000/15)
  }
  detach(){
    if(this.timer!==undefined)clearInterval(this.timer);this.timer=undefined
    this.canvas.removeEventListener('pointermove',this.move);this.canvas.removeEventListener('pointerdown',this.down)
    this.canvas.removeEventListener('pointerup',this.move);this.canvas.removeEventListener('pointercancel',this.release)
    this.canvas.removeEventListener('lostpointercapture',this.release);this.canvas.removeEventListener('wheel',this.wheel)
    this.canvas.removeEventListener('pointerleave',this.leave)
    this.canvas.removeEventListener('contextmenu',this.context);window.removeEventListener('blur',this.blur)
    this.buttons=0;this.inside=false
  }
  private send(){if(this.point){const p=this.view.world(this.point.x,this.point.y);this.options.onInput?.({...p,buttons:[...(this.buttons&1?[0]:[]),...(this.buttons&2?[2]:[])],choiceNumber:-1})}}
  private move=(e:PointerEvent)=>{const box=this.canvas.getBoundingClientRect();this.point={x:e.clientX-box.left,y:e.clientY-box.top};this.inside=this.point.x>=0&&this.point.y>=0&&this.point.x<=box.width&&this.point.y<=box.height;const changed=this.buttons!==e.buttons;this.buttons=e.buttons;if(changed)this.send()}
  private down=(e:PointerEvent)=>{this.canvas.setPointerCapture(e.pointerId);this.move(e)}
  private release=()=>{this.buttons=0;this.send()}
  private blur=()=>{this.inside=false;this.release()}
  private leave=()=>{this.inside=false}
  private context=(e:Event)=>e.preventDefault()
  private wheel=(e:WheelEvent)=>{e.preventDefault();this.view.zoomBy(Math.exp(-e.deltaY*(e.deltaMode===1?33:1)*.0015));this.options.onZoom?.(this.view.zoom)}
}
