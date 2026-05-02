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
    const result = await refreshTokens()
    if (!result.ok) {
      if (result.reason === 'rejected') {
        console.warn('[fetchWithAuth] Token refresh rejected, logging out and redirecting to /login')
        logout()
        getPlatformConfig().navigate('/login')
        throw new Error('Session expired')
      }
      // Network/transient failure: surface the error without logging out so the
      // user keeps their session and the next request can try again.
      console.warn('[fetchWithAuth] Token refresh failed transiently, surfacing error without logout')
      throw new Error('Token refresh failed (network)')
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
    const result = await refreshTokens()

    if (result.ok) {
      console.debug('[fetchWithAuth] Token refreshed after 401, retrying request to:', url)
      return fetchWithAuth(url, options, false)
    }

    if (result.reason === 'rejected') {
      console.warn('[fetchWithAuth] Token refresh rejected after 401, logging out and redirecting to /login')
      logout()
      getPlatformConfig().navigate('/login')
      throw new Error('Session expired')
    }

    // Network failure during a 401 retry — return the original 401 response so
    // the caller can decide what to do, rather than killing the whole session.
    console.warn('[fetchWithAuth] Token refresh failed transiently after 401, returning original response')
    return response
  }

  return response
}
