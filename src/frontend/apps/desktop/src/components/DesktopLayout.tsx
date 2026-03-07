import { DesktopSidebar } from './DesktopSidebar'
import { ErrorBoundary } from './ErrorBoundary'
import { ChatPage } from '../pages/ChatPage'
import { NotesPage } from '../pages/NotesPage'
import { TasksPage } from '../pages/TasksPage'
import { ResearchPage } from '../pages/ResearchPage'
import { ProjectsPage } from '../pages/ProjectsPage'
import { PlaceholderPage } from '../pages/PlaceholderPage'

type Page = 'chat' | 'conversations' | 'notes' | 'research' | 'tasks' | 'projects' | 'settings'

const pageComponents: Record<Page, React.ComponentType> = {
  chat: ChatPage,
  conversations: () => <PlaceholderPage title="Conversations" description="Your conversation history" />,
  notes: NotesPage,
  research: ResearchPage,
  tasks: TasksPage,
  projects: ProjectsPage,
  settings: () => <PlaceholderPage title="Settings" description="Configure DonkeyWork" />,
}

export function DesktopLayout({ currentPage, onNavigate }: { currentPage: Page; onNavigate: (page: Page) => void }) {
  const PageComponent = pageComponents[currentPage]

  return (
    <div className="flex h-screen overflow-hidden">
      <DesktopSidebar currentPage={currentPage} onNavigate={onNavigate} />
      <main className="flex-1 overflow-hidden">
        <ErrorBoundary key={currentPage}>
          <PageComponent />
        </ErrorBoundary>
      </main>
    </div>
  )
}
