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
  const [expiresAt, setExpiresAt] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let active = true
    // Restore a non-expired browser session; the API revalidates account state per request.
    api
      .restoreSession()
      .then((session) => {
        if (!active || !session) return
        setUser(session.user)
        setToken(session.token)
        setExpiresAt(session.expiresAt)
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
      setExpiresAt(null)
    }
    window.addEventListener('campus-events:unauthorized', clearSession)
    return () => window.removeEventListener('campus-events:unauthorized', clearSession)
  }, [])

  useEffect(() => {
    if (!expiresAt) return
    const remainingMs = new Date(expiresAt).getTime() - Date.now()
    const timeout = window.setTimeout(() => {
      void api.logout()
      setUser(null)
      setToken(null)
      setExpiresAt(null)
    }, Math.max(remainingMs, 0))
    return () => window.clearTimeout(timeout)
  }, [expiresAt])

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
        setExpiresAt(session.expiresAt)
        return session
      },
      register: async (name, email, password) => {
        const session = await api.register(name, email, password)
        setUser(session.user)
        setToken(session.token)
        setExpiresAt(session.expiresAt)
        return session
      },
      googleLogin: async (idToken) => {
        const session = await api.googleLogin(idToken)
        setUser(session.user)
        setToken(session.token)
        setExpiresAt(session.expiresAt)
        return session
      },
      logout: async () => {
        await api.logout()
        setUser(null)
        setToken(null)
        setExpiresAt(null)
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
