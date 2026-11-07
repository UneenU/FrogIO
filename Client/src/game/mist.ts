import type { RectState } from '@/types/contract'
import type { Viewport } from './viewport'
import { mixColor, type WorldLighting } from './worldLighting'

/** Fog applies outside the authoritative server rectangle, not the decorative rounded corners. */
export function mistOpacity(x: number, y: number, height: number | null | undefined, bounds: RectState) {
  if (x >= bounds.x && x <= bounds.x + bounds.width && y >= bounds.y && y <= bounds.y + bounds.height) return 1
  const h = typeof height === 'number' && Number.isFinite(height) ? height : 0
  const distanceX = Math.max(bounds.x - x, 0, x - bounds.x - bounds.width)
  const distanceY = Math.max(bounds.y - y, 0, y - bounds.y - bounds.height)
  const mistDepth = Math.min(1, Math.hypot(distanceX, distanceY) / 5)
  const heightOpacity = Math.max(0, Math.min(1, (h + 100) / 150))
  return 1 - mistDepth * (1 - heightOpacity)
}

/** A cached, gently drifting vapour texture. No blur or gradients in the frame loop. */
export class MistPainter {
  private texture: HTMLCanvasElement | null = null
  private patterns = new WeakMap<CanvasRenderingContext2D, CanvasPattern>()
  private getPattern(ctx: CanvasRenderingContext2D) {
    let pattern = this.patterns.get(ctx)
    if (pattern) return pattern
    if (!this.texture) {
      const texture = document.createElement('canvas')
      texture.width = texture.height = 256
      const g = texture.getContext('2d')!
      for (let i = 0; i < 14; i++) {
        const x = (i * 97 + 31) % 256, y = (i * 61 + 19) % 256, radius = 35 + (i % 4) * 8
        for (const ox of [-256, 0, 256]) for (const oy of [-256, 0, 256]) {
          const gradient = g.createRadialGradient(x + ox, y + oy, 0, x + ox, y + oy, radius)
          gradient.addColorStop(0, 'rgba(238,251,255,.23)')
          gradient.addColorStop(1, 'rgba(222,245,255,0)')
          g.fillStyle = gradient; g.fillRect(x + ox - radius, y + oy - radius, radius * 2, radius * 2)
        }
      }
      this.texture = texture
    }
    pattern = ctx.createPattern(this.texture, 'repeat')!
    this.patterns.set(ctx, pattern)
    return pattern
  }
  private edgePattern: CanvasPattern | null = null
  drawEdges(ctx: CanvasRenderingContext2D, bounds: RectState, light: WorldLighting, now: number) {
    if (!this.edgePattern) {
      this.getPattern(ctx)
      const tile = document.createElement('canvas'); tile.width = 256; tile.height = 64
      const g = tile.getContext('2d')!
      g.drawImage(this.texture!, 0, 0)
      g.globalCompositeOperation = 'destination-in'
      const fade = g.createLinearGradient(0, 0, 0, 64)
      fade.addColorStop(0, 'transparent'); fade.addColorStop(.35, 'white'); fade.addColorStop(1, 'transparent')
      g.fillStyle = fade; g.fillRect(0, 0, 256, 64)
      this.edgePattern = ctx.createPattern(tile, 'repeat')!
    }
    const depth = Math.min(7, bounds.width / 8, bounds.height / 8)
    const outside = Math.min(5, Math.min(bounds.width, bounds.height) * .025)
    const strip = (x: number, y: number, length: number, angle: number) => {
      ctx.save(); ctx.translate(x, y); ctx.rotate(angle)
      ctx.translate(0, -outside); ctx.scale(.35, (depth + outside) / 64)
      const drift = (now * .7) % 256
      ctx.translate(drift, 0); ctx.fillStyle = this.edgePattern!
      ctx.fillRect(-drift, 0, length / .35, 64); ctx.restore()
    }
    ctx.save(); ctx.globalAlpha = .6 + light.daylight * .2
    strip(bounds.x, bounds.y, bounds.width, 0)
    strip(bounds.x + bounds.width, bounds.y + bounds.height, bounds.width, Math.PI)
    strip(bounds.x + bounds.width, bounds.y, bounds.height, Math.PI / 2)
    strip(bounds.x, bounds.y + bounds.height, bounds.height, -Math.PI / 2)
    ctx.restore()
  }
  draw(ctx: CanvasRenderingContext2D, view: Viewport, bounds: RectState, light: WorldLighting, now: number) {
    const p0 = view.world(0, 0), p1 = view.world(view.width, view.height)
    const margin = 2
    ctx.save(); ctx.beginPath()
    ctx.rect(p0.x - margin, p0.y - margin, p1.x - p0.x + margin * 2, p1.y - p0.y + margin * 2)
    ctx.rect(bounds.x, bounds.y, bounds.width, bounds.height)
    ctx.clip('evenodd')
    ctx.fillStyle = mixColor('#28465b', '#d5e8ee', light.daylight)
    ctx.globalAlpha = .38; ctx.fillRect(p0.x - margin, p0.y - margin, p1.x - p0.x + margin * 2, p1.y - p0.y + margin * 2)
    ctx.globalAlpha = .58 + light.daylight * .2
    ctx.translate(now * .25, now * .11); ctx.scale(.35, .35); ctx.fillStyle = this.getPattern(ctx)
    ctx.fillRect((p0.x - margin - now * .25) / .35, (p0.y - margin - now * .11) / .35,
      (p1.x - p0.x + margin * 2) / .35, (p1.y - p0.y + margin * 2) / .35)
    ctx.restore()
  }
}
