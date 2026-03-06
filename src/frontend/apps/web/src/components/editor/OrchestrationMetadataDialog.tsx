import { useState } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Button,
  Input,
  Label,
  Textarea,
} from '@donkeywork/ui'

interface OrchestrationMetadataDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  name: string
  description: string
  onSave: (name: string, description: string) => void
}

// Inner component that resets when key changes
function OrchestrationMetadataDialogContent({
  name,
  description,
  onSave,
  onClose,
}: {
  name: string
  description: string
  onSave: (name: string, description: string) => void
  onClose: () => void
}) {
  const [localName, setLocalName] = useState(name)
  const [localDescription, setLocalDescription] = useState(description)
  const [error, setError] = useState('')

  const validateName = (value: string): boolean => {
    // Validate: a-z, 0-9, -, _ only
    const regex = /^[a-z0-9_-]+$/
    if (!value.trim()) {
      setError('Name is required')
      return false
    }
    if (!regex.test(value)) {
      setError('Name can only contain lowercase letters, numbers, hyphens, and underscores')
      return false
    }
    setError('')
    return true
  }

  const handleSave = () => {
    if (validateName(localName)) {
      onSave(localName, localDescription)
      onClose()
    }
  }

  const handleNameChange = (value: string) => {
    setLocalName(value)
    validateName(value)
  }

  return (
    <>
      <DialogHeader>
        <DialogTitle>Orchestration Metadata</DialogTitle>
        <DialogDescription>
          Update the name and description for this orchestration.
        </DialogDescription>
      </DialogHeader>

      <div className="space-y-4 py-4">
        <div className="space-y-2">
          <Label htmlFor="name">Name</Label>
          <Input
            id="name"
            placeholder="my_orchestration"
            value={localName}
            onChange={(e) => handleNameChange(e.target.value)}
            className={error ? 'border-destructive' : ''}
          />
          {error && (
            <p className="text-sm text-destructive">{error}</p>
          )}
          <p className="text-xs text-muted-foreground">
            Only lowercase letters, numbers, hyphens, and underscores
          </p>
        </div>

        <div className="space-y-2">
          <Label htmlFor="description">Description</Label>
          <Textarea
            id="description"
            placeholder="Describe what this orchestration does..."
            value={localDescription}
            onChange={(e) => setLocalDescription(e.target.value)}
            rows={4}
          />
        </div>
      </div>

      <DialogFooter>
        <Button variant="outline" onClick={onClose}>
          Cancel
        </Button>
        <Button onClick={handleSave}>
          Save
        </Button>
      </DialogFooter>
    </>
  )
}

export function OrchestrationMetadataDialog({
  open,
  onOpenChange,
  name,
  description,
  onSave,
}: OrchestrationMetadataDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        {/* Use key to reset internal state when props change */}
        <OrchestrationMetadataDialogContent
          key={`${name}-${description}-${open}`}
          name={name}
          description={description}
          onSave={onSave}
          onClose={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  )
}
