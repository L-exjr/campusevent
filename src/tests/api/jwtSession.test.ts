import { readJwtSessionClaims } from '../../api/jwtSession'

function tokenWith(payload: object) {
  const encode = (value: object) => btoa(JSON.stringify(value))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '')
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode(payload)}.signature`
}

describe('readJwtSessionClaims', () => {
  it('does not reject a server-issued token because the browser clock is ahead', () => {
    vi.setSystemTime(new Date('2030-01-01T00:00:00Z'))
    const claims = readJwtSessionClaims(tokenWith({
      exp: Date.parse('2026-08-06T14:00:00Z') / 1000,
      role: 'Student',
      userId: 'user-1',
    }))

    expect(claims).toEqual({
      expiresAt: '2026-08-06T14:00:00.000Z',
      role: 'student',
      userId: 'user-1',
    })
  })
})
