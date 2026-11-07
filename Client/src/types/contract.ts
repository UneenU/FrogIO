// ---------- REST ----------
export interface LobbyCreateRequest { name: string; maxPlayers: number }
export interface LobbyCreateResponse { lobbyId: string }
export interface LobbySummary { lobbyId: string; lobbyName: string }

export interface Hero {
  id: string
  name: string
  jumpDistance: number
  tongueLength: number
  size: number
  mass: number
  speed: number
}

export type FrogType = 'common-frog' | 'heavy-frog' | 'sticky-frog' | 'sniper-frog'

// ---------- Hub: лобби ----------
export interface LobbyMember { userId: number; nickname: string; isReady: boolean; heroId: string | null }


export interface LobbyMemberPayload {
  userId?: number
  playerId?: number
  nickname?: string
  isReady?: boolean
  chosenHero?: string | null
  chosenFrog?: string | null
  heroId?: string | null
}

export interface LobbyState {
  lobbyId: string
  selfId: number
  maxPlayers: number
  members: LobbyMemberPayload[]
}

export const HUB_ERROR_CODES = [
  'LOBBY_NOT_FOUND',
  'LOBBY_FULL',
  'MATCH_IN_PROGRESS',
  'INVALID_HERO',
  'INVALID_MODE',
  'NOT_IN_LOBBY',
  'NOT_ALL_READY',
] as const
export type HubErrorCode = (typeof HUB_ERROR_CODES)[number]

export const GAME_MODES = [{ id: 'default', label: 'Default' }] as const
export const MAX_PLAYERS = 4

// ---------- Hub: матч ----------
// Координаты и размеры в условных единицах (у.е.), начало слева сверху, ось Y вниз.

export interface PlayerInfo { userId: number; frogId: number; heroId?: string | null; heroType?: FrogType | null }
/** MatchStarted(init): приходит один раз перед первым UpdateWorldState. */
export interface MatchInit {
  worldWidth: number
  worldHeight: number
  players: PlayerInfo[]
}

export interface PadState {
  id: number
  x: number
  y: number
  radius: number
  height?: number
}
export interface FlyState {
  id: number
  type: string
  effect: string
  x: number
  y: number
  height: number
  radius: number
  angle: number
}
export interface TongueInfo {
  kind: string
  state: number
  tipX: number
  tipY: number
  phase: number
  width: number
}
export interface FrogInfo {
  id: number
  /** Тип лягушки, например Common Frog, Heavy Frog или Sticky Frog. */
  name: string
  x: number
  y: number
  height: number
  radius: number
  /** Радианы: Atan2(Facing.Y, Facing.X). */
  angle: number
  state: number
  effects: string[]
  padId: number | null
  tongueDetails: TongueInfo
}
/** Круговая волна от лягушки. */
export interface SplashState { id: number; x: number; y: number; radius: number; height?: number }
/** Хищная птица над картой. height — высота над водой (у.е.). */
export interface StorkState {
  id: number
  x: number
  y: number
  radius: number
  height: number
  angle: number
  /** Core.StorkPhase (число). Для отрисовки вид зависит только от height. */
  phase: number
}
export interface PlayerScore { playerId: number; score: number }
/** Словарь очков матча: ключ — ID пользователя, значение — количество очков. */
export type MatchScores = Record<string, number> | Map<number | string, number>
/** Фаза сужения платформы (enum C# может сериализоваться числом или строкой). */
export type ShrinkPhase = number | string
export interface RectState { x: number; y: number; width: number; height: number }
/** Геометрия платформы и состояние её сужения, приходящие с сервера. */
export interface PlatformState extends RectState {
  shrinkPhase: ShrinkPhase
  shrinkTicksLeft: number
  nextBounds: RectState | null
}
export interface MatchState {
  tick: number
  time: number
  timeLeft: number
  isEnded: boolean
  /** Текущий час игрового мира (0..23), задаётся сервером. */
  worldHour: number
  platform: PlatformState
  frogs: FrogInfo[]
  pads: PadState[]
  flies: FlyState[]
  splashes: SplashState[]
  storks: StorkState[]
  scores: MatchScores
}

/** Один вариант улучшения, сериализованный из Core.Systems.LevelListPosition. */
export interface LevelListPosition {
  name: string
  value: number
}

/** Статистика одного пользователя в завершённом матче. */
export interface UserStatistic {
  score: number
  /** Рейтинг пользователя после пересчёта по итогам матча. */
  newRating: number
}

/** Статистика завершённого матча, присылаемая событием MatchEnded(statistic). */
export interface MatchStatistic {
  userStatistics?: Record<string, UserStatistic> | Map<number | string, UserStatistic>
  /** Поддержка старого формата результата во время обновления сервера/клиента. */
  scores?: PlayerScore[]
  [key: string]: unknown
}

/** Старое имя типа оставлено для совместимости с существующими импортами. */
export type MatchResult = MatchStatistic

/** Кнопки мыши в кодах DOM MouseEvent.button. */
export const MouseButton = { Left: 0, Right: 2 } as const

/** Команда игрока: HandleCommandAsync(input). Приходит при каждом изменении курсора или кнопок. */
export interface Input {
  /** Курсор в мировых координатах (у.е.), а не в пикселях экрана. */
  x: number
  y: number
  /** Нажатые сейчас кнопки: 0 — левая (перемещение), 2 — правая (атака). Пустой список: ничего не нажато. */
  buttons: number[]
  /** Индекс улучшения из последнего полученного списка; -1 означает обычный ввод. */
  choiceNumber: number
}

// ---------- REST: история матчей ----------

export type MatchStatus = number | string

/** GET /api/user/{userId}/matches-history -> UserMatchHistoryDto[] */
export interface UserMatchHistoryDto {
  matchId: string
  startedAtUtc: string
  finishedAtUtc: string
  status: MatchStatus
  gameMode: string | null
  place: number
  score: number
  ratingBefore: number
  ratingAfter: number
}
