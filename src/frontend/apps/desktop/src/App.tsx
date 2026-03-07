import { useState, useCallback } from 'react'
import { PlatformProvider } from '@donkeywork/platform'
import { desktopPlatformConfig, setDesktopNavigate } from './platform/desktop-platform'
import { useDesktopAuth } from './hooks/useDesktopAuth'
import { DesktopLayout } from './components/DesktopLayout'
import { LoginPage } from './pages/LoginPage'

type Page = 'chat' | 'conversations' | 'notes' | 'research' | 'tasks' | 'projects' | 'settings'

export interface PageParams {
  conversationId?: string
}

function AuthenticatedApp() {
  const [currentPage, setCurrentPage] = useState<Page>('chat')
  const [pageParams, setPageParams] = useState<PageParams>({})

  const handleNavigate = useCallback((page: Page, params?: PageParams) => {
    setCurrentPage(page)
    setPageParams(params ?? {})
  }, [])

  const navigate = useCallback((path: string) => {
    const page = path.replace(/^\//, '').split('/')[0] || 'chat'
    handleNavigate(page as Page)
  }, [handleNavigate])

  // Wire up platform navigation
  setDesktopNavigate(navigate)

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
