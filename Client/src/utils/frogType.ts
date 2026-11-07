import type { FrogType, Hero } from '@/types/contract'

/**
 * Приводит выбранного героя или имя лягушки из FrogInfo к каталогу ассетов.
 * Если найден Hero по id, его name приоритетнее старого идентификатора.
 */
export function frogTypeFromHero(value: string | null | undefined, heroes: Pick<Hero, 'id' | 'name'>[] = []): FrogType {
  if (!value) return 'common-frog'

  const selected = heroes.find((hero) => hero.id.toLowerCase() === value.toLowerCase())
    ?? heroes.find((hero) => hero.name.toLowerCase() === value.toLowerCase())
  const source = (selected?.name ?? value).toLowerCase().replace(/[^a-z0-9]+/g, '-')

  if (source.includes('sniper')) return 'sniper-frog'
  if (source.includes('sticky')) return 'sticky-frog'
  if (source.includes('heavy')) return 'heavy-frog'
  if (source.includes('common')) return 'common-frog'

  // Совместимость со старыми идентификаторами, пока клиенты и сервер обновляются.
  if (!selected && source.includes('vex')) return 'sticky-frog'
  if (!selected && source.includes('rook')) return 'heavy-frog'
  return 'common-frog'
}
