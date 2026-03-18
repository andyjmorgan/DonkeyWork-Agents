import { Button } from '@donkeywork/ui'
import { Logo } from '@/components/branding/Logo'
import { Github } from 'lucide-react'

function MicrosoftIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 21 21" fill="none" xmlns="http://www.w3.org/2000/svg">
      <rect x="1" y="1" width="9" height="9" fill="#F25022" />
      <rect x="11" y="1" width="9" height="9" fill="#7FBA00" />
      <rect x="1" y="11" width="9" height="9" fill="#00A4EF" />
      <rect x="11" y="11" width="9" height="9" fill="#FFB900" />
    </svg>
  )
}

function GoogleIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
      <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z" fill="#4285F4" />
      <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
      <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05" />
      <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
    </svg>
  )
}

export function LoginPage() {
  const handleGitHubLogin = () => {
    window.location.href = '/api/v1/auth/login?idpHint=github'
  }

  const handleMicrosoftLogin = () => {
    window.location.href = '/api/v1/auth/login?idpHint=microsoft'
  }

  const handleGoogleLogin = () => {
    window.location.href = '/api/v1/auth/login?idpHint=google'
  }

  return (
    <div className="flex min-h-screen flex-col bg-background">
      {/* Main content */}
      <main className="flex flex-1 flex-col items-center justify-center p-4">
        <div className="w-full max-w-sm space-y-6 text-center">
          <div className="flex flex-col items-center space-y-4">
            <Logo size="lg" showText={false} />
            <div className="space-y-2">
              <h1 className="text-3xl font-bold">Welcome back</h1>
              <p className="text-muted-foreground">
                Sign in to manage your AI agents
              </p>
            </div>
          </div>

          <div className="flex justify-center gap-4">
            <Button
              size="icon"
              variant="outline"
              className="h-12 w-12 rounded-xl"
              onClick={handleGoogleLogin}
              title="Sign in with Google"
            >
              <GoogleIcon className="h-6 w-6" />
            </Button>

            <Button
              size="icon"
              variant="outline"
              className="h-12 w-12 rounded-xl"
              onClick={handleGitHubLogin}
              title="Sign in with GitHub"
            >
              <Github className="h-6 w-6" />
            </Button>

            <Button
              size="icon"
              variant="outline"
              className="h-12 w-12 rounded-xl"
              onClick={handleMicrosoftLogin}
              title="Sign in with Microsoft"
            >
              <MicrosoftIcon className="h-6 w-6" />
            </Button>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="p-4 text-center text-sm text-muted-foreground">
        Built with questionable decisions and caffeine
      </footer>
    </div>
  )
}
