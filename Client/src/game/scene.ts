import type { MatchState, MatchInit, FrogInfo, StorkState } from '@/types/contract'
import { frogTypeFromHero } from '@/utils/frogType'
import { artExtent, countFrames, drawArt } from './art'
import { birdFrame, updateBirdAnimation, type BirdAnimationState } from './birdAnimation'
import { effectGlowColor } from './effectColors'
import { visualRadius, heightScale, hawkRadius } from './geometry'
import { drawGlow, FIREFLY_GLOW_COLOR } from './glow'
import { hoveredPad } from './picking'
import { PlatformPainter, platformPath } from './platform'
import { MistPainter, mistOpacity } from './mist'
import { drawShovelTongue, textureTongue } from './tongue'
import type { Viewport } from './viewport'
import { mixColor, WorldClock } from './worldLighting'

type Position = { x: number; y: number; angle?: number }
type Animation = { name: string; since: number }
type BirdVisual = StorkState & { animation: BirdAnimationState; frame: number; visualRadius: number }

export class Scene {
  private frogs = new Map<number, Position>()
  private flies = new Map<number, Position>()
  private animations = new Map<number, Animation>()
  private birdMotion = new Map<string, BirdAnimationState>()
  private birds: BirdVisual[] = []
  private state: MatchState | null = null
  private received = 0
  private interval = .08
  private blend = 1
  private clock = new WorldClock()
  private platform = new PlatformPainter()
  private mist = new MistPainter()

  reset() {
    this.state = null
    this.frogs.clear(); this.flies.clear(); this.animations.clear(); this.birdMotion.clear()
    this.birds = []; this.clock.reset()
    this.received = 0; this.interval = .08; this.blend = 1
  }

  setState(state: MatchState, now: number) {
    if (this.state) {
      this.blend = Math.min(1, Math.max(0, (now - this.received) / this.interval))
      for (const [items, map] of [[this.state.frogs, this.frogs], [this.state.flies, this.flies]] as const) {
        const next = new Map<number, Position>()
        for (const item of items ?? []) next.set(item.id, this.position(item, map))
        map.clear()
        for (const [id, point] of next) map.set(id, point)
      }
      this.interval = Math.min(.15, Math.max(.025, now - this.received))
    }
    const live = new Set(state.frogs.map(frog => frog.id))
    for (const id of this.animations.keys()) if (!live.has(id)) this.animations.delete(id)

    const liveBirds = new Set<string>()
    const occurrences = new Map<string, number>()
    this.birds = (state.storks ?? []).map((bird, index) => {
      const id = String(bird.id ?? `index-${index}`)
      const occurrence = occurrences.get(id) ?? 0
      occurrences.set(id, occurrence + 1)
      const key = `${id}:${occurrence}`
      liveBirds.add(key)
      const animation = updateBirdAnimation(this.birdMotion.get(key), bird.height, now)
      this.birdMotion.set(key, animation)
      // Copy the authoritative coordinates. No interpolation by potentially
      // recycled bird IDs, screen offsets or per-frame silhouette bounds.
      return { ...bird, animation, frame: 0, visualRadius: hawkRadius(bird) }
    })
    for (const key of this.birdMotion.keys()) if (!liveBirds.has(key)) this.birdMotion.delete(key)
    this.clock.setHour(state.worldHour, now)
    this.state = state
    this.received = now
  }

  private position(item: Position & { id: number }, map: Map<number, Position>): Position {
    const old = map.get(item.id)
    if (!old) return { x: item.x, y: item.y, angle: item.angle ?? 0 }
    const delta = Math.atan2(Math.sin((item.angle ?? 0) - (old.angle ?? 0)), Math.cos((item.angle ?? 0) - (old.angle ?? 0)))
    return {
      x: old.x + (item.x - old.x) * this.blend,
      y: old.y + (item.y - old.y) * this.blend,
      angle: (old.angle ?? 0) + delta * this.blend,
    }
  }

  private animation(frog: FrogInfo, now: number) {
    const name = (frog.state & 16) || frog.tongueDetails?.state ? 'attack' : frog.state & 2 ? 'jump' : frog.state & 4 ? 'swim' : 'idle'
    let animation = this.animations.get(frog.id)
    if (animation?.name !== name) {
      animation = { name, since: now }
      this.animations.set(frog.id, animation)
    }
    const key = `${frogTypeFromHero(frog.name)}/${name}`
    const count = countFrames(key), fps = name === 'idle' ? 4 : name === 'attack' ? 10 : 8
    const index = Math.floor(Math.max(0, now - animation!.since) * fps)
    return { key, frame: name === 'idle' || name === 'swim' ? index % count : Math.min(count - 1, index) }
  }

  draw(ctx: CanvasRenderingContext2D, view: Viewport, init: MatchInit, now: number, self: number | null,
    debug = false, pointer: { x: number; y: number } | null = null) {
    const state = this.state
    const light = this.clock.sample(now)
    this.blend = Math.min(1, Math.max(0, (now - this.received) / this.interval))
    const bounds = state?.platform ?? { x: 0, y: 0, width: init.worldWidth, height: init.worldHeight }
    ctx.fillStyle = mixColor('#112b44', '#a6dbe9', light.daylight)
    ctx.fillRect(0, 0, view.width, view.height)
    ctx.save(); ctx.translate(view.left, view.top); ctx.scale(view.scale, view.scale)
    this.mist.draw(ctx, view, bounds, light, now)
    this.platform.draw(ctx, view, bounds, light, now)
    this.mist.drawEdges(ctx, bounds, light, now)
    if (!state) { ctx.restore(); return }

    const left = view.world(0, 0).x, top = view.world(0, 0).y
    const right = view.world(view.width, view.height).x, bottom = view.world(view.width, view.height).y
    const visible = (x: number, y: number, radius: number) => x + radius >= left && x - radius <= right && y + radius >= top && y - radius <= bottom

    for (const splash of state.splashes ?? []) {
      ctx.globalAlpha = mistOpacity(splash.x, splash.y, splash.height, bounds)
      ctx.strokeStyle = '#e6fff5a0'; ctx.lineWidth = .5
      ctx.beginPath(); ctx.arc(splash.x, splash.y, visualRadius(splash), 0, Math.PI * 2); ctx.stroke()
      ctx.globalAlpha = 1
    }

    for (const pad of state.pads ?? []) {
      const radius = visualRadius(pad)
      if (!visible(pad.x, pad.y, radius * 1.5)) continue
      const key = `lily-pad-round-${((pad.id % 3) + 3) % 3 + 1}`
      ctx.globalAlpha = mistOpacity(pad.x, pad.y, pad.height, bounds)
      drawArt(ctx, key, 0, pad.x, pad.y, radius, Math.sin(pad.id * 7.31) * Math.PI)
      ctx.globalAlpha = 1
    }
    const selected = hoveredPad(state.pads ?? [], pointer)
    if (selected) {
      const radius = visualRadius(selected)
      ctx.globalAlpha = mistOpacity(selected.x, selected.y, selected.height, bounds)
      ctx.beginPath(); ctx.arc(selected.x, selected.y, radius, 0, Math.PI * 2)
      ctx.fillStyle = '#ffffb82b'; ctx.fill()
      ctx.strokeStyle = '#fff29b'; ctx.lineWidth = Math.max(.18, radius * .045); ctx.stroke()
      ctx.globalAlpha = 1
    }

    for (const bird of this.birds) {
      const key = bird.animation.phase
      // Sample once: shadow and model must never use different dive frames.
      bird.frame = birdFrame(bird.animation, now, countFrames(key))
      bird.visualRadius = hawkRadius(bird)
      if (!visible(bird.x, bird.y, artExtent(key, bird.visualRadius))) continue
      ctx.globalAlpha = .24 * mistOpacity(bird.x, bird.y, bird.height, bounds)
      drawArt(ctx, key, bird.frame, bird.x, bird.y, bird.visualRadius, bird.angle - Math.PI / 2, true)
      ctx.globalAlpha = 1
    }

    for (const frog of state.frogs ?? []) {
      const point = this.position(frog, this.frogs), radius = visualRadius(frog)
      const animation = this.animation(frog, now)
      if (!visible(point.x, point.y, artExtent(animation.key, radius))) continue
      ctx.globalAlpha = mistOpacity(point.x, point.y, frog.height, bounds)
      drawArt(ctx, animation.key, animation.frame, point.x, point.y, radius, (point.angle ?? 0) - Math.PI / 2)
      if (frog.id === self || debug) {
        ctx.strokeStyle = frog.id === self ? '#ffdf4d' : '#ff546b'; ctx.lineWidth = .16
        ctx.beginPath(); ctx.arc(point.x, point.y, radius, 0, Math.PI * 2); ctx.stroke()
      }
      const tongue = frog.tongueDetails
      if (tongue?.state && Number.isFinite(tongue.tipX) && Number.isFinite(tongue.tipY)) {
        const mouthX = point.x + Math.cos(point.angle ?? 0) * radius * 1.15
        const mouthY = point.y + Math.sin(point.angle ?? 0) * radius * 1.15
        const width = Math.max(.2, (tongue.width || frog.radius * .2) * heightScale(frog.height))
        if (tongue.kind?.toLowerCase() === 'shovel') {
          drawShovelTongue(ctx, mouthX, mouthY, tongue.tipX, tongue.tipY, width)
        } else {
          ctx.lineCap = 'round'; ctx.strokeStyle = '#241726'; ctx.lineWidth = width + .25
          ctx.beginPath(); ctx.moveTo(mouthX, mouthY); ctx.lineTo(tongue.tipX, tongue.tipY); ctx.stroke()
          ctx.strokeStyle = tongue.kind?.toLowerCase() === 'sticky' ? '#c84883' : '#f980a4'
          ctx.lineWidth = width; ctx.stroke()
          ctx.save(); ctx.translate(mouthX, mouthY)
          ctx.rotate(Math.atan2(tongue.tipY - mouthY, tongue.tipX - mouthX))
          const length = Math.hypot(tongue.tipX - mouthX, tongue.tipY - mouthY)
          ctx.beginPath(); ctx.roundRect(-width / 2, -width / 2, length + width, width, width / 2)
          textureTongue(ctx, 0, 0, length, 0, width)
          ctx.restore()
        }
      }
      ctx.globalAlpha = 1
    }

    for (const insect of state.flies ?? []) {
      const point = this.position(insect, this.flies), radius = visualRadius(insect)
      if (!visible(point.x, point.y, radius * 3.8)) continue
      ctx.globalAlpha = mistOpacity(point.x, point.y, insect.height, bounds)
      const type = (insect.type ?? '').toLowerCase()
      const key = type.includes('firefly') || type.includes('светля') ? 'firefly' : type.includes('mosquito') || type.includes('комар') ? 'mosquito' : 'fly'
      const effectColor = effectGlowColor(insect.effect)
      if (effectColor) drawGlow(ctx, effectColor, point.x, point.y, radius * (key === 'firefly' ? 3.8 : 3.1))
      if (key === 'firefly') drawGlow(ctx, FIREFLY_GLOW_COLOR, point.x, point.y, radius * (effectColor ? 2.7 : 3.2))
      drawArt(ctx, key, Math.floor(now * 16 + insect.id) % countFrames(key), point.x, point.y,
        radius * 2.3, (point.angle ?? 0) + (key === 'firefly' ? Math.PI / 2 : -Math.PI / 2))
      ctx.globalAlpha = 1
    }

    for (const bird of this.birds) {
      const alpha = Math.min(1, Math.max(0, (300 - bird.height) / 100))
      const key = bird.animation.phase
      if (alpha <= 0 || !visible(bird.x, bird.y, artExtent(key, bird.visualRadius))) continue
      ctx.globalAlpha = alpha * mistOpacity(bird.x, bird.y, bird.height, bounds)
      drawArt(ctx, key, bird.frame, bird.x, bird.y, bird.visualRadius, bird.angle - Math.PI / 2)
      ctx.globalAlpha = 1
    }
    const next = state.platform?.nextBounds
    if (next) {
      ctx.strokeStyle = '#ffce45'; ctx.lineWidth = .6; ctx.setLineDash([2, 1])
      ctx.beginPath(); platformPath(ctx, next); ctx.stroke(); ctx.setLineDash([])
    }
    ctx.restore()
  }

  target(id: number | null) { return id === null ? undefined : this.state?.frogs.find(frog => frog.id === id) }
}
