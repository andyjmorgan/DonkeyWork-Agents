import { useState } from 'react'
import { Github, Loader2 } from 'lucide-react'

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

          <button
            onClick={handleLogin}
            disabled={isLoading}
            className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-xl bg-foreground text-background font-medium text-sm transition-opacity hover:opacity-90 disabled:opacity-50 cursor-pointer disabled:cursor-not-allowed"
          >
            {isLoading ? (
              <>
                <Loader2 className="h-5 w-5 animate-spin" />
                Waiting for browser...
              </>
            ) : (
              <>
                <Github className="h-5 w-5" />
                Sign in with GitHub
              </>
            )}
          </button>

          {isLoading && (
            <p className="text-xs text-muted-foreground">
              Complete sign-in in your browser, then come back here
            </p>
          )}
        </div>
      </main>

      <footer className="p-4 text-center text-xs text-muted-foreground">
        Built with questionable decisions and caffeine
      </footer>
    </div>
  )
}
