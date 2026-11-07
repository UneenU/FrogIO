export function heightScale(height: number | null | undefined): number {
  return 1 + (typeof height === 'number' && Number.isFinite(height) ? height : 0) / 100
}

export function visualRadius(object: { radius: number; height?: number | null }): number {
  return Math.max(0, object.radius * heightScale(object.height))
}

export function hawkRadius(object: { radius: number; height?: number | null }): number {
  const height = typeof object.height === 'number' && Number.isFinite(object.height) ? object.height : 0
  return Math.max(0, object.radius * (1 + height / 1000))
}
