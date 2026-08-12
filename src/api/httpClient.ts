const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()
const API_BASE_URL = (
  configuredApiBaseUrl || (import.meta.env.DEV ? 'http://localhost:5080/api' : '')
).replace(/\/$/, '')

function apiBaseUrl() {
  if (API_BASE_URL) return API_BASE_URL

  // Keep configuration failures scoped to API-backed features. Throwing while
  // this module loads prevents React from mounting and turns a recoverable
  // deployment mistake into a completely blank page.
  throw new Error('The API is not configured for this deployment.')
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
    response = await fetch(`${apiBaseUrl()}${path}`, { ...options, headers })
  } catch (caught) {
    if (caught instanceof Error && caught.name === 'AbortError') throw caught
    throw new Error(
      'The API is unavailable. Check that the backend is running and try again.',
      { cause: caught },
    )
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

export async function apiDownload(path: string): Promise<Blob> {
  const headers = new Headers()
  const token = readStoredSession()?.token
  if (token) headers.set('Authorization', `Bearer ${token}`)
  const response = await fetch(`${apiBaseUrl()}${path}`, { headers })
  if (!response.ok) {
    const body = await response.json().catch(() => null) as ApiErrorBody | null
    if (response.status === 401 && token) {
      clearStoredSession()
      window.dispatchEvent(new Event('campus-events:unauthorized'))
    }
    throw new Error(body?.error || `The request failed with status ${response.status}.`)
  }
  return response.blob()
}

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
