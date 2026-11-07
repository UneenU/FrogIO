import { ApiError } from '@/api/http'
import { HubError } from '@/hub/gameHub'

const HUB_TEXT: Record<string, string> = {
  LOBBY_NOT_FOUND: 'Лобби с таким именем не найдено.',
  LOBBY_FULL: 'В лобби нет свободных мест.',
  MATCH_IN_PROGRESS: 'В этом лобби матч уже идёт.',
  INVALID_HERO: 'Сервер не знает такого героя. Выберите другого.',
  INVALID_MODE: 'Сервер не знает такого режима.',
  NOT_ALL_READY: 'Не все игроки готовы.',
  NOT_IN_LOBBY: 'Вы больше не в лобби. Зайдите заново.',
  CONNECTION_FAILED: 'Не удалось подключиться к серверу. Проверьте, что он запущен.',
  NOT_CONNECTED: 'Нет соединения с сервером.',
}

function responseMessage(body: string): string | null {
  if (!body) return null
  try {
    const value: unknown = JSON.parse(body)
    if (!value || typeof value !== 'object') return null
    const record = value as Record<string, unknown>
    for (const key of ['error', 'message', 'detail', 'title']) {
      const message = record[key]
      if (typeof message === 'string' && message.trim()) {
        const normalized = message.toLocaleLowerCase()
        if ((normalized.includes('email') && (normalized.includes('nickname') || normalized.includes('already'))) || normalized.includes('already exists')) {
          return 'Пользователь с таким email или никнеймом уже существует.'
        }
        if (normalized.includes('invalid credential') || normalized.includes('invalid email or password') || normalized.includes('incorrect password')) {
          return 'Неверный email или пароль.'
        }
        if (normalized.includes('invalid antiforgery') || normalized.includes('antiforgery token')) {
          return 'Не удалось проверить защитный токен. Повторите запрос.'
        }
        return message
      }
    }

    const errors = record.errors
    if (errors && typeof errors === 'object') {
      const messages = Object.values(errors as Record<string, unknown>)
        .flatMap((item) => Array.isArray(item) ? item : [item])
        .filter((item): item is string => typeof item === 'string')
      if (messages.length) return messages.join(' ')
    }
  } catch {
    // Сервер мог вернуть обычный текст вместо JSON.
  }
  return null
}

export function errorText(e: unknown): string {
  if (e instanceof HubError) return HUB_TEXT[e.code] ?? 'Ошибка сервера. Попробуйте ещё раз.'
  if (e instanceof ApiError) {
    if (e.status === 0) return 'Не удалось связаться с сервером. Проверьте, что он запущен.'
    if (e.status === 401) return 'Неверный email или пароль.'
    const serverMessage = responseMessage(e.body)
    if (serverMessage) return serverMessage
    if (e.status === 400) return 'Проверьте введённые данные и попробуйте ещё раз.'
    if (e.status === 401) return 'Неверный email или пароль.'
    if (e.status === 403) return 'Недостаточно прав для выполнения действия.'
    if (e.status === 409) return 'Пользователь с таким email или никнеймом уже существует.'
    return `Сервер вернул ошибку ${e.status}.`
  }
  if (e instanceof Error && e.message) return e.message
  return 'Что-то пошло не так. Попробуйте ещё раз.'
}
