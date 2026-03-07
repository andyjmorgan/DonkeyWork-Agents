import { useCallback } from 'react'
import type { Page, PageParams } from '../types'
import { DesktopSidebar } from './DesktopSidebar'
import { ErrorBoundary } from './ErrorBoundary'
import { ChatPage } from '../pages/ChatPage'
import { ConversationsPage } from '../pages/ConversationsPage'
import { NotesPage } from '../pages/NotesPage'
import { NoteEditorPage } from '../pages/NoteEditorPage'
import { TasksPage } from '../pages/TasksPage'
import { TaskEditorPage } from '../pages/TaskEditorPage'
import { ResearchPage } from '../pages/ResearchPage'
import { ResearchEditorPage } from '../pages/ResearchEditorPage'
import { ProjectsPage } from '../pages/ProjectsPage'
import { ProjectDetailPage } from '../pages/ProjectDetailPage'
import { MilestoneDetailPage } from '../pages/MilestoneDetailPage'
import { PlaceholderPage } from '../pages/PlaceholderPage'

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
        return <NotesPage onNavigate={onNavigate} />
      case 'note-editor':
        return <NoteEditorPage noteId={pageParams.noteId!} onNavigate={onNavigate} />
      case 'tasks':
        return <TasksPage onNavigate={onNavigate} />
      case 'task-editor':
        return <TaskEditorPage taskId={pageParams.taskId} isNew={pageParams.isNew} onNavigate={onNavigate} />
      case 'research':
        return <ResearchPage onNavigate={onNavigate} />
      case 'research-editor':
        return <ResearchEditorPage researchId={pageParams.researchId} isNew={pageParams.isNew} onNavigate={onNavigate} />
      case 'projects':
        return <ProjectsPage onNavigate={onNavigate} />
      case 'project-detail':
        return <ProjectDetailPage projectId={pageParams.projectId!} onNavigate={onNavigate} />
      case 'milestone-detail':
        return <MilestoneDetailPage projectId={pageParams.projectId!} milestoneId={pageParams.milestoneId!} onNavigate={onNavigate} />
      case 'settings':
        return <PlaceholderPage title="Settings" description="Configure DonkeyWork" />
      default:
        return null
    }
  }

  return (
    <div className="flex h-screen overflow-hidden">
      <DesktopSidebar currentPage={currentPage} onNavigate={handleSidebarNavigate} onOpenConversation={openConversation} />
      <main className="flex-1 overflow-hidden flex flex-col">
        {/* Drag region for title bar area */}
        <div className="h-12 shrink-0" data-tauri-drag-region />
        <div className="flex-1 overflow-hidden">
          <ErrorBoundary key={currentPage}>
            {renderPage()}
          </ErrorBoundary>
        </div>
      </main>
    </div>
  )
}
