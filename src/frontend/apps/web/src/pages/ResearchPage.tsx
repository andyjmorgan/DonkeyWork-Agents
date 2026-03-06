import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Loader2, Search, FlaskConical, Trash2, X, CheckSquare } from 'lucide-react'
import { Button, Badge } from '@donkeywork/ui'
import { research, type ResearchSummary, type ResearchStatus } from '@/lib/api'

const statusVariants: Record<ResearchStatus, 'pending' | 'inProgress' | 'success' | 'destructive' | 'warning'> = {
  NotStarted: 'pending',
  InProgress: 'inProgress',
  Completed: 'success',
  Cancelled: 'destructive',
}

export function ResearchPage() {
  const navigate = useNavigate()
  const [isLoading, setIsLoading] = useState(true)
  const [researchList, setResearchList] = useState<ResearchSummary[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [isBulkDeleting, setIsBulkDeleting] = useState(false)

  useEffect(() => {
    loadResearch()
  }, [])

  const loadResearch = async () => {
    try {
      setIsLoading(true)
      const data = await research.list()
      setResearchList(data)
    } catch (error) {
      console.error('Failed to load research:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleCreate = () => {
    navigate('/research/new')
  }

  const handleDelete = async (id: string, subject: string) => {
    if (!window.confirm(`Are you sure you want to delete "${subject}"?`)) return

    try {
      setDeletingId(id)
      await research.delete(id)
      await loadResearch()
    } catch (error) {
      console.error('Failed to delete research:', error)
    } finally {
      setDeletingId(null)
    }
  }

  const toggleSelect = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }

  const handleBulkDelete = async () => {
    if (selectedIds.size === 0) return
    if (!window.confirm(`Delete ${selectedIds.size} selected research item${selectedIds.size > 1 ? 's' : ''}?`)) return

    try {
      setIsBulkDeleting(true)
      await Promise.all(Array.from(selectedIds).map((id) => research.delete(id)))
      setSelectedIds(new Set())
      await loadResearch()
    } catch (error) {
      console.error('Failed to bulk delete research:', error)
    } finally {
      setIsBulkDeleting(false)
    }
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Research</h1>
          <p className="text-muted-foreground">
            Track investigations and findings
          </p>
        </div>
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4" />
          <span className="hidden sm:inline">New Research</span>
        </Button>
      </div>

      {/* Bulk action bar */}
      {selectedIds.size > 0 && (
        <div className="flex items-center gap-3 rounded-lg border border-border bg-muted/50 px-4 py-2">
          <span className="text-sm font-medium">{selectedIds.size} selected</span>
          <Button
            variant="destructive"
            size="sm"
            onClick={handleBulkDelete}
            disabled={isBulkDeleting}
          >
            {isBulkDeleting ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin mr-1.5" />
            ) : (
              <Trash2 className="h-3.5 w-3.5 mr-1.5" />
            )}
            Delete
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setSelectedIds(new Set())}
          >
            <X className="h-3.5 w-3.5 mr-1.5" />
            Clear
          </Button>
        </div>
      )}

      {researchList.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <FlaskConical className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No research yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Start a new research topic to track your investigations
          </p>
          <Button className="mt-4" onClick={handleCreate}>
            <Plus className="h-4 w-4" />
            New Research
          </Button>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {researchList.map((item) => {
            const isSelected = selectedIds.has(item.id)
            return (
              <div
                key={item.id}
                className={`group relative rounded-lg border bg-card hover:shadow-md transition-all cursor-pointer flex flex-col h-[250px] ${
                  item.status === 'Completed' || item.status === 'Cancelled' ? 'opacity-75' : ''
                } ${isSelected ? 'border-primary ring-1 ring-primary' : 'border-border'}`}
                onClick={() => navigate(`/research/${item.id}`)}
              >
                <div className="flex-1 p-4 min-h-0">
                  <div className="flex items-start justify-between gap-2 mb-2">
                    <div className="flex items-center gap-2 min-w-0">
                      <Search className="h-4 w-4 text-cyan-500 shrink-0" />
                      <h3 className="font-semibold truncate">{item.subject}</h3>
                    </div>
                    <div className="flex items-center gap-1 shrink-0">
                      {/* Selection checkbox */}
                      <button
                        onClick={(e) => {
                          e.stopPropagation()
                          toggleSelect(item.id)
                        }}
                        className={`h-7 w-7 rounded flex items-center justify-center transition-all ${
                          isSelected
                            ? 'bg-primary text-primary-foreground'
                            : 'opacity-0 group-hover:opacity-100 hover:bg-accent'
                        }`}
                      >
                        <CheckSquare className="h-3.5 w-3.5" />
                      </button>
                      <Badge variant={statusVariants[item.status]}>
                        {item.status.replace(/([A-Z])/g, ' $1').trim()}
                      </Badge>
                    </div>
                  </div>
                  {item.contentPreview && (
                    <p className="text-sm text-muted-foreground line-clamp-3 mt-2">
                      {item.contentPreview}
                    </p>
                  )}
                  {item.tags.length > 0 && (
                    <div className="flex gap-1 flex-wrap mt-2">
                      {item.tags.slice(0, 3).map((tag) => (
                        <Badge key={tag.id} variant="secondary" className="text-xs">
                          {tag.name}
                        </Badge>
                      ))}
                    </div>
                  )}
                </div>
                <div className="border-t border-border px-4 py-2.5 flex items-center justify-between text-xs text-muted-foreground">
                  <div className="flex items-center gap-3">
                    <span>{item.noteCount} note{item.noteCount !== 1 ? 's' : ''}</span>
                    {item.completedAt && (
                      <span>{new Date(item.completedAt).toLocaleDateString()}</span>
                    )}
                  </div>
                  <button
                    className="opacity-0 group-hover:opacity-100 text-destructive hover:text-destructive/80 transition-opacity"
                    onClick={(e) => { e.stopPropagation(); handleDelete(item.id, item.subject) }}
                    disabled={deletingId === item.id}
                  >
                    {deletingId === item.id ? (
                      <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    ) : (
                      'Delete'
                    )}
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
