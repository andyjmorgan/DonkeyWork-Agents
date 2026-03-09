import { useState, useCallback, useEffect, useRef } from 'react'
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

  // Keep a ref to currentPage so callbacks don't go stale
  const currentPageRef = useRef(currentPage)
  currentPageRef.current = currentPage

  const handleNewItem = useCallback(() => {
    const page = currentPageRef.current
    if (page === 'notes' || page === 'note-editor') {
      handleNavigate('note-editor', { isNew: true })
    } else if (page === 'tasks' || page === 'task-editor') {
      handleNavigate('task-editor', { isNew: true })
    } else if (page === 'research' || page === 'research-editor') {
      handleNavigate('research-editor', { isNew: true })
    } else {
      handleNavigate('chat')
    }
  }, [handleNavigate])

  const handleCloseItem = useCallback(() => {
    const page = currentPageRef.current
    const backMap: Partial<Record<Page, Page>> = {
      'note-editor': 'notes',
      'task-editor': 'tasks',
      'research-editor': 'research',
      'project-detail': 'projects',
      'milestone-detail': 'projects',
    }
    const target = backMap[page]
    if (target) handleNavigate(target)
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
        case 'new_item':
          handleNewItem()
          break
        case 'close_item':
          handleCloseItem()
          break
        case 'go_chat':
          handleNavigate('chat')
          break
        case 'go_notes':
          handleNavigate('notes')
          break
        case 'go_research':
          handleNavigate('research')
          break
        case 'go_tasks':
          handleNavigate('tasks')
          break
        case 'go_projects':
          handleNavigate('projects')
          break
        case 'go_search':
          handleNavigate('conversations')
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
  }, [handleNavigate, handleNewItem, handleCloseItem])

  // Escape key navigates back from editor/detail pages
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') handleCloseItem()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [handleCloseItem])

  return <DesktopLayout currentPage={currentPage} pageParams={pageParams} onNavigate={handleNavigate} />
}

function AppContent() {
  const { startLogin, isAuthenticated } = useDesktopAuth()

  if (!isAuthenticated) {
    return <LoginPage onLogin={(provider) => startLogin(provider)} />
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
