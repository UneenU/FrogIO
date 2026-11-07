/** Gameplay effect colours are independent of the insect species and artwork. */
export const EFFECT_GLOW_COLORS = {
  speed: '#ff8a28',
  experience: '#b36aff',
  unknown: '#ffffff',
} as const

export const EFFECT_GLOW_RULES = [
  { color: EFFECT_GLOW_COLORS.speed, tokens: ['ускор', 'speed', 'haste'] },
  { color: EFFECT_GLOW_COLORS.experience, tokens: ['опыт', 'прозрен', 'experience', 'expboost', 'xp'] },
] as const

export function effectGlowColor(effect: string | null | undefined): string | null {
  const name = String(effect ?? '').toLowerCase().replace(/ё/g, 'е').replace(/[\s_-]/g, '')
  if (!name || ['none', 'noeffect', 'безэффекта', 'нет', 'null'].includes(name)) return null
  for (const rule of EFFECT_GLOW_RULES) {
    if (rule.tokens.some(token => name.includes(token))) return rule.color
  }
  return EFFECT_GLOW_COLORS.unknown
}
