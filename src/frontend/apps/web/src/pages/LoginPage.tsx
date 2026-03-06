import { Button } from '@donkeywork/ui'
import { Logo } from '@/components/branding/Logo'
import { Github } from 'lucide-react'

export function LoginPage() {
  const handleGitHubLogin = () => {
    // Redirect to backend auth endpoint with GitHub identity provider hint
    window.location.href = '/api/v1/auth/login?idpHint=github'
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

          <Button
            size="lg"
            className="w-full"
            onClick={handleGitHubLogin}
          >
            <Github className="mr-2 h-5 w-5" />
            Sign in with GitHub
          </Button>
        </div>
      </main>

      {/* Footer */}
      <footer className="p-4 text-center text-sm text-muted-foreground">
        Built with questionable decisions and caffeine
      </footer>
    </div>
  )
}
