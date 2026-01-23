import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export interface User {
  id: string
  email?: string
  name?: string
  username?: string
}

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  expiresAt: number | null
  user: User | null
  isAuthenticated: boolean

  setTokens: (accessToken: string, refreshToken: string | null, expiresIn: number) => void
  setUser: (user: User) => void
  logout: () => void
  isTokenExpired: () => boolean
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      user: null,
      isAuthenticated: false,

      setTokens: (accessToken, refreshToken, expiresIn) => {
        const expiresAt = Date.now() + expiresIn * 1000
        set({
          accessToken,
          refreshToken,
          expiresAt,
          isAuthenticated: true,
        })
      },

      setUser: (user) => {
        set({ user })
      },

      logout: () => {
        set({
          accessToken: null,
          refreshToken: null,
          expiresAt: null,
          user: null,
          isAuthenticated: false,
        })
      },

      isTokenExpired: () => {
        const { expiresAt } = get()
        if (!expiresAt) return true
        // Consider expired if less than 30 seconds remaining
        return Date.now() > expiresAt - 30000
      },
    }),
    {
      name: 'donkeywork-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        expiresAt: state.expiresAt,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)
