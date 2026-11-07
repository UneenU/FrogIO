import { MatchRenderer } from './renderer'
import { loadArt } from './art'
import type { MatchInit, MatchState } from '@/types/contract'
import { drawFrogGallery } from './frogGallery'

// A separate entry point exercises the real renderer without auth or a backend.
const canvas = document.querySelector<HTMLCanvasElement>('#pond')!
const init: MatchInit = { worldWidth: 280, worldHeight: 200, players: [] }
const renderer = new MatchRenderer(canvas)
const state: MatchState = {
  tick: 0, time: 0, timeLeft: 180, isEnded: false, worldHour: 12,
  platform: { x: 0, y: 0, width: 280, height: 200, shrinkPhase: 'Idle', shrinkTicksLeft: 0, nextBounds: null },
  frogs: [
    { id: 1, name: 'Common Frog', x: 112, y: 109, radius: 4, height: 0, angle: -.8, state: 1, effects: [], padId: 1, tongueDetails: { kind: 'Plain', state: 0, tipX: 0, tipY: 0, phase: 0, width: 1 } },
    { id: 2, name: 'Heavy Frog', x: 164, y: 134, radius: 4.8, height: 0, angle: -2.1, state: 1, effects: [], padId: 2, tongueDetails: { kind: 'Shovel', state: 0, tipX: 0, tipY: 0, phase: 0, width: 1 } },
    { id: 3, name: 'Sticky Frog', x: 202, y: 93, radius: 3.8, height: 0, angle: .3, state: 1, effects: [], padId: 3, tongueDetails: { kind: 'Sticky', state: 0, tipX: 0, tipY: 0, phase: 0, width: 1 } },
  ],
  pads: [
    { id: 1, x: 112, y: 109, radius: 10 }, { id: 2, x: 164, y: 134, radius: 12 },
    { id: 3, x: 202, y: 93, radius: 11 }, { id: 9, x: 79, y: 62, radius: 10 },
    { id: 18, x: 156, y: 57, radius: 9 }, { id: 6, x: 61, y: 135, radius: 8 },
    { id: 7, x: 226, y: 150, radius: 10 }, { id: 27, x: 103, y: 171, radius: 9 },
    { id: 12, x: 247, y: 49, radius: 8 }, { id: 5, x: 29, y: 70, radius: 9 },
  ],
  flies: Array.from({ length: 16 }, (_, i) => ({ id: i + 1, type: ['PlainFly', 'Mosquito', 'Firefly'][i % 3], effect: i === 9 ? 'speed' : i === 10 ? 'xp' : i === 12 ? 'unknown-effect' : '', x: 0, y: 0, height: 0, radius: 1.25, angle: 0 })),
  storks: [{ id: 1, x: 185, y: 60, radius: 19, height: 120, angle: 0, phase: 0 }], splashes: [], scores: {},
}
let paused = false
let hour = 12
let time = 0
let last = performance.now()
let frame = 0
let shrinkMode = 0
const gallery = document.querySelector<HTMLCanvasElement>('#frog-gallery')!
void loadArt().then(() => {
  renderer.setInit(init)
  renderer.start()
  update(performance.now())
})
const select = document.querySelector<HTMLSelectElement>('#view')!
// MatchRenderer intentionally keeps camera private; use input's public zoom handling.
function setView() {
  renderer.resetView()
  if (select.value === 'wide') canvas.dispatchEvent(new WheelEvent('wheel', { deltaY: 545, cancelable: true }))
}
select.addEventListener('change', setView)
for (const [id, value] of [['day', 12], ['night', 0]] as const) {
  document.getElementById(id)!.addEventListener('click', () => {
    hour = value
    // Keep the scene clock so the preview also shows the smooth transition.
    setView()
    document.getElementById('day')!.setAttribute('aria-pressed', String(hour === 12))
    document.getElementById('night')!.setAttribute('aria-pressed', String(hour === 0))
  })
}
document.getElementById('pause')!.addEventListener('click', () => {
  paused = !paused
  document.getElementById('pause')!.setAttribute('aria-pressed', String(paused))
  document.getElementById('pause')!.textContent = paused ? 'Продолжить' : 'Пауза'
})
document.getElementById('shrink')!.addEventListener('click', () => {
  shrinkMode = (shrinkMode + 1) % 3
  document.getElementById('shrink')!.setAttribute('aria-pressed', String(shrinkMode !== 0))
  document.getElementById('shrink')!.textContent = ['Сужение', 'Сузить', 'Восстановить'][shrinkMode]
})
function update(now: number) {
  frame = requestAnimationFrame(update)
  if (!paused) time += Math.min((now - last) / 1000, .05)
  last = now
  state.time = time
  state.tick++
  drawFrogGallery(gallery, time)
  state.worldHour = hour
  state.platform = shrinkMode === 2
    ? { x: 35, y: 25, width: 210, height: 150, shrinkPhase: 'Idle', shrinkTicksLeft: 0, nextBounds: null }
    : { x: 0, y: 0, width: 280, height: 200, shrinkPhase: shrinkMode === 1 ? 'Warning' : 'Idle', shrinkTicksLeft: 30,
      nextBounds: shrinkMode === 1 ? { x: 35, y: 25, width: 210, height: 150 } : null }
  state.flies.forEach((f, i) => {
    const phase = time * (.24 + i * .009) + i * 2.4
    f.x = 62 + (i % 5) * 34 + Math.cos(phase) * 8
    f.y = 60 + Math.floor(i / 5) * 34 + Math.sin(phase * 1.3) * 7
    f.angle = Math.atan2(Math.cos(phase * 1.3) * 1.3, -Math.sin(phase))
  })
  const bird = state.storks[0]
  bird.x = 153 + Math.sin(time * .2) * 42
  bird.y = 76 + Math.cos(time * .2) * 10
  bird.angle = Math.atan2(-Math.sin(time * .2) * 10, Math.cos(time * .2) * 42)
  bird.height = 105 + Math.sin(time * .7) * 65
  renderer.setState(state)
}
window.addEventListener('pagehide', () => { cancelAnimationFrame(frame); renderer.destroy() }, { once: true })
