import { useState, useCallback, useEffect } from 'react'
import { listen } from '@tauri-apps/api/event'
import { PlatformProvider } from '@donkeywork/platform'
import { useThemeStore } from '@donkeywork/stores'
import { desktopPlatformConfig, setDesktopNavigate } from './platform/desktop-platform'
import { useDesktopAuth } from './hooks/useDesktopAuth'
import { useNotificationHub } from './hooks/useNotificationHub'
import { useAutoUpdater } from './hooks/useAutoUpdater'
import { DesktopLayout } from './components/DesktopLayout'
import { LoginPage } from './pages/LoginPage'
import type { Page, PageParams } from './types'

function AuthenticatedApp() {
  useNotificationHub()
  useAutoUpdater()

  const [currentPage, setCurrentPage] = useState<Page>('chat')
  const [pageParams, setPageParams] = useState<PageParams>({})

  const handleNavigate = useCallback((page: Page, params?: PageParams) => {
    setCurrentPage(page)
    setPageParams(params ?? {})
  }, [])

  const navigate = useCallback((path: string) => {
    const segments = path.replace(/^\//, '').split('/')
    const page = segments[0] || 'chat'
    handleNavigate(page as Page)
  }, [handleNavigate])

  // Wire up platform navigation
  setDesktopNavigate(navigate)

  // Listen for native menu events from Tauri
  useEffect(() => {
    const unlisten = listen<string>('menu-event', (event) => {
      switch (event.payload) {
        case 'new_conversation':
          handleNavigate('chat')
          break
        case 'toggle_theme':
          useThemeStore.getState().toggleTheme()
          break
        case 'preferences':
          handleNavigate('settings')
          break
      }
    })
    return () => {
      unlisten.then((fn) => fn())
    }
  }, [handleNavigate])

  return <DesktopLayout currentPage={currentPage} pageParams={pageParams} onNavigate={handleNavigate} />
}

function AppContent() {
  const { startLogin, isAuthenticated } = useDesktopAuth()

  if (!isAuthenticated) {
    return <LoginPage onLogin={startLogin} />
  }

  return <AuthenticatedApp />
}

export default function App() {
  return (
    <PlatformProvider config={desktopPlatformConfig}>
      <AppContent />
    </PlatformProvider>
  )
}
