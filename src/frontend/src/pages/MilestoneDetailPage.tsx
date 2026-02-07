import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
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
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { MarkdownEditor } from '@/components/editor/MarkdownEditor'
import { MarkdownViewer } from '@/components/editor/MarkdownViewer'
import { ContentCard } from '@/components/workspace/ContentCard'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  projects,
  milestones,
  tasks,
  notes,
  type ProjectDetails,
  type MilestoneDetails,
  type Task,
  type MilestoneStatus,
} from '@/lib/api'

type TabType = 'overview' | 'notes' | 'tasks'

export function MilestoneDetailPage() {
  const { projectId, milestoneId } = useParams<{ projectId: string; milestoneId: string }>()
  const navigate = useNavigate()

  const [isLoading, setIsLoading] = useState(true)
  const [project, setProject] = useState<ProjectDetails | null>(null)
  const [milestone, setMilestone] = useState<MilestoneDetails | null>(null)
  const [activeTab, setActiveTab] = useState<TabType>('overview')

  // Name editing
  const [isEditingName, setIsEditingName] = useState(false)
  const [nameValue, setNameValue] = useState('')
  const [isSavingName, setIsSavingName] = useState(false)

  // Content editing - start in readonly mode by default
  const [isEditingContent, setIsEditingContent] = useState(false)
  const [contentValue, setContentValue] = useState('')
  const [isSavingContent, setIsSavingContent] = useState(false)

  // Inline editing for status and dueDate
  const [statusValue, setStatusValue] = useState<MilestoneStatus>('NotStarted')
  const [dueDateValue, setDueDateValue] = useState('')

  // Refresh states
  const [isRefreshingNotes, setIsRefreshingNotes] = useState(false)
  const [isRefreshingTasks, setIsRefreshingTasks] = useState(false)

  useEffect(() => {
    if (projectId && milestoneId) {
      loadData()
    }
  }, [projectId, milestoneId])

  const loadData = async () => {
    if (!projectId || !milestoneId) return

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
    if (!projectId || !milestoneId) return
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
    if (!projectId || !milestoneId) return
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
    if (!projectId || !milestone || !nameValue.trim()) return

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
    if (!projectId || !milestone) return

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
    if (!projectId || !milestone) return

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

  const handleDueDateChange = async (newDueDate: string) => {
    if (!projectId || !milestone) return

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

  const createAndEditNote = async () => {
    if (!milestoneId) return

    const timestamp = new Date().toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    })

    try {
      const newNote = await notes.create({
        title: `Untitled Note - ${timestamp}`,
        content: '',
        milestoneId,
      })
      navigate(`/notes/${newNote.id}`)
    } catch (error) {
      console.error('Failed to create note:', error)
    }
  }

  const createAndEditTask = async () => {
    if (!milestoneId) return

    const timestamp = new Date().toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    })

    try {
      const newTask = await tasks.create({
        title: `Untitled Task - ${timestamp}`,
        description: '',
        priority: 'Medium',
        milestoneId,
      })
      navigate(`/tasks/${newTask.id}`)
    } catch (error) {
      console.error('Failed to create task:', error)
    }
  }

  const handleToggleTaskStatus = async (task: Task) => {
    try {
      if (task.status === 'Completed') {
        // Toggle back to Pending
        await tasks.update(task.id, {
          title: task.title,
          description: task.description,
          priority: task.priority,
          status: 'Pending',
          sortOrder: task.sortOrder,
        })
      } else {
        // Delete the task when completing
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
        <Button variant="outline" className="mt-4" onClick={() => navigate('/workspace')}>
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
    <div className="space-y-6">
      {/* Breadcrumb Header */}
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-2 min-w-0">
          <Button variant="ghost" size="icon" onClick={() => navigate(`/workspace/${projectId}`)}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div className="flex items-center gap-2 text-sm text-muted-foreground min-w-0">
            <button
              onClick={() => navigate(`/workspace/${projectId}`)}
              className="flex items-center gap-1.5 hover:text-foreground transition-colors min-w-0"
            >
              <FolderKanban className="h-4 w-4 shrink-0" />
              <span className="truncate max-w-[80px] sm:max-w-[120px] md:max-w-[200px]">{project.name}</span>
            </button>
            <ChevronRight className="h-4 w-4 shrink-0" />
            <span className="flex items-center gap-1.5 text-foreground font-medium min-w-0">
              <Target className="h-4 w-4 shrink-0" />
              <span className="truncate max-w-[80px] sm:max-w-[120px] md:max-w-[200px]">{milestone.name}</span>
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
            <DropdownMenuItem onClick={() => createAndEditTask()}>
              <CheckSquare className="h-4 w-4 mr-2 text-emerald-500" />
              Task
            </DropdownMenuItem>
            <DropdownMenuItem onClick={createAndEditNote}>
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
              <div className="text-3xl font-bold text-primary">
                {milestone.tasks.length > 0
                  ? Math.round((completedTasks / milestone.tasks.length) * 100)
                  : 0}%
              </div>
              <div className="text-sm text-muted-foreground">Progress</div>
            </div>
          </div>

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
              <div className="rounded-lg border border-border flex flex-col" style={{ height: 'calc(100vh - 450px)', minHeight: '300px' }}>
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
        </div>
      )}

      {activeTab === 'notes' && (
        <div className="space-y-4">
          {milestone.notes.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border p-8 text-center">
              <StickyNote className="h-8 w-8 mx-auto text-muted-foreground" />
              <p className="mt-2 text-sm text-muted-foreground">No notes yet</p>
              <Button variant="outline" className="mt-4" onClick={createAndEditNote}>
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
                <Button variant="outline" size="sm" onClick={createAndEditNote}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Note
                </Button>
              </div>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {milestone.notes.map((note) => (
                  <ContentCard
                    key={note.id}
                    title={note.title}
                    content={note.content}
                    onClick={() => navigate(`/notes/${note.id}`)}
                    onDelete={() => handleDeleteNote(note.id)}
                    date={note.updatedAt || note.createdAt}
                    icon={<FileText className="h-5 w-5 text-muted-foreground" />}
                  />
                ))}
              </div>
            </>
          )}
        </div>
      )}

      {activeTab === 'tasks' && (
        <div className="space-y-4">
          {milestone.tasks.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border p-8 text-center">
              <CheckSquare className="h-8 w-8 mx-auto text-muted-foreground" />
              <p className="mt-2 text-sm text-muted-foreground">No tasks yet</p>
              <Button variant="outline" className="mt-4" onClick={() => createAndEditTask()}>
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
                <Button variant="outline" size="sm" onClick={() => createAndEditTask()}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Task
                </Button>
              </div>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {milestone.tasks.map((task) => (
                  <ContentCard
                    key={task.id}
                    title={task.title}
                    content={task.description}
                    onClick={() => navigate(`/tasks/${task.id}`)}
                    onDelete={() => handleDeleteTask(task.id)}
                    date={task.updatedAt || task.createdAt}
                    status={task.status}
                    priority={task.priority}
                    dueDate={task.dueDate}
                    onToggleComplete={() => handleToggleTaskStatus(task)}
                    isCompleted={task.status === 'Completed'}
                  />
                ))}
              </div>
            </>
          )}
        </div>
      )}

    </div>
  )
}
