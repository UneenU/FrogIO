import type { Hero, FrogType } from '@/types/contract'
import { frogTypeFromHero } from '@/utils/frogType'

// Фон тайтл-экрана остаётся отдельным необязательным ресурсом.
const bgFiles = import.meta.glob<string>('../assets/title-bg.{png,webp,jpg}', {
  eager: true,
  query: '?url',
  import: 'default',
})

// Для карточек лобби используется отдельно вырезанный первый кадр idle, без CSS-прокрутки спрайт-листа.
const frogIdleFiles = import.meta.glob<string>('../assets/game/frogs/*/idle-first.png', {
  eager: true,
  query: '?url',
  import: 'default',
})
const idleUrls = new Map<FrogType, string>()
for (const [path, url] of Object.entries(frogIdleFiles)) {
  const type = path.split('/').slice(-2, -1)[0] as FrogType
  idleUrls.set(type, url)
}

const bgByType: Record<FrogType, string> = {
  'common-frog': '#d5f397',
  'heavy-frog': '#ffe0a0',
  'sticky-frog': '#a7eee1',
  'sniper-frog': '#b8ddc7',
}

export const heroImage = (hero: Pick<Hero, 'id' | 'name'>): string | null =>
  idleUrls.get(frogTypeFromHero(hero.name, [hero])) ?? null
export const heroBackground = (hero: Pick<Hero, 'id' | 'name'>) =>
  bgByType[frogTypeFromHero(hero.name, [hero])]

export const titleBackground: string | null = Object.values(bgFiles)[0] ?? null
