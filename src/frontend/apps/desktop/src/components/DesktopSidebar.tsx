import { useState, useEffect } from 'react'
import { Bubbles, MessageSquare, StickyNote, FlaskConical, CheckSquare, FolderKanban, Settings, Sun, Moon, Trash2 } from 'lucide-react'
import { useThemeStore } from '@donkeywork/stores'
import { conversations, type ConversationSummary } from '@donkeywork/api-client'
import type { Page } from '../types'

type SidebarPage = 'chat' | 'conversations' | 'notes' | 'research' | 'tasks' | 'projects' | 'settings'

/** Maps sub-pages to the parent sidebar item that should be highlighted */
const parentPageMap: Partial<Record<Page, SidebarPage>> = {
  'note-editor': 'notes',
  'task-editor': 'tasks',
  'research-editor': 'research',
  'project-detail': 'projects',
  'milestone-detail': 'projects',
}

function getActiveSidebarPage(currentPage: Page): SidebarPage {
  return parentPageMap[currentPage] ?? (currentPage as SidebarPage)
}

interface NavItem {
  id: SidebarPage
  label: string
  icon: React.ComponentType<{ className?: string }>
  section: 'main' | 'workspace' | 'system'
}

const navItems: NavItem[] = [
  { id: 'chat', label: 'Navi', icon: Bubbles, section: 'main' },
  { id: 'conversations', label: 'History', icon: MessageSquare, section: 'main' },
  { id: 'notes', label: 'Notes', icon: StickyNote, section: 'workspace' },
  { id: 'research', label: 'Research', icon: FlaskConical, section: 'workspace' },
  { id: 'tasks', label: 'Tasks', icon: CheckSquare, section: 'workspace' },
  { id: 'projects', label: 'Projects', icon: FolderKanban, section: 'workspace' },
  { id: 'settings', label: 'Settings', icon: Settings, section: 'system' },
]

function NavButton({ item, isActive, onClick }: { item: NavItem; isActive: boolean; onClick: () => void }) {
  const Icon = item.icon
  return (
    <button
      onClick={onClick}
      className={`flex items-center gap-2.5 w-full px-2.5 py-1.5 rounded-lg text-[13px] font-medium transition-colors cursor-pointer ${
        isActive
          ? 'bg-accent/15 text-accent'
          : 'text-muted-foreground hover:bg-muted hover:text-foreground'
      }`}
    >
      <Icon className="w-4 h-4 shrink-0" />
      <span>{item.label}</span>
    </button>
  )
}

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60000)
  if (mins < 1) return 'now'
  if (mins < 60) return `${mins}m`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `${hours}h`
  const days = Math.floor(hours / 24)
  if (days < 7) return `${days}d`
  return `${Math.floor(days / 7)}w`
}

function RecentConversations({ onOpenConversation }: { onOpenConversation: (id: string) => void }) {
  const [items, setItems] = useState<ConversationSummary[]>([])

  useEffect(() => {
    let cancelled = false
    conversations.listNavi(0, 5).then((res) => {
      if (!cancelled) setItems(res.items)
    }).catch(() => {})
    return () => { cancelled = true }
  }, [])

  const handleDelete = async (e: React.MouseEvent, conv: ConversationSummary) => {
    e.stopPropagation()
    await conversations.delete(conv.id)
    setItems((prev) => prev.filter((c) => c.id !== conv.id))
  }

  if (items.length === 0) return null

  return (
    <div className="mt-1 space-y-0.5">
      {items.map((conv) => (
        <button
          key={conv.id}
          onClick={() => onOpenConversation(conv.id)}
          className="flex items-center gap-2 w-full px-2.5 py-1 pl-7 rounded-lg text-xs transition-colors cursor-pointer group text-muted-foreground/70 hover:bg-muted hover:text-foreground"
        >
          <MessageSquare className="h-3 w-3 shrink-0 text-cyan-500/60" />
          <span className="truncate flex-1 text-left">{conv.title}</span>
          <span className="text-[10px] text-muted-foreground/40 shrink-0 group-hover:hidden">
            {timeAgo(conv.updatedAt ?? conv.createdAt)}
          </span>
          <span
            role="button"
            onClick={(e) => handleDelete(e, conv)}
            className="hidden group-hover:flex items-center justify-center shrink-0 rounded p-0.5 text-muted-foreground/40 hover:text-red-400 transition-colors"
          >
            <Trash2 className="h-3 w-3" />
          </span>
        </button>
      ))}
    </div>
  )
}

interface DesktopSidebarProps {
  currentPage: Page
  onNavigate: (page: Page) => void
  onOpenConversation: (id: string) => void
}

export function DesktopSidebar({ currentPage, onNavigate, onOpenConversation }: DesktopSidebarProps) {
  const { theme, toggleTheme } = useThemeStore()
  const activeSidebarPage = getActiveSidebarPage(currentPage)

  const sections = {
    main: navItems.filter(i => i.section === 'main'),
    workspace: navItems.filter(i => i.section === 'workspace'),
    system: navItems.filter(i => i.section === 'system'),
  }

  return (
    <div className="w-52 flex flex-col bg-sidebar border-r border-sidebar-border" data-tauri-drag-region>
      {/* Traffic light inset area */}
      <div className="h-12 flex items-end px-3 pb-2" data-tauri-drag-region>
        <div className="flex items-center gap-2 pl-16">
          <span className="text-sm font-semibold text-foreground">DonkeyWork</span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-2 py-2 space-y-4 overflow-y-auto">
        <div className="space-y-0.5">
          {sections.main.map(item => (
            <NavButton key={item.id} item={item} isActive={activeSidebarPage === item.id} onClick={() => onNavigate(item.id)} />
          ))}
          <RecentConversations onOpenConversation={onOpenConversation} />
        </div>

        <div>
          <div className="px-2.5 py-1 text-[11px] font-semibold text-muted-foreground/60 uppercase tracking-wider">
            Workspace
          </div>
          <div className="space-y-0.5 mt-1">
            {sections.workspace.map(item => (
              <NavButton key={item.id} item={item} isActive={activeSidebarPage === item.id} onClick={() => onNavigate(item.id)} />
            ))}
          </div>
        </div>

        <div>
          <div className="space-y-0.5">
            {sections.system.map(item => (
              <NavButton key={item.id} item={item} isActive={activeSidebarPage === item.id} onClick={() => onNavigate(item.id)} />
            ))}
          </div>
        </div>
      </nav>

      {/* Theme toggle */}
      <div className="px-2 py-2 border-t border-sidebar-border">
        <button
          onClick={toggleTheme}
          className="flex items-center gap-2.5 w-full px-2.5 py-1.5 rounded-lg text-[13px] text-muted-foreground hover:bg-muted hover:text-foreground transition-colors cursor-pointer"
        >
          {theme === 'dark' ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
          <span>{theme === 'dark' ? 'Light Mode' : 'Dark Mode'}</span>
        </button>
      </div>
    </div>
  )
}
