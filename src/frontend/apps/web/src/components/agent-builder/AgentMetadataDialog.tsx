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

interface AgentMetadataDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  name: string
  description: string
  displayName: string
  icon: string
  onSave: (name: string, description: string, displayName: string, icon: string) => void
}

function AgentMetadataDialogContent({
  name,
  description,
  displayName,
  icon,
  onSave,
  onClose,
}: {
  name: string
  description: string
  displayName: string
  icon: string
  onSave: (name: string, description: string, displayName: string, icon: string) => void
  onClose: () => void
}) {
  const [localName, setLocalName] = useState(name)
  const [localDescription, setLocalDescription] = useState(description)
  const [localDisplayName, setLocalDisplayName] = useState(displayName)
  const [localIcon, setLocalIcon] = useState(icon)
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
      onSave(localName, localDescription, localDisplayName, localIcon)
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
        <DialogTitle>Agent Identity</DialogTitle>
        <DialogDescription>
          Configure the agent's name, appearance, and description.
        </DialogDescription>
      </DialogHeader>

      <div className="space-y-4 py-4">
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="agent-name">Name</Label>
            <Input
              id="agent-name"
              placeholder="my_agent"
              value={localName}
              onChange={(e) => handleNameChange(e.target.value)}
              className={error ? 'border-destructive' : ''}
            />
            {error && <p className="text-sm text-destructive">{error}</p>}
            <p className="text-xs text-muted-foreground">
              Lowercase letters, numbers, hyphens, and underscores
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="agent-display-name">Display Name</Label>
            <Input
              id="agent-display-name"
              placeholder="My Agent"
              value={localDisplayName}
              onChange={(e) => setLocalDisplayName(e.target.value)}
            />
            <p className="text-xs text-muted-foreground">
              Shown in chat cards and the agent side panel
            </p>
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="agent-icon">Icon</Label>
          <Input
            id="agent-icon"
            value={localIcon}
            onChange={(e) => setLocalIcon(e.target.value)}
            placeholder="lucide icon name or image URL"
          />
          <p className="text-xs text-muted-foreground">
            A lucide icon name (e.g. "brain", "search") or an image URL (png, svg, ico)
          </p>
        </div>

        <div className="space-y-2">
          <Label htmlFor="agent-description">Description</Label>
          <Textarea
            id="agent-description"
            placeholder="Describe what this agent does..."
            value={localDescription}
            onChange={(e) => setLocalDescription(e.target.value)}
            rows={3}
          />
        </div>
      </div>

      <DialogFooter>
        <Button variant="outline" onClick={onClose}>
          Cancel
        </Button>
        <Button onClick={handleSave}>Save</Button>
      </DialogFooter>
    </>
  )
}

export function AgentMetadataDialog({
  open,
  onOpenChange,
  name,
  description,
  displayName,
  icon,
  onSave,
}: AgentMetadataDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[600px]">
        <AgentMetadataDialogContent
          key={`${name}-${description}-${displayName}-${icon}-${open}`}
          name={name}
          description={description}
          displayName={displayName}
          icon={icon}
          onSave={onSave}
          onClose={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  )
}
