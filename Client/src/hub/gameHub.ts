import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'
import {
  HUB_ERROR_CODES,
  type HubErrorCode,
  type Input,
  type LevelListPosition,
  type LobbyMemberPayload,
  type LobbyState,
  type MatchInit,
  type MatchStatistic,
  type MatchState,
} from '@/types/contract'

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/+$/, '')
const HUB_URL = `${API_BASE_URL}/hubs/game`

/** События сервер → клиент (IGameHub). Имена совпадают с C#, регистр SignalR не учитывает. */
export interface HubEventHandlers {
  PlayerJoinedTheLobby(player: LobbyMemberPayload): void
  PlayerLeftTheLobby(player: LobbyMemberPayload): void
  PlayerReadyChanged(playerId: number, isReady: boolean, selectedHeroId?: string | null): void
  MatchStarted(init: MatchInit): void
  UpdateWorldState(state: MatchState): void
  LevelUp(positions: LevelListPosition[]): void
  MatchEnded(statistic: MatchStatistic): void
  ReceiveChatMessage(message: string): void
  /** Сервер просит этот экземпляр клиента мягко закрыть старое соединение. */
  YouWereDisconnected(): void | Promise<void>
}

export type HubErrorKind = HubErrorCode | 'UNKNOWN' | 'NOT_CONNECTED' | 'CONNECTION_FAILED'

export class HubError extends Error {
  constructor(public readonly code: HubErrorKind, message: string = code) {
    super(message)
  }
}

/** HubException приходит текстом вида "... HubException: LOBBY_FULL": ищем известный код. */
function toHubError(e: unknown): HubError {
  const msg = e instanceof Error ? e.message : String(e)
  const code = HUB_ERROR_CODES.find((c) => msg.includes(c)) ?? 'UNKNOWN'
  return new HubError(code, msg)
}

class GameHubClient {
  private conn: HubConnection | null = null
  private startPromise: Promise<void> | null = null

  get isConnected() {
    return this.conn?.state === HubConnectionState.Connected
  }

  /**
   * Открывает соединение, если его ещё нет. Если подключение уже существует,
   * повторный вызов ничего не пересоздаёт — тайтл, лобби и матч используют один hub.
   */
  async connect(handlers: Partial<HubEventHandlers>, onClose: (err?: Error) => void) {
    if (this.conn?.state === HubConnectionState.Connected) return
    if (this.startPromise) {
      await this.startPromise
      return
    }

    // Убираем только действительно завершённый экземпляр. Существующее
    // активное соединение здесь никогда не закрывается ради повторного connect().
    if (this.conn && this.conn.state !== HubConnectionState.Disconnected) return

    const builder = new HubConnectionBuilder()
      .withUrl(HUB_URL, { withCredentials: true })
      .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
    // The supplied working client used JSON. MessagePack is opt-in so a missing
    // local .env cannot silently change the server handshake protocol.
    if (import.meta.env.VITE_HUB_PROTOCOL === 'msgpack') {
      builder.withHubProtocol(new MessagePackHubProtocol())
    }
    const conn = builder.build()
    let started = false

    for (const [name, handler] of Object.entries(handlers)) {
      // Обрабатываем мягкое закрытие отдельно, чтобы старое соединение не могло
      // случайно отключить более новое при запоздавшем событии.
      if (name.toLowerCase() === 'youweredisconnected') continue
      conn.on(name, handler as (...args: unknown[]) => void)
    }
    if (handlers.YouWereDisconnected) {
      conn.on('YouWereDisconnected', async () => {
        if (this.conn !== conn) return
        await handlers.YouWereDisconnected?.()
        if (this.conn !== conn) return
        this.conn = null
        this.startPromise = null
        if (conn.state !== HubConnectionState.Disconnected) await conn.stop().catch(() => {})
      })
    }
    conn.onclose((err) => {
      if (this.conn !== conn) return
      this.conn = null
      this.startPromise = null
      // Ошибка начального start() будет обработана вызывающей стороной; onClose
      // предназначен для неожиданного закрытия уже поднятого соединения.
      if (started) onClose(err)
    })

    // Сохраняем ссылку ДО start(): сервер может прислать MatchStarted и первые
    // UpdateWorldState ещё до того, как start() завершится.
    this.conn = conn
    const starting = (async () => {
      try {
        await conn.start()
        started = true
      } catch (e) {
        if (this.conn === conn) this.conn = null
        throw new HubError('CONNECTION_FAILED', e instanceof Error ? e.message : String(e))
      }
    })()
    this.startPromise = starting

    try {
      await starting
    } finally {
      if (this.startPromise === starting) this.startPromise = null
    }
  }

  async disconnect() {
    const conn = this.conn
    this.conn = null // onclose не должен считать ожидаемый stop() сетевым обрывом
    this.startPromise = null
    if (conn && conn.state !== HubConnectionState.Disconnected) {
      await conn.stop().catch(() => {})
    }
  }

  private async invoke<T = void>(method: string, ...args: unknown[]): Promise<T> {
    if (!this.conn || !this.isConnected) throw new HubError('NOT_CONNECTED')
    try {
      return await this.conn.invoke<T>(method, ...args)
    } catch (e) {
      throw toHubError(e)
    }
  }

  private send(method: string, ...args: unknown[]) {
    if (!this.conn || !this.isConnected) return
    this.conn.send(method, ...args).catch(() => {})
  }

  // --- клиент → сервер ---
  joinLobby = (lobbyId: string) => this.invoke<LobbyState>('JoinLobby', lobbyId)
  leaveLobby = () => this.invoke('LeaveLobby')
  startMatch = () => this.invoke('StartMatch')
  setReady = (ready: boolean, heroId: string | null, mode: string | null) =>
    this.invoke('SetReady', ready, heroId, mode)
  /** Команды идут часто, поэтому send: без ожидания ответа сервера. */
  sendCommand = (input: Input) => this.send('HandleCommandAsync', input)
}

export const gameHub = new GameHubClient()
