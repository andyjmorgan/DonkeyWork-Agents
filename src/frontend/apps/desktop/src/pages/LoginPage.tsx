import { useState } from 'react'
import { Github, Loader2, X } from 'lucide-react'

export function LoginPage({ onLogin }: { onLogin: () => Promise<void> }) {
  const [isLoading, setIsLoading] = useState(false)

  const handleLogin = async () => {
    setIsLoading(true)
    try {
      await onLogin()
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
            <button
              onClick={handleLogin}
              className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-xl bg-foreground text-background font-medium text-sm transition-opacity hover:opacity-90 cursor-pointer"
            >
              <Github className="h-5 w-5" />
              Sign in with GitHub
            </button>
          )}
        </div>
      </main>

      <footer className="p-4 text-center text-xs text-muted-foreground">
        Built with questionable decisions and caffeine
      </footer>
    </div>
  )
}
