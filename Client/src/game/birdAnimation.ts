export type HawkPhase = 'hawk-fly' | 'hawk-dive' | 'hawk-rise'
export interface BirdAnimationState {
  phase: HawkPhase
  since: number
  height: number
  movedAt: number
}

export function updateBirdAnimation(previous: BirdAnimationState | undefined, height: number, now: number): BirdAnimationState {
  if (!previous) return { phase: 'hawk-fly', since: now, height, movedAt: now }
  // Accumulate tiny height deltas instead of repeatedly switching on float jitter.
  const delta = height - previous.height
  const moving = Math.abs(delta) >= .25
  let phase = previous.phase
  if (moving) phase = delta > 0 ? 'hawk-rise' : 'hawk-dive'
  else if (now - previous.movedAt > .5) phase = 'hawk-fly'
  // A phase must be visible for at least 120 ms; one packet cannot reset it.
  if (phase !== previous.phase && now - previous.since >= .12) {
    previous.phase = phase
    previous.since = now
  }
  if (moving) { previous.height = height; previous.movedAt = now }
  return previous
}

/** One sample is shared by the model and its shadow, including the dive endpoint. */
export function birdFrame(state: BirdAnimationState, now: number, count: number): number {
  const index = Math.floor(Math.max(0, now - state.since) * 9)
  return state.phase === 'hawk-dive' ? Math.min(count - 1, index) : index % count
}
