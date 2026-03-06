import { Toaster as SonnerToaster } from 'sonner'
import { useThemeStore } from '@/store/theme'

export function Toaster() {
  const { theme } = useThemeStore()

  return (
    <SonnerToaster
      theme={theme}
      position="bottom-right"
      toastOptions={{
        classNames: {
          toast: 'border border-border bg-card text-foreground',
          title: 'text-foreground font-medium',
          description: 'text-muted-foreground',
          error: 'border-destructive/50 bg-destructive/10 text-destructive',
          success: 'border-green-500/50 bg-green-500/10 text-green-600 dark:text-green-400',
        },
      }}
    />
  )
}
