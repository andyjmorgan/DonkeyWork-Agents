import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { MessageSquare, Trash2, ExternalLink, Loader2, ChevronLeft, ChevronRight } from 'lucide-react'
import {
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@donkeywork/ui'
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

export function ConversationsPage() {
  const navigate = useNavigate()
  const [items, setItems] = useState<ConversationSummary[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(0)
  const [loading, setLoading] = useState(true)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  const loadConversations = async (offset = 0) => {
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
  }

  useEffect(() => {
    loadConversations(page * PAGE_SIZE)
  }, [page])

  const totalPages = Math.ceil(totalCount / PAGE_SIZE)
  const canGoBack = page > 0
  const canGoForward = page < totalPages - 1

  const handleOpen = (id: string) => {
    navigate(`/agent-chat/${id}`)
  }

  const handleDelete = async (e: React.MouseEvent, id: string) => {
    e.stopPropagation()
    if (!confirm('Are you sure you want to delete this conversation? This action cannot be undone.')) {
      return
    }

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

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Conversations</h1>
        <p className="text-muted-foreground">
          Browse and manage all your chat conversations
        </p>
      </div>

      {items.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <MessageSquare className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No conversations yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Start a new chat to get going
          </p>
          <Button className="mt-4" onClick={() => navigate('/agent-chat')}>
            New Chat
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile view - card layout */}
          <div className="space-y-3 md:hidden">
            {items.map((conv) => (
              <div
                key={conv.id}
                className="rounded-lg border border-border bg-card p-4 space-y-2 cursor-pointer hover:border-accent/30 transition-colors"
                onClick={() => handleOpen(conv.id)}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="text-sm font-medium truncate">{conv.title}</div>
                    {conv.orchestrationName && (
                      <div className="text-sm">
                        <span className="text-muted-foreground">Orchestration: </span>
                        <span>{conv.orchestrationName}</span>
                      </div>
                    )}
                    <div className="text-sm">
                      <span className="text-muted-foreground">Messages: </span>
                      <span>{conv.messageCount}</span>
                    </div>
                    <div className="text-sm">
                      <span className="text-muted-foreground">Updated: </span>
                      <span>{timeAgo(conv.updatedAt ?? conv.createdAt)}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={(e) => { e.stopPropagation(); handleOpen(conv.id) }}
                    >
                      <ExternalLink className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive hover:text-destructive hover:bg-destructive/10"
                      onClick={(e) => handleDelete(e, conv.id)}
                      disabled={deletingId === conv.id}
                    >
                      {deletingId === conv.id ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
                    </Button>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop view - table layout */}
          <div className="hidden md:block rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Title</TableHead>
                  <TableHead>Orchestration</TableHead>
                  <TableHead>Messages</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead>Updated</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((conv) => (
                  <TableRow
                    key={conv.id}
                    className="cursor-pointer"
                    onClick={() => handleOpen(conv.id)}
                  >
                    <TableCell className="font-medium max-w-[300px] truncate">
                      {conv.title}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {conv.orchestrationName || '-'}
                    </TableCell>
                    <TableCell>{conv.messageCount}</TableCell>
                    <TableCell className="text-muted-foreground">
                      {timeAgo(conv.createdAt)}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {timeAgo(conv.updatedAt ?? conv.createdAt)}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8"
                          onClick={(e) => { e.stopPropagation(); handleOpen(conv.id) }}
                          title="Open"
                        >
                          <ExternalLink className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-destructive hover:text-destructive hover:bg-destructive/10"
                          onClick={(e) => handleDelete(e, conv.id)}
                          disabled={deletingId === conv.id}
                          title="Delete"
                        >
                          {deletingId === conv.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Trash2 className="h-4 w-4" />
                          )}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-4">
              <p className="text-sm text-muted-foreground">
                Showing {page * PAGE_SIZE + 1}-{Math.min((page + 1) * PAGE_SIZE, totalCount)} of {totalCount}
              </p>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage(p => p - 1)}
                  disabled={!canGoBack || loading}
                >
                  <ChevronLeft className="h-4 w-4" />
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage(p => p + 1)}
                  disabled={!canGoForward || loading}
                >
                  Next
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
