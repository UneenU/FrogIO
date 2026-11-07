import { request } from './http'
import {
  MAX_PLAYERS,
  type LobbyCreateRequest,
  type LobbyCreateResponse,
  type LobbySummary,
} from '@/types/contract'

export function createLobby(name: string, maxPlayers = MAX_PLAYERS) {
  const body: LobbyCreateRequest = { name, maxPlayers }
  return request<LobbyCreateResponse>('/api/lobbies', { method: 'POST', body: JSON.stringify(body) })
}

/** GET /api/lobbies?lobbyName=... -> [{ lobbyId, lobbyName }] */
export const findLobbies = (lobbyName: string) =>
  request<LobbySummary[]>(`/api/lobbies?lobbyName=${encodeURIComponent(lobbyName)}`)
