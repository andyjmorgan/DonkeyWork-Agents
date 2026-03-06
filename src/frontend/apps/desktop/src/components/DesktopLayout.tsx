import { DesktopSidebar } from './DesktopSidebar'
import { ChatPage } from '../pages/ChatPage'
import { PlaceholderPage } from '../pages/PlaceholderPage'

type Page = 'chat' | 'conversations' | 'notes' | 'research' | 'tasks' | 'projects' | 'settings'

const pageComponents: Record<Page, React.ComponentType> = {
  chat: ChatPage,
  conversations: () => <PlaceholderPage title="Conversations" description="Your conversation history" />,
  notes: () => <PlaceholderPage title="Notes" description="Create and manage notes" />,
  research: () => <PlaceholderPage title="Research" description="Research items and findings" />,
  tasks: () => <PlaceholderPage title="Tasks" description="Track your tasks" />,
  projects: () => <PlaceholderPage title="Projects" description="Manage projects and milestones" />,
  settings: () => <PlaceholderPage title="Settings" description="Configure DonkeyWork" />,
}

export function DesktopLayout({ currentPage, onNavigate }: { currentPage: Page; onNavigate: (page: Page) => void }) {
  const PageComponent = pageComponents[currentPage]

  return (
    <div className="flex h-screen overflow-hidden">
      <DesktopSidebar currentPage={currentPage} onNavigate={onNavigate} />
      <main className="flex-1 overflow-hidden">
        <PageComponent />
      </main>
    </div>
  )
}
