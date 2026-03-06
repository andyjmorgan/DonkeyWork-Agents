import { Bubbles, MessageSquare, StickyNote, FlaskConical, CheckSquare, FolderKanban, Settings, Sun, Moon } from 'lucide-react'
import { useThemeStore } from '@donkeywork/stores'

type Page = 'chat' | 'conversations' | 'notes' | 'research' | 'tasks' | 'projects' | 'settings'

interface NavItem {
  id: Page
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

export function DesktopSidebar({ currentPage, onNavigate }: { currentPage: Page; onNavigate: (page: Page) => void }) {
  const { theme, toggleTheme } = useThemeStore()

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
            <NavButton key={item.id} item={item} isActive={currentPage === item.id} onClick={() => onNavigate(item.id)} />
          ))}
        </div>

        <div>
          <div className="px-2.5 py-1 text-[11px] font-semibold text-muted-foreground/60 uppercase tracking-wider">
            Workspace
          </div>
          <div className="space-y-0.5 mt-1">
            {sections.workspace.map(item => (
              <NavButton key={item.id} item={item} isActive={currentPage === item.id} onClick={() => onNavigate(item.id)} />
            ))}
          </div>
        </div>

        <div>
          <div className="space-y-0.5">
            {sections.system.map(item => (
              <NavButton key={item.id} item={item} isActive={currentPage === item.id} onClick={() => onNavigate(item.id)} />
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
