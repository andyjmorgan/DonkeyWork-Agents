import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { FolderOpen, Loader2, Plus, Trash2, Pencil, Music } from 'lucide-react'
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@donkeywork/ui'
import { audioCollections, type AudioCollection } from '@donkeywork/api-client'
import { useAudioRecordingEventsStore } from '@donkeywork/stores'
import { AudioCollectionFormDialog } from '@/components/audio/AudioCollectionFormDialog'

const PAGE_SIZE = 20

export function AudioCollectionsPage() {
  const [collections, setCollections] = useState<AudioCollection[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [createOpen, setCreateOpen] = useState(false)
  const [editTarget, setEditTarget] = useState<AudioCollection | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<AudioCollection | null>(null)
  const [deleting, setDeleting] = useState(false)
  const revision = useAudioRecordingEventsStore((s) => s.revision)

  const load = async () => {
    setLoading(true)
    try {
      const response = await audioCollections.list(0, PAGE_SIZE)
      setCollections(response.items)
      setTotalCount(response.totalCount)
    } catch (error) {
      console.error('Failed to load audio collections:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [revision])

  const handleDelete = async () => {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await audioCollections.delete(deleteTarget.id)
      await load()
    } catch (error) {
      console.error('Failed to delete collection:', error)
    } finally {
      setDeleting(false)
      setDeleteTarget(null)
    }
  }

  return (
    <div className="container mx-auto max-w-5xl space-y-6 p-4 md:p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Collections</h1>
          <p className="text-sm text-muted-foreground">
            Audio folders — chapters of ordered recordings grouped together.
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          New Collection
        </Button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      ) : collections.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <FolderOpen className="h-12 w-12 text-muted-foreground/30 mb-4" />
          <h3 className="text-lg font-medium">No collections yet</h3>
          <p className="text-sm text-muted-foreground mt-1">
            Create a collection to group recordings like a daily news feed or an audiobook.
          </p>
          <Button onClick={() => setCreateOpen(true)} className="gap-2 mt-4">
            <Plus className="h-4 w-4" />
            Create your first collection
          </Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {collections.map((collection) => (
            <div
              key={collection.id}
              className="rounded-2xl border border-border bg-card p-5 space-y-3 hover:border-accent/30 transition-colors"
            >
              <div className="flex items-start justify-between gap-3">
                <Link to={`/audio-collections/${collection.id}`} className="flex-1 min-w-0 space-y-1">
                  <div className="flex items-center gap-2">
                    <Music className="h-4 w-4 text-pink-500 shrink-0" />
                    <div className="font-semibold truncate">{collection.name}</div>
                  </div>
                  {collection.description && (
                    <p className="text-xs text-muted-foreground line-clamp-2">{collection.description}</p>
                  )}
                </Link>
                <div className="flex gap-1 shrink-0">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={(e) => {
                      e.preventDefault()
                      setEditTarget(collection)
                    }}
                    className="text-muted-foreground hover:text-foreground"
                  >
                    <Pencil className="h-3.5 w-3.5" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={(e) => {
                      e.preventDefault()
                      setDeleteTarget(collection)
                    }}
                    className="text-muted-foreground hover:text-destructive"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </div>
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span>
                  {collection.recordingCount} {collection.recordingCount === 1 ? 'recording' : 'recordings'}
                </span>
                {collection.defaultVoice && <span>Voice: {collection.defaultVoice}</span>}
              </div>
            </div>
          ))}
        </div>
      )}

      {!loading && totalCount > collections.length && (
        <p className="text-xs text-muted-foreground text-center">
          Showing first {collections.length} of {totalCount}
        </p>
      )}

      <AudioCollectionFormDialog
        open={createOpen || !!editTarget}
        mode={editTarget ? 'edit' : 'create'}
        initial={editTarget}
        onClose={() => {
          setCreateOpen(false)
          setEditTarget(null)
        }}
        onSaved={() => {
          setCreateOpen(false)
          setEditTarget(null)
          load()
        }}
      />

      <Dialog open={!!deleteTarget} onOpenChange={() => setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Collection</DialogTitle>
            <DialogDescription>
              Delete <span className="font-medium">{deleteTarget?.name}</span>? The recordings inside stay
              in your library — they just become unfiled. This cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)}>Cancel</Button>
            <Button variant="destructive" onClick={handleDelete} disabled={deleting}>
              {deleting ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Delete'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
