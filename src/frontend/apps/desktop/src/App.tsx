import { useState, useCallback } from 'react'
import { PlatformProvider } from '@donkeywork/platform'
import { desktopPlatformConfig, setDesktopNavigate } from './platform/desktop-platform'
import { DesktopLayout } from './components/DesktopLayout'

type Page = 'chat' | 'conversations' | 'notes' | 'research' | 'tasks' | 'projects' | 'settings'

export default function App() {
  const [currentPage, setCurrentPage] = useState<Page>('chat')

  const navigate = useCallback((path: string) => {
    const page = path.replace(/^\//, '').split('/')[0] || 'chat'
    setCurrentPage(page as Page)
  }, [])

  // Wire up platform navigation
  setDesktopNavigate(navigate)

  return (
    <PlatformProvider config={desktopPlatformConfig}>
      <DesktopLayout currentPage={currentPage} onNavigate={setCurrentPage} />
    </PlatformProvider>
  )
}
