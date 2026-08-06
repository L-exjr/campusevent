import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { api } from '../api'
import type { User } from '../types'
import { AuthContext, type AuthContextValue } from './AuthContext'

interface AuthProviderProps {
  children: ReactNode
}

export default function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let active = true
    // Restore the browser session; the API remains authoritative for token lifetime and account state.
    api
      .restoreSession()
      .then((session) => {
        if (!active || !session) return
        setUser(session.user)
        setToken(session.token)
      })
      .catch(() => api.logout())
      .finally(() => active && setLoading(false))
    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    const clearSession = () => {
      setUser(null)
      setToken(null)
    }
    window.addEventListener('campus-events:unauthorized', clearSession)
    return () => window.removeEventListener('campus-events:unauthorized', clearSession)
  }, [])

  // JWT lifetime is enforced by the API. Using the browser clock to end a
  // session can immediately sign in-and-out users whose device clock is ahead.
  // Any server 401 is handled by the unauthorized event above.

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      loading,
      isAuthenticated: Boolean(user && token),
      login: async (email, password) => {
        const session = await api.login(email, password)
        setUser(session.user)
        setToken(session.token)
        return session
      },
      register: async (name, email, password) => {
        const session = await api.register(name, email, password)
        setUser(session.user)
        setToken(session.token)
        return session
      },
      googleLogin: async (idToken) => {
        const session = await api.googleLogin(idToken)
        setUser(session.user)
        setToken(session.token)
        return session
      },
      logout: async () => {
        await api.logout()
        setUser(null)
        setToken(null)
      },
      updateProfileImage: async (imageUrl) => {
        if (!user) throw new Error('Authentication is required.')
        const updated = await api.updateProfile(user.id, imageUrl)
        setUser(updated)
        return updated
      },
    }),
    [loading, token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
