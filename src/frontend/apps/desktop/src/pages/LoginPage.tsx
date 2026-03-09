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
            <div className="space-y-3">
              <button
                onClick={() => handleLogin('github')}
                className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-xl bg-foreground text-background font-medium text-sm transition-opacity hover:opacity-90 cursor-pointer"
              >
                <Github className="h-5 w-5" />
                Sign in with GitHub
              </button>

              <div className="relative">
                <div className="absolute inset-0 flex items-center">
                  <span className="w-full border-t border-border" />
                </div>
                <div className="relative flex justify-center text-xs uppercase">
                  <span className="bg-background px-2 text-muted-foreground">or</span>
                </div>
              </div>

              <button
                onClick={() => handleLogin('microsoft')}
                className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-xl border border-border text-foreground font-medium text-sm transition-colors hover:bg-muted cursor-pointer"
              >
                <MicrosoftIcon className="h-5 w-5" />
                Sign in with Microsoft
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
