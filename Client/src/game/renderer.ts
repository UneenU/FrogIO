import type { MatchInit,MatchState } from '@/types/contract'
import { Controls,type RendererOptions } from './controls'
import { Viewport } from './viewport'
import { Scene } from './scene'
import { loadArt } from './art'
export type { RendererOptions } from './controls'

/** One RAF and one input timer per mounted match. No image processing in draw(). */
export class MatchRenderer {
  private view=new Viewport();private scene=new Scene();private controls:Controls
  private ctx:CanvasRenderingContext2D;private init:MatchInit|null=null;private self:number|null=null
  private raf=0;private active=false;private destroyed=false;private observer:ResizeObserver|null=null;private dpr=1;private last=0
  private debug=new URLSearchParams(location.search).has('hitboxes')
  constructor(private canvas:HTMLCanvasElement,private options:RendererOptions={}){
    this.ctx=canvas.getContext('2d',{alpha:false})!;this.controls=new Controls(canvas,this.view,options)
  }
  setInit(init:MatchInit){this.init=init;this.scene.reset();this.view.x=init.worldWidth/2;this.view.y=init.worldHeight/2;this.view.reset();this.options.onZoom?.(1)}
  setState(state:MatchState){this.scene.setState(state,performance.now()/1000)}
  setSelfFrog(id:number|null){this.self=id}
  resetView(){this.view.reset();this.options.onZoom?.(1)}
  start(){
    if(this.active||this.destroyed)return;this.active=true;this.resize()
    this.observer=new ResizeObserver(this.resize);this.observer.observe(this.canvas)
    this.controls.attach();document.addEventListener('visibilitychange',this.visibility)
    void loadArt().then(()=>{if(this.active&&!this.destroyed&&!document.hidden&&!this.raf)this.raf=requestAnimationFrame(this.frame)}).catch(error=>console.error('Match assets could not be decoded',error))
  }
  destroy(){if(this.destroyed)return;this.destroyed=true;this.active=false;cancelAnimationFrame(this.raf);this.raf=0;this.observer?.disconnect();this.controls.detach();document.removeEventListener('visibilitychange',this.visibility);this.scene.reset()}
  private resize=()=>{
    const rect=this.canvas.getBoundingClientRect();this.view.width=Math.max(1,rect.width);this.view.height=Math.max(1,rect.height)
    this.dpr=Math.min(window.devicePixelRatio||1,1.5,Math.sqrt(2_000_000/(this.view.width*this.view.height)))
    this.canvas.width=Math.round(this.view.width*this.dpr);this.canvas.height=Math.round(this.view.height*this.dpr)
  }
  private visibility=()=>{if(document.hidden){cancelAnimationFrame(this.raf);this.raf=0}else if(this.active&&!this.destroyed&&!this.raf){this.last=0;this.raf=requestAnimationFrame(this.frame)}}
  private frame=(ms:number)=>{
    this.raf=0;if(!this.active||this.destroyed||document.hidden)return
    const now=ms/1000,dt=this.last?Math.min(.1,now-this.last):0;this.last=now
    const target=this.scene.target(this.self)
    if(target){const k=dt?1-Math.exp(-12*dt):1;this.view.x+=(target.x-this.view.x)*k;this.view.y+=(target.y-this.view.y)*k}
    this.ctx.setTransform(this.dpr,0,0,this.dpr,0,0)
    if(this.init)this.scene.draw(this.ctx,this.view,this.init,now,this.self,this.debug,this.controls.pointerWorld)
    this.raf=requestAnimationFrame(this.frame)
  }
}
