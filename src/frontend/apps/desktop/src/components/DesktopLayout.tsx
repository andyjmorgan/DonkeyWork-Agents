import { useCallback } from 'react'
import type { PageParams } from '../App'
import { DesktopSidebar } from './DesktopSidebar'
import { ErrorBoundary } from './ErrorBoundary'
import { ChatPage } from '../pages/ChatPage'
import { ConversationsPage } from '../pages/ConversationsPage'
import { NotesPage } from '../pages/NotesPage'
import { TasksPage } from '../pages/TasksPage'
import { ResearchPage } from '../pages/ResearchPage'
import { ProjectsPage } from '../pages/ProjectsPage'
import { PlaceholderPage } from '../pages/PlaceholderPage'

type Page = 'chat' | 'conversations' | 'notes' | 'research' | 'tasks' | 'projects' | 'settings'

interface DesktopLayoutProps {
  currentPage: Page
  pageParams: PageParams
  onNavigate: (page: Page, params?: PageParams) => void
}

export function DesktopLayout({ currentPage, pageParams, onNavigate }: DesktopLayoutProps) {
  const openConversation = useCallback((conversationId: string) => {
    onNavigate('chat', { conversationId })
  }, [onNavigate])

  const handleSidebarNavigate = useCallback((page: Page) => {
    onNavigate(page)
  }, [onNavigate])

  function renderPage() {
    switch (currentPage) {
      case 'chat':
        return <ChatPage conversationId={pageParams.conversationId} />
      case 'conversations':
        return <ConversationsPage onOpenConversation={openConversation} onNewChat={() => onNavigate('chat')} />
      case 'notes':
        return <NotesPage />
      case 'research':
        return <ResearchPage />
      case 'tasks':
        return <TasksPage />
      case 'projects':
        return <ProjectsPage />
      case 'settings':
        return <PlaceholderPage title="Settings" description="Configure DonkeyWork" />
      default:
        return null
    }
  }

  return (
    <div className="flex h-screen overflow-hidden">
      <DesktopSidebar currentPage={currentPage} onNavigate={handleSidebarNavigate} onOpenConversation={openConversation} />
      <main className="flex-1 overflow-hidden">
        <ErrorBoundary key={currentPage}>
          {renderPage()}
        </ErrorBoundary>
      </main>
    </div>
  )
}
