import { render, type RenderOptions } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../context/AuthContext'
import type { User } from '../types'

interface TestRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  user?: User | null
  initialEntries?: string[]
  authOverrides?: Partial<AuthContextValue>
}

export function authValue(
  user: User | null,
  overrides: Partial<AuthContextValue> = {},
): AuthContextValue {
  return {
    user,
    token: user ? 'test-token' : null,
    loading: false,
    isAuthenticated: Boolean(user),
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  }
}

export function renderWithAuth(
  ui: ReactElement,
  { user = null, initialEntries = ['/'], authOverrides, ...options }: TestRenderOptions = {},
) {
  const auth = authValue(user, authOverrides)
  return {
    auth,
    ...render(
      <AuthContext.Provider value={auth}>
        <MemoryRouter initialEntries={initialEntries}>{ui}</MemoryRouter>
      </AuthContext.Provider>,
      options,
    ),
  }
}
