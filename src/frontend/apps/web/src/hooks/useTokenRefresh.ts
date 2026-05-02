import { useEffect, useRef, useCallback } from 'react'
import { useAuthStore } from '@donkeywork/stores'
import { getPlatformConfig } from '@donkeywork/platform'

const FALLBACK_DELAY_MS = 30_000
const MIN_DELAY_MS = 5_000
const NETWORK_BACKOFF_MS = 15_000

/**
 * Hook that schedules proactive access-token refreshes ahead of expiry.
 *
 * Refreshes are scheduled against `expiresAt` rather than via a fixed interval,
 * so we always wake up inside the refresh window regardless of access-token
 * lifetime. Transient (network) refresh failures back off and retry; only a
 * Keycloak rejection (refresh token revoked or session ended) triggers logout.
 */
export function useTokenRefresh() {
  const timerRef = useRef<number | null>(null)
  // Indirection so scheduleNext can recursively re-arm itself from inside its own
  // setTimeout body without forward-referencing the useCallback before declaration.
  const scheduleNextRef = useRef<(() => void) | null>(null)
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)

  const clearTimer = useCallback(() => {
    if (timerRef.current !== null) {
      window.clearTimeout(timerRef.current)
      timerRef.current = null
    }
  }, [])

  const checkAndRefreshToken = useCallback(async (): Promise<'ok' | 'rejected' | 'network' | 'noop'> => {
    const state = useAuthStore.getState()
    if (!state.isAuthenticated) return 'noop'

    if (!state.isTokenExpired() && !state.shouldRefreshToken()) return 'noop'

    const result = await state.refreshTokens()
    if (result.ok) return 'ok'

    if (result.reason === 'rejected') {
      console.error('[TokenRefresh] Refresh token rejected by IdP, logging out')
      state.logout()
      getPlatformConfig().navigate('/login')
      return 'rejected'
    }

    console.warn('[TokenRefresh] Transient refresh failure, will retry')
    return 'network'
  }, [])

  // Schedule the next refresh based on the current token's expiry.
  const scheduleNext = useCallback(() => {
    clearTimer()

    const state = useAuthStore.getState()
    if (!state.isAuthenticated) return

    const { expiresAt, tokenIssuedAt } = state
    let delay = FALLBACK_DELAY_MS

    if (expiresAt) {
      const now = Date.now()
      const lifetime = tokenIssuedAt ? expiresAt - tokenIssuedAt : 0
      // Aim to refresh when 80% of the token's lifetime has elapsed (i.e. 20% remaining).
      const refreshAt = lifetime > 0 ? expiresAt - lifetime * 0.2 : expiresAt - 30_000
      delay = Math.max(MIN_DELAY_MS, refreshAt - now)
    }

    timerRef.current = window.setTimeout(async () => {
      const outcome = await checkAndRefreshToken()
      if (outcome === 'rejected') return // logout already navigated away
      if (outcome === 'network') {
        // Back off and retry without touching session state.
        timerRef.current = window.setTimeout(() => scheduleNextRef.current?.(), NETWORK_BACKOFF_MS)
        return
      }
      scheduleNextRef.current?.()
    }, delay)
  }, [checkAndRefreshToken, clearTimer])

  // Keep the ref pointing at the latest scheduleNext so the recursive call
  // path above always sees the current closure.
  useEffect(() => {
    scheduleNextRef.current = scheduleNext
  }, [scheduleNext])

  useEffect(() => {
    if (!isAuthenticated) {
      clearTimer()
      return
    }

    // Catch tokens that are already expired (or in the refresh window) at mount,
    // then schedule the next check against the post-refresh expiry.
    void checkAndRefreshToken().then((outcome) => {
      if (outcome !== 'rejected') scheduleNext()
    })

    return clearTimer
  }, [isAuthenticated, checkAndRefreshToken, scheduleNext, clearTimer])

  // Re-check when the user comes back to the tab — a tab that slept past expiry
  // wouldn't have fired its scheduled timer at the right moment.
  useEffect(() => {
    if (!isAuthenticated) return

    const onVisible = () => {
      if (document.visibilityState !== 'visible') return
      void checkAndRefreshToken().then((outcome) => {
        if (outcome !== 'rejected') scheduleNext()
      })
    }

    document.addEventListener('visibilitychange', onVisible)
    window.addEventListener('focus', onVisible)
    return () => {
      document.removeEventListener('visibilitychange', onVisible)
      window.removeEventListener('focus', onVisible)
    }
  }, [isAuthenticated, checkAndRefreshToken, scheduleNext])
}
