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
  ChevronDown,
} from 'lucide-react'
import {
  Button,
  Input,
  Badge,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
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

  // Notes section
  const [notesOpen, setNotesOpen] = useState(false)

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
    setStatus(newStatus)
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

  // Show result section when status is Completed or result has content
  const showResultSection = status === 'Completed' || result.trim().length > 0

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

  const noteCount = researchData?.notes?.length ?? 0

  return (
    <div className="max-w-4xl mx-auto space-y-4">
      {/* Top bar: back/breadcrumb + actions */}
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
            <span className="text-foreground font-medium truncate max-w-[200px] md:max-w-[350px]">
              {isNewResearch ? 'New Research' : (title || researchData?.title)}
            </span>
          </div>
          <span className="sm:hidden text-sm font-medium truncate min-w-0">
            {isNewResearch ? 'New Research' : (title || researchData?.title)}
          </span>
        </div>
        <div className="flex items-center gap-1 sm:gap-2 shrink-0">
          {!isNewResearch && (
            <Button
              variant="ghost"
              size="icon"
              className="text-destructive hover:text-destructive hover:bg-destructive/10"
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

      {/* Title + Status + Tags */}
      <div className="space-y-3">
        {/* Title as large heading input */}
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Research topic..."
          className="w-full bg-transparent text-2xl sm:text-3xl font-bold text-foreground placeholder:text-muted-foreground/50 border-none outline-none focus:outline-none"
        />

        {/* Status + Tags row */}
        <div className="flex items-center gap-2 flex-wrap">
          <Select value={status} onValueChange={(v) => handleStatusChange(v as ResearchStatus)}>
            <SelectTrigger className="w-auto h-7 text-xs gap-1.5 rounded-full px-3">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="NotStarted">Not Started</SelectItem>
              <SelectItem value="InProgress">In Progress</SelectItem>
              <SelectItem value="Completed">Completed</SelectItem>
              <SelectItem value="Cancelled">Cancelled</SelectItem>
            </SelectContent>
          </Select>

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

      {/* Plan / Body section */}
      <div className="space-y-1.5">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Search className="h-4 w-4 text-cyan-500" />
          <span className="font-medium">Investigation</span>
        </div>
        <MarkdownEditor
          content={plan}
          onChange={setPlan}
          placeholder="What is being researched? Write the details, findings, and analysis here..."
        />
      </div>

      {/* Result section — visually distinct */}
      {showResultSection && (
        <div className="space-y-1.5">
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <FileText className="h-4 w-4 text-emerald-500" />
            <span className="font-medium">Result</span>
            {status === 'Completed' && (
              <Badge variant="success" className="text-xs py-0.5">Completed</Badge>
            )}
          </div>
          <div className="border-l-2 border-emerald-500/40 pl-4 bg-emerald-500/[0.03] rounded-r-2xl py-3 pr-3">
            <MarkdownEditor
              content={result}
              onChange={setResult}
              placeholder="What was the outcome? Summarize the findings..."
            />
          </div>
        </div>
      )}

      {/* Notes section — collapsible */}
      {!isNewResearch && (
        <Collapsible open={notesOpen} onOpenChange={setNotesOpen}>
          <CollapsibleTrigger className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors w-full py-2">
            <ChevronDown className={`h-4 w-4 transition-transform ${notesOpen ? '' : '-rotate-90'}`} />
            <StickyNote className="h-4 w-4 text-blue-500" />
            <span className="font-medium">Notes</span>
            {noteCount > 0 && (
              <span className="text-xs text-muted-foreground">({noteCount})</span>
            )}
          </CollapsibleTrigger>
          <CollapsibleContent>
            <div className="pt-2 space-y-2">
              {noteCount > 0 ? (
                researchData!.notes.map((note) => (
                  <div
                    key={note.id}
                    className="rounded-xl border border-border bg-card p-3 hover:border-accent/30 hover:shadow-sm transition-all cursor-pointer"
                    onClick={() => nav.goToNote(note.id)}
                  >
                    <div className="flex items-center gap-2">
                      <FileText className="h-4 w-4 text-blue-500 shrink-0" />
                      <span className="font-medium text-sm truncate">{note.title}</span>
                    </div>
                    {note.content && (
                      <p className="text-xs text-muted-foreground line-clamp-2 mt-1 pl-6">
                        {note.content}
                      </p>
                    )}
                  </div>
                ))
              ) : (
                <p className="text-sm text-muted-foreground italic pl-6">
                  No notes attached to this research yet.
                </p>
              )}
            </div>
          </CollapsibleContent>
        </Collapsible>
      )}
    </div>
  )
}
