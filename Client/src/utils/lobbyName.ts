// 3–20 символов: буквы, цифры, '-' и '_'. Регистр сервер не учитывает.
const RE = /^[A-Za-z0-9_-]{3,20}$/

export const normalizeLobbyName = (s: string) => s.trim()
export const isValidLobbyName = (s: string) => RE.test(normalizeLobbyName(s))
