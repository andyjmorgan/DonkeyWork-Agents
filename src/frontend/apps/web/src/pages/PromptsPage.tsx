import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2, FileText } from 'lucide-react'
import {
  Button,
  Badge,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
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
  type PromptSummary,
  type PromptType,
  type CreatePromptRequest,
  type UpdatePromptRequest,
  type PromptDetails,
} from '@donkeywork/api-client'

interface PromptFormData {
  name: string
  description: string
  content: string
  promptType: PromptType
}

const defaultFormData: PromptFormData = {
  name: '',
  description: '',
  content: '',
  promptType: 'User',
}

export function PromptsPage() {
  const [isLoading, setIsLoading] = useState(true)
  const [items, setItems] = useState<PromptSummary[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState<PromptFormData>(defaultFormData)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    loadItems()
  }, [])

  const loadItems = async () => {
    try {
      setIsLoading(true)
      const data = await promptsApi.list()
      setItems(data)
    } catch (error) {
      console.error('Failed to load prompts:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const openCreateDialog = () => {
    setEditingId(null)
    setFormData(defaultFormData)
    setDialogOpen(true)
  }

  const openEditDialog = async (id: string) => {
    try {
      const details: PromptDetails = await promptsApi.get(id)
      setEditingId(id)
      setFormData({
        name: details.name,
        description: details.description || '',
        content: details.content,
        promptType: details.promptType,
      })
      setDialogOpen(true)
    } catch (error) {
      console.error('Failed to load prompt:', error)
    }
  }

  const handleSave = async () => {
    try {
      setIsSaving(true)
      if (editingId) {
        const update: UpdatePromptRequest = {
          name: formData.name,
          description: formData.description || undefined,
          content: formData.content,
          promptType: formData.promptType,
        }
        await promptsApi.update(editingId, update)
      } else {
        const create: CreatePromptRequest = {
          name: formData.name,
          description: formData.description || undefined,
          content: formData.content,
          promptType: formData.promptType,
        }
        await promptsApi.create(create)
      }
      setDialogOpen(false)
      await loadItems()
    } catch (error) {
      console.error('Failed to save prompt:', error)
    } finally {
      setIsSaving(false)
    }
  }

  const handleDelete = async (id: string, name: string) => {
    if (!window.confirm(`Are you sure you want to delete "${name}"? This action cannot be undone.`)) {
      return
    }
    try {
      setDeletingId(id)
      await promptsApi.delete(id)
      await loadItems()
    } catch (error) {
      console.error('Failed to delete prompt:', error)
    } finally {
      setDeletingId(null)
    }
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Prompts</h1>
          <p className="text-muted-foreground">Create and manage reusable prompt templates</p>
        </div>
        <Button onClick={openCreateDialog}>
          <Plus className="h-4 w-4" />
          <span className="hidden sm:inline">New Prompt</span>
        </Button>
      </div>

      {items.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <FileText className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No prompts yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Get started by creating your first prompt template
          </p>
          <Button className="mt-4" onClick={openCreateDialog}>
            <Plus className="h-4 w-4" />
            Create Prompt
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile view - card layout */}
          <div className="space-y-3 md:hidden">
            {items.map((item) => (
              <div key={item.id} className="rounded-lg border border-border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="text-sm">
                      <span className="text-muted-foreground">Name: </span>
                      <span className="font-medium">{item.name}</span>
                    </div>
                    {item.description && (
                      <div className="text-sm">
                        <span className="text-muted-foreground">Description: </span>
                        <span>{item.description}</span>
                      </div>
                    )}
                    <div className="text-sm flex items-center gap-2">
                      <span className="text-muted-foreground">Type: </span>
                      {item.promptType === 'System' ? (
                        <Badge variant="secondary">System</Badge>
                      ) : (
                        <Badge variant="outline">User</Badge>
                      )}
                    </div>
                    <div className="text-sm">
                      <span className="text-muted-foreground">Created: </span>
                      <span>{new Date(item.createdAt).toLocaleDateString()}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => openEditDialog(item.id)}
                    >
                      <Edit className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive"
                      onClick={() => handleDelete(item.id, item.name)}
                      disabled={deletingId === item.id}
                    >
                      {deletingId === item.id ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
                    </Button>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop view - table layout */}
          <div className="hidden md:block rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell className="font-medium">{item.name}</TableCell>
                    <TableCell className="max-w-md">
                      {item.description || (
                        <span className="text-muted-foreground italic">No description</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {item.promptType === 'System' ? (
                        <Badge variant="secondary">System</Badge>
                      ) : (
                        <Badge variant="outline">User</Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(item.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8"
                          onClick={() => openEditDialog(item.id)}
                        >
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-destructive hover:text-destructive"
                          onClick={() => handleDelete(item.id, item.name)}
                          disabled={deletingId === item.id}
                        >
                          {deletingId === item.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Trash2 className="h-4 w-4" />
                          )}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </>
      )}

      {/* Create/Edit Dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{editingId ? 'Edit Prompt' : 'Create Prompt'}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="prompt-name">Name</Label>
              <Input
                id="prompt-name"
                placeholder="e.g. Code Review Assistant"
                value={formData.name}
                onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="prompt-description">Description</Label>
              <Textarea
                id="prompt-description"
                placeholder="What does this prompt do?"
                rows={2}
                value={formData.description}
                onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="prompt-type">Type</Label>
              <Select
                value={formData.promptType}
                onValueChange={(val) => setFormData((prev) => ({ ...prev, promptType: val as PromptType }))}
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
                rows={8}
                className="font-mono text-sm"
                value={formData.content}
                onChange={(e) => setFormData((prev) => ({ ...prev, content: e.target.value }))}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleSave}
              disabled={isSaving || !formData.name.trim() || !formData.content.trim()}
            >
              {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
              {editingId ? 'Save' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
