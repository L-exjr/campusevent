import { createContext } from 'react'
import type { AuthSession, User } from '../types'

export interface AuthContextValue {
  user: User | null
  token: string | null
  loading: boolean
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<AuthSession>
  register: (name: string, email: string, password: string) => Promise<AuthSession>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
