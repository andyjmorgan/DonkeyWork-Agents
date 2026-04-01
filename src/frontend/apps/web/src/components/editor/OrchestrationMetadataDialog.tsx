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
  friendlyName: string
  onSave: (name: string, description: string, friendlyName: string) => void
}

function OrchestrationMetadataDialogContent({
  name,
  description,
  friendlyName,
  onSave,
  onClose,
}: {
  name: string
  description: string
  friendlyName: string
  onSave: (name: string, description: string, friendlyName: string) => void
  onClose: () => void
}) {
  const [localName, setLocalName] = useState(name)
  const [localDescription, setLocalDescription] = useState(description)
  const [localFriendlyName, setLocalFriendlyName] = useState(friendlyName)
  const [error, setError] = useState('')

  const validateName = (value: string): boolean => {
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
      onSave(localName, localDescription, localFriendlyName)
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
        <DialogTitle>Orchestration Settings</DialogTitle>
        <DialogDescription>
          Update the name, description, and display name for this orchestration.
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
          <Label htmlFor="friendlyName">Display Name</Label>
          <Input
            id="friendlyName"
            placeholder="My Orchestration"
            value={localFriendlyName}
            onChange={(e) => setLocalFriendlyName(e.target.value)}
          />
          <p className="text-xs text-muted-foreground">
            Shown when this orchestration is used as a tool or exposed via MCP
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
  friendlyName,
  onSave,
}: OrchestrationMetadataDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <OrchestrationMetadataDialogContent
          key={`${name}-${description}-${friendlyName}-${open}`}
          name={name}
          description={description}
          friendlyName={friendlyName}
          onSave={onSave}
          onClose={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  )
}
