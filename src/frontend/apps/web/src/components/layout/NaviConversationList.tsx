import { useEffect, useState } from 'react'
import { NavLink, useLocation, useNavigate } from 'react-router-dom'
import { conversations } from '@/lib/api'
import type { ConversationSummary } from '@/lib/api'
import { MessageSquare, Trash2 } from 'lucide-react'
import { cn } from '@/lib/utils'

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

export function NaviConversationList({ onNavigate }: { onNavigate?: () => void }) {
  const [items, setItems] = useState<ConversationSummary[]>([])
  const location = useLocation()
  const navigate = useNavigate()

  useEffect(() => {
    let cancelled = false
    conversations.listNavi(0, 5).then((res) => {
      if (!cancelled) setItems(res.items)
    }).catch(() => {})
    return () => { cancelled = true }
  }, [location.pathname])

  const handleDelete = async (e: React.MouseEvent, conv: ConversationSummary) => {
    e.preventDefault()
    e.stopPropagation()
    await conversations.delete(conv.id)
    setItems((prev) => prev.filter((c) => c.id !== conv.id))
    // If we're viewing the deleted conversation, navigate away
    if (location.pathname === `/agent-chat/${conv.id}`) {
      navigate('/agent-chat', { replace: true })
      onNavigate?.()
    }
  }

  if (items.length === 0) return null

  return (
    <div className="mt-1 space-y-0.5">
      {items.map((conv) => (
        <NavLink
          key={conv.id}
          to={`/agent-chat/${conv.id}`}
          onClick={onNavigate}
          className={({ isActive }) =>
            cn(
              'flex items-center gap-2.5 rounded-md px-3 py-1.5 pl-9 text-xs transition-colors group',
              isActive
                ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                : 'text-sidebar-foreground/70 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
            )
          }
        >
          <MessageSquare className="h-3 w-3 shrink-0 text-cyan-500/60" />
          <span className="truncate flex-1">{conv.title}</span>
          <span className="text-[10px] text-sidebar-foreground/40 shrink-0 group-hover:hidden">
            {timeAgo(conv.updatedAt ?? conv.createdAt)}
          </span>
          <button
            onClick={(e) => handleDelete(e, conv)}
            className="hidden group-hover:flex items-center justify-center shrink-0 rounded p-0.5 text-sidebar-foreground/40 hover:text-red-400 transition-colors cursor-pointer"
          >
            <Trash2 className="h-3 w-3" />
          </button>
        </NavLink>
      ))}
    </div>
  )
}
