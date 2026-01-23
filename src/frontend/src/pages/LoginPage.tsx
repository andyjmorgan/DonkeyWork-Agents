import { Button } from '@/components/ui/button'
import { ThemeToggle } from '@/components/layout/ThemeToggle'
import { Logo } from '@/components/branding/Logo'

export function LoginPage() {
  const handleLogin = () => {
    // Redirect to backend auth endpoint which handles PKCE and Keycloak redirect
    window.location.href = '/api/v1/auth/login'
  }

  return (
    <div className="flex min-h-screen flex-col bg-background">
      {/* Header */}
      <header className="flex items-center justify-between p-4">
        <Logo size="sm" />
        <ThemeToggle />
      </header>

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
            onClick={handleLogin}
          >
            Sign In
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
