const wrap = (hour: number) => ((hour % 24) + 24) % 24
const smooth = (x: number) => {
  const t = Math.max(0, Math.min(1, x))
  return t * t * (3 - 2 * t)
}
export interface WorldLighting { hour: number; daylight: number; night: number }

/** Follow authoritative worldHour, including fractional hours and 23 -> 0.
 * Repeated snapshots with the same hour do not restart the transition.
 */
export class WorldClock {
  private initialized = false
  private from = 12
  private target = 12
  private since = 0
  private duration = 1

  reset() { this.initialized = false; this.from = this.target = 12; this.since = 0; this.duration = 1 }
  setHour(hour: number, now: number) {
    if (!Number.isFinite(hour)) return
    hour = wrap(hour)
    if (!this.initialized) {
      this.initialized = true
      this.from = this.target = hour
      this.since = now
      return
    }
    const delta = wrap(hour - wrap(this.target) + 12) - 12
    if (Math.abs(delta) < 1e-6) return
    this.from = this.unwrappedHour(now)
    this.target += delta
    this.since = now
    this.duration = Math.max(.35, Math.min(3, Math.abs(this.target - this.from) * .9))
  }
  private unwrappedHour(now: number) {
    return this.from + (this.target - this.from) * smooth((now - this.since) / this.duration)
  }
  sample(now: number): WorldLighting {
    const hour = wrap(this.unwrappedHour(now))
    const daylight = smooth((hour - 5) / 2) * (1 - smooth((hour - 20) / 2))
    return { hour, daylight, night: 1 - daylight }
  }
}

export function mixColor(night: string, day: string, daylight: number): string {
  const n = Number.parseInt(night.slice(1), 16), d = Number.parseInt(day.slice(1), 16)
  const channel = (shift: number) => Math.round(((n >> shift) & 255) + (((d >> shift) & 255) - ((n >> shift) & 255)) * daylight)
  return `rgb(${channel(16)},${channel(8)},${channel(0)})`
}
