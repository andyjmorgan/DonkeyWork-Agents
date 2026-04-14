import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import { createPlatformStorage, getPlatformConfig } from '@donkeywork/platform'

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
  hasHydrated: boolean

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
      hasHydrated: false,

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

        if (state.isRefreshing && state.refreshPromise) {
          console.debug('[Auth] Token refresh already in progress, reusing existing promise')
          return state.refreshPromise
        }

        const { refreshToken, expiresAt } = state
        if (!refreshToken) {
          console.warn('[Auth] Cannot refresh: no refresh token available')
          return false
        }

        const timeRemaining = expiresAt ? Math.round((expiresAt - Date.now()) / 1000) : 'unknown'
        console.debug(`[Auth] Starting token refresh. Token expires in ${timeRemaining}s. RefreshToken preview: ${refreshToken.substring(0, 20)}...`)

        const refreshPromise = (async () => {
          set({ isRefreshing: true })

          const maxAttempts = 3
          const baseDelay = 1000

          for (let attempt = 1; attempt <= maxAttempts; attempt++) {
            try {
              const currentRefreshToken = get().refreshToken
              if (!currentRefreshToken) {
                console.warn('[Auth] Refresh token disappeared during retry')
                break
              }

              const { apiBaseUrl } = getPlatformConfig()
              const response = await fetch(`${apiBaseUrl}/api/v1/auth/refresh`, {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                },
                body: JSON.stringify({ refreshToken: currentRefreshToken }),
              })

              if (response.ok) {
                const data: RefreshTokenResponse = await response.json()
                const now = Date.now()
                const newExpiresAt = now + data.expiresIn * 1000
                set({
                  accessToken: data.accessToken,
                  refreshToken: data.refreshToken ?? currentRefreshToken,
                  expiresAt: newExpiresAt,
                  tokenIssuedAt: now,
                  isRefreshing: false,
                  refreshPromise: null,
                })
                console.debug(`[Auth] Token refresh successful. New token expires in ${data.expiresIn}s`)
                return true
              }

              if (response.status === 400 || response.status === 401) {
                try {
                  const errorBody = await response.json()
                  console.error(`[Auth] Token refresh rejected (${response.status}):`, {
                    error: errorBody.error,
                    errorDescription: errorBody.error_description,
                  })
                } catch {
                  console.error(`[Auth] Token refresh rejected (${response.status})`)
                }
                break
              }

              console.warn(`[Auth] Token refresh attempt ${attempt}/${maxAttempts} failed with status ${response.status}`)
            } catch (error) {
              console.warn(`[Auth] Token refresh attempt ${attempt}/${maxAttempts} threw:`, error)
            }

            if (attempt < maxAttempts) {
              const delay = baseDelay * Math.pow(2, attempt - 1)
              console.debug(`[Auth] Retrying token refresh in ${delay}ms`)
              await new Promise(resolve => setTimeout(resolve, delay))
            }
          }

          console.error('[Auth] Token refresh failed after all attempts')
          set({ isRefreshing: false, refreshPromise: null })
          return false
        })()

        set({ refreshPromise })
        return refreshPromise
      },
    }),
    {
      name: 'donkeywork-auth',
      storage: createJSONStorage(() => createPlatformStorage()),
      onRehydrateStorage: () => () => {
        useAuthStore.setState({ hasHydrated: true })
      },
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
