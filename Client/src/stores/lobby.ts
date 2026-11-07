import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { gameHub, type HubEventHandlers } from '@/hub/gameHub'
import { useMatchStore } from '@/stores/match'
import { useAuthStore } from '@/stores/auth'
import { GAME_MODES, MAX_PLAYERS, type Input, type LobbyMember, type LobbyMemberPayload, type LobbyState, type MatchStatistic } from '@/types/contract'

export type Phase = 'idle' | 'connecting' | 'lobby' | 'match' | 'result'

export const useLobbyStore = defineStore('lobby', () => {
  const match = useMatchStore()
  const auth = useAuthStore()

  const phase = ref<Phase>('idle')
  const lobbyId = ref('')
  const lobbyName = ref('')
  const selfId = ref<number | null>(null)
  const maxPlayers = ref(MAX_PLAYERS)
  const members = ref<LobbyMember[]>([])
  const heroId = ref<string | null>(null)
  const mode = ref<string>(GAME_MODES[0].id)
  /** Сообщение для тайтл-скрина (обрыв связи, неудачный вход по ссылке). */
  const notice = ref<string | null>(null)
  /** После YouWereDisconnected не переподключаем эту вкладку автоматически: иначе она
   * могла бы тут же вытеснить новое соединение из другой вкладки. */
  const autoConnectBlocked = ref(false)

  const isReady = computed(() => members.value.find((m) => m.userId === selfId.value)?.isReady ?? false)
  /** Кнопка «Начать матч» доступна, когда готовы все, кто сейчас в лобби. */
  const allReady = computed(() => members.value.length > 0 && members.value.every((m) => m.isReady))

  /** Возвращает ID независимо от того, какое имя поля использует DTO сервера. */
  function memberId(payload: LobbyMemberPayload | null | undefined): number | null {
    if (!payload) return null
    const id = payload.userId ?? payload.playerId
    return typeof id === 'number' && Number.isFinite(id) ? id : null
  }

  /** Приводит DTO события или JoinLobby к единому формату для UI. */
  function normalizeMember(payload: LobbyMemberPayload): LobbyMember | null {
    const userId = memberId(payload)
    if (userId === null) {
      if (import.meta.env.DEV) console.warn('[lobby] получен участник без userId/playerId', payload)
      return null
    }

    return {
      userId,
      nickname: payload.nickname ?? String(userId),
      isReady: payload.isReady ?? false,
      heroId: payload.chosenHero ?? payload.chosenFrog ?? payload.heroId ?? null,
    }
  }

  /** Дедупликация по ID защищает список и от повторных событий, и от дублей в snapshot. */
  function normalizeMembers(payloads: LobbyMemberPayload[]): LobbyMember[] {
    const byId = new Map<number, LobbyMember>()
    for (const payload of payloads) {
      const member = normalizeMember(payload)
      if (member) byId.set(member.userId, member)
    }
    return [...byId.values()]
  }

  function applyState(s: LobbyState) {
    const normalized = normalizeMembers(s.members)
    if (import.meta.env.DEV && !normalized.some((m) => m.userId === s.selfId)) {
      console.warn('[lobby] selfId не найден среди members после нормализации', { ...s, members: normalized })
    }
    lobbyId.value = s.lobbyId
    selfId.value = s.selfId
    maxPlayers.value = s.maxPlayers
    members.value = normalized
  }

  function reset() {
    phase.value = 'idle'
    lobbyId.value = ''
    lobbyName.value = ''
    selfId.value = null
    members.value = []
    match.clear()
  }

  function setReadyLocal(id: number | null, ready: boolean, selectedHeroId?: string | null) {
    if (id === null) return
    // Обновляем все совпадения на случай, если старое состояние уже содержало дубли.
    members.value = members.value.map((member) =>
      member.userId === id ? { ...member, isReady: ready, heroId: selectedHeroId ?? member.heroId } : member,
    )
  }

  const handlers: Partial<HubEventHandlers> = {
    PlayerJoinedTheLobby: (payload) => {
      const player = normalizeMember(payload)
      if (!player) return

      const index = members.value.findIndex((m) => m.userId === player.userId)
      if (index === -1) {
        members.value.push(player)
      } else {
        // Обновляем данные существующего участника, не добавляя дубликат.
        members.value[index] = { ...members.value[index], ...player, heroId: player.heroId ?? members.value[index].heroId }
      }
    },
    PlayerLeftTheLobby: (payload) => {
      const id = memberId(payload)
      if (id === null) return
      members.value = members.value.filter((m) => m.userId !== id)
    },
    PlayerReadyChanged: (id, ready, selectedHeroId) => setReadyLocal(id, ready, selectedHeroId),
    MatchStarted: (init) => {
      // Тип тела каждой лягушки приходит непосредственно в FrogInfo.name.
      match.start(init)
      // При восстановлении вкладки LobbyState ещё не запрашивался, но ID игрока
      // уже известен из cookie-сессии и нужен для управления своей лягушкой/HUD.
      if (auth.user) selfId.value = auth.user.userId
      phase.value = 'match'
    },
    UpdateWorldState: (s) => match.update(s),
    LevelUp: (positions) => match.enqueueLevelUp(positions),
    MatchEnded: (statistic: MatchStatistic) => {
      match.setResult(statistic)
      phase.value = 'result'
    },
    YouWereDisconnected: () => {
      autoConnectBlocked.value = true
      notice.value = 'Это соединение закрыто: аккаунт уже используется в другой вкладке или окне.'
      // GameHubClient закроет именно тот экземпляр соединения, который получил событие.
      reset()
    },
  }

  function handleUnexpectedClose() {
    if (phase.value !== 'idle') notice.value = 'Соединение с сервером потеряно.'
    reset()
  }

  async function ensureConnected() {
    await gameHub.connect(handlers, handleUnexpectedClose)
  }

  /** Подключение при открытии тайтл-скрина для уже аутентифицированного пользователя. */
  async function connectFromTitle(force = false) {
    if (force) autoConnectBlocked.value = false
    if (autoConnectBlocked.value) return
    await ensureConnected()
  }

  // Hub event callbacks can change the phase during an awaited network call.
  // A function read prevents TypeScript from retaining the pre-await narrowing.
  function hasActiveMatch() {
    return phase.value === 'match' || phase.value === 'result'
  }

  /** Бросает HubError. При ошибке соединение закрывается, состояние сбрасывается. */
  async function join(id: string, name = '') {
    if (phase.value === 'connecting') return
    phase.value = 'connecting'
    // Явное действие пользователя разрешает новую попытку даже после мягкого отключения.
    autoConnectBlocked.value = false
    try {
      await ensureConnected()
      // OnConnectedAsync мог уже восстановить активный матч. В таком случае
      // не пытаемся вступить в новое лобби поверх восстановленного состояния.
      if (hasActiveMatch()) return

      const state = await gameHub.joinLobby(id)
      // За время сетевого вызова могло прийти MatchStarted.
      if (hasActiveMatch()) return
      applyState(state)
      lobbyName.value = name
      phase.value = 'lobby'
    } catch (e) {
      await gameHub.disconnect()
      reset()
      throw e
    }
  }

  async function leave() {
    // На тайтл-скрине тоже может быть активно hub-соединение, хотя phase === idle.
    if (phase.value !== 'idle' && gameHub.isConnected) {
      try {
        await gameHub.leaveLobby()
      } catch {
        /* всё равно уходим */
      }
    }
    await gameHub.disconnect()
    reset()
  }

  async function disconnectHub() {
    await gameHub.disconnect()
    reset()
  }

  /** Ready ↔ Not ready. Выбор героя и режима на клиенте блокируется, пока игрок готов. */
  async function toggleReady() {
    if (isReady.value) {
      await gameHub.setReady(false, null, null)
      setReadyLocal(selfId.value, false)
    } else {
      if (!heroId.value) return
      await gameHub.setReady(true, heroId.value, mode.value)
      setReadyLocal(selfId.value, true, heroId.value)
    }
  }

  /** Просит сервер создать матч. Переход на экран матча произойдёт по событию MatchStarted. */
  const startMatch = () => gameHub.startMatch()

  const sendInput = (input: Input) => gameHub.sendCommand(input)

  return {
    phase, lobbyId, lobbyName, selfId, maxPlayers, members, heroId, mode, notice, isReady, allReady,
    join, leave, connectFromTitle, disconnectHub, toggleReady, startMatch, sendInput,
  }
})
