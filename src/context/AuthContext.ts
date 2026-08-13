import { createContext } from 'react'
import type { AuthSession, User } from '../types'

export interface AuthContextValue {
  user: User | null
  loading: boolean
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<AuthSession>
  register: (name: string, email: string, password: string) => Promise<AuthSession>
  googleLogin: (idToken: string) => Promise<AuthSession>
  logout: () => Promise<void>
  updateProfileImage: (imageUrl: string | null) => Promise<User>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
