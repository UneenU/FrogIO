export class Viewport {
  width=1; height=1; x=0; y=0; zoom=1
  get scale(){return this.width/200*this.zoom}
  get left(){return this.width/2-this.x*this.scale}
  get top(){return this.height/2-this.y*this.scale}
  world(x:number,y:number){return {x:(x-this.left)/this.scale,y:(y-this.top)/this.scale}}
  reset(){this.zoom=1}
  zoomBy(factor:number){this.zoom=Math.min(8,Math.max(.5,this.zoom*factor))}
}
