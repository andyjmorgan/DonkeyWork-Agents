import { useState, useEffect } from 'react'
import { Loader2 } from 'lucide-react'
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@donkeywork/ui'
import {
  audioCollections,
  tts,
  type AudioCollection,
  type TtsRecording,
} from '@donkeywork/api-client'

interface MoveRecordingDialogProps {
  recording: TtsRecording | null
  onClose: () => void
  onMoved: (recording: TtsRecording) => void
}

const UNFILED = '__unfiled__'

export function MoveRecordingDialog({ recording, onClose, onMoved }: MoveRecordingDialogProps) {
  const open = !!recording
  const [collections, setCollections] = useState<AudioCollection[]>([])
  const [loading, setLoading] = useState(false)
  const [target, setTarget] = useState<string>(UNFILED)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setTarget(recording?.collectionId ?? UNFILED)
    setError(null)
    setLoading(true)
    audioCollections
      .list(0, 100)
      .then((r) => setCollections(r.items))
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load collections'))
      .finally(() => setLoading(false))
  }, [open, recording])

  const handleSave = async () => {
    if (!recording) return
    setSaving(true)
    setError(null)
    try {
      const next = await tts.moveRecording(recording.id, {
        collectionId: target === UNFILED ? null : target,
      })
      onMoved(next)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to move recording')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(o) => !o && !saving && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Move "{recording?.name}"</DialogTitle>
          <DialogDescription>
            Move this recording into another collection, or set it as unfiled.
          </DialogDescription>
        </DialogHeader>
        <div className="py-2 space-y-3">
          <div className="space-y-2">
            <Label>Destination</Label>
            {loading ? (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading collections…
              </div>
            ) : (
              <Select value={target} onValueChange={setTarget}>
                <SelectTrigger>
                  <SelectValue placeholder="Select a collection" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={UNFILED}>Unfiled (no collection)</SelectItem>
                  {collections.map((c) => (
                    <SelectItem key={c.id} value={c.id}>
                      {c.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={saving || loading}>
            {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Move'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
