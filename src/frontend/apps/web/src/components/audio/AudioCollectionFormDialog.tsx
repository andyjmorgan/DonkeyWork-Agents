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
import { audioCollections, type AudioCollection } from '@donkeywork/api-client'

interface AudioCollectionFormDialogProps {
  open: boolean
  mode: 'create' | 'edit'
  initial?: AudioCollection | null
  onClose: () => void
  onSaved: (collection: AudioCollection) => void
}

export function AudioCollectionFormDialog({
  open,
  mode,
  initial,
  onClose,
  onSaved,
}: AudioCollectionFormDialogProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [defaultVoice, setDefaultVoice] = useState('')
  const [defaultModel, setDefaultModel] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setName(initial?.name ?? '')
    setDescription(initial?.description ?? '')
    setDefaultVoice(initial?.defaultVoice ?? '')
    setDefaultModel(initial?.defaultModel ?? '')
    setError(null)
  }, [open, initial])

  const handleSubmit = async () => {
    if (!name.trim()) {
      setError('Name is required.')
      return
    }

    setSaving(true)
    setError(null)
    try {
      const result =
        mode === 'create'
          ? await audioCollections.create({
              name: name.trim(),
              description: description.trim() || undefined,
              defaultVoice: defaultVoice.trim() || undefined,
              defaultModel: defaultModel.trim() || undefined,
            })
          : await audioCollections.update(initial!.id, {
              name: name.trim(),
              description: description.trim(),
              defaultVoice: defaultVoice.trim(),
              defaultModel: defaultModel.trim(),
            })
      onSaved(result)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save collection.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{mode === 'create' ? 'New Collection' : 'Edit Collection'}</DialogTitle>
          <DialogDescription>
            Collections group related recordings as ordered chapters (e.g. "Daily AI News", an audiobook).
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-2">
            <Label htmlFor="collection-name">Name</Label>
            <Input
              id="collection-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Daily AI News"
              autoFocus
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="collection-description">Description</Label>
            <Textarea
              id="collection-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Short description of what lives here."
              rows={2}
            />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="default-voice">Default voice</Label>
              <Input
                id="default-voice"
                value={defaultVoice}
                onChange={(e) => setDefaultVoice(e.target.value)}
                placeholder="alloy / Kore"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="default-model">Default model</Label>
              <Input
                id="default-model"
                value={defaultModel}
                onChange={(e) => setDefaultModel(e.target.value)}
                placeholder="tts-1 / gemini-2.5-flash-preview-tts"
              />
            </div>
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={saving}>
            {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : mode === 'create' ? 'Create' : 'Save'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
