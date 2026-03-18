import { useState } from 'react'
import { Github, Loader2, X } from 'lucide-react'

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

export function LoginPage({ onLogin }: { onLogin: (provider: string) => Promise<void> }) {
  const [isLoading, setIsLoading] = useState(false)

  const handleLogin = async (provider: string) => {
    setIsLoading(true)
    try {
      await onLogin(provider)
    } catch (e) {
      console.error('Login failed:', e)
      setIsLoading(false)
    }
  }

  const handleCancel = () => {
    setIsLoading(false)
  }

  return (
    <div className="flex h-screen flex-col bg-background" data-tauri-drag-region>
      {/* Drag region for title bar */}
      <div className="h-12 shrink-0" data-tauri-drag-region />

      <main className="flex flex-1 flex-col items-center justify-center p-4">
        <div className="w-full max-w-sm space-y-6 text-center">
          <div className="space-y-2">
            <h1 className="text-3xl font-bold text-foreground">DonkeyWork</h1>
            <p className="text-muted-foreground">
              Sign in to get started
            </p>
          </div>

          {isLoading ? (
            <>
              <div className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-xl bg-muted text-muted-foreground font-medium text-sm">
                <Loader2 className="h-5 w-5 animate-spin" />
                Waiting for browser...
              </div>

              <p className="text-xs text-muted-foreground">
                Complete sign-in in your browser, then come back here
              </p>

              <button
                onClick={handleCancel}
                className="flex items-center justify-center gap-2 w-full px-4 py-2 rounded-xl border border-border text-sm text-muted-foreground hover:bg-muted transition-colors cursor-pointer"
              >
                <X className="h-4 w-4" />
                Cancel
              </button>
            </>
          ) : (
            <div className="flex gap-3">
              <button
                onClick={() => handleLogin('google')}
                className="flex flex-1 items-center justify-center gap-2 px-4 py-2.5 rounded-xl border border-border text-foreground font-medium text-sm transition-colors hover:bg-muted cursor-pointer"
              >
                <GoogleIcon className="h-5 w-5" />
                Google
              </button>

              <button
                onClick={() => handleLogin('github')}
                className="flex flex-1 items-center justify-center gap-2 px-4 py-2.5 rounded-xl border border-border text-foreground font-medium text-sm transition-colors hover:bg-muted cursor-pointer"
              >
                <Github className="h-5 w-5" />
                GitHub
              </button>

              <button
                onClick={() => handleLogin('microsoft')}
                className="flex flex-1 items-center justify-center gap-2 px-4 py-2.5 rounded-xl border border-border text-foreground font-medium text-sm transition-colors hover:bg-muted cursor-pointer"
              >
                <MicrosoftIcon className="h-5 w-5" />
                Microsoft
              </button>
            </div>
          )}
        </div>
      </main>

      <footer className="p-4 text-center text-xs text-muted-foreground">
        Built with questionable decisions and caffeine
      </footer>
    </div>
  )
}
