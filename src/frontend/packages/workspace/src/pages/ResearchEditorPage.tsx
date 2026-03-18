import { useState, useEffect } from 'react'
import {
  ArrowLeft,
  Loader2,
  Save,
  Trash2,
  ChevronRight,
  FlaskConical,
  Search,
  FileText,
  StickyNote,
  X,
  Plus,
} from 'lucide-react'
import {
  Button,
  Input,
  Label,
  Badge,
  Textarea,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@donkeywork/ui'
import { MarkdownEditor } from '@donkeywork/editor'
import {
  research,
  type ResearchDetails,
  type ResearchStatus,
  type TagRequest,
} from '@donkeywork/api-client'
import type { WorkspaceNavigation } from '../types'

export function ResearchEditorPage({ researchId, isNew, nav }: { researchId?: string; isNew?: boolean; nav: WorkspaceNavigation }) {
  const isNewResearch = isNew ?? false

  const [isLoading, setIsLoading] = useState(!isNewResearch)
  const [isSaving, setIsSaving] = useState(false)
  const [researchData, setResearchData] = useState<ResearchDetails | null>(null)

  // Form fields
  const [title, setTitle] = useState('')
  const [plan, setPlan] = useState('')
  const [result, setResult] = useState('')
  const [status, setStatus] = useState<ResearchStatus>('NotStarted')
  const [tags, setTags] = useState<TagRequest[]>([])
  const [newTagName, setNewTagName] = useState('')

  // Completion dialog
  const [showCompletionDialog, setShowCompletionDialog] = useState(false)
  const [pendingStatus, setPendingStatus] = useState<ResearchStatus | null>(null)
  const [resultValue, setResultValue] = useState('')
  const [isSubmittingCompletion, setIsSubmittingCompletion] = useState(false)

  // Active tab
  const [activeTab, setActiveTab] = useState('results')

  useEffect(() => {
    if (researchId && !isNewResearch) {
      loadResearch()
    }
  }, [researchId])

  const loadResearch = async () => {
    if (!researchId || isNewResearch) return

    try {
      setIsLoading(true)
      const data = await research.get(researchId)
      setResearchData(data)
      setTitle(data.title)
      setPlan(data.plan || '')
      setResult(data.result || '')
      setStatus(data.status)
      setTags(data.tags.map((t) => ({ name: t.name })))
      setResultValue(data.result || '')
    } catch (error) {
      console.error('Failed to load research:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleSave = async () => {
    if (!title.trim()) return

    try {
      setIsSaving(true)

      if (isNewResearch) {
        const created = await research.create({
          title,
          plan,
          status,
          tags,
        })
        nav.goToResearch(created.id)
      } else if (researchData) {
        await research.update(researchData.id, {
          title,
          plan,
          result: result || undefined,
          status,
          tags,
        })
        nav.goToResearchList()
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
      nav.goToResearchList()
    } catch (error) {
      console.error('Failed to delete research:', error)
    }
  }

  const handleStatusChange = (newStatus: ResearchStatus) => {
    if (newStatus === 'Completed') {
      if (!plan.trim()) {
        setActiveTab('results')
        return
      }
      setPendingStatus(newStatus)
      setShowCompletionDialog(true)
    } else if (newStatus === 'Cancelled') {
      setStatus(newStatus)
    } else {
      setStatus(newStatus)
    }
  }

  const handleSubmitCompletion = async () => {
    if (!pendingStatus || !resultValue.trim()) return
    if (pendingStatus === 'Completed' && !plan.trim()) return

    if (!isNewResearch && researchData) {
      try {
        setIsSubmittingCompletion(true)
        await research.update(researchData.id, {
          title,
          plan,
          result: resultValue,
          status: pendingStatus,
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
      setResult(resultValue)
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

  if (!isNewResearch && !researchData) {
    return (
      <div className="flex flex-col items-center justify-center h-64">
        <p className="text-muted-foreground">Research not found</p>
        <Button variant="outline" className="mt-4" onClick={() => nav.goToResearchList()}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Research
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb Header */}
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-1 sm:gap-2 min-w-0">
          <Button variant="ghost" size="icon" className="shrink-0" onClick={() => nav.goToResearchList()}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div className="hidden sm:flex items-center gap-2 text-sm text-muted-foreground min-w-0">
            <button
              onClick={() => nav.goToResearchList()}
              className="flex items-center gap-1.5 hover:text-foreground transition-colors"
            >
              <FlaskConical className="h-4 w-4 shrink-0 text-cyan-500" />
              <span>Research</span>
            </button>
            <ChevronRight className="h-4 w-4 shrink-0" />
            <span className="flex items-center gap-1.5 text-foreground font-medium min-w-0">
              <Search className="h-4 w-4 shrink-0 text-cyan-500" />
              <span className="truncate max-w-[150px] md:max-w-[250px]">
                {isNewResearch ? 'New Research' : researchData?.title}
              </span>
            </span>
          </div>
          <span className="sm:hidden text-sm font-medium truncate min-w-0">
            {isNewResearch ? 'New Research' : researchData?.title}
          </span>
        </div>
        <div className="flex items-center gap-1 sm:gap-2 shrink-0">
          {!isNewResearch && (
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
            disabled={isSaving || !title.trim()}
          >
            {isSaving && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
            <Save className="h-4 w-4 sm:mr-2" />
            <span className="hidden sm:inline">{isNewResearch ? 'Create' : 'Save'}</span>
          </Button>
        </div>
      </div>

      {/* Research Details Card */}
      <div className="rounded-lg border border-border bg-card p-4">
        <div className="flex flex-col sm:flex-row sm:items-center gap-3">
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
                <SelectItem value="Completed" disabled={!plan.trim()}>Completed</SelectItem>
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
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        placeholder="Research topic"
        className="text-xl font-semibold border-0 px-0 focus-visible:ring-0 focus-visible:ring-offset-0"
      />

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="results">Results</TabsTrigger>
          {!isNewResearch && (
            <TabsTrigger value="notes">
              Notes {researchData?.notes && researchData.notes.length > 0 && `(${researchData.notes.length})`}
            </TabsTrigger>
          )}
        </TabsList>

        <TabsContent value="results" className="mt-4 space-y-6">
          {/* Result summary (shown for completed research) */}
          {result && (
            <div className="rounded-lg border border-border bg-card p-4 space-y-2">
              <Label className="text-sm font-medium">Result</Label>
              <Textarea
                value={result}
                onChange={(e) => setResult(e.target.value)}
                placeholder="Research outcome..."
                rows={4}
              />
            </div>
          )}

          <MarkdownEditor
            content={plan}
            onChange={setPlan}
            placeholder="Research findings and results..."
          />
        </TabsContent>

        {!isNewResearch && (
          <TabsContent value="notes" className="mt-4">
            {researchData?.notes && researchData.notes.length > 0 ? (
              <div className="space-y-3">
                {researchData.notes.map((note) => (
                  <div
                    key={note.id}
                    className="rounded-lg border border-border bg-card p-4 hover:shadow-md transition-all cursor-pointer"
                    onClick={() => nav.goToNote(note.id)}
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
            ) : (
              <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-8 text-center">
                <StickyNote className="h-8 w-8 text-muted-foreground" />
                <p className="mt-2 text-sm text-muted-foreground">
                  No notes attached to this research yet
                </p>
              </div>
            )}
          </TabsContent>
        )}
      </Tabs>

      {/* Completion Dialog */}
      <Dialog open={showCompletionDialog} onOpenChange={setShowCompletionDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Complete Research</DialogTitle>
            <DialogDescription>
              Provide the result of this research before marking it as completed.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label>Result</Label>
            <Textarea
              value={resultValue}
              onChange={(e) => setResultValue(e.target.value)}
              placeholder="What was the outcome of this research..."
              rows={4}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => { setShowCompletionDialog(false); setPendingStatus(null) }}>
              Cancel
            </Button>
            <Button
              onClick={handleSubmitCompletion}
              disabled={!resultValue.trim() || isSubmittingCompletion}
            >
              {isSubmittingCompletion && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              Mark Complete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
