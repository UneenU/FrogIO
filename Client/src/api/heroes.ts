import { request } from './http'
import type { Hero } from '@/types/contract'

export const fetchHeroes = () => request<Hero[]>('/api/heroes')
