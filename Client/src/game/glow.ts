export const FIREFLY_GLOW_COLOR = '#ffe34d'
const textures = new Map<string, HTMLCanvasElement>()

/** Rasterize four small colour textures once, never use shadowBlur per insect. */
function texture(color: string) {
  let cached = textures.get(color)
  if (cached) return cached
  cached = document.createElement('canvas')
  cached.width = cached.height = 96
  const ctx = cached.getContext('2d')!
  const value = Number.parseInt(color.slice(1), 16)
  const rgb = `${value >> 16},${(value >> 8) & 255},${value & 255}`
  const gradient = ctx.createRadialGradient(48, 48, 0, 48, 48, 48)
  gradient.addColorStop(0, `rgba(${rgb},0.72)`)
  gradient.addColorStop(.4, `rgba(${rgb},0.48)`)
  gradient.addColorStop(.72, `rgba(${rgb},0.19)`)
  gradient.addColorStop(1, `rgba(${rgb},0)`)
  ctx.fillStyle = gradient
  ctx.fillRect(0, 0, 96, 96)
  textures.set(color, cached)
  return cached
}

export function drawGlow(ctx: CanvasRenderingContext2D, color: string, x: number, y: number, radius: number) {
  if (radius > 0) ctx.drawImage(texture(color), x - radius, y - radius, radius * 2, radius * 2)
}
