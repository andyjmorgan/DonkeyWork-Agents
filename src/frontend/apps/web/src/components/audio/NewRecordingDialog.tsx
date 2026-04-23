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
  Input,
  Label,
  Textarea,
} from '@donkeywork/ui'
import { tts, type AudioCollection } from '@donkeywork/api-client'

interface NewRecordingDialogProps {
  open: boolean
  collection: AudioCollection
  onClose: () => void
  onStarted: () => void
}

export function NewRecordingDialog({ open, collection, onClose, onStarted }: NewRecordingDialogProps) {
  const [name, setName] = useState('')
  const [chapterTitle, setChapterTitle] = useState('')
  const [text, setText] = useState('')
  const [voice, setVoice] = useState('')
  const [model, setModel] = useState('')
  const [instructions, setInstructions] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setName('')
    setChapterTitle('')
    setText('')
    setVoice(collection.defaultVoice ?? '')
    setModel(collection.defaultModel ?? '')
    setInstructions('')
    setError(null)
  }, [open, collection])

  const handleSubmit = async () => {
    if (!name.trim()) {
      setError('Name is required.')
      return
    }
    if (!text.trim()) {
      setError('Text is required.')
      return
    }
    if (!model.trim()) {
      setError('Model is required (set a default on the collection or fill it in here).')
      return
    }
    if (!voice.trim()) {
      setError('Voice is required.')
      return
    }

    setSubmitting(true)
    setError(null)
    try {
      await tts.startGeneration({
        text: text.trim(),
        name: name.trim(),
        model: model.trim(),
        voice: voice.trim(),
        instructions: instructions.trim() || undefined,
        collectionId: collection.id,
        chapterTitle: chapterTitle.trim() || undefined,
      })
      onStarted()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start generation.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(o) => !o && !submitting && onClose()}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>New Recording in "{collection.name}"</DialogTitle>
          <DialogDescription>
            Long text will be split into chunks and stitched automatically. Status updates live.
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="rec-name">Name</Label>
              <Input
                id="rec-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Daily AI News - 2026-04-23"
                autoFocus
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="rec-chapter">Chapter title (optional)</Label>
              <Input
                id="rec-chapter"
                value={chapterTitle}
                onChange={(e) => setChapterTitle(e.target.value)}
                placeholder="Episode 42"
              />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="rec-text">Text</Label>
            <Textarea
              id="rec-text"
              value={text}
              onChange={(e) => setText(e.target.value)}
              placeholder="Paste the script or article — markdown is respected on chunk boundaries."
              rows={8}
            />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="rec-model">Model</Label>
              <Input
                id="rec-model"
                value={model}
                onChange={(e) => setModel(e.target.value)}
                placeholder="tts-1 / gemini-2.5-flash-preview-tts"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="rec-voice">Voice</Label>
              <Input
                id="rec-voice"
                value={voice}
                onChange={(e) => setVoice(e.target.value)}
                placeholder="alloy / Kore"
              />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="rec-instructions">Voice instructions (optional)</Label>
            <Textarea
              id="rec-instructions"
              value={instructions}
              onChange={(e) => setInstructions(e.target.value)}
              placeholder="Speak warmly with moderate pacing."
              rows={2}
            />
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={submitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={submitting}>
            {submitting ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Generate'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
