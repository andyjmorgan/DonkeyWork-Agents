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

  const checkAndRefreshToken = useCallback(async () => {
    // Always get fresh state to avoid stale closures
    const state = useAuthStore.getState()
    if (!state.isAuthenticated) return

    // Also check if token is expired (not just should refresh)
    if (state.isTokenExpired()) {
      const refreshed = await state.refreshTokens()
      if (!refreshed) {
        // Refresh failed - log the user out
        state.logout()
        window.location.href = '/api/v1/auth/logout'
      }
    } else if (state.shouldRefreshToken()) {
      // Proactively refresh before expiry
      const refreshed = await state.refreshTokens()
      if (!refreshed) {
        // Refresh failed - log the user out
        state.logout()
        window.location.href = '/api/v1/auth/logout'
      }
    }
  }, [])

  useEffect(() => {
    if (!isAuthenticated) {
      // Clear interval if user is not authenticated
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
      return
    }

    // Initial check
    checkAndRefreshToken()

    // Set up periodic checks
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
