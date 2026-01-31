import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2, FileText, StickyNote } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { notes, type Note, type CreateNoteRequest } from '@/lib/api'

export function NotesPage() {
  const [isLoading, setIsLoading] = useState(true)
  const [notesList, setNotesList] = useState<Note[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [editingNote, setEditingNote] = useState<Note | null>(null)
  const [viewingNote, setViewingNote] = useState<Note | null>(null)
  const [formData, setFormData] = useState<CreateNoteRequest>({
    title: '',
    content: '',
  })

  useEffect(() => {
    loadNotes()
  }, [])

  const loadNotes = async () => {
    try {
      setIsLoading(true)
      const data = await notes.listStandalone()
      setNotesList(data)
    } catch (error) {
      console.error('Failed to load notes:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleCreate = async () => {
    if (!formData.title.trim()) return

    try {
      setIsCreating(true)
      await notes.create(formData)
      setIsDialogOpen(false)
      setFormData({ title: '', content: '' })
      await loadNotes()
    } catch (error) {
      console.error('Failed to create note:', error)
    } finally {
      setIsCreating(false)
    }
  }

  const handleUpdate = async () => {
    if (!editingNote || !formData.title.trim()) return

    try {
      setIsCreating(true)
      await notes.update(editingNote.id, {
        title: formData.title,
        content: formData.content,
        sortOrder: editingNote.sortOrder,
      })
      setEditingNote(null)
      setFormData({ title: '', content: '' })
      await loadNotes()
    } catch (error) {
      console.error('Failed to update note:', error)
    } finally {
      setIsCreating(false)
    }
  }

  const handleDelete = async (noteId: string, noteTitle: string) => {
    if (!window.confirm(`Are you sure you want to delete "${noteTitle}"?`)) {
      return
    }

    try {
      setDeletingId(noteId)
      await notes.delete(noteId)
      await loadNotes()
    } catch (error) {
      console.error('Failed to delete note:', error)
    } finally {
      setDeletingId(null)
    }
  }

  const openEditDialog = (note: Note) => {
    setEditingNote(note)
    setFormData({
      title: note.title,
      content: note.content,
    })
  }

  const closeDialog = () => {
    setIsDialogOpen(false)
    setEditingNote(null)
    setFormData({ title: '', content: '' })
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
          <h1 className="text-2xl font-bold">Notes</h1>
          <p className="text-muted-foreground">
            Your standalone notes and ideas
          </p>
        </div>
        <Button onClick={() => setIsDialogOpen(true)}>
          <Plus className="h-4 w-4" />
          <span className="hidden sm:inline">New Note</span>
        </Button>
      </div>

      {notesList.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <StickyNote className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No notes yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Create your first note to capture your ideas
          </p>
          <Button className="mt-4" onClick={() => setIsDialogOpen(true)}>
            <Plus className="h-4 w-4" />
            Create Note
          </Button>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {notesList.map((note) => (
            <div
              key={note.id}
              className="group rounded-lg border border-border bg-card p-4 hover:shadow-md transition-shadow cursor-pointer"
              onClick={() => setViewingNote(note)}
            >
              <div className="flex items-start justify-between gap-2">
                <div className="flex items-center gap-2 min-w-0">
                  <FileText className="h-5 w-5 text-muted-foreground shrink-0" />
                  <h3 className="font-medium truncate">{note.title}</h3>
                </div>
                <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity shrink-0">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7"
                    onClick={(e) => {
                      e.stopPropagation()
                      openEditDialog(note)
                    }}
                  >
                    <Edit className="h-3 w-3" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7 text-destructive"
                    onClick={(e) => {
                      e.stopPropagation()
                      handleDelete(note.id, note.title)
                    }}
                    disabled={deletingId === note.id}
                  >
                    {deletingId === note.id ? (
                      <Loader2 className="h-3 w-3 animate-spin" />
                    ) : (
                      <Trash2 className="h-3 w-3" />
                    )}
                  </Button>
                </div>
              </div>
              {note.content && (
                <p className="mt-2 text-sm text-muted-foreground line-clamp-3">
                  {note.content}
                </p>
              )}
              <div className="mt-3 flex items-center gap-2">
                {note.tags.length > 0 && (
                  <div className="flex items-center gap-1 flex-wrap">
                    {note.tags.slice(0, 3).map((tag) => (
                      <Badge key={tag.id} variant="secondary" className="text-xs">
                        {tag.name}
                      </Badge>
                    ))}
                    {note.tags.length > 3 && (
                      <Badge variant="secondary" className="text-xs">
                        +{note.tags.length - 3}
                      </Badge>
                    )}
                  </div>
                )}
                <span className="text-xs text-muted-foreground ml-auto">
                  {new Date(note.createdAt).toLocaleDateString()}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create/Edit Note Dialog */}
      <Dialog open={isDialogOpen || !!editingNote} onOpenChange={closeDialog}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{editingNote ? 'Edit Note' : 'Create New Note'}</DialogTitle>
            <DialogDescription>
              {editingNote ? 'Update your note' : 'Add a new note to capture your ideas'}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="title">Title</Label>
              <Input
                id="title"
                value={formData.title}
                onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                placeholder="Note title"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="content">Content</Label>
              <Textarea
                id="content"
                value={formData.content || ''}
                onChange={(e) => setFormData({ ...formData, content: e.target.value })}
                placeholder="Write your note here (supports markdown)"
                rows={10}
                className="font-mono text-sm"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={closeDialog}>
              Cancel
            </Button>
            <Button
              onClick={editingNote ? handleUpdate : handleCreate}
              disabled={isCreating || !formData.title.trim()}
            >
              {isCreating ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
              {editingNote ? 'Update' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* View Note Dialog */}
      <Dialog open={!!viewingNote} onOpenChange={() => setViewingNote(null)}>
        <DialogContent className="max-w-2xl max-h-[80vh] overflow-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5" />
              {viewingNote?.title}
            </DialogTitle>
          </DialogHeader>
          <div className="py-4">
            {viewingNote?.content ? (
              <div className="prose prose-sm dark:prose-invert max-w-none whitespace-pre-wrap">
                {viewingNote.content}
              </div>
            ) : (
              <p className="text-muted-foreground italic">No content</p>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setViewingNote(null)}>
              Close
            </Button>
            <Button onClick={() => {
              if (viewingNote) {
                openEditDialog(viewingNote)
                setViewingNote(null)
              }
            }}>
              <Edit className="h-4 w-4" />
              Edit
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
