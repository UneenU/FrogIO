<script setup lang="ts">
import { onMounted, ref } from 'vue'
import manifest from '@/assets/match/manifest.json'
import pad1 from '@/assets/match/lily-pad-round-1.png'
import pad2 from '@/assets/match/lily-pad-round-2.png'
import pad3 from '@/assets/match/lily-pad-round-3.png'
import common from '@/assets/match/common-frog-attack.png'
import heavy from '@/assets/match/heavy-frog-jump.png'
import sticky from '@/assets/match/sticky-frog-attack.png'
import mosquito from '@/assets/match/mosquito.png'
import { textureTongue } from '@/game/tongue'

const width = 640
const height = 440
const canvas = ref<HTMLCanvasElement | null>(null)
const spriteUrls = {
  'lily-pad-round-1': pad1,
  'lily-pad-round-2': pad2,
  'lily-pad-round-3': pad3,
  'common-frog/attack': common,
  'heavy-frog/jump': heavy,
  'sticky-frog/attack': sticky,
  mosquito,
} satisfies Partial<Record<keyof typeof manifest, string>>
type Sprite = keyof typeof spriteUrls

const target = { x: 323, y: 198 }
const pads = [
  { sprite: 'lily-pad-round-1', x: 85, y: 309, radius: 69, rotation: 0.7 },
  { sprite: 'lily-pad-round-2', x: 514, y: 254, radius: 79, rotation: -1.8 },
  { sprite: 'lily-pad-round-3', x: 149, y: 85, radius: 59, rotation: -0.4 },
  { sprite: 'lily-pad-round-2', x: 441, y: 70, radius: 42, rotation: 2.1 },
  { sprite: 'lily-pad-round-1', x: 580, y: 117, radius: 31, rotation: -0.8 },
  { sprite: 'lily-pad-round-3', x: 340, y: 363, radius: 45, rotation: 1.1 },
  { sprite: 'lily-pad-round-2', x: 556, y: 385, radius: 28, rotation: 2.7 },
  { sprite: 'lily-pad-round-1', x: 55, y: 172, radius: 23, rotation: -2.2 },
] satisfies { sprite: Sprite; x: number; y: number; radius: number; rotation: number }[]
const frogs = [
  { sprite: 'common-frog/attack', x: 85, y: 309, radius: 34, frame: 1, extension: .9, color: '#f980a4' },
  { sprite: 'sticky-frog/attack', x: 514, y: 254, radius: 38, frame: 1, extension: .62, color: '#c84883' },
  { sprite: 'heavy-frog/jump', x: 213, y: 131, radius: 34, frame: 3, extension: 0, color: '' },
] satisfies { sprite: Sprite; x: number; y: number; radius: number; frame: number; extension: number; color: string }[]

function drawTongue(ctx: CanvasRenderingContext2D, x: number, y: number, length: number,
  angle: number, radius: number, extension: number, color: string) {
  const start = radius * 1.15
  const end = start + (length - start) * extension
  const thickness = radius * .18
  ctx.save()
  ctx.translate(x, y)
  ctx.rotate(angle)
  ctx.lineCap = 'round'
  ctx.beginPath()
  ctx.moveTo(start, 0)
  ctx.lineTo(end, 0)
  ctx.strokeStyle = '#241726'
  ctx.lineWidth = thickness + 2
  ctx.stroke()
  ctx.strokeStyle = color
  ctx.lineWidth = thickness
  ctx.stroke()
  ctx.beginPath()
  ctx.roundRect(start - thickness / 2, -thickness / 2, end - start + thickness, thickness, thickness / 2)
  textureTongue(ctx, start, 0, end, 0, thickness)
  ctx.restore()
}

onMounted(async () => {
  const surface = canvas.value
  const ctx = surface?.getContext('2d')
  if (!surface || !ctx) return

  const images = new Map<Sprite, HTMLImageElement>()
  await Promise.all((Object.keys(spriteUrls) as Sprite[]).map(async (key) => {
    const image = new Image()
    image.src = spriteUrls[key]
    try {
      await image.decode()
      images.set(key, image)
    } catch {
      // One missing decoration should not hide the rest of the title art.
    }
  }))
  if (canvas.value !== surface) return
  ctx.scale(surface.width / width, surface.height / height)

  function drawSprite(key: Sprite, frame: number, x: number, y: number, radius: number, rotation: number) {
    const image = images.get(key)
    if (!image || !ctx) return
    const { size, bodyRadius } = manifest[key]
    const drawnSize = radius * size / bodyRadius
    ctx.save()
    ctx.translate(x, y)
    ctx.rotate(rotation)
    ctx.drawImage(image, frame * size, 0, size, size, -drawnSize / 2, -drawnSize / 2, drawnSize, drawnSize)
    ctx.restore()
  }

  for (const pad of pads) {
    drawSprite(pad.sprite, 0, pad.x, pad.y, pad.radius, pad.rotation)
  }
  for (const frog of frogs) {
    const dx = target.x - frog.x
    const dy = target.y - frog.y
    const angle = Math.atan2(dy, dx)
    if (!frog.extension) {
      ctx.fillStyle = '#163b4150'
      ctx.beginPath()
      ctx.ellipse(frog.x - 11, frog.y + 19, 37, 29, 0, 0, Math.PI * 2)
      ctx.fill()
    }
    drawSprite(frog.sprite, frog.frame, frog.x, frog.y, frog.radius, angle - Math.PI / 2)
    if (frog.extension) {
      drawTongue(ctx, frog.x, frog.y, Math.hypot(dx, dy), angle, frog.radius, frog.extension, frog.color)
    }
  }
  drawSprite('mosquito', 1, target.x, target.y, 22, -.65)
})
</script>

<template>
  <canvas ref="canvas" class="pond-art" :width="width * 2" :height="height * 2" aria-hidden="true" />
</template>

<style scoped>
.pond-art {
  display: block;
  width: 100%;
  height: auto;
}
</style>
