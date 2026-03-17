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

interface SkillRenameDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  currentName: string
  onRename: (newName: string) => Promise<void>
}

export function SkillRenameDialog({ open, onOpenChange, currentName, onRename }: SkillRenameDialogProps) {
  const [newName, setNewName] = useState(currentName)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      setNewName(currentName)
      setError(null)
    }
  }, [open, currentName])

  const validate = (name: string): string | null => {
    if (!name.trim()) return 'Name is required'
    if (!NAME_PATTERN.test(name)) return 'Name must start with a letter or number and contain only letters, numbers, dots, hyphens, or underscores'
    return null
  }

  const handleSubmit = async () => {
    const validationError = validate(newName)
    if (validationError) {
      setError(validationError)
      return
    }
    if (newName === currentName) {
      onOpenChange(false)
      return
    }

    try {
      setSubmitting(true)
      await onRename(newName)
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Rename failed')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Rename</DialogTitle>
          <DialogDescription>Enter a new name for "{currentName}"</DialogDescription>
        </DialogHeader>
        <div className="space-y-2">
          <Label htmlFor="rename-input">New name</Label>
          <Input
            id="rename-input"
            value={newName}
            onChange={(e) => {
              setNewName(e.target.value)
              setError(null)
            }}
            onKeyDown={(e) => {
              if (e.key === 'Enter') handleSubmit()
            }}
            autoFocus
          />
          {error && <p className="text-sm text-red-500">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={submitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={submitting}>
            {submitting ? 'Renaming...' : 'Rename'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
