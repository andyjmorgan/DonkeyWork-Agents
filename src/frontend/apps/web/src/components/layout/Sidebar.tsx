import { useState } from 'react'
import { NavLink } from 'react-router-dom'
import { Bot, Key, KeyRound, Lock, X, PlayCircle, List, FolderKanban, CheckSquare, StickyNote, Folder, Shield, Link as LinkIcon, Bubbles, File, Server, Globe, FlaskConical, Plus, ChevronDown, MessageSquare, Zap, Brain, Plug, Hammer, Volume2, Calendar, Cpu, Activity } from 'lucide-react'
import { cn } from '@/lib/utils'
import {
  Button,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@donkeywork/ui'
import { Logo } from '@/components/branding/Logo'
import { NaviConversationList } from '@/components/layout/NaviConversationList'

interface SidebarProps {
  open: boolean
  onClose: () => void
}

interface NavItem {
  name: string
  href: string
  icon: React.ComponentType<{ className?: string }>
  iconColor?: string
}

interface NavGroup {
  name: string
  icon: React.ComponentType<{ className?: string }>
  items: NavItem[]
}

const navigationGroups: NavGroup[] = [
  {
    name: 'Workspace',
    icon: FolderKanban,
    items: [
      { name: 'Projects', href: '/workspace', icon: Folder, iconColor: 'text-amber-500' },
      { name: 'Tasks', href: '/tasks', icon: CheckSquare, iconColor: 'text-emerald-500' },
      { name: 'Notes', href: '/notes', icon: StickyNote, iconColor: 'text-blue-500' },
      { name: 'Research', href: '/research', icon: FlaskConical, iconColor: 'text-cyan-500' },
      { name: 'Files', href: '/files', icon: File, iconColor: 'text-amber-500' },
      { name: 'Listen', href: '/audio-collections', icon: Volume2, iconColor: 'text-pink-500' },
    ],
  },
  {
    name: 'Build',
    icon: Bot,
    items: [
      { name: 'Orchestrations', href: '/orchestrations', icon: List, iconColor: 'text-cyan-500' },
      { name: 'Agent Definitions', href: '/agent-definitions', icon: Brain, iconColor: 'text-blue-500' },
      { name: 'Schedules', href: '/schedules', icon: Calendar, iconColor: 'text-orange-500' },
    ],
  },
  {
    name: 'Traces',
    icon: Activity,
    items: [
      { name: 'Orchestration Traces', href: '/executions', icon: PlayCircle, iconColor: 'text-violet-500' },
      { name: 'Agent Traces', href: '/agent-executions', icon: Cpu, iconColor: 'text-rose-500' },
      { name: 'MCP Traces', href: '/mcp-traces', icon: Server, iconColor: 'text-teal-500' },
    ],
  },
  {
    name: 'Integrations',
    icon: Plug,
    items: [
      { name: 'MCP Servers', href: '/mcp-servers', icon: Server, iconColor: 'text-teal-500' },
      { name: 'A2A Servers', href: '/a2a-servers', icon: Globe, iconColor: 'text-teal-500' },
      { name: 'Skills', href: '/skills', icon: Zap, iconColor: 'text-violet-500' },
    ],
  },
  {
    name: 'Toolbox',
    icon: Hammer,
    items: [
      { name: 'Prompts', href: '/prompts', icon: File, iconColor: 'text-emerald-500' },
      { name: 'Sandbox Settings', href: '/sandbox-settings', icon: Server, iconColor: 'text-orange-500' },
    ],
  },
  {
    name: 'Secrets',
    icon: Lock,
    items: [
      { name: 'API Keys', href: '/api-keys', icon: Key, iconColor: 'text-yellow-500' },
      { name: 'Credentials', href: '/credentials', icon: KeyRound, iconColor: 'text-rose-500' },
      { name: 'OAuth Clients', href: '/oauth-clients', icon: Shield, iconColor: 'text-purple-500' },
      { name: 'Connected Accounts', href: '/connected-accounts', icon: LinkIcon, iconColor: 'text-green-500' },
    ],
  },
]

export function Sidebar({ open, onClose }: SidebarProps) {
  const [historyOpen, setHistoryOpen] = useState(false)

  return (
    <>
      {/* Mobile overlay */}
      {open && (
        <div
          className="fixed inset-0 z-40 bg-black/50 md:hidden"
          onClick={onClose}
        />
      )}

      {/* Sidebar */}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-50 w-64 bg-sidebar border-r border-sidebar-border transform transition-transform duration-200 ease-in-out md:relative md:translate-x-0',
          open ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        <div className="flex h-full flex-col">
          {/* Logo */}
          <div className="flex h-14 items-center justify-between border-b border-sidebar-border px-4">
            <Logo size="sm" />
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 md:hidden"
              onClick={onClose}
            >
              <X className="h-5 w-5" />
            </Button>
          </div>

          {/* Navigation */}
          <nav className="flex-1 space-y-4 p-4 overflow-y-auto">
            {/* Navi section */}
            <div>
              <div className="flex items-center gap-2 px-3 py-2 text-xs font-semibold uppercase tracking-wider text-sidebar-foreground/60">
                <Bubbles className="h-4 w-4" />
                Navi
              </div>
              <div className="mt-1 space-y-1">
                <NavLink
                  to="/agent-chat"
                  end
                  onClick={onClose}
                  className={({ isActive }: { isActive: boolean }) =>
                    cn(
                      'flex items-center gap-3 rounded-md px-3 py-2 pl-9 text-sm font-medium transition-colors',
                      isActive
                        ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                        : 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
                    )
                  }
                >
                  <Plus className={cn('h-4 w-4', 'text-cyan-500')} />
                  New Chat
                </NavLink>
              </div>
              <Collapsible open={historyOpen} onOpenChange={setHistoryOpen}>
                <CollapsibleTrigger className="flex w-full items-center gap-2 rounded-md px-3 py-1.5 pl-9 text-xs text-sidebar-foreground/50 hover:text-sidebar-foreground/80 transition-colors cursor-pointer">
                  <ChevronDown className={cn('h-3 w-3 transition-transform', historyOpen && 'rotate-180')} />
                  Conversations
                </CollapsibleTrigger>
                <CollapsibleContent>
                  <NaviConversationList onNavigate={onClose} />
                  <NavLink
                    to="/conversations"
                    onClick={onClose}
                    className="flex w-full items-center gap-2 rounded-md px-3 py-1.5 pl-9 text-xs text-sidebar-foreground/50 hover:text-sidebar-foreground/80 transition-colors"
                  >
                    <MessageSquare className="h-3 w-3 text-violet-500/60" />
                    Show all
                  </NavLink>
                </CollapsibleContent>
              </Collapsible>
            </div>

            {/* Grouped navigation */}
            {navigationGroups.map((group) => (
              <div key={group.name}>
                <div className="flex items-center gap-2 px-3 py-2 text-xs font-semibold uppercase tracking-wider text-sidebar-foreground/60">
                  <group.icon className="h-4 w-4" />
                  {group.name}
                </div>
                <div className="mt-1 space-y-1">
                  {group.items.map((item) => (
                    <NavLink
                      key={item.name}
                      to={item.href}
                      onClick={onClose}
                      className={({ isActive }: { isActive: boolean }) =>
                        cn(
                          'flex items-center gap-3 rounded-md px-3 py-2 pl-9 text-sm font-medium transition-colors',
                          isActive
                            ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                            : 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
                        )
                      }
                    >
                      <item.icon className={cn('h-4 w-4', item.iconColor)} />
                      {item.name}
                    </NavLink>
                  ))}
                </div>
              </div>
            ))}
          </nav>

          {/* Footer */}
          <div className="border-t border-sidebar-border p-4">
            <p className="text-xs text-sidebar-foreground/60">
              Build something rad
            </p>
          </div>
        </div>
      </aside>
    </>
  )
}
