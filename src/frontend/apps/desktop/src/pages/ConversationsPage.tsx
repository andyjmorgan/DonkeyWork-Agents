import { useState, useEffect, useCallback } from 'react'
import { MessageSquare, Trash2, Loader2, ChevronLeft, ChevronRight, Plus, RefreshCw } from 'lucide-react'
import { Button } from '@donkeywork/ui'
import { conversations, type ConversationSummary } from '@donkeywork/api-client'

const PAGE_SIZE = 20

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60000)
  if (mins < 1) return 'just now'
  if (mins < 60) return `${mins}m ago`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  if (days < 7) return `${days}d ago`
  return `${Math.floor(days / 7)}w ago`
}

interface ConversationsPageProps {
  onOpenConversation: (id: string) => void
  onNewChat: () => void
}

export function ConversationsPage({ onOpenConversation, onNewChat }: ConversationsPageProps) {
  const [items, setItems] = useState<ConversationSummary[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(0)
  const [loading, setLoading] = useState(true)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  const loadConversations = useCallback(async (offset = 0) => {
    setLoading(true)
    try {
      const response = await conversations.listNavi(offset, PAGE_SIZE)
      setItems(response.items)
      setTotalCount(response.totalCount)
    } catch (error) {
      console.error('Failed to load conversations:', error)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadConversations(page * PAGE_SIZE)
  }, [page, loadConversations])

  const totalPages = Math.ceil(totalCount / PAGE_SIZE)
  const canGoBack = page > 0
  const canGoForward = page < totalPages - 1

  const handleDelete = async (e: React.MouseEvent, id: string) => {
    e.stopPropagation()
    try {
      setDeletingId(id)
      await conversations.delete(id)
      setItems(prev => prev.filter(c => c.id !== id))
      setTotalCount(prev => prev - 1)
    } catch (error) {
      console.error('Failed to delete conversation:', error)
    } finally {
      setDeletingId(null)
    }
  }

  return (
    <div className="flex h-full flex-col overflow-hidden">
      <div className="flex items-center justify-between px-6 pt-6 pb-4">
        <div>
          <h1 className="text-lg font-semibold">Conversations</h1>
          <p className="text-sm text-muted-foreground">Your chat history</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => loadConversations(page * PAGE_SIZE)}>
            <RefreshCw className="h-4 w-4" />
          </Button>
          <Button size="sm" onClick={onNewChat}>
            <Plus className="h-4 w-4 mr-1.5" />
            New Chat
          </Button>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-6 pb-6">
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : items.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
            <div className="rounded-full bg-muted p-3">
              <MessageSquare className="h-6 w-6 text-muted-foreground" />
            </div>
            <h3 className="mt-3 text-sm font-semibold">No conversations yet</h3>
            <p className="mt-1 text-xs text-muted-foreground">Start a new chat to get going</p>
            <Button size="sm" className="mt-3" onClick={onNewChat}>
              New Chat
            </Button>
          </div>
        ) : (
          <div className="space-y-1">
            {items.map((conv) => (
              <div
                key={conv.id}
                className="group flex items-center gap-3 rounded-lg px-3 py-2.5 cursor-pointer hover:bg-muted/50 transition-colors"
                onClick={() => onOpenConversation(conv.id)}
              >
                <MessageSquare className="h-4 w-4 shrink-0 text-muted-foreground" />
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-medium truncate">{conv.title}</div>
                  <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    {conv.orchestrationName && (
                      <>
                        <span>{conv.orchestrationName}</span>
                        <span>·</span>
                      </>
                    )}
                    <span>{conv.messageCount} messages</span>
                    <span>·</span>
                    <span>{timeAgo(conv.updatedAt ?? conv.createdAt)}</span>
                  </div>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 opacity-0 group-hover:opacity-100 text-muted-foreground hover:text-destructive transition-opacity"
                  onClick={(e) => handleDelete(e, conv.id)}
                  disabled={deletingId === conv.id}
                >
                  {deletingId === conv.id ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <Trash2 className="h-3.5 w-3.5" />
                  )}
                </Button>
              </div>
            ))}

            {totalPages > 1 && (
              <div className="flex items-center justify-between pt-4">
                <p className="text-xs text-muted-foreground">
                  {page * PAGE_SIZE + 1}-{Math.min((page + 1) * PAGE_SIZE, totalCount)} of {totalCount}
                </p>
                <div className="flex items-center gap-1.5">
                  <Button variant="outline" size="sm" className="h-7 text-xs" onClick={() => setPage(p => p - 1)} disabled={!canGoBack || loading}>
                    <ChevronLeft className="h-3.5 w-3.5" />
                    Prev
                  </Button>
                  <Button variant="outline" size="sm" className="h-7 text-xs" onClick={() => setPage(p => p + 1)} disabled={!canGoForward || loading}>
                    Next
                    <ChevronRight className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
