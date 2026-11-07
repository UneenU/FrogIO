/** Draw inside the caller's tongue clipping path. */
export function textureTongue(ctx: CanvasRenderingContext2D, x: number, y: number,
  tipX: number, tipY: number, width: number) {
  const length = Math.hypot(tipX - x, tipY - y)
  if (length < .01 || width <= 0) return
  ctx.save(); ctx.clip(); ctx.translate(x, y); ctx.rotate(Math.atan2(tipY - y, tipX - x))
  const shade = ctx.createLinearGradient(0, -width / 2, 0, width / 2)
  shade.addColorStop(0, '#ffffff40'); shade.addColorStop(.35, '#ffffff08'); shade.addColorStop(1, '#68173e55')
  ctx.fillStyle = shade; ctx.fillRect(0, -width / 2, length, width)
  const groove = ctx.createLinearGradient(0, -width * .14, 0, width * .14)
  groove.addColorStop(0, '#70213e00'); groove.addColorStop(.4, '#70213e90')
  groove.addColorStop(.65, '#70213e60'); groove.addColorStop(1, '#ffd8e570')
  ctx.fillStyle = groove
  ctx.beginPath(); ctx.moveTo(length * .04, 0)
  ctx.bezierCurveTo(length * .25, -width * .18, length * .75, -width * .18, length * .96, 0)
  ctx.bezierCurveTo(length * .75, width * .18, length * .25, width * .18, length * .04, 0)
  ctx.fill()
  ctx.restore()
}

/** The tip is the midpoint of the wide base; the opposite vertex is at the mouth. */
export function drawShovelTongue(ctx: CanvasRenderingContext2D, mouthX: number, mouthY: number,
  tipX: number, tipY: number, width: number) {
  const dx = tipX - mouthX, dy = tipY - mouthY, length = Math.hypot(dx, dy)
  if (length < 1e-6 || width <= 0) return
  const nx = -dy / length * width / 2, ny = dx / length * width / 2
  const points = [{ x: mouthX, y: mouthY }, { x: tipX + nx, y: tipY + ny }, { x: tipX - nx, y: tipY - ny }]
  // Round just a small part of each corner, keeping the full supplied base width.
  const toward = (a: typeof points[number], b: typeof points[number]) => ({ x: a.x + (b.x - a.x) * .035, y: a.y + (b.y - a.y) * .035 })
  const start = toward(points[0], points[2])
  ctx.save(); ctx.beginPath(); ctx.moveTo(start.x, start.y)
  for (let i = 0; i < 3; i++) {
    const current = points[i], next = points[(i + 1) % 3]
    const after = toward(current, next), before = toward(next, current)
    ctx.quadraticCurveTo(current.x, current.y, after.x, after.y)
    ctx.lineTo(before.x, before.y)
  }
  ctx.closePath(); ctx.fillStyle = '#e987a4'; ctx.fill()
  ctx.strokeStyle = '#241726'; ctx.lineWidth = Math.min(.25, width * .08); ctx.lineJoin = 'round'; ctx.stroke()
  textureTongue(ctx, mouthX, mouthY, tipX, tipY, width)
  ctx.restore()
}
