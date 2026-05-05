import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import {
  FolderOpen,
  Loader2,
  Plus,
  Trash2,
  Pencil,
  Music,
  FolderInput,
  Volume2,
  Circle,
  CheckCircle2,
  CircleAlert,
  Hourglass,
} from 'lucide-react'
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Progress,
} from '@donkeywork/ui'
import {
  audioCollections,
  tts,
  type AudioCollection,
  type TtsRecording,
  type TtsRecordingStatus,
} from '@donkeywork/api-client'
import { useAudioRecordingEventsStore } from '@donkeywork/stores'
import { AudioCollectionFormDialog } from '@/components/audio/AudioCollectionFormDialog'
import { AudioPlayer } from '@/components/audio/AudioPlayer'
import { MoveRecordingDialog } from '@/components/audio/MoveRecordingDialog'

const COLLECTIONS_PAGE_SIZE = 20
const UNFILED_PAGE_SIZE = 50

export function AudioCollectionsPage() {
  const [collections, setCollections] = useState<AudioCollection[]>([])
  const [collectionsTotal, setCollectionsTotal] = useState(0)
  const [collectionsLoading, setCollectionsLoading] = useState(true)

  const [unfiled, setUnfiled] = useState<TtsRecording[]>([])
  const [unfiledTotal, setUnfiledTotal] = useState(0)
  const [unfiledLoading, setUnfiledLoading] = useState(true)
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const [createOpen, setCreateOpen] = useState(false)
  const [editTarget, setEditTarget] = useState<AudioCollection | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<AudioCollection | null>(null)
  const [deleting, setDeleting] = useState(false)

  const [moveTarget, setMoveTarget] = useState<TtsRecording | null>(null)
  const [deleteRecordingId, setDeleteRecordingId] = useState<string | null>(null)
  const [deletingRecording, setDeletingRecording] = useState(false)

  const revision = useAudioRecordingEventsStore((s) => s.revision)

  const loadCollections = async () => {
    setCollectionsLoading(true)
    try {
      const response = await audioCollections.list(0, COLLECTIONS_PAGE_SIZE)
      setCollections(response.items)
      setCollectionsTotal(response.totalCount)
    } catch (error) {
      console.error('Failed to load audio collections:', error)
    } finally {
      setCollectionsLoading(false)
    }
  }

  const loadUnfiled = async () => {
    setUnfiledLoading(true)
    try {
      const response = await tts.listRecordings(0, UNFILED_PAGE_SIZE, true)
      setUnfiled(response.items)
      setUnfiledTotal(response.totalCount)
    } catch (error) {
      console.error('Failed to load unfiled recordings:', error)
    } finally {
      setUnfiledLoading(false)
    }
  }

  useEffect(() => {
    loadCollections()
    loadUnfiled()
  }, [revision])

  const handleDeleteCollection = async () => {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await audioCollections.delete(deleteTarget.id)
      await loadCollections()
      await loadUnfiled()
    } catch (error) {
      console.error('Failed to delete collection:', error)
    } finally {
      setDeleting(false)
      setDeleteTarget(null)
    }
  }

  const handleDeleteRecording = async () => {
    if (!deleteRecordingId) return
    setDeletingRecording(true)
    try {
      await tts.deleteRecording(deleteRecordingId)
      if (expandedId === deleteRecordingId) setExpandedId(null)
      await loadUnfiled()
    } catch (error) {
      console.error('Failed to delete recording:', error)
    } finally {
      setDeletingRecording(false)
      setDeleteRecordingId(null)
    }
  }

  return (
    <div className="container mx-auto max-w-5xl space-y-8 p-4 md:p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Listen</h1>
          <p className="text-sm text-muted-foreground">
            Collections of AI-generated mini-podcasts. Group recordings into chapters, or leave them loose below.
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          New Collection
        </Button>
      </div>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Collections</h2>
        {collectionsLoading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : collections.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 text-center rounded-2xl border border-dashed border-border">
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
          <>
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
            {collectionsTotal > collections.length && (
              <p className="text-xs text-muted-foreground text-center">
                Showing first {collections.length} of {collectionsTotal}
              </p>
            )}
          </>
        )}
      </section>

      <section className="space-y-3">
        <div className="flex items-baseline justify-between">
          <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Loose recordings</h2>
          {unfiledTotal > 0 && (
            <span className="text-xs text-muted-foreground">
              {unfiledTotal} {unfiledTotal === 1 ? 'recording' : 'recordings'} not in any collection
            </span>
          )}
        </div>
        {unfiledLoading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : unfiled.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-8 text-center rounded-2xl border border-dashed border-border">
            <Volume2 className="h-10 w-10 text-muted-foreground/30 mb-3" />
            <p className="text-sm text-muted-foreground">
              No loose recordings. Recordings created without a collection show up here.
            </p>
          </div>
        ) : (
          <div className="space-y-2">
            {unfiled.map((recording) => (
              <UnfiledRow
                key={recording.id}
                recording={recording}
                expanded={expandedId === recording.id}
                onToggle={() =>
                  setExpandedId(expandedId === recording.id ? null : recording.id)
                }
                onMove={() => setMoveTarget(recording)}
                onDelete={() => setDeleteRecordingId(recording.id)}
              />
            ))}
            {unfiledTotal > unfiled.length && (
              <p className="text-xs text-muted-foreground text-center pt-2">
                Showing first {unfiled.length} of {unfiledTotal}
              </p>
            )}
          </div>
        )}
      </section>

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
          loadCollections()
        }}
      />

      <MoveRecordingDialog
        recording={moveTarget}
        onClose={() => setMoveTarget(null)}
        onMoved={() => {
          setMoveTarget(null)
          loadUnfiled()
          loadCollections()
        }}
      />

      <Dialog open={!!deleteTarget} onOpenChange={() => setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Collection</DialogTitle>
            <DialogDescription>
              Delete <span className="font-medium">{deleteTarget?.name}</span>? The recordings inside stay
              in your library — they just become loose. This cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)}>Cancel</Button>
            <Button variant="destructive" onClick={handleDeleteCollection} disabled={deleting}>
              {deleting ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Delete'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={!!deleteRecordingId} onOpenChange={() => setDeleteRecordingId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Recording</DialogTitle>
            <DialogDescription>
              This will permanently delete the recording and its audio file. This cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteRecordingId(null)}>Cancel</Button>
            <Button variant="destructive" onClick={handleDeleteRecording} disabled={deletingRecording}>
              {deletingRecording ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Delete'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

interface UnfiledRowProps {
  recording: TtsRecording
  expanded: boolean
  onToggle: () => void
  onMove: () => void
  onDelete: () => void
}

function UnfiledRow({ recording, expanded, onToggle, onMove, onDelete }: UnfiledRowProps) {
  const ready = recording.status === 'Ready'

  return (
    <div
      className={`rounded-xl border transition-colors ${
        expanded
          ? 'border-pink-500/40 bg-pink-500/[0.04] ring-1 ring-pink-500/20'
          : 'border-border bg-card'
      }`}
    >
      <div
        className={`flex items-center gap-3 p-3 ${ready ? 'cursor-pointer hover:bg-secondary/30' : ''}`}
        onClick={ready ? onToggle : undefined}
      >
        <StatusIcon status={recording.status} />
        <div className="flex-1 min-w-0">
          <div className="font-medium truncate">{recording.name}</div>
          <div className="text-xs text-muted-foreground flex items-center gap-3">
            {recording.status === 'Generating' && (
              <span className="flex items-center gap-2">
                Generating
                <Progress value={recording.progress * 100} className="h-1 w-24" />
              </span>
            )}
            {recording.status === 'Pending' && <span>Pending…</span>}
            {recording.status === 'Failed' && (
              <span className="text-destructive" title={recording.errorMessage ?? undefined}>
                Failed: {recording.errorMessage ?? 'unknown error'}
              </span>
            )}
            {ready && recording.voice && <span>Voice: {recording.voice}</span>}
            {ready && recording.model && <span>Model: {recording.model}</span>}
            {ready && <span>{formatSize(recording.sizeBytes)}</span>}
          </div>
        </div>
        <div className="flex gap-1 shrink-0">
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation()
              onMove()
            }}
            className="text-muted-foreground hover:text-foreground"
            title="Move to collection"
          >
            <FolderInput className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation()
              onDelete()
            }}
            className="text-muted-foreground hover:text-destructive"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      </div>
      {expanded && ready && (
        <div className="p-3 pt-0">
          <AudioPlayer recordingId={recording.id} name={recording.name} transcript={recording.transcript} />
        </div>
      )}
    </div>
  )
}

function StatusIcon({ status }: { status: TtsRecordingStatus }) {
  switch (status) {
    case 'Ready':
      return <CheckCircle2 className="h-4 w-4 text-emerald-500 shrink-0" />
    case 'Generating':
      return <Hourglass className="h-4 w-4 text-pink-500 shrink-0" />
    case 'Failed':
      return <CircleAlert className="h-4 w-4 text-destructive shrink-0" />
    default:
      return <Circle className="h-4 w-4 text-muted-foreground shrink-0" />
  }
}

function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
