import { useState } from 'react'
import { Loader2 } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Button,
  Input,
  Label,
  Textarea,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@donkeywork/ui'
import {
  prompts as promptsApi,
  type PromptType,
  type CreatePromptRequest,
} from '@donkeywork/api-client'

interface CreatePromptDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated?: (prompt: { id: string; name: string }) => void
}

export function CreatePromptDialog({ open, onOpenChange, onCreated }: CreatePromptDialogProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [content, setContent] = useState('')
  const [promptType, setPromptType] = useState<PromptType>('System')
  const [isSaving, setIsSaving] = useState(false)

  const resetForm = () => {
    setName('')
    setDescription('')
    setContent('')
    setPromptType('System')
  }

  const handleOpenChange = (nextOpen: boolean) => {
    if (!nextOpen) resetForm()
    onOpenChange(nextOpen)
  }

  const handleSave = async () => {
    try {
      setIsSaving(true)
      const request: CreatePromptRequest = {
        name,
        description: description || undefined,
        content,
        promptType,
      }
      const created = await promptsApi.create(request)
      resetForm()
      onOpenChange(false)
      onCreated?.({ id: created.id, name: created.name })
    } catch (error) {
      console.error('Failed to create prompt:', error)
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>Create Prompt</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label htmlFor="prompt-name">Name</Label>
            <Input
              id="prompt-name"
              placeholder="e.g. Code Review Assistant"
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="prompt-description">Description</Label>
            <Textarea
              id="prompt-description"
              placeholder="What does this prompt do?"
              rows={2}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="prompt-type">Type</Label>
            <Select
              value={promptType}
              onValueChange={(val) => setPromptType(val as PromptType)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="System">System</SelectItem>
                <SelectItem value="User">User</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="prompt-content">Content</Label>
            <Textarea
              id="prompt-content"
              placeholder="Enter the prompt content..."
              rows={16}
              className="font-mono text-sm"
              value={content}
              onChange={(e) => setContent(e.target.value)}
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => handleOpenChange(false)}>
            Cancel
          </Button>
          <Button
            onClick={handleSave}
            disabled={isSaving || !name.trim() || !content.trim()}
          >
            {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            Create
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
