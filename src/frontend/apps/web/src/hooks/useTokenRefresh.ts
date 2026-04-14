import { useEffect, useRef, useCallback } from 'react'
import { useAuthStore } from '@donkeywork/stores'

const TOKEN_REFRESH_INTERVAL = 60000 // Check every minute

/**
 * Hook that periodically checks if the access token needs refreshing
 * and refreshes it proactively when the user is idle.
 *
 * This helps prevent token expiration during long idle periods
 * and provides a smoother user experience by avoiding unexpected logouts.
 */
export function useTokenRefresh() {
  const intervalRef = useRef<number | null>(null)
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)

  const consecutiveFailures = useRef(0)
  const maxConsecutiveFailures = 3

  const checkAndRefreshToken = useCallback(async () => {
    const state = useAuthStore.getState()
    if (!state.isAuthenticated) return

    if (state.isTokenExpired() || state.shouldRefreshToken()) {
      const refreshed = await state.refreshTokens()
      if (refreshed) {
        consecutiveFailures.current = 0
        return
      }

      consecutiveFailures.current++
      console.warn(`[TokenRefresh] Refresh failed (${consecutiveFailures.current}/${maxConsecutiveFailures})`)

      if (state.isTokenExpired() && consecutiveFailures.current >= maxConsecutiveFailures) {
        console.error('[TokenRefresh] Token expired and refresh failed repeatedly, logging out')
        consecutiveFailures.current = 0
        state.logout()
        window.location.href = '/api/v1/auth/logout'
      }
    }
  }, [])

  useEffect(() => {
    if (!isAuthenticated) {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
      return
    }

    // Initial check
    checkAndRefreshToken()

    intervalRef.current = window.setInterval(checkAndRefreshToken, TOKEN_REFRESH_INTERVAL)

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
    }
  }, [isAuthenticated, checkAndRefreshToken])

  // Also refresh when the window regains focus (user comes back from another tab)
  useEffect(() => {
    if (!isAuthenticated) return

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        checkAndRefreshToken()
      }
    }

    const handleFocus = () => {
      checkAndRefreshToken()
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)
    window.addEventListener('focus', handleFocus)

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      window.removeEventListener('focus', handleFocus)
    }
  }, [isAuthenticated, checkAndRefreshToken])
}
