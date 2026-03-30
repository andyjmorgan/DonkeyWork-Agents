import { useEffect, useRef, useCallback } from 'react'
import { invoke } from '@tauri-apps/api/core'
import { listen } from '@tauri-apps/api/event'
import { useAuthStore } from '@donkeywork/stores'

interface StoredTokens {
  accessToken: string
  refreshToken: string | null
  expiresAt: number
  tokenIssuedAt: number
}

interface AuthTokens {
  accessToken: string
  refreshToken: string | null
  expiresIn: number
}

function parseJwt(token: string) {
  try {
    const base64Url = token.split('.')[1]
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    return JSON.parse(jsonPayload)
  } catch {
    return null
  }
}

export function useDesktopAuth() {
  const { setTokens, setUser, logout, isAuthenticated } = useAuthStore()
  const isLoggingIn = useRef(false)

  // Restore session on mount
  useEffect(() => {
    async function restoreSession() {
      try {
        const stored = await invoke<StoredTokens | null>('get_tokens')
        if (!stored) return

        const now = Date.now()
        if (stored.expiresAt <= now) {
          // Token expired - try refresh
          try {
            const refreshed = await invoke<AuthTokens>('refresh_tokens')
            setTokens(refreshed.accessToken, refreshed.refreshToken, refreshed.expiresIn)
            const payload = parseJwt(refreshed.accessToken)
            if (payload) {
              setUser({
                id: payload.sub,
                email: payload.email,
                name: payload.name,
                username: payload.preferred_username,
              })
            }
          } catch {
            await invoke('clear_tokens')
            logout()
          }
          return
        }

        // Token still valid - restore
        const expiresIn = Math.round((stored.expiresAt - now) / 1000)
        setTokens(stored.accessToken, stored.refreshToken, expiresIn)
        const payload = parseJwt(stored.accessToken)
        if (payload) {
          setUser({
            id: payload.sub,
            email: payload.email,
            name: payload.name,
            username: payload.preferred_username,
          })
        }
      } catch (e) {
        console.error('Failed to restore session:', e)
      }
    }

    restoreSession()
  }, [setTokens, setUser, logout])

  // Listen for background token refresh events from Rust
  useEffect(() => {
    const unlistenRefreshed = listen<AuthTokens>('tokens-refreshed', (event) => {
      const tokens = event.payload
      setTokens(tokens.accessToken, tokens.refreshToken, tokens.expiresIn)
    })

    const unlistenExpired = listen('auth-expired', () => {
      logout()
    })

    return () => {
      unlistenRefreshed.then((fn) => fn())
      unlistenExpired.then((fn) => fn())
    }
  }, [setTokens, logout])

  // Periodic token refresh from React side
  const checkAndRefreshToken = useCallback(async () => {
    const state = useAuthStore.getState()
    if (!state.isAuthenticated) return

    if (state.isTokenExpired() || state.shouldRefreshToken()) {
      try {
        const tokens = await invoke<AuthTokens>('refresh_tokens')
        setTokens(tokens.accessToken, tokens.refreshToken, tokens.expiresIn)
      } catch {
        logout()
      }
    }
  }, [setTokens, logout])

  useEffect(() => {
    if (!isAuthenticated) return

    const interval = window.setInterval(checkAndRefreshToken, 60000)
    return () => clearInterval(interval)
  }, [isAuthenticated, checkAndRefreshToken])

  const startLogin = useCallback(async (provider: string) => {
    if (isLoggingIn.current) return
    isLoggingIn.current = true
    try {
      // start_auth opens browser, waits for callback, exchanges code, returns tokens
      const tokens = await invoke<AuthTokens>('start_auth', { idpHint: provider })
      setTokens(tokens.accessToken, tokens.refreshToken, tokens.expiresIn)
      const payload = parseJwt(tokens.accessToken)
      if (payload) {
        setUser({
          id: payload.sub,
          email: payload.email,
          name: payload.name,
          username: payload.preferred_username,
        })
      }
    } finally {
      isLoggingIn.current = false
    }
  }, [setTokens, setUser])

  const handleLogout = useCallback(async () => {
    await invoke('clear_tokens')
    logout()
  }, [logout])

  return { startLogin, handleLogout, isAuthenticated }
}
