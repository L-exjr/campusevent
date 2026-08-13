const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()
const API_BASE_URL = (configuredApiBaseUrl || (import.meta.env.DEV ? 'http://localhost:5080/api' : '')).replace(/\/$/, '')
function apiBaseUrl() { if (API_BASE_URL) return API_BASE_URL; throw new Error('The API is not configured for this deployment.') }
interface ApiErrorBody { error?: string }
let csrfToken: string | null = null
async function getCsrfToken() {
  if (csrfToken) return csrfToken
  const response = await fetch(`${apiBaseUrl()}/auth/csrf`, { credentials: 'include' })
  if (!response.ok) throw new Error('The security token could not be initialized.')
  csrfToken = (await response.json() as { token: string }).token
  return csrfToken
}
function isStateChanging(method?: string) { return ['POST', 'PUT', 'PATCH', 'DELETE'].includes((method ?? 'GET').toUpperCase()) }
export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  if (options.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  if (isStateChanging(options.method)) headers.set('X-CSRF-TOKEN', await getCsrfToken())
  let response: Response
  try { response = await fetch(`${apiBaseUrl()}${path}`, { ...options, headers, credentials: 'include' }) }
  catch (caught) { if (caught instanceof Error && caught.name === 'AbortError') throw caught; throw new Error('The API is unavailable. Check that the backend is running and try again.', { cause: caught }) }
  const body = response.status === 204 ? null : await response.json().catch(() => null) as ApiErrorBody | T | null
  if (!response.ok) { if (response.status === 401) window.dispatchEvent(new Event('campus-events:unauthorized')); const message = body && typeof body === 'object' && 'error' in body ? body.error : null; throw new Error(message || `The request failed with status ${response.status}.`) }
  return body as T
}
export async function apiDownload(path: string): Promise<Blob> {
  const response = await fetch(`${apiBaseUrl()}${path}`, { credentials: 'include' })
  if (!response.ok) { if (response.status === 401) window.dispatchEvent(new Event('campus-events:unauthorized')); const body = await response.json().catch(() => null) as ApiErrorBody | null; throw new Error(body?.error || `The request failed with status ${response.status}.`) }
  return response.blob()
}
export interface PaginatedResponse<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number }
