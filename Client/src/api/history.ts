import { request } from './http'
import type { UserMatchHistoryDto } from '@/types/contract'


export const fetchMatchHistory = (userId: number, page = 1, pageSize = 10) =>
  request<UserMatchHistoryDto[]>(
    `/api/user/${encodeURIComponent(userId)}/matches-history?page=${page}&page-size=${pageSize}`,
  )
