import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  Button,
  Input,
  Label,
} from '@donkeywork/ui'

const NAME_PATTERN = /^[a-zA-Z0-9][a-zA-Z0-9._-]*$/

interface SkillNewItemDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  mode: 'file' | 'folder'
  onCreate: (name: string) => Promise<void>
}

export function SkillNewItemDialog({ open, onOpenChange, mode, onCreate }: SkillNewItemDialogProps) {
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      setName('')
      setError(null)
    }
  }, [open])

  const validate = (value: string): string | null => {
    if (!value.trim()) return 'Name is required'
    if (!NAME_PATTERN.test(value)) return 'Name must start with a letter or number and contain only letters, numbers, dots, hyphens, or underscores'
    return null
  }

  const handleSubmit = async () => {
    const validationError = validate(name)
    if (validationError) {
      setError(validationError)
      return
    }

    try {
      setSubmitting(true)
      await onCreate(name)
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : `Failed to create ${mode}`)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>New {mode === 'file' ? 'File' : 'Folder'}</DialogTitle>
          <DialogDescription>
            Enter a name for the new {mode}.
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-2">
          <Label htmlFor="new-item-input">Name</Label>
          <Input
            id="new-item-input"
            value={name}
            onChange={(e) => {
              setName(e.target.value)
              setError(null)
            }}
            onKeyDown={(e) => {
              if (e.key === 'Enter') handleSubmit()
            }}
            placeholder={mode === 'file' ? 'example.md' : 'my-folder'}
            autoFocus
          />
          {error && <p className="text-sm text-red-500">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={submitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={submitting}>
            {submitting ? 'Creating...' : 'Create'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
