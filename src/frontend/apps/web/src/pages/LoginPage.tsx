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

export function LoginPage() {
  const handleGitHubLogin = () => {
    window.location.href = '/api/v1/auth/login?idpHint=github'
  }

  const handleMicrosoftLogin = () => {
    window.location.href = '/api/v1/auth/login?idpHint=microsoft'
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

          <div className="flex gap-3">
            <Button
              size="lg"
              variant="outline"
              className="flex-1"
              onClick={handleGitHubLogin}
            >
              <Github className="mr-2 h-5 w-5" />
              GitHub
            </Button>

            <Button
              size="lg"
              variant="outline"
              className="flex-1"
              onClick={handleMicrosoftLogin}
            >
              <MicrosoftIcon className="mr-2 h-5 w-5" />
              Microsoft
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
