import type { PadState } from '@/types/contract'
import { visualRadius } from './geometry'

export function hoveredPad(pads: PadState[], pointer: { x: number; y: number } | null): PadState | undefined {
  if (!pointer) return undefined
  for (let i = pads.length - 1; i >= 0; i--) {
    const pad = pads[i]
    if (Math.hypot(pointer.x - pad.x, pointer.y - pad.y) <= visualRadius(pad)) return pad
  }
  return undefined
}
