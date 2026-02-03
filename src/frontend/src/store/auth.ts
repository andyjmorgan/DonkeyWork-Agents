import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export interface User {
  id: string
  email?: string
  name?: string
  username?: string
}

interface RefreshTokenResponse {
  accessToken: string
  refreshToken: string | null
  expiresIn: number
  tokenType: string
}

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  expiresAt: number | null
  tokenIssuedAt: number | null
  user: User | null
  isAuthenticated: boolean
  isRefreshing: boolean
  refreshPromise: Promise<boolean> | null

  setTokens: (accessToken: string, refreshToken: string | null, expiresIn: number) => void
  setUser: (user: User) => void
  logout: () => void
  isTokenExpired: () => boolean
  shouldRefreshToken: () => boolean
  refreshTokens: () => Promise<boolean>
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      tokenIssuedAt: null,
      user: null,
      isAuthenticated: false,
      isRefreshing: false,
      refreshPromise: null,

      setTokens: (accessToken, refreshToken, expiresIn) => {
        const now = Date.now()
        const expiresAt = now + expiresIn * 1000
        set({
          accessToken,
          refreshToken,
          expiresAt,
          tokenIssuedAt: now,
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
          tokenIssuedAt: null,
          user: null,
          isAuthenticated: false,
          isRefreshing: false,
          refreshPromise: null,
        })
      },

      isTokenExpired: () => {
        const { expiresAt } = get()
        if (!expiresAt) return true
        // Consider expired if less than 30 seconds remaining
        return Date.now() > expiresAt - 30000
      },

      shouldRefreshToken: () => {
        const { expiresAt, tokenIssuedAt, refreshToken } = get()
        if (!expiresAt || !refreshToken) return false

        const now = Date.now()
        const timeRemaining = expiresAt - now

        // If we have the issue time, calculate 80% of token lifetime
        if (tokenIssuedAt) {
          const tokenLifetime = expiresAt - tokenIssuedAt
          const refreshThreshold = tokenLifetime * 0.2 // Refresh when 20% of lifetime remains (80% elapsed)
          return timeRemaining <= refreshThreshold
        }

        // Fallback: refresh if less than 2 minutes remaining
        const minRefreshBuffer = 120000 // 2 minutes minimum buffer
        return timeRemaining <= minRefreshBuffer
      },

      refreshTokens: async () => {
        const state = get()

        // If already refreshing, return the existing promise
        if (state.isRefreshing && state.refreshPromise) {
          return state.refreshPromise
        }

        const { refreshToken } = state
        if (!refreshToken) {
          return false
        }

        // Create the refresh promise
        const refreshPromise = (async () => {
          set({ isRefreshing: true })

          try {
            const response = await fetch('/api/v1/auth/refresh', {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify({ refreshToken }),
            })

            if (!response.ok) {
              // Refresh failed - logout
              get().logout()
              return false
            }

            const data: RefreshTokenResponse = await response.json()

            // Update tokens
            const now = Date.now()
            const expiresAt = now + data.expiresIn * 1000
            set({
              accessToken: data.accessToken,
              refreshToken: data.refreshToken ?? refreshToken,
              expiresAt,
              tokenIssuedAt: now,
              isRefreshing: false,
              refreshPromise: null,
            })

            return true
          } catch {
            set({ isRefreshing: false, refreshPromise: null })
            return false
          }
        })()

        set({ refreshPromise })
        return refreshPromise
      },
    }),
    {
      name: 'donkeywork-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        expiresAt: state.expiresAt,
        tokenIssuedAt: state.tokenIssuedAt,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)
