import { useEffect, useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '@donkeywork/stores'

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

export function LoginCallbackPage() {
  const navigate = useNavigate()
  const { setTokens, setUser } = useAuthStore()

  const initialState = useMemo(() => {
    const hash = window.location.hash.substring(1)
    const params = new URLSearchParams(hash)
    const errorParam = params.get('error')
    const errorDescription = params.get('error_description')

    if (errorParam) {
      return { error: errorDescription || errorParam, params: null }
    }
    return { error: null, params }
  }, [])

  const [error, setError] = useState<string | null>(initialState.error)

  useEffect(() => {
    // If we already have an error from initial parsing, don't proceed
    if (initialState.error) return

    const params = initialState.params
    if (!params) return

    const accessToken = params.get('access_token')
    const refreshToken = params.get('refresh_token')
    const expiresIn = params.get('expires_in')

    if (!accessToken) {
      setError('No access token received')
      return
    }

    setTokens(accessToken, refreshToken, parseInt(expiresIn || '300', 10))

    const payload = parseJwt(accessToken)
    if (payload) {
      setUser({
        id: payload.sub,
        email: payload.email,
        name: payload.name,
        username: payload.preferred_username,
      })
    }

    window.history.replaceState(null, '', window.location.pathname)

    // Redirect to the app
    navigate('/agent-chat', { replace: true })
  }, [navigate, setTokens, setUser, initialState])

  if (error) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center bg-background p-4">
        <div className="w-full max-w-sm space-y-4 text-center">
          <h1 className="text-2xl font-bold text-destructive">Login Failed</h1>
          <p className="text-muted-foreground">{error}</p>
          <a
            href="/login"
            className="inline-block text-sm text-primary hover:underline"
          >
            Try again
          </a>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-background p-4">
      <div className="w-full max-w-sm space-y-4 text-center">
        <h1 className="text-2xl font-bold">Signing you in...</h1>
        <p className="text-muted-foreground">Hang tight</p>
      </div>
    </div>
  )
}
