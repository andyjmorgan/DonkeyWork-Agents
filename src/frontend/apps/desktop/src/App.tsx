import { useState, useCallback } from 'react'
import { PlatformProvider } from '@donkeywork/platform'
import { desktopPlatformConfig, setDesktopNavigate } from './platform/desktop-platform'
import { useDesktopAuth } from './hooks/useDesktopAuth'
import { DesktopLayout } from './components/DesktopLayout'
import { LoginPage } from './pages/LoginPage'

type Page = 'chat' | 'conversations' | 'notes' | 'research' | 'tasks' | 'projects' | 'settings'

function AuthenticatedApp() {
  const [currentPage, setCurrentPage] = useState<Page>('chat')

  const navigate = useCallback((path: string) => {
    const page = path.replace(/^\//, '').split('/')[0] || 'chat'
    setCurrentPage(page as Page)
  }, [])

  // Wire up platform navigation
  setDesktopNavigate(navigate)

  return <DesktopLayout currentPage={currentPage} onNavigate={setCurrentPage} />
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
