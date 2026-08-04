const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()
const API_BASE_URL = (
  configuredApiBaseUrl || (import.meta.env.DEV ? 'http://localhost:5080/api' : '')
).replace(/\/$/, '')

if (!API_BASE_URL) {
  throw new Error('VITE_API_BASE_URL must be configured for production builds.')
}

const SESSION_KEY = 'campus_events_api_session'

export interface StoredApiSession {
  token: string
  expiresAt: string
  user: unknown
}

interface ApiErrorBody {
  error?: string
}

export function readStoredSession(): StoredApiSession | null {
  const value = window.sessionStorage.getItem(SESSION_KEY)
  if (!value) return null
  try {
    return JSON.parse(value) as StoredApiSession
  } catch {
    window.sessionStorage.removeItem(SESSION_KEY)
    return null
  }
}

export function writeStoredSession(session: StoredApiSession) {
  window.sessionStorage.setItem(SESSION_KEY, JSON.stringify(session))
}

export function clearStoredSession() {
  window.sessionStorage.removeItem(SESSION_KEY)
}

export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  if (options.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  const token = readStoredSession()?.token
  if (token) headers.set('Authorization', `Bearer ${token}`)

  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers })
  } catch {
    throw new Error('The API is unavailable. Check that the backend is running and try again.')
  }

  const body = response.status === 204
    ? null
    : await response.json().catch(() => null) as ApiErrorBody | T | null

  if (!response.ok) {
    if (response.status === 401 && token) {
      clearStoredSession()
      window.dispatchEvent(new Event('campus-events:unauthorized'))
    }
    const message = body && typeof body === 'object' && 'error' in body
      ? body.error
      : null
    throw new Error(message || `The request failed with status ${response.status}.`)
  }

  return body as T
}

export async function fetchAllPages<T>(path: string): Promise<T[]> {
  const separator = path.includes('?') ? '&' : '?'
  const first = await apiRequest<PaginatedResponse<T>>(`${path}${separator}page=1&pageSize=100`)
  if (first.totalPages <= 1) return first.items
  const remainingPageCount = first.totalPages - 1
  const remaining = new Array<PaginatedResponse<T>>(remainingPageCount)
  let nextPageIndex = 0
  const worker = async () => {
    while (nextPageIndex < remainingPageCount) {
      const pageIndex = nextPageIndex
      nextPageIndex += 1
      remaining[pageIndex] = await apiRequest<PaginatedResponse<T>>(
        `${path}${separator}page=${pageIndex + 2}&pageSize=100`,
      )
    }
  }
  await Promise.all(
    Array.from({ length: Math.min(4, remainingPageCount) }, () => worker()),
  )
  return [first, ...remaining].flatMap((page) => page.items)
}

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
