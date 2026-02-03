import { useAuthStore } from '@/store/auth'

/**
 * Performs a fetch request with automatic token refresh handling.
 *
 * This function:
 * 1. Proactively refreshes the token if it's about to expire
 * 2. Retries the request on 401 responses after refreshing the token
 * 3. Logs the user out and redirects to login if refresh fails
 *
 * @param url - The URL to fetch
 * @param options - Fetch options (headers, body, etc.)
 * @param retryOnUnauthorized - Whether to retry on 401 (set to false for recursive calls)
 * @returns The fetch Response object
 */
export async function fetchWithAuth(
  url: string,
  options: RequestInit = {},
  retryOnUnauthorized = true
): Promise<Response> {
  const { logout, refreshTokens, shouldRefreshToken, isTokenExpired } = useAuthStore.getState()

  // Check if token is completely expired first
  if (isTokenExpired() && retryOnUnauthorized) {
    const refreshed = await refreshTokens()
    if (!refreshed) {
      logout()
      window.location.href = '/login'
      throw new Error('Session expired')
    }
  }
  // Proactively refresh token if it's about to expire (but not yet expired)
  else if (shouldRefreshToken() && retryOnUnauthorized) {
    // Don't block on proactive refresh - just fire and let it complete
    // The token should still be valid for the current request
    refreshTokens().catch(() => {
      // Proactive refresh failed, but current request might still work
      // The 401 handler below will catch it if not
    })
  }

  // Get potentially updated token after refresh
  const currentToken = useAuthStore.getState().accessToken

  const response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${currentToken}`,
    },
  })

  if (response.status === 401 && retryOnUnauthorized) {
    // Try to refresh the token
    const refreshed = await refreshTokens()

    if (refreshed) {
      // Retry the request with the new token (don't retry again on 401)
      return fetchWithAuth(url, options, false)
    }

    // Refresh failed - logout and redirect
    logout()
    window.location.href = '/login'
    throw new Error('Session expired')
  }

  return response
}
