import { oauth, type OAuthProvider } from '@/lib/api'

export function useOAuthFlow() {
  const initiateFlow = async (provider: OAuthProvider, scopes?: string[]) => {
    try {
      const response = await oauth.getAuthorizationUrl(provider, scopes)
      // Backend sets cookies, just redirect to authorization URL
      window.location.href = response.authorizationUrl
    } catch (error) {
      console.error('Failed to initiate OAuth flow:', error)
      throw error
    }
  }

  const disconnect = async (tokenId: string) => {
    try {
      await oauth.disconnectToken(tokenId)
      return true
    } catch (error) {
      console.error('Failed to disconnect account:', error)
      throw error
    }
  }

  const refresh = async (tokenId: string) => {
    try {
      const result = await oauth.refreshToken(tokenId)
      if (!result.success) {
        throw new Error(result.error || 'Token refresh failed')
      }
      return true
    } catch (error) {
      console.error('Failed to refresh token:', error)
      throw error
    }
  }

  return { initiateFlow, disconnect, refresh }
}
