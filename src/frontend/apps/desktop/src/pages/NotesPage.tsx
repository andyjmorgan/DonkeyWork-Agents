import { useState, useEffect, useMemo } from 'react'
import { Plus, Loader2, StickyNote, FileText, Trash2, Search, X } from 'lucide-react'
import { Button, Input, Card, Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@donkeywork/ui'
import { notes, type Note } from '@donkeywork/api-client'
import { NoteEditorPage } from './NoteEditorPage'

export function NotesPage() {
  const [isLoading, setIsLoading] = useState(true)
  const [isCreating, setIsCreating] = useState(false)
  const [notesList, setNotesList] = useState<Note[]>([])
  const [searchQuery, setSearchQuery] = useState('')
  const [editingNoteId, setEditingNoteId] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Note | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

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

  const filteredNotes = useMemo(() => {
    if (!searchQuery.trim()) return notesList
    const query = searchQuery.toLowerCase()
    return notesList.filter(
      (note) =>
        note.title.toLowerCase().includes(query) ||
        (note.content && note.content.toLowerCase().includes(query)) ||
        note.tags.some((tag) => tag.name.toLowerCase().includes(query))
    )
  }, [notesList, searchQuery])

  const createAndEditNote = async () => {
    const timestamp = new Date().toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    })

    try {
      setIsCreating(true)
      const newNote = await notes.create({
        title: `Untitled Note - ${timestamp}`,
        content: '',
      })
      setEditingNoteId(newNote.id)
    } catch (error) {
      console.error('Failed to create note:', error)
    } finally {
      setIsCreating(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteTarget) return

    try {
      setIsDeleting(true)
      await notes.delete(deleteTarget.id)
      setDeleteTarget(null)
      await loadNotes()
    } catch (error) {
      console.error('Failed to delete note:', error)
    } finally {
      setIsDeleting(false)
    }
  }

  const handleBackFromEditor = () => {
    setEditingNoteId(null)
    loadNotes()
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    })
  }

  const getPreview = (content?: string) => {
    if (!content) return 'No content'
    // Strip markdown formatting for preview
    const stripped = content
      .replace(/#{1,6}\s/g, '')
      .replace(/\*\*|__/g, '')
      .replace(/\*|_/g, '')
      .replace(/`{1,3}[^`]*`{1,3}/g, '')
      .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
      .replace(/!\[([^\]]*)\]\([^)]+\)/g, '$1')
      .replace(/^\s*[-*+]\s/gm, '')
      .replace(/^\s*\d+\.\s/gm, '')
      .replace(/\n+/g, ' ')
      .trim()
    return stripped.length > 120 ? stripped.slice(0, 120) + '...' : stripped
  }

  // Show editor view if a note is selected
  if (editingNoteId) {
    return <NoteEditorPage noteId={editingNoteId} onBack={handleBackFromEditor} />
  }

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="px-4 pt-4 pb-3 space-y-3 shrink-0">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-semibold text-foreground">Notes</h1>
            <p className="text-xs text-muted-foreground mt-0.5">
              {notesList.length} {notesList.length === 1 ? 'note' : 'notes'}
            </p>
          </div>
          <Button size="sm" onClick={createAndEditNote} disabled={isCreating}>
            {isCreating ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Plus className="h-4 w-4" />
            )}
            <span className="ml-1">New Note</span>
          </Button>
        </div>

        {/* Search bar */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search notes..."
            className="pl-9 pr-8"
          />
          {searchQuery && (
            <button
              onClick={() => setSearchQuery('')}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
      </div>

      {/* Notes list */}
      <div className="flex-1 overflow-y-auto px-4 pb-4">
        {filteredNotes.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
            <div className="rounded-full bg-muted p-4">
              <StickyNote className="h-8 w-8 text-muted-foreground" />
            </div>
            {searchQuery ? (
              <>
                <h3 className="mt-4 text-lg font-semibold">No matching notes</h3>
                <p className="mt-2 text-sm text-muted-foreground">
                  Try a different search term
                </p>
                <Button variant="outline" className="mt-4" onClick={() => setSearchQuery('')}>
                  Clear Search
                </Button>
              </>
            ) : (
              <>
                <h3 className="mt-4 text-lg font-semibold">No notes yet</h3>
                <p className="mt-2 text-sm text-muted-foreground">
                  Create your first note to capture your ideas
                </p>
                <Button className="mt-4" onClick={createAndEditNote} disabled={isCreating}>
                  {isCreating ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Plus className="h-4 w-4" />
                  )}
                  <span className="ml-1">Create Note</span>
                </Button>
              </>
            )}
          </div>
        ) : (
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {filteredNotes.map((note) => (
              <Card
                key={note.id}
                className="group cursor-pointer transition-colors hover:border-accent/30 hover:shadow-md"
                onClick={() => setEditingNoteId(note.id)}
              >
                <div className="p-4 space-y-2">
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex items-center gap-2 min-w-0">
                      <FileText className="h-4 w-4 text-muted-foreground shrink-0" />
                      <h3 className="font-medium text-sm truncate">{note.title}</h3>
                    </div>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-7 w-7 opacity-0 group-hover:opacity-100 transition-opacity text-destructive hover:text-destructive shrink-0"
                      onClick={(e) => {
                        e.stopPropagation()
                        setDeleteTarget(note)
                      }}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                  <p className="text-xs text-muted-foreground line-clamp-3">
                    {getPreview(note.content)}
                  </p>
                  <div className="flex items-center justify-between pt-1">
                    <span className="text-[11px] text-muted-foreground/70">
                      {formatDate(note.updatedAt || note.createdAt)}
                    </span>
                    {note.tags.length > 0 && (
                      <div className="flex items-center gap-1">
                        {note.tags.slice(0, 2).map((tag) => (
                          <span
                            key={tag.id}
                            className="text-[10px] px-1.5 py-0.5 rounded-full bg-accent/10 text-accent"
                          >
                            {tag.name}
                          </span>
                        ))}
                        {note.tags.length > 2 && (
                          <span className="text-[10px] text-muted-foreground">
                            +{note.tags.length - 2}
                          </span>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </Card>
            ))}
          </div>
        )}
      </div>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteTarget !== null} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Note</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete "{deleteTarget?.title}"? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)} disabled={isDeleting}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleDelete} disabled={isDeleting}>
              {isDeleting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
