import { useState, useEffect, useRef, useCallback } from 'react'
import { ArrowLeft, Loader2, Trash2, Clock } from 'lucide-react'
import { Button, Input, Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@donkeywork/ui'
import { MarkdownEditor } from '@donkeywork/editor'
import { notes, type Note } from '@donkeywork/api-client'

interface NoteEditorPageProps {
  noteId: string
  onBack: () => void
}

export function NoteEditorPage({ noteId, onBack }: NoteEditorPageProps) {
  const [isLoading, setIsLoading] = useState(true)
  const [note, setNote] = useState<Note | null>(null)
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [isSaving, setIsSaving] = useState(false)
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const lastSavedTitle = useRef('')
  const lastSavedContent = useRef('')

  useEffect(() => {
    loadNote()
    return () => {
      if (saveTimerRef.current) {
        clearTimeout(saveTimerRef.current)
      }
    }
  }, [noteId])

  const loadNote = async () => {
    try {
      setIsLoading(true)
      const noteData = await notes.get(noteId)
      setNote(noteData)
      setTitle(noteData.title)
      setContent(noteData.content || '')
      lastSavedTitle.current = noteData.title
      lastSavedContent.current = noteData.content || ''
    } catch (error) {
      console.error('Failed to load note:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const saveNote = useCallback(async (currentTitle: string, currentContent: string) => {
    if (!note || !currentTitle.trim()) return
    if (currentTitle === lastSavedTitle.current && currentContent === lastSavedContent.current) return

    try {
      setIsSaving(true)
      const updated = await notes.update(note.id, {
        title: currentTitle,
        content: currentContent,
        sortOrder: note.sortOrder,
        projectId: note.projectId,
        milestoneId: note.milestoneId,
      })
      setNote(updated)
      lastSavedTitle.current = currentTitle
      lastSavedContent.current = currentContent
    } catch (error) {
      console.error('Failed to save note:', error)
    } finally {
      setIsSaving(false)
    }
  }, [note])

  const scheduleSave = useCallback((newTitle: string, newContent: string) => {
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current)
    }
    saveTimerRef.current = setTimeout(() => {
      saveNote(newTitle, newContent)
    }, 1500)
  }, [saveNote])

  const handleTitleChange = (newTitle: string) => {
    setTitle(newTitle)
    scheduleSave(newTitle, content)
  }

  const handleContentChange = (newContent: string) => {
    setContent(newContent)
    scheduleSave(title, newContent)
  }

  const handleDelete = async () => {
    if (!note) return

    try {
      setIsDeleting(true)
      await notes.delete(note.id)
      setShowDeleteDialog(false)
      onBack()
    } catch (error) {
      console.error('Failed to delete note:', error)
    } finally {
      setIsDeleting(false)
    }
  }

  const handleBack = () => {
    // Flush any pending save before navigating back
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current)
      saveNote(title, content)
    }
    onBack()
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    })
  }

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!note) {
    return (
      <div className="flex flex-col items-center justify-center h-full gap-4">
        <p className="text-muted-foreground">Note not found</p>
        <Button variant="outline" onClick={onBack}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Notes
        </Button>
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between gap-3 px-4 py-3 border-b border-border shrink-0">
        <div className="flex items-center gap-2 min-w-0">
          <Button variant="ghost" size="icon" onClick={handleBack} className="shrink-0">
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            {isSaving && (
              <span className="flex items-center gap-1">
                <Loader2 className="h-3 w-3 animate-spin" />
                Saving...
              </span>
            )}
          </div>
        </div>
        <Button
          variant="ghost"
          size="icon"
          className="text-destructive hover:text-destructive shrink-0"
          onClick={() => setShowDeleteDialog(true)}
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      </div>

      {/* Title */}
      <div className="px-4 pt-4 shrink-0">
        <Input
          value={title}
          onChange={(e) => handleTitleChange(e.target.value)}
          placeholder="Note title"
          className="text-xl font-semibold border-0 px-0 focus-visible:ring-0 focus-visible:ring-offset-0 bg-transparent"
        />
      </div>

      {/* Timestamps */}
      <div className="flex items-center gap-4 px-4 py-2 text-xs text-muted-foreground shrink-0">
        <span className="flex items-center gap-1">
          <Clock className="h-3 w-3" />
          Created {formatDate(note.createdAt)}
        </span>
        {note.updatedAt && (
          <span className="flex items-center gap-1">
            <Clock className="h-3 w-3" />
            Updated {formatDate(note.updatedAt)}
          </span>
        )}
      </div>

      {/* Editor */}
      <div className="flex-1 min-h-0 px-4 pb-4">
        <MarkdownEditor
          content={content}
          onChange={handleContentChange}
          placeholder="Start writing your note..."
          className="h-full"
        />
      </div>

      {/* Delete Confirmation Dialog */}
      <Dialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Note</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete "{note.title}"? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDeleteDialog(false)} disabled={isDeleting}>
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
