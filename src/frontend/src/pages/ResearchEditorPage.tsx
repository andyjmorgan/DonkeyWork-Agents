import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Loader2,
  Save,
  Trash2,
  ChevronRight,
  FlaskConical,
  Search,
  FileText,
  X,
  Plus,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Textarea } from '@/components/ui/textarea'
import { MarkdownViewer } from '@/components/editor/MarkdownViewer'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  research,
  type ResearchDetails,
  type ResearchStatus,
  type TagRequest,
} from '@/lib/api'

export function ResearchEditorPage() {
  const { researchId } = useParams<{ researchId: string }>()
  const navigate = useNavigate()
  const isNew = researchId === 'new'

  const [isLoading, setIsLoading] = useState(!isNew)
  const [isSaving, setIsSaving] = useState(false)
  const [researchData, setResearchData] = useState<ResearchDetails | null>(null)

  // Form fields
  const [subject, setSubject] = useState('')
  const [content, setContent] = useState('')
  const [status, setStatus] = useState<ResearchStatus>('NotStarted')
  const [tags, setTags] = useState<TagRequest[]>([])
  const [newTagName, setNewTagName] = useState('')

  // Completion notes dialog
  const [showCompletionDialog, setShowCompletionDialog] = useState(false)
  const [pendingStatus, setPendingStatus] = useState<ResearchStatus | null>(null)
  const [completionNotesValue, setCompletionNotesValue] = useState('')
  const [isSubmittingCompletion, setIsSubmittingCompletion] = useState(false)

  useEffect(() => {
    if (researchId && !isNew) {
      loadResearch()
    }
  }, [researchId])

  const loadResearch = async () => {
    if (!researchId || isNew) return

    try {
      setIsLoading(true)
      const data = await research.get(researchId)
      setResearchData(data)
      setSubject(data.subject)
      setContent(data.content || '')
      setStatus(data.status)
      setTags(data.tags.map((t) => ({ name: t.name })))
      setCompletionNotesValue(data.completionNotes || '')
    } catch (error) {
      console.error('Failed to load research:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleSave = async () => {
    if (!subject.trim()) return

    try {
      setIsSaving(true)

      if (isNew) {
        const created = await research.create({
          subject,
          content,
          status,
          tags,
        })
        navigate(`/research/${created.id}`, { replace: true })
      } else if (researchData) {
        await research.update(researchData.id, {
          subject,
          content,
          summary: researchData.summary,
          status,
          completionNotes: (status === 'Completed' || status === 'Cancelled') ? completionNotesValue || undefined : undefined,
          tags,
        })
        navigate('/research')
      }
    } catch (error) {
      console.error('Failed to save research:', error)
    } finally {
      setIsSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!researchData || !window.confirm('Are you sure you want to delete this research?')) return

    try {
      await research.delete(researchData.id)
      navigate('/research')
    } catch (error) {
      console.error('Failed to delete research:', error)
    }
  }

  const handleStatusChange = (newStatus: ResearchStatus) => {
    if (newStatus === 'Completed' || newStatus === 'Cancelled') {
      setPendingStatus(newStatus)
      setShowCompletionDialog(true)
    } else {
      setStatus(newStatus)
      setCompletionNotesValue('')
    }
  }

  const handleSubmitCompletionNotes = async () => {
    if (!pendingStatus || !completionNotesValue.trim()) return

    if (!isNew && researchData) {
      try {
        setIsSubmittingCompletion(true)
        await research.update(researchData.id, {
          subject,
          content,
          summary: researchData.summary,
          status: pendingStatus,
          completionNotes: completionNotesValue,
          tags,
        })
        await loadResearch()
      } catch (error) {
        console.error('Failed to update status:', error)
      } finally {
        setIsSubmittingCompletion(false)
      }
    } else {
      setStatus(pendingStatus)
    }

    setShowCompletionDialog(false)
    setPendingStatus(null)
  }

  const handleAddTag = () => {
    const trimmed = newTagName.trim()
    if (!trimmed || tags.some((t) => t.name.toLowerCase() === trimmed.toLowerCase())) return
    setTags([...tags, { name: trimmed }])
    setNewTagName('')
  }

  const handleRemoveTag = (index: number) => {
    setTags(tags.filter((_, i) => i !== index))
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!isNew && !researchData) {
    return (
      <div className="flex flex-col items-center justify-center h-64">
        <p className="text-muted-foreground">Research not found</p>
        <Button variant="outline" className="mt-4" onClick={() => navigate('/research')}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Research
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb Header */}
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-2 min-w-0">
          <Button variant="ghost" size="icon" onClick={() => navigate('/research')}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div className="flex items-center gap-2 text-sm text-muted-foreground min-w-0">
            <button
              onClick={() => navigate('/research')}
              className="flex items-center gap-1.5 hover:text-foreground transition-colors"
            >
              <FlaskConical className="h-4 w-4 shrink-0 text-cyan-500" />
              <span>Research</span>
            </button>
            <ChevronRight className="h-4 w-4 shrink-0" />
            <span className="flex items-center gap-1.5 text-foreground font-medium min-w-0">
              <Search className="h-4 w-4 shrink-0 text-cyan-500" />
              <span className="truncate max-w-[100px] sm:max-w-[150px] md:max-w-[250px]">
                {isNew ? 'New Research' : researchData?.subject}
              </span>
            </span>
          </div>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          {!isNew && (
            <Button
              variant="ghost"
              size="icon"
              className="text-destructive"
              onClick={handleDelete}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          )}
          <Button
            onClick={handleSave}
            disabled={isSaving || !subject.trim()}
          >
            {isSaving && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
            <Save className="h-4 w-4 mr-2" />
            {isNew ? 'Create' : 'Save'}
          </Button>
        </div>
      </div>

      {/* Research Details Card */}
      <div className="rounded-lg border border-border bg-card p-4">
        <div className="flex flex-wrap items-center gap-3">
          {/* Status */}
          <div className="flex items-center gap-2">
            <Label className="text-sm text-muted-foreground">Status:</Label>
            <Select value={status} onValueChange={(v) => handleStatusChange(v as ResearchStatus)}>
              <SelectTrigger className="w-[130px] h-8">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="NotStarted">Not Started</SelectItem>
                <SelectItem value="InProgress">In Progress</SelectItem>
                <SelectItem value="Completed">Completed</SelectItem>
                <SelectItem value="Cancelled">Cancelled</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Tags */}
          <div className="flex items-center gap-2 flex-wrap">
            {tags.map((tag, idx) => (
              <Badge key={idx} variant="secondary" className="text-xs gap-1">
                {tag.name}
                <button onClick={() => handleRemoveTag(idx)} className="hover:text-destructive">
                  <X className="h-3 w-3" />
                </button>
              </Badge>
            ))}
            <div className="flex items-center gap-1">
              <Input
                value={newTagName}
                onChange={(e) => setNewTagName(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); handleAddTag() } }}
                placeholder="Add tag..."
                className="w-[100px] h-7 text-xs"
              />
              <Button variant="ghost" size="icon" className="h-7 w-7" onClick={handleAddTag} disabled={!newTagName.trim()}>
                <Plus className="h-3 w-3" />
              </Button>
            </div>
          </div>
        </div>

      </div>

      {/* Topic */}
      <Input
        value={subject}
        onChange={(e) => setSubject(e.target.value)}
        placeholder="Research topic"
        className="text-xl font-semibold border-0 px-0 focus-visible:ring-0 focus-visible:ring-offset-0"
      />

      {/* Description */}
      <Textarea
        value={content}
        onChange={(e) => setContent(e.target.value)}
        placeholder="Describe what needs to be researched..."
        rows={3}
        className="resize-none"
      />

      {/* Notes */}
      {!isNew && researchData?.notes && researchData.notes.length > 0 && (
        <div className="space-y-3">
          <h3 className="text-sm font-medium text-muted-foreground">Notes ({researchData.notes.length})</h3>
          {researchData.notes.map((note) => (
            <div
              key={note.id}
              className="rounded-lg border border-border bg-card p-4 hover:shadow-md transition-all cursor-pointer"
              onClick={() => navigate(`/notes/${note.id}`)}
            >
              <div className="flex items-center gap-2">
                <FileText className="h-4 w-4 text-blue-500 shrink-0" />
                <h4 className="font-medium truncate">{note.title}</h4>
              </div>
              {note.content && (
                <p className="text-sm text-muted-foreground line-clamp-2 mt-1 pl-6">
                  {note.content}
                </p>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Result */}
      {researchData?.completionNotes ? (
        <div className="rounded-lg border border-border bg-card p-4">
          <MarkdownViewer
            content={researchData.completionNotes}
            className="min-h-[200px]"
          />
          {researchData.completedAt && (
            <p className="text-xs text-muted-foreground mt-4 pt-3 border-t border-border">
              {status === 'Completed' ? 'Completed' : 'Cancelled'} {new Date(researchData.completedAt).toLocaleDateString()}
            </p>
          )}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <FlaskConical className="h-8 w-8 text-muted-foreground" />
          <p className="mt-2 text-sm text-muted-foreground">
            No result yet &mdash; mark as completed to add findings
          </p>
        </div>
      )}

      {/* Completion Notes Dialog */}
      <Dialog open={showCompletionDialog} onOpenChange={setShowCompletionDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {pendingStatus === 'Completed' ? 'Complete Research' : 'Cancel Research'}
            </DialogTitle>
            <DialogDescription>
              {pendingStatus === 'Completed'
                ? 'Provide notes about what was accomplished in this research.'
                : 'Provide a reason for cancelling this research.'}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label>{pendingStatus === 'Completed' ? 'Completion Notes' : 'Cancellation Reason'}</Label>
            <Textarea
              value={completionNotesValue}
              onChange={(e) => setCompletionNotesValue(e.target.value)}
              placeholder={pendingStatus === 'Completed' ? 'What was accomplished...' : 'Why is this being cancelled...'}
              rows={4}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => { setShowCompletionDialog(false); setPendingStatus(null) }}>
              Cancel
            </Button>
            <Button
              onClick={handleSubmitCompletionNotes}
              disabled={!completionNotesValue.trim() || isSubmittingCompletion}
            >
              {isSubmittingCompletion && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              {pendingStatus === 'Completed' ? 'Mark Complete' : 'Mark Cancelled'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
