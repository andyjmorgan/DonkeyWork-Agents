import { useState, useEffect } from 'react'
import {
  ArrowLeft,
  Loader2,
  Plus,
  Target,
  CheckSquare,
  FileText,
  Calendar,
  FolderKanban,
  ChevronRight,
  Save,
  Pencil,
  LayoutDashboard,
  StickyNote,
  RefreshCw,
  Trash2,
} from 'lucide-react'
import {
  Button,
  Badge,
  Input,
  Label,
  Textarea,
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
  Progress,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@donkeywork/ui'
import { MarkdownEditor, MarkdownViewer } from '@donkeywork/editor'
import {
  projects,
  milestones,
  tasks,
  notes,
  type ProjectDetails,
  type MilestoneDetails,
  type Task,
  type Note,
  type MilestoneStatus,
  type TaskStatus,
  type TaskPriority,
} from '@donkeywork/api-client'

type TabType = 'overview' | 'notes' | 'tasks'

interface NavigateParams {
  page: string
  projectId?: string
  milestoneId?: string
}

interface MilestoneDetailPageProps {
  projectId: string
  milestoneId: string
  onNavigate: (params: NavigateParams) => void
}

const statusVariants: Record<string, 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning' | 'pending' | 'inProgress' | 'muted'> = {
  NotStarted: 'pending',
  InProgress: 'inProgress',
  Completed: 'success',
  OnHold: 'warning',
  Cancelled: 'destructive',
  Pending: 'pending',
}

const priorityColors: Record<TaskPriority, string> = {
  Low: 'text-slate-400',
  Medium: 'text-blue-400',
  High: 'text-orange-400',
  Critical: 'text-red-400',
}

export function MilestoneDetailPage({ projectId, milestoneId, onNavigate }: MilestoneDetailPageProps) {
  const [isLoading, setIsLoading] = useState(true)
  const [project, setProject] = useState<ProjectDetails | null>(null)
  const [milestone, setMilestone] = useState<MilestoneDetails | null>(null)
  const [activeTab, setActiveTab] = useState<TabType>('overview')

  // Name editing
  const [isEditingName, setIsEditingName] = useState(false)
  const [nameValue, setNameValue] = useState('')
  const [isSavingName, setIsSavingName] = useState(false)

  // Content editing
  const [isEditingContent, setIsEditingContent] = useState(false)
  const [contentValue, setContentValue] = useState('')
  const [isSavingContent, setIsSavingContent] = useState(false)

  // Inline editing for status and dueDate
  const [statusValue, setStatusValue] = useState<MilestoneStatus>('NotStarted')
  const [dueDateValue, setDueDateValue] = useState('')

  // Refresh states
  const [isRefreshingNotes, setIsRefreshingNotes] = useState(false)
  const [isRefreshingTasks, setIsRefreshingTasks] = useState(false)

  // Completion notes dialog state
  const [showCompletionDialog, setShowCompletionDialog] = useState(false)
  const [pendingStatus, setPendingStatus] = useState<MilestoneStatus | null>(null)
  const [completionNotesValue, setCompletionNotesValue] = useState('')
  const [isSubmittingCompletion, setIsSubmittingCompletion] = useState(false)

  useEffect(() => {
    loadData()
  }, [projectId, milestoneId])

  const loadData = async () => {
    try {
      setIsLoading(true)
      const [projectData, milestoneData] = await Promise.all([
        projects.get(projectId),
        milestones.get(projectId, milestoneId),
      ])
      setProject(projectData)
      setMilestone(milestoneData)
      setNameValue(milestoneData.name)
      setContentValue(milestoneData.content || '')
      setStatusValue(milestoneData.status)
      setDueDateValue(milestoneData.dueDate || '')
    } catch (error) {
      console.error('Failed to load data:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleRefreshNotes = async () => {
    try {
      setIsRefreshingNotes(true)
      const milestoneData = await milestones.get(projectId, milestoneId)
      setMilestone(milestoneData)
    } catch (error) {
      console.error('Failed to refresh notes:', error)
    } finally {
      setIsRefreshingNotes(false)
    }
  }

  const handleRefreshTasks = async () => {
    try {
      setIsRefreshingTasks(true)
      const milestoneData = await milestones.get(projectId, milestoneId)
      setMilestone(milestoneData)
    } catch (error) {
      console.error('Failed to refresh tasks:', error)
    } finally {
      setIsRefreshingTasks(false)
    }
  }

  const handleSaveName = async () => {
    if (!milestone || !nameValue.trim()) return

    try {
      setIsSavingName(true)
      await milestones.update(projectId, milestone.id, {
        name: nameValue.trim(),
        content: milestone.content,
        status: milestone.status,
        dueDate: milestone.dueDate || undefined,
        sortOrder: milestone.sortOrder,
      })
      setMilestone({ ...milestone, name: nameValue.trim() })
      setIsEditingName(false)
    } catch (error) {
      console.error('Failed to save milestone name:', error)
    } finally {
      setIsSavingName(false)
    }
  }

  const handleSaveContent = async () => {
    if (!milestone) return

    try {
      setIsSavingContent(true)
      await milestones.update(projectId, milestone.id, {
        name: milestone.name,
        content: contentValue,
        status: statusValue,
        dueDate: dueDateValue || undefined,
        sortOrder: milestone.sortOrder,
      })
      setMilestone({ ...milestone, content: contentValue })
      setIsEditingContent(false)
    } catch (error) {
      console.error('Failed to save milestone content:', error)
    } finally {
      setIsSavingContent(false)
    }
  }

  const handleStatusChange = async (newStatus: MilestoneStatus) => {
    if (!milestone) return

    if (newStatus === 'Completed' || newStatus === 'Cancelled') {
      setPendingStatus(newStatus)
      setCompletionNotesValue('')
      setShowCompletionDialog(true)
      return
    }

    try {
      setStatusValue(newStatus)
      await milestones.update(projectId, milestone.id, {
        name: milestone.name,
        content: milestone.content,
        status: newStatus,
        dueDate: milestone.dueDate || undefined,
        sortOrder: milestone.sortOrder,
      })
      setMilestone({ ...milestone, status: newStatus })
    } catch (error) {
      console.error('Failed to update status:', error)
      setStatusValue(milestone.status)
    }
  }

  const handleSubmitCompletionNotes = async () => {
    if (!milestone || !pendingStatus || !completionNotesValue.trim()) return

    try {
      setIsSubmittingCompletion(true)
      const updated = await milestones.update(projectId, milestone.id, {
        name: milestone.name,
        content: milestone.content,
        status: pendingStatus,
        completionNotes: completionNotesValue.trim(),
        dueDate: milestone.dueDate || undefined,
        sortOrder: milestone.sortOrder,
      })
      setMilestone(updated)
      setStatusValue(pendingStatus)
      setShowCompletionDialog(false)
      setPendingStatus(null)
      setCompletionNotesValue('')
    } catch (error) {
      console.error('Failed to update milestone status:', error)
    } finally {
      setIsSubmittingCompletion(false)
    }
  }

  const handleDueDateChange = async (newDueDate: string) => {
    if (!milestone) return

    try {
      const dueDate = newDueDate ? new Date(newDueDate).toISOString() : undefined
      setDueDateValue(dueDate || '')
      await milestones.update(projectId, milestone.id, {
        name: milestone.name,
        content: milestone.content,
        status: milestone.status,
        dueDate,
        sortOrder: milestone.sortOrder,
      })
      setMilestone({ ...milestone, dueDate })
    } catch (error) {
      console.error('Failed to update due date:', error)
      setDueDateValue(milestone.dueDate || '')
    }
  }

  const handleCreateNote = async () => {
    const timestamp = new Date().toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    })

    try {
      await notes.create({
        title: `Untitled Note - ${timestamp}`,
        content: '',
        milestoneId,
      })
      await loadData()
    } catch (error) {
      console.error('Failed to create note:', error)
    }
  }

  const handleCreateTask = async () => {
    const timestamp = new Date().toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    })

    try {
      await tasks.create({
        title: `Untitled Task - ${timestamp}`,
        description: '',
        priority: 'Medium',
        milestoneId,
      })
      await loadData()
    } catch (error) {
      console.error('Failed to create task:', error)
    }
  }

  const handleToggleTaskStatus = async (task: Task) => {
    try {
      if (task.status === 'Completed') {
        await tasks.update(task.id, {
          title: task.title,
          description: task.description,
          priority: task.priority,
          status: 'Pending',
          sortOrder: task.sortOrder,
        })
      } else {
        await tasks.delete(task.id)
      }
      await loadData()
    } catch (error) {
      console.error('Failed to update task:', error)
    }
  }

  const handleDeleteTask = async (taskId: string) => {
    if (!window.confirm('Are you sure you want to delete this task?')) return

    try {
      await tasks.delete(taskId)
      await loadData()
    } catch (error) {
      console.error('Failed to delete task:', error)
    }
  }

  const handleDeleteNote = async (noteId: string) => {
    if (!window.confirm('Are you sure you want to delete this note?')) return

    try {
      await notes.delete(noteId)
      await loadData()
    } catch (error) {
      console.error('Failed to delete note:', error)
    }
  }

  const pendingTasks = milestone?.tasks.filter(t => t.status !== 'Completed').length || 0
  const completedTasks = milestone?.tasks.filter(t => t.status === 'Completed').length || 0
  const progressPercent = milestone && milestone.tasks.length > 0
    ? Math.round((completedTasks / milestone.tasks.length) * 100)
    : 0

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!project || !milestone) {
    return (
      <div className="flex flex-col items-center justify-center h-64">
        <p className="text-muted-foreground">Milestone not found</p>
        <Button variant="outline" className="mt-4" onClick={() => onNavigate({ page: 'projects' })}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Projects
        </Button>
      </div>
    )
  }

  const TabButton = ({ tab, icon: Icon, iconColor, label, count }: { tab: TabType; icon: React.ElementType; iconColor?: string; label: string; count?: number }) => (
    <button
      onClick={() => setActiveTab(tab)}
      className={`flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-lg transition-colors ${
        activeTab === tab
          ? 'bg-primary/10 text-primary border border-primary/30'
          : 'text-muted-foreground hover:bg-muted'
      }`}
    >
      <Icon className={`h-4 w-4 ${iconColor || ''}`} />
      {label}
      {count !== undefined && count > 0 && (
        <span className={`text-xs px-1.5 py-0.5 rounded-full ${activeTab === tab ? 'bg-primary/20' : 'bg-muted'}`}>
          {count}
        </span>
      )}
    </button>
  )

  return (
    <div className="space-y-6 p-6 overflow-y-auto h-full">
      {/* Breadcrumb Header */}
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-2 min-w-0">
          <Button variant="ghost" size="icon" onClick={() => onNavigate({ page: 'project-detail', projectId })}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div className="flex items-center gap-2 text-sm text-muted-foreground min-w-0">
            <button
              onClick={() => onNavigate({ page: 'project-detail', projectId })}
              className="flex items-center gap-1.5 hover:text-foreground transition-colors min-w-0"
            >
              <FolderKanban className="h-4 w-4 shrink-0" />
              <span className="truncate max-w-[120px]">{project.name}</span>
            </button>
            <ChevronRight className="h-4 w-4 shrink-0" />
            <span className="flex items-center gap-1.5 text-foreground font-medium min-w-0">
              <Target className="h-4 w-4 shrink-0" />
              <span className="truncate max-w-[120px]">{milestone.name}</span>
            </span>
          </div>
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline">
              <Plus className="h-4 w-4 mr-2" />
              Add
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={handleCreateTask}>
              <CheckSquare className="h-4 w-4 mr-2 text-emerald-500" />
              Task
            </DropdownMenuItem>
            <DropdownMenuItem onClick={handleCreateNote}>
              <FileText className="h-4 w-4 mr-2 text-blue-500" />
              Note
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Milestone Header with inline editing */}
      <div className="rounded-lg border border-border bg-card p-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          {isEditingName ? (
            <div className="flex items-center gap-2 flex-1">
              <Input
                value={nameValue}
                onChange={(e) => setNameValue(e.target.value)}
                className="text-xl font-bold h-10 max-w-md"
                autoFocus
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleSaveName()
                  if (e.key === 'Escape') {
                    setNameValue(milestone.name)
                    setIsEditingName(false)
                  }
                }}
              />
              <Button
                size="sm"
                onClick={handleSaveName}
                disabled={isSavingName || !nameValue.trim()}
              >
                {isSavingName && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
                <Save className="h-4 w-4" />
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  setNameValue(milestone.name)
                  setIsEditingName(false)
                }}
              >
                Cancel
              </Button>
            </div>
          ) : (
            <div className="flex items-center gap-2 group">
              <h1 className="text-xl font-bold">{milestone.name}</h1>
              <Button
                variant="ghost"
                size="icon"
                className="h-7 w-7 opacity-0 group-hover:opacity-100 transition-opacity"
                onClick={() => setIsEditingName(true)}
              >
                <Pencil className="h-4 w-4" />
              </Button>
            </div>
          )}
          <div className="flex items-center gap-3 flex-wrap">
            <Select value={statusValue} onValueChange={(value) => handleStatusChange(value as MilestoneStatus)}>
              <SelectTrigger className="w-[140px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="NotStarted">Not Started</SelectItem>
                <SelectItem value="InProgress">In Progress</SelectItem>
                <SelectItem value="OnHold">On Hold</SelectItem>
                <SelectItem value="Completed">Completed</SelectItem>
                <SelectItem value="Cancelled">Cancelled</SelectItem>
              </SelectContent>
            </Select>
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-muted-foreground" />
              <Input
                type="date"
                className="w-[150px]"
                value={dueDateValue ? new Date(dueDateValue).toISOString().split('T')[0] : ''}
                onChange={(e) => handleDueDateChange(e.target.value)}
              />
            </div>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 overflow-x-auto pb-1">
        <TabButton tab="overview" icon={LayoutDashboard} iconColor="text-slate-500" label="Overview" />
        <TabButton tab="notes" icon={StickyNote} iconColor="text-blue-500" label="Notes" count={milestone.notes.length} />
        <TabButton tab="tasks" icon={CheckSquare} iconColor="text-emerald-500" label="Tasks" count={milestone.tasks.length} />
      </div>

      {/* Tab Content */}
      {activeTab === 'overview' && (
        <div className="space-y-6">
          {/* Stats Cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="rounded-lg border border-border bg-card p-4">
              <div className="text-3xl font-bold text-amber-500">{pendingTasks}</div>
              <div className="text-sm text-muted-foreground">Pending</div>
            </div>
            <div className="rounded-lg border border-border bg-card p-4">
              <div className="text-3xl font-bold text-emerald-500">{completedTasks}</div>
              <div className="text-sm text-muted-foreground">Completed</div>
            </div>
            <div className="rounded-lg border border-border bg-card p-4">
              <div className="text-3xl font-bold text-blue-500">{milestone.notes.length}</div>
              <div className="text-sm text-muted-foreground">Notes</div>
            </div>
            <div className="rounded-lg border border-border bg-card p-4">
              <div className="text-3xl font-bold text-primary">{progressPercent}%</div>
              <div className="text-sm text-muted-foreground">Progress</div>
            </div>
          </div>

          {/* Progress Bar */}
          {milestone.tasks.length > 0 && (
            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Task Progress</span>
                <span className="font-medium">{completedTasks}/{milestone.tasks.length}</span>
              </div>
              <Progress value={progressPercent} className="h-2" />
            </div>
          )}

          {/* Milestone Content */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold flex items-center gap-2">
                <FileText className="h-5 w-5" />
                Milestone Content
              </h2>
              {!isEditingContent ? (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setIsEditingContent(true)}
                >
                  <Pencil className="h-4 w-4 mr-2" />
                  Edit
                </Button>
              ) : (
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      setContentValue(milestone.content || '')
                      setIsEditingContent(false)
                    }}
                  >
                    Cancel
                  </Button>
                  <Button
                    size="sm"
                    onClick={handleSaveContent}
                    disabled={isSavingContent}
                  >
                    {isSavingContent && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
                    <Save className="h-4 w-4 mr-2" />
                    Save
                  </Button>
                </div>
              )}
            </div>
            {isEditingContent ? (
              <div className="rounded-lg border border-border flex flex-col" style={{ height: '400px', minHeight: '300px' }}>
                <MarkdownEditor
                  content={contentValue}
                  onChange={setContentValue}
                  placeholder="Write your milestone content here..."
                  className="flex-1 h-full"
                />
              </div>
            ) : (
              <div className="rounded-lg border border-border bg-card p-4">
                {milestone.content ? (
                  <MarkdownViewer content={milestone.content} />
                ) : (
                  <p className="text-sm text-muted-foreground italic">No content yet. Click Edit to add some.</p>
                )}
              </div>
            )}
          </div>

          {/* Success Criteria */}
          {milestone.successCriteria && (
            <div className="space-y-3">
              <h2 className="text-lg font-semibold flex items-center gap-2">
                <CheckSquare className="h-5 w-5 text-emerald-500" />
                Success Criteria
              </h2>
              <div className="rounded-lg border border-border bg-card p-4">
                <MarkdownViewer content={milestone.successCriteria} />
              </div>
            </div>
          )}

          {/* Completion Notes */}
          {(milestone.status === 'Completed' || milestone.status === 'Cancelled') && milestone.completionNotes && (
            <div className={`rounded-lg border p-4 ${milestone.status === 'Completed' ? 'border-emerald-500/30 bg-emerald-500/5' : 'border-red-500/30 bg-red-500/5'}`}>
              <h3 className="text-sm font-semibold mb-2 flex items-center gap-2">
                {milestone.status === 'Completed' ? (
                  <CheckSquare className="h-4 w-4 text-emerald-500" />
                ) : (
                  <Trash2 className="h-4 w-4 text-red-500" />
                )}
                {milestone.status === 'Completed' ? 'Completion Notes' : 'Cancellation Notes'}
              </h3>
              <MarkdownViewer content={milestone.completionNotes} />
              {milestone.completedAt && (
                <p className="text-xs text-muted-foreground mt-2">
                  {milestone.status === 'Completed' ? 'Completed' : 'Cancelled'} on {new Date(milestone.completedAt).toLocaleDateString()}
                </p>
              )}
            </div>
          )}
        </div>
      )}

      {activeTab === 'notes' && (
        <div className="space-y-4">
          {milestone.notes.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border p-8 text-center">
              <StickyNote className="h-8 w-8 mx-auto text-muted-foreground" />
              <p className="mt-2 text-sm text-muted-foreground">No notes yet</p>
              <Button variant="outline" className="mt-4" onClick={handleCreateNote}>
                <Plus className="h-4 w-4 mr-2" />
                Add Note
              </Button>
            </div>
          ) : (
            <>
              <div className="flex justify-end gap-2">
                <Button
                  variant="outline"
                  size="icon"
                  onClick={handleRefreshNotes}
                  disabled={isRefreshingNotes}
                >
                  <RefreshCw className={`h-4 w-4 ${isRefreshingNotes ? 'animate-spin' : ''}`} />
                </Button>
                <Button variant="outline" size="sm" onClick={handleCreateNote}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Note
                </Button>
              </div>
              <div className="space-y-2">
                {milestone.notes.map((note) => (
                  <MilestoneNoteCard key={note.id} note={note} onDelete={() => handleDeleteNote(note.id)} />
                ))}
              </div>
            </>
          )}
        </div>
      )}

      {activeTab === 'tasks' && (
        <div className="space-y-6">
          {milestone.tasks.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border p-8 text-center">
              <CheckSquare className="h-8 w-8 mx-auto text-muted-foreground" />
              <p className="mt-2 text-sm text-muted-foreground">No tasks yet</p>
              <Button variant="outline" className="mt-4" onClick={handleCreateTask}>
                <Plus className="h-4 w-4 mr-2" />
                Add Task
              </Button>
            </div>
          ) : (
            <>
              <div className="flex justify-end gap-2">
                <Button
                  variant="outline"
                  size="icon"
                  onClick={handleRefreshTasks}
                  disabled={isRefreshingTasks}
                >
                  <RefreshCw className={`h-4 w-4 ${isRefreshingTasks ? 'animate-spin' : ''}`} />
                </Button>
                <Button variant="outline" size="sm" onClick={handleCreateTask}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Task
                </Button>
              </div>

              {/* Pending Tasks */}
              {pendingTasks > 0 && (
                <div className="space-y-3">
                  <h3 className="text-sm font-semibold text-muted-foreground flex items-center gap-2">
                    Pending
                    <span className="text-xs px-1.5 py-0.5 rounded-full bg-amber-500/20 text-amber-500">
                      {pendingTasks}
                    </span>
                  </h3>
                  <div className="space-y-2">
                    {milestone.tasks.filter(t => t.status !== 'Completed').map((task) => (
                      <MilestoneTaskCard
                        key={task.id}
                        task={task}
                        onToggleComplete={() => handleToggleTaskStatus(task)}
                        onDelete={() => handleDeleteTask(task.id)}
                      />
                    ))}
                  </div>
                </div>
              )}

              {/* Completed Tasks */}
              {completedTasks > 0 && (
                <div className="space-y-3">
                  <h3 className="text-sm font-semibold text-muted-foreground flex items-center gap-2">
                    Completed
                    <span className="text-xs px-1.5 py-0.5 rounded-full bg-emerald-500/20 text-emerald-500">
                      {completedTasks}
                    </span>
                  </h3>
                  <div className="space-y-2">
                    {milestone.tasks.filter(t => t.status === 'Completed').map((task) => (
                      <MilestoneTaskCard
                        key={task.id}
                        task={task}
                        onToggleComplete={() => handleToggleTaskStatus(task)}
                        onDelete={() => handleDeleteTask(task.id)}
                      />
                    ))}
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* Completion Notes Dialog */}
      <Dialog open={showCompletionDialog} onOpenChange={() => { setShowCompletionDialog(false); setPendingStatus(null) }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {pendingStatus === 'Completed' ? 'Complete Milestone' : 'Cancel Milestone'}
            </DialogTitle>
            <DialogDescription>
              {pendingStatus === 'Completed'
                ? 'Provide notes about what was accomplished.'
                : 'Provide a reason for cancellation.'}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="completion-notes">
                {pendingStatus === 'Completed' ? 'Completion Notes' : 'Cancellation Reason'}
              </Label>
              <Textarea
                id="completion-notes"
                value={completionNotesValue}
                onChange={(e) => setCompletionNotesValue(e.target.value)}
                placeholder={pendingStatus === 'Completed' ? 'What was accomplished...' : 'Why is this being cancelled...'}
                rows={4}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => { setShowCompletionDialog(false); setPendingStatus(null) }}>Cancel</Button>
            <Button
              onClick={handleSubmitCompletionNotes}
              disabled={isSubmittingCompletion || !completionNotesValue.trim()}
              variant={pendingStatus === 'Cancelled' ? 'destructive' : 'default'}
            >
              {isSubmittingCompletion && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              {pendingStatus === 'Completed' ? 'Complete' : 'Cancel Milestone'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

// Inline card components

function MilestoneTaskCard({
  task,
  onToggleComplete,
  onDelete,
}: {
  task: Task
  onToggleComplete: () => void
  onDelete: () => void
}) {
  const isCompleted = task.status === 'Completed'

  return (
    <div className="rounded-lg border border-border bg-card p-3 flex items-start gap-3">
      <button
        onClick={(e) => { e.stopPropagation(); onToggleComplete() }}
        className="mt-0.5 shrink-0"
      >
        {isCompleted ? (
          <CheckSquare className="h-4 w-4 text-emerald-500" />
        ) : (
          <div className="h-4 w-4 rounded border border-muted-foreground hover:border-primary transition-colors" />
        )}
      </button>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span className={`text-sm font-medium truncate ${isCompleted ? 'line-through text-muted-foreground' : ''}`}>
            {task.title}
          </span>
          <Badge variant={statusVariants[task.status] || 'secondary'} className="text-[10px] px-1.5 py-0">
            {task.status.replace(/([A-Z])/g, ' $1').trim()}
          </Badge>
          <span className={`text-[10px] font-medium ${priorityColors[task.priority]}`}>
            {task.priority}
          </span>
        </div>
        {task.description && (
          <p className="text-xs text-muted-foreground mt-1 line-clamp-2">{task.description}</p>
        )}
        {task.dueDate && (
          <div className="flex items-center gap-1 mt-1.5 text-xs text-muted-foreground">
            <Calendar className="h-3 w-3" />
            {new Date(task.dueDate).toLocaleDateString()}
          </div>
        )}
      </div>
      <Button
        variant="ghost"
        size="icon"
        className="h-7 w-7 text-destructive hover:text-destructive shrink-0"
        onClick={(e) => { e.stopPropagation(); onDelete() }}
      >
        <Trash2 className="h-3.5 w-3.5" />
      </Button>
    </div>
  )
}

function MilestoneNoteCard({ note, onDelete }: { note: Note; onDelete: () => void }) {
  return (
    <div className="rounded-lg border border-border bg-card p-3">
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <FileText className="h-4 w-4 text-blue-500 shrink-0" />
            <span className="text-sm font-medium truncate">{note.title}</span>
          </div>
          {note.content && (
            <p className="text-xs text-muted-foreground mt-1 line-clamp-2 ml-6">{note.content}</p>
          )}
          <p className="text-[10px] text-muted-foreground mt-1.5 ml-6">
            {new Date(note.updatedAt || note.createdAt).toLocaleDateString()}
          </p>
        </div>
        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7 text-destructive hover:text-destructive shrink-0"
          onClick={(e) => { e.stopPropagation(); onDelete() }}
        >
          <Trash2 className="h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  )
}
