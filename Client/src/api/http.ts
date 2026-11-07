const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/+$/, '')
const CSRF_PATH = '/api/auth/csrf'

export class ApiError extends Error {
  constructor(public readonly status: number, public readonly body = '', public readonly url = '') {
    super(`HTTP ${status}`)
    this.name = 'ApiError'
  }
}

function resolveUrl(url: string) {
  if (/^https?:\/\//i.test(url)) return url
  return `${API_BASE_URL}${url.startsWith('/') ? url : `/${url}`}`
}

let csrfToken: string | null = null

async function loadCsrfToken(forceRefresh = false): Promise<string> {
  if (csrfToken && !forceRefresh) return csrfToken

  let response: Response
  try {
    response = await fetch(resolveUrl(CSRF_PATH), {
      method: 'GET',
      credentials: 'include',
      headers: { Accept: 'application/json' },
    })
  } catch {
    throw new ApiError(0, '', CSRF_PATH)
  }

  if (!response.ok) {
    throw new ApiError(response.status, await response.text().catch(() => ''), CSRF_PATH)
  }

  const result = (await response.json()) as { requestToken?: string }
  if (!result.requestToken) throw new Error('Сервер не вернул CSRF-токен.')
  csrfToken = result.requestToken
  return csrfToken
}

function isSafeMethod(method: string) {
  return ['GET', 'HEAD', 'OPTIONS', 'TRACE'].includes(method.toUpperCase())
}

function isCsrfError(error: ApiError) {
  if (error.status !== 400) return false
  const body = error.body.toLocaleLowerCase()
  return body.includes('antiforgery') || body.includes('csrf') || body.includes('invalid antiforgery')
}

export async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const method = (init?.method ?? 'GET').toUpperCase()
  const headers = new Headers(init?.headers)
  headers.set('Accept', 'application/json')
  if (init?.body != null && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const perform = async (refreshCsrf = false): Promise<Response> => {
    if (!isSafeMethod(method) && url !== CSRF_PATH) {
      headers.set('X-CSRF-TOKEN', await loadCsrfToken(refreshCsrf))
    }

    try {
      return await fetch(resolveUrl(url), {
        ...init,
        method,
        headers,
        credentials: 'include',
      })
    } catch {
      throw new ApiError(0, '', url)
    }
  }

  let response = await perform()
  if (!response.ok) {
    const body = await response.text().catch(() => '')
    const error = new ApiError(response.status, body, url)
    // При долгоживущей вкладке CSRF-cookie мог обновиться. Повторяем только
    // явно распознанную CSRF-ошибку, а не произвольный запрос с бизнес-ошибкой.
    if (isCsrfError(error) && !isSafeMethod(method) && url !== CSRF_PATH) {
      csrfToken = null
      response = await perform(true)
      if (!response.ok) throw new ApiError(response.status, await response.text().catch(() => ''), url)
    } else {
      throw error
    }
  }

  return (response.status === 204 ? undefined : await response.json()) as T
}
