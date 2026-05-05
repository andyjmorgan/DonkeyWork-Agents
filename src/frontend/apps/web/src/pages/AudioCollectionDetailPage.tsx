import { useState, useEffect, useCallback } from 'react'
import { useParams, Link } from 'react-router-dom'
import {
  ChevronLeft,
  FolderOpen,
  Loader2,
  Music,
  Plus,
  Trash2,
  Pencil,
  CircleAlert,
  Hourglass,
  CheckCircle2,
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
import { AudioPlayer } from '@/components/audio/AudioPlayer'
import { AudioCollectionFormDialog } from '@/components/audio/AudioCollectionFormDialog'
import { NewRecordingDialog } from '@/components/audio/NewRecordingDialog'

export function AudioCollectionDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [collection, setCollection] = useState<AudioCollection | null>(null)
  const [recordings, setRecordings] = useState<TtsRecording[]>([])
  const [loading, setLoading] = useState(true)
  const [editOpen, setEditOpen] = useState(false)
  const [newOpen, setNewOpen] = useState(false)
  const [deleteRecordingId, setDeleteRecordingId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const revision = useAudioRecordingEventsStore((s) => s.revision)
  const lastCollectionId = useAudioRecordingEventsStore((s) => s.lastCollectionId)

  const load = useCallback(async () => {
    if (!id) return
    setLoading(true)
    try {
      const [coll, recs] = await Promise.all([
        audioCollections.get(id),
        audioCollections.listRecordings(id, 0, 100),
      ])
      setCollection(coll)
      setRecordings(recs.items)
    } catch (error) {
      console.error('Failed to load collection:', error)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    load()
  }, [load])

  useEffect(() => {
    if (!id) return
    // Refresh whenever any audio recording updates (covers move-in/move-out)
    // or specifically when the update belongs to this collection.
    if (revision > 0 && (lastCollectionId === id || lastCollectionId === null)) {
      load()
    }
  }, [revision, lastCollectionId, id, load])

  const handleDeleteRecording = async () => {
    if (!deleteRecordingId) return
    setDeleting(true)
    try {
      await tts.deleteRecording(deleteRecordingId)
      if (expandedId === deleteRecordingId) setExpandedId(null)
      await load()
    } catch (error) {
      console.error('Failed to delete recording:', error)
    } finally {
      setDeleting(false)
      setDeleteRecordingId(null)
    }
  }

  if (loading && !collection) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!collection) {
    return (
      <div className="container mx-auto max-w-5xl p-4 md:p-6">
        <Link to="/audio-collections" className="text-sm text-muted-foreground hover:text-foreground">
          ← Back to collections
        </Link>
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <FolderOpen className="h-12 w-12 text-muted-foreground/30 mb-4" />
          <h3 className="text-lg font-medium">Collection not found</h3>
        </div>
      </div>
    )
  }

  return (
    <div className="container mx-auto max-w-5xl space-y-6 p-4 md:p-6">
      <Link
        to="/audio-collections"
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
      >
        <ChevronLeft className="h-4 w-4" />
        Collections
      </Link>

      <div className="flex items-start justify-between gap-4">
        <div className="space-y-1 min-w-0">
          <h1 className="text-2xl font-bold truncate">{collection.name}</h1>
          {collection.description && (
            <p className="text-sm text-muted-foreground">{collection.description}</p>
          )}
          <div className="flex items-center gap-3 text-xs text-muted-foreground pt-1">
            <span>
              {recordings.length} {recordings.length === 1 ? 'chapter' : 'chapters'}
            </span>
            {collection.defaultVoice && <span>Voice: {collection.defaultVoice}</span>}
            {collection.defaultModel && <span>Model: {collection.defaultModel}</span>}
          </div>
        </div>
        <div className="flex gap-2 shrink-0">
          <Button variant="outline" onClick={() => setEditOpen(true)} className="gap-2">
            <Pencil className="h-4 w-4" />
            Edit
          </Button>
          <Button onClick={() => setNewOpen(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            New Recording
          </Button>
        </div>
      </div>

      {recordings.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center rounded-2xl border border-dashed border-border">
          <Music className="h-12 w-12 text-muted-foreground/30 mb-4" />
          <h3 className="text-lg font-medium">No recordings yet</h3>
          <p className="text-sm text-muted-foreground mt-1">
            Add the first chapter — it'll chunk long text automatically and stitch into one MP3.
          </p>
          <Button onClick={() => setNewOpen(true)} className="gap-2 mt-4">
            <Plus className="h-4 w-4" />
            New Recording
          </Button>
        </div>
      ) : (
        <div className="space-y-2">
          {recordings.map((recording, index) => (
            <RecordingRow
              key={recording.id}
              recording={recording}
              index={index}
              expanded={expandedId === recording.id}
              onToggle={() =>
                setExpandedId(expandedId === recording.id ? null : recording.id)
              }
              onDelete={() => setDeleteRecordingId(recording.id)}
            />
          ))}
        </div>
      )}

      <AudioCollectionFormDialog
        open={editOpen}
        mode="edit"
        initial={collection}
        onClose={() => setEditOpen(false)}
        onSaved={(next) => {
          setCollection(next)
          setEditOpen(false)
        }}
      />

      <NewRecordingDialog
        open={newOpen}
        collection={collection}
        onClose={() => setNewOpen(false)}
        onStarted={async () => {
          setNewOpen(false)
          await load()
        }}
      />

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
            <Button variant="destructive" onClick={handleDeleteRecording} disabled={deleting}>
              {deleting ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Delete'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

interface RecordingRowProps {
  recording: TtsRecording
  index: number
  expanded: boolean
  onToggle: () => void
  onDelete: () => void
}

function RecordingRow({ recording, index, expanded, onToggle, onDelete }: RecordingRowProps) {
  const title = recording.chapterTitle ?? recording.name
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
        <div className="w-8 text-center text-sm text-muted-foreground tabular-nums shrink-0">
          {recording.sequenceNumber ?? index + 1}
        </div>
        <StatusIcon status={recording.status} />
        <div className="flex-1 min-w-0">
          <div className="font-medium truncate">{title}</div>
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
            {ready && <span>{formatSize(recording.sizeBytes)}</span>}
          </div>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={(e) => {
            e.stopPropagation()
            onDelete()
          }}
          className="text-muted-foreground hover:text-destructive shrink-0"
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      </div>
      {expanded && ready && (
        <div className="p-3 pt-0">
          <AudioPlayer recordingId={recording.id} name={title} transcript={recording.transcript} />
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
      return <Loader2 className="h-4 w-4 animate-spin text-pink-500 shrink-0" />
    case 'Pending':
      return <Hourglass className="h-4 w-4 text-muted-foreground shrink-0" />
    case 'Failed':
      return <CircleAlert className="h-4 w-4 text-destructive shrink-0" />
  }
}

function formatSize(bytes: number) {
  if (!bytes) return ''
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
