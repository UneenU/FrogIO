import manifest from '@/assets/match/manifest.json'
const files = import.meta.glob<string>('../assets/match/*.png', { eager:true, query:'?url', import:'default' })
type Definition = { file:string; size:number; count:number; bodyRadius:number; heading:string }
export const definitions: Record<string,Definition> = manifest
type FrameSource = ImageBitmap | HTMLCanvasElement
const images = new Map<string,FrameSource[]>()
let pending: Promise<void> | undefined
export function loadArt() {
  return pending ??= Promise.all(Object.entries(files).map(async ([path,url]) => {
    const filename = path.split('/').pop()!
    const base = filename.replace('-shadow.png', '.png')
    const definition = Object.values(definitions).find(def => def.file === base)
    if (!definition) return
    const image=new Image();image.src=url;await image.decode()
    if (image.naturalWidth !== definition.size * definition.count || image.naturalHeight !== definition.size) {
      throw new Error(`Invalid sprite sheet dimensions: ${filename}`)
    }
    // Separate GPU images prevent sampling a neighbouring atlas frame when a
    // large, rotated hawk is scaled up. This runs only during asset loading.
    const frames = await Promise.all(Array.from({ length: definition.count }, async (_, index): Promise<FrameSource> => {
      if (typeof createImageBitmap === 'function') {
        return createImageBitmap(image, index * definition.size, 0, definition.size, definition.size)
      }
      const frame = document.createElement('canvas')
      frame.width = frame.height = definition.size
      frame.getContext('2d')!.drawImage(image, index * definition.size, 0, definition.size, definition.size, 0, 0, definition.size, definition.size)
      return frame
    }))
    images.set(filename, frames)
  })).then(()=>{}).catch(error => { pending = undefined; throw error })
}
export function drawArt(ctx:CanvasRenderingContext2D,key:string,frame:number,x:number,y:number,radius:number,angle=0,shadow=false) {
  const def=definitions[key];if(!def)return
  const frames=images.get(shadow?def.file.replace('.png','-shadow.png'):def.file);if(!frames)return
  if (![x,y,radius,angle,frame].every(Number.isFinite) || radius <= 0) return
  const size=radius*def.size/def.bodyRadius
  const index=Math.max(0,Math.min(def.count-1,Math.floor(frame)))
  ctx.save();ctx.translate(x,y);ctx.rotate(angle)
  ctx.drawImage(frames[index],-size/2,-size/2,size,size)
  ctx.restore()
}
export function countFrames(key:string) { return definitions[key]?.count ?? 1 }

/** Rotated square extent, including feet/wings and height scaling. */
export function artExtent(key: string, radius: number) {
  const definition = definitions[key]
  return definition ? radius * definition.size / definition.bodyRadius / Math.SQRT2 : radius
}
