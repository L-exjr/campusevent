import type { Role } from '../types'

interface JwtPayload {
  exp?: unknown
  role?: unknown
  sub?: unknown
  userId?: unknown
}

export interface JwtSessionClaims {
  expiresAt: string
  role: Role
  userId: string
}

function decodePayload(token: string): JwtPayload {
  const segments = token.split('.')
  if (segments.length !== 3 || !segments[1]) throw new Error('The session token is invalid.')

  const base64 = segments[1].replace(/-/g, '+').replace(/_/g, '/')
  const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')

  try {
    const bytes = Uint8Array.from(atob(padded), (character) => character.charCodeAt(0))
    const payload = JSON.parse(new TextDecoder().decode(bytes)) as unknown
    if (!payload || typeof payload !== 'object' || Array.isArray(payload)) {
      throw new Error()
    }
    return payload as JwtPayload
  } catch {
    throw new Error('The session token is invalid.')
  }
}

function parseRole(value: unknown): Role | null {
  if (typeof value !== 'string') return null
  const role = value.toLowerCase()
  return role === 'student' || role === 'organizer' || role === 'admin' ? role : null
}

export function readJwtSessionClaims(token: string): JwtSessionClaims {
  const payload = decodePayload(token)
  const userId = typeof payload.userId === 'string' ? payload.userId : payload.sub
  const role = parseRole(payload.role)
  const expiresAtMs = typeof payload.exp === 'number' ? payload.exp * 1000 : Number.NaN

  if (typeof userId !== 'string' || !userId || !role || !Number.isFinite(expiresAtMs)) {
    throw new Error('The session token is missing required claims.')
  }
  if (expiresAtMs <= Date.now()) throw new Error('Your session has expired. Please sign in again.')

  return {
    expiresAt: new Date(expiresAtMs).toISOString(),
    role,
    userId,
  }
}
