import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Loader2, FileText, StickyNote } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ContentCard } from '@/components/workspace/ContentCard'
import { notes, type Note } from '@/lib/api'

export function NotesPage() {
  const navigate = useNavigate()
  const [isLoading, setIsLoading] = useState(true)
  const [isCreating, setIsCreating] = useState(false)
  const [notesList, setNotesList] = useState<Note[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)

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

  const createAndEditNote = async () => {
    const timestamp = new Date().toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    })

    try {
      setIsCreating(true)
      const newNote = await notes.create({
        title: `Untitled Note - ${timestamp}`,
        content: '',
      })
      navigate(`/notes/${newNote.id}`)
    } catch (error) {
      console.error('Failed to create note:', error)
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
        <Button onClick={createAndEditNote} disabled={isCreating}>
          {isCreating ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Plus className="h-4 w-4" />
          )}
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
          <Button className="mt-4" onClick={createAndEditNote} disabled={isCreating}>
            {isCreating ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Plus className="h-4 w-4" />
            )}
            Create Note
          </Button>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {notesList.map((note) => (
            <ContentCard
              key={note.id}
              title={note.title}
              content={note.content}
              onClick={() => navigate(`/notes/${note.id}`)}
              onDelete={() => handleDelete(note.id, note.title)}
              isDeleting={deletingId === note.id}
              date={note.updatedAt || note.createdAt}
              icon={<FileText className="h-5 w-5 text-muted-foreground" />}
              tags={note.tags}
            />
          ))}
        </div>
      )}
    </div>
  )
}
