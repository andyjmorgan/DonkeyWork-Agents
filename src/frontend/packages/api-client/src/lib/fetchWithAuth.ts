import { useAuthStore } from '@donkeywork/stores'
import { getPlatformConfig } from '@donkeywork/platform'

export async function fetchWithAuth(
  url: string,
  options: RequestInit = {},
  retryOnUnauthorized = true
): Promise<Response> {
  const { logout, refreshTokens, shouldRefreshToken, isTokenExpired } = useAuthStore.getState()

  if (isTokenExpired() && retryOnUnauthorized) {
    console.debug('[fetchWithAuth] Token expired, attempting refresh before request to:', url)
    const refreshed = await refreshTokens()
    if (!refreshed) {
      console.warn('[fetchWithAuth] Token refresh failed, logging out and redirecting to /login')
      logout()
      getPlatformConfig().navigate('/login')
      throw new Error('Session expired')
    }
    console.debug('[fetchWithAuth] Token refreshed successfully, proceeding with request')
  }
  // Proactively refresh token if it's about to expire (but not yet expired)
  else if (shouldRefreshToken() && retryOnUnauthorized) {
    console.debug('[fetchWithAuth] Token nearing expiry, triggering proactive refresh')
    refreshTokens().catch(() => {
      console.warn('[fetchWithAuth] Proactive token refresh failed (request may still succeed)')
    })
  }

  const currentToken = useAuthStore.getState().accessToken

  const response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${currentToken}`,
    },
  })

  if (response.status === 401 && retryOnUnauthorized) {
    console.debug('[fetchWithAuth] Got 401 response from:', url, '- attempting token refresh')
    const refreshed = await refreshTokens()

    if (refreshed) {
      console.debug('[fetchWithAuth] Token refreshed after 401, retrying request to:', url)
      return fetchWithAuth(url, options, false)
    }

    console.warn('[fetchWithAuth] Token refresh failed after 401, logging out and redirecting to /login')
    logout()
    getPlatformConfig().navigate('/login')
    throw new Error('Session expired')
  }

  return response
}
