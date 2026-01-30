import { useEffect, useRef, useCallback } from 'react'
import { useAuthStore } from '@/store/auth'

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
  const {
    isAuthenticated,
    shouldRefreshToken,
    refreshTokens,
    logout,
  } = useAuthStore()

  const checkAndRefreshToken = useCallback(async () => {
    if (!isAuthenticated) return

    if (shouldRefreshToken()) {
      const refreshed = await refreshTokens()

      if (!refreshed) {
        // Refresh failed - log the user out
        logout()
        window.location.href = '/login'
      }
    }
  }, [isAuthenticated, shouldRefreshToken, refreshTokens, logout])

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
