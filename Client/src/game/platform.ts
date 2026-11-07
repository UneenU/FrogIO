import type { RectState } from '@/types/contract'
import type { Viewport } from './viewport'
import { mixColor, type WorldLighting } from './worldLighting'

export function cornerRadius(bounds: RectState) { return Math.min(3, Math.min(bounds.width, bounds.height) * .025) }
export function platformPath(ctx: CanvasRenderingContext2D, bounds: RectState, expansion = 0) {
  ctx.roundRect(bounds.x - expansion, bounds.y - expansion,
    bounds.width + 2 * expansion, bounds.height + 2 * expansion, cornerRadius(bounds) + expansion)
}

/** One tiny repeating texture, four fills for the streams, no filter/tile cache. */
export class PlatformPainter {
  private texture: HTMLCanvasElement | null = null
  private patterns = new WeakMap<CanvasRenderingContext2D, CanvasPattern>()
  private pattern(ctx: CanvasRenderingContext2D) {
    let pattern = this.patterns.get(ctx)
    if (pattern) return pattern
    if (!this.texture) {
      this.texture = document.createElement('canvas')
      this.texture.width = this.texture.height = 64
      const g = this.texture.getContext('2d')!
      g.lineCap = 'round'
      for (let i = 0; i < 8; i++) {
        const x = i * 8 + 3, y = (i * 19) % 64
        g.strokeStyle = i % 2 ? '#d8fff57a' : '#ffffffb0'
        g.lineWidth = i % 3 ? 1.5 : 3
        g.beginPath(); g.moveTo(x, y); g.lineTo(x, y + 12); g.stroke()
        g.beginPath(); g.moveTo(x, y - 64); g.lineTo(x, y - 52); g.stroke()
      }
    }
    pattern = ctx.createPattern(this.texture, 'repeat')!
    this.patterns.set(ctx, pattern)
    return pattern
  }

  draw(ctx: CanvasRenderingContext2D, view: Viewport, bounds: RectState, light: WorldLighting, now: number) {
    const { daylight } = light
    const fall = Math.min(5, Math.min(bounds.width, bounds.height) * .025)
    const topColor = mixColor('#22627b', '#45d3b6', daylight)
    const bottomColor = mixColor('#153c5c', '#179fa8', daylight)
    ctx.save()
    // Paint the platform and waterfall as one continuous surface. The gradient
    // extends to the outside edge and naturally clamps at its top/bottom stops.
    const water = ctx.createLinearGradient(0, bounds.y, 0, bounds.y + bounds.height)
    water.addColorStop(0, topColor); water.addColorStop(1, bottomColor)
    ctx.beginPath(); platformPath(ctx, bounds, fall)
    ctx.fillStyle = water; ctx.fill()
    ctx.save()
    ctx.beginPath(); platformPath(ctx, bounds, fall); platformPath(ctx, bounds)
    ctx.clip('evenodd')
    const pattern = this.pattern(ctx)
    const strip = (x: number, y: number, length: number, angle: number) => {
      ctx.save(); ctx.translate(x, y); ctx.rotate(angle); ctx.scale(.12, .12)
      const offset = (now * 26) % 64
      ctx.translate(0, offset); ctx.fillStyle = pattern
      ctx.fillRect(0, -offset, length / .12, fall / .12); ctx.restore()
    }
    ctx.globalAlpha = .55 + .3 * daylight
    strip(bounds.x + bounds.width, bounds.y, bounds.width, Math.PI)
    strip(bounds.x, bounds.y + bounds.height, bounds.width, 0)
    strip(bounds.x + bounds.width, bounds.y + bounds.height, bounds.height, -Math.PI / 2)
    strip(bounds.x, bounds.y, bounds.height, Math.PI / 2)
    ctx.restore()
    // The server rectangle is unchanged. Rounding affects only paint and clipping.
    ctx.save(); ctx.beginPath(); platformPath(ctx, bounds); ctx.clip()
    const left = Math.max(bounds.x, view.world(0, 0).x), top = Math.max(bounds.y, view.world(0, 0).y)
    const right = Math.min(bounds.x + bounds.width, view.world(view.width, view.height).x)
    const bottom = Math.min(bounds.y + bounds.height, view.world(view.width, view.height).y)
    ctx.strokeStyle = `rgba(202,255,237,${.1 + .15 * daylight})`; ctx.lineWidth = .16; ctx.beginPath()
    for (let y = Math.floor(top / 20) * 20; y < bottom; y += 20) {
      for (let x = Math.floor(left / 26) * 26; x < right; x += 26) {
        ctx.moveTo(x, y); ctx.quadraticCurveTo(x + 3, y + Math.sin(now * .6 + x + y), x + 6, y)
      }
    }
    ctx.stroke(); ctx.restore()
    // Sparse foam on the outside lip only; nothing outlines the platform itself.
    ctx.beginPath(); platformPath(ctx, bounds, Math.max(0, fall - .25))
    ctx.strokeStyle = `rgba(229,255,250,${.35 + .3 * daylight})`
    ctx.lineWidth = .42; ctx.lineCap = 'round'; ctx.setLineDash([.4, 1.7, .8, 2.4])
    ctx.lineDashOffset = -now * .45; ctx.stroke(); ctx.setLineDash([])
    ctx.restore()
  }
}
