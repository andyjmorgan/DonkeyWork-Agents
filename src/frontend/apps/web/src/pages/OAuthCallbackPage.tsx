import { useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Loader2 } from 'lucide-react'

export default function OAuthCallbackPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()

  useEffect(() => {
    const success = searchParams.get('success')
    const error = searchParams.get('error')
    const provider = searchParams.get('provider')

    if (success === 'true') {
      console.log(`Successfully connected ${provider} account`)
    } else if (error) {
      console.error(`Failed to connect account: ${error}`)
    }

    // Redirect to connected accounts page after 2 seconds
    const timer = setTimeout(() => navigate('/connected-accounts'), 2000)
    return () => clearTimeout(timer)
  }, [searchParams, navigate])

  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="flex flex-col items-center gap-4">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <p className="text-muted-foreground">Processing OAuth callback...</p>
      </div>
    </div>
  )
}
