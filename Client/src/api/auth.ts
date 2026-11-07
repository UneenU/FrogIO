import { request } from './http'

export interface AuthUser {
  userId: number
  nickname: string
  email: string
  rating: number
}

export interface RegisterPayload {
  nickname: string
  email: string
  password: string
}

export interface LoginPayload {
  email: string
  password: string
}

export const getCurrentUser = () => request<AuthUser>('/api/auth/me')
export const register = (payload: RegisterPayload) =>
  request<AuthUser>('/api/auth/register', { method: 'POST', body: JSON.stringify(payload) })
export const login = (payload: LoginPayload) =>
  request<AuthUser>('/api/auth/login', { method: 'POST', body: JSON.stringify(payload) })
export const logout = () => request<void>('/api/auth/logout', { method: 'POST' })
