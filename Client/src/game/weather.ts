import { WeatherKind, type WeatherState } from '@/types/contract'
import type { Viewport } from './viewport'

export function weatherKind(weather?: WeatherState): WeatherKind {
  switch (weather?.kind) {
    case WeatherKind.Rain: case 'Rain': return WeatherKind.Rain
    case WeatherKind.Fog: case 'Fog': return WeatherKind.Fog
    default: return WeatherKind.Clear
  }
}

export function weatherLabel(weather?: WeatherState): string {
  switch (weatherKind(weather)) {
    case WeatherKind.Rain: return 'Дождь'
    case WeatherKind.Fog: return 'Туман'
    default: return 'Ясно'
  }
}

/** Weather is drawn over the world in CSS pixels, before the separate HTML HUD. */
export class WeatherPainter {
  private rain = 0
  private fog = 0
  private fogRadius = 100
  private lastDraw: number | null = null
  private readonly transitionSeconds = 3

  reset() {
    this.rain = this.fog = 0
    this.fogRadius = 100
    this.lastDraw = null
  }

  draw(ctx: CanvasRenderingContext2D, view: Viewport, weather: WeatherState | undefined,
    center: { x: number; y: number }, now: number) {
    const kind = weatherKind(weather)
    const elapsed = this.lastDraw === null ? 0 : Math.max(0, now - this.lastDraw)
    this.lastDraw = now
    const step = elapsed / this.transitionSeconds
    const approach = (value: number, active: boolean) => active ? Math.min(1, value + step) : Math.max(0, value - step)
    this.rain = approach(this.rain, kind === WeatherKind.Rain)
    this.fog = approach(this.fog, kind === WeatherKind.Fog)
    if (kind === WeatherKind.Fog) {
      const radius = weather?.visibilityRadius
      this.fogRadius = typeof radius === 'number' && Number.isFinite(radius) && radius > 0 ? radius : 100
    }
    if (this.rain > 0) this.drawRain(ctx, view, now, this.rain)
    if (this.fog > 0) this.drawFog(ctx, view, center, this.fogRadius, this.fog)
  }

  private drawRain(ctx: CanvasRenderingContext2D, view: Viewport, now: number, intensity: number) {
    ctx.save()
    ctx.fillStyle = `rgba(31,59,82,${0.13 * intensity})`
    ctx.fillRect(0, 0, view.width, view.height)
    const count = Math.min(260, Math.max(60, Math.floor(view.width * view.height / 6000)))
    ctx.strokeStyle = 'rgba(211,235,248,0.65)'
    ctx.lineWidth = 1.2
    ctx.lineCap = 'round'
    const drops = count * intensity
    for (let i = 0; i < Math.ceil(drops); i++) {
      const speed = 430 + (i % 7) * 37
      const x = ((i * 137.31 - now * 120) % (view.width + 40) + view.width + 40) % (view.width + 40) - 20
      const y = (i * 97.13 + now * speed) % (view.height + 40) - 20
      const length = 10 + i % 13
      ctx.globalAlpha = Math.min(1, drops - i)
      ctx.beginPath()
      ctx.moveTo(x, y)
      ctx.lineTo(x - length * 0.25, y + length)
      ctx.stroke()
    }
    ctx.restore()
  }

  private drawFog(ctx: CanvasRenderingContext2D, view: Viewport, center: { x: number; y: number }, radius: number, intensity: number) {
    if (!Number.isFinite(radius) || radius <= 0) radius = 100
    const x = view.left + center.x * view.scale
    const y = view.top + center.y * view.scale
    const pixels = radius * view.scale
    ctx.save()
    ctx.globalAlpha = 1
    ctx.globalCompositeOperation = 'source-over'
    const smooth = (value: number) => value * value * (3 - 2 * value)
    const veil = smooth(Math.min(1, intensity * 2))
    const conceal = smooth(Math.max(0, intensity * 2 - 1))
    ctx.fillStyle = `rgba(255,255,255,${0.5 * veil})`
    ctx.fillRect(0, 0, view.width, view.height)
    
    if (conceal > 0) {
      const edge = ctx.createRadialGradient(x, y, pixels * 0.85, x, y, pixels)
      edge.addColorStop(0, 'rgba(255,255,255,0)')
      edge.addColorStop(0.5, `rgba(255,255,255,${conceal * 0.5})`)
      edge.addColorStop(1, `rgba(255,255,255,${conceal})`)
      ctx.fillStyle = edge
      ctx.fillRect(0, 0, view.width, view.height)
    }
    ctx.restore()
  }
}
