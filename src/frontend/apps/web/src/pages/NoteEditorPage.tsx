import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Loader2, Save, Trash2, ChevronRight, FolderKanban, FileText, StickyNote } from 'lucide-react'
import { Button, Input } from '@donkeywork/ui'
import { MarkdownEditor } from '@donkeywork/editor'
import { notes, projects, type Note, type ProjectDetails } from '@donkeywork/api-client'

export function NoteEditorPage() {
  const { noteId } = useParams<{ noteId: string }>()
  const navigate = useNavigate()

  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [note, setNote] = useState<Note | null>(null)
  const [project, setProject] = useState<ProjectDetails | null>(null)
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')

  useEffect(() => {
    if (noteId) {
      loadNote()
    }
  }, [noteId])

  const loadNote = async () => {
    if (!noteId) return

    try {
      setIsLoading(true)
      const noteData = await notes.get(noteId)
      setNote(noteData)
      setTitle(noteData.title)
      setContent(noteData.content || '')

      // Load project if note belongs to one
      if (noteData.projectId) {
        const projectData = await projects.get(noteData.projectId)
        setProject(projectData)
      }
    } catch (error) {
      console.error('Failed to load note:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleSave = async () => {
    if (!note || !title.trim()) return

    try {
      setIsSaving(true)
      await notes.update(note.id, {
        title,
        content,
        sortOrder: note.sortOrder,
        projectId: note.projectId,
        milestoneId: note.milestoneId,
      })
      // Navigate back
      if (project) {
        navigate(`/workspace/${project.id}`)
      } else {
        navigate('/notes')
      }
    } catch (error) {
      console.error('Failed to save note:', error)
    } finally {
      setIsSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!note || !window.confirm('Are you sure you want to delete this note?')) return

    try {
      await notes.delete(note.id)
      if (project) {
        navigate(`/workspace/${project.id}`)
      } else {
        navigate('/notes')
      }
    } catch (error) {
      console.error('Failed to delete note:', error)
    }
  }

  const handleBack = () => {
    if (project) {
      navigate(`/workspace/${project.id}`)
    } else {
      navigate('/notes')
    }
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!note) {
    return (
      <div className="flex flex-col items-center justify-center h-64">
        <p className="text-muted-foreground">Note not found</p>
        <Button variant="outline" className="mt-4" onClick={() => navigate('/notes')}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Notes
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb Header */}
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-2 min-w-0">
          <Button variant="ghost" size="icon" onClick={handleBack}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div className="flex items-center gap-2 text-sm text-muted-foreground min-w-0">
            {project ? (
              <>
                <button
                  onClick={() => navigate(`/workspace/${project.id}`)}
                  className="flex items-center gap-1.5 hover:text-foreground transition-colors min-w-0"
                >
                  <FolderKanban className="h-4 w-4 shrink-0" />
                  <span className="truncate max-w-[80px] sm:max-w-[120px] md:max-w-[200px]">{project.name}</span>
                </button>
                <ChevronRight className="h-4 w-4 shrink-0" />
                <span className="flex items-center gap-1.5 text-foreground font-medium min-w-0">
                  <FileText className="h-4 w-4 shrink-0" />
                  <span className="truncate max-w-[80px] sm:max-w-[120px] md:max-w-[200px]">{note.title}</span>
                </span>
              </>
            ) : (
              <>
                <button
                  onClick={() => navigate('/notes')}
                  className="flex items-center gap-1.5 hover:text-foreground transition-colors"
                >
                  <StickyNote className="h-4 w-4 shrink-0" />
                  <span>Notes</span>
                </button>
                <ChevronRight className="h-4 w-4 shrink-0" />
                <span className="flex items-center gap-1.5 text-foreground font-medium min-w-0">
                  <FileText className="h-4 w-4 shrink-0" />
                  <span className="truncate max-w-[100px] sm:max-w-[150px] md:max-w-[250px]">{note.title}</span>
                </span>
              </>
            )}
          </div>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <Button
            variant="ghost"
            size="icon"
            className="text-destructive"
            onClick={handleDelete}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
          <Button onClick={handleSave} disabled={isSaving || !title.trim()}>
            {isSaving && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
            <Save className="h-4 w-4 mr-2" />
            Save
          </Button>
        </div>
      </div>

      {/* Note Editor */}
      <div className="space-y-4">
        <Input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Note title"
          className="text-xl font-semibold border-0 px-0 focus-visible:ring-0 focus-visible:ring-offset-0"
        />
        <MarkdownEditor
          content={content}
          onChange={setContent}
          placeholder="Start writing your note..."
          className="min-h-[calc(100vh-300px)]"
        />
      </div>
    </div>
  )
}
