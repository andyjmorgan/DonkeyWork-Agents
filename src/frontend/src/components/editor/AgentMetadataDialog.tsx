import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'

interface AgentMetadataDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  name: string
  description: string
  onSave: (name: string, description: string) => void
}

export function AgentMetadataDialog({
  open,
  onOpenChange,
  name,
  description,
  onSave,
}: AgentMetadataDialogProps) {
  const [localName, setLocalName] = useState(name)
  const [localDescription, setLocalDescription] = useState(description)
  const [error, setError] = useState('')

  // Update local state when props change
  useEffect(() => {
    setLocalName(name)
    setLocalDescription(description)
    setError('')
  }, [name, description, open])

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
      onOpenChange(false)
    }
  }

  const handleNameChange = (value: string) => {
    setLocalName(value)
    validateName(value)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Agent Metadata</DialogTitle>
          <DialogDescription>
            Update the name and description for this agent.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label htmlFor="name">Name</Label>
            <Input
              id="name"
              placeholder="my_agent"
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
              placeholder="Describe what this agent does..."
              value={localDescription}
              onChange={(e) => setLocalDescription(e.target.value)}
              rows={4}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave}>
            Save
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
