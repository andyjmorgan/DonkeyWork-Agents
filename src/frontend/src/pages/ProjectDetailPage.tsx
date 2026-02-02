import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Loader2,
  Trash2,
  Plus,
  Target,
  CheckSquare,
  FileText,
  Calendar,
  MoreHorizontal,
  Pencil,
  Save,
  LayoutDashboard,
  StickyNote,
} from 'lucide-react'
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
import { MarkdownEditor } from '@/components/editor/MarkdownEditor'
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
  todos,
  notes,
  type ProjectDetails,
  type MilestoneSummary,
  type Todo,
  type CreateMilestoneRequest,
  type ProjectStatus,
  type MilestoneStatus,
  type TodoPriority,
} from '@/lib/api'

type DialogType = 'milestone' | null
type TabType = 'overview' | 'milestones' | 'notes' | 'tasks'

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [isLoading, setIsLoading] = useState(true)
  const [project, setProject] = useState<ProjectDetails | null>(null)
  const [projectMilestones, setProjectMilestones] = useState<MilestoneSummary[]>([])
  const [activeTab, setActiveTab] = useState<TabType>('overview')

  // Project content editing - start in edit mode by default
  const [isEditingContent, setIsEditingContent] = useState(true)
  const [contentValue, setContentValue] = useState('')
  const [isSavingContent, setIsSavingContent] = useState(false)

  const [dialogType, setDialogType] = useState<DialogType>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [editingMilestone, setEditingMilestone] = useState<MilestoneSummary | null>(null)

  // Form states
  const [milestoneForm, setMilestoneForm] = useState<CreateMilestoneRequest>({
    name: '',
    content: '',
  })

  useEffect(() => {
    if (id) {
      loadProject()
    }
  }, [id])

  const loadProject = async () => {
    if (!id) return

    try {
      setIsLoading(true)
      const [projectData, milestonesData] = await Promise.all([
        projects.get(id),
        milestones.list(id),
      ])
      setProject(projectData)
      setProjectMilestones(milestonesData)
      setContentValue(projectData.content || '')
    } catch (error) {
      console.error('Failed to load project:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleSaveContent = async () => {
    if (!id || !project) return

    try {
      setIsSavingContent(true)
      await projects.update(id, {
        name: project.name,
        status: project.status,
        successCriteria: project.successCriteria,
        content: contentValue,
      })
      setProject({ ...project, content: contentValue })
      setIsEditingContent(false)
    } catch (error) {
      console.error('Failed to save project content:', error)
    } finally {
      setIsSavingContent(false)
    }
  }

  const openDialog = (type: DialogType) => {
    setDialogType(type)
  }

  const closeDialog = () => {
    setDialogType(null)
    setEditingMilestone(null)
    setMilestoneForm({ name: '', content: '' })
  }

  const openEditMilestone = (milestone: MilestoneSummary) => {
    setEditingMilestone(milestone)
    setMilestoneForm({
      name: milestone.name,
      content: milestone.content,
      dueDate: milestone.dueDate,
      status: milestone.status,
    })
    setDialogType('milestone')
  }

  const createAndEditNote = async (milestoneId: string | null = null) => {
    if (!id) return

    const timestamp = new Date().toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    })

    try {
      setIsSubmitting(true)
      const newNote = await notes.create({
        title: `Untitled Note - ${timestamp}`,
        content: '',
        projectId: milestoneId ? undefined : id,
        milestoneId: milestoneId || undefined,
      })
      navigate(`/notes/${newNote.id}`)
    } catch (error) {
      console.error('Failed to create note:', error)
    } finally {
      setIsSubmitting(false)
    }
  }

  const createAndEditTask = async (milestoneId: string | null = null) => {
    if (!id) return

    const timestamp = new Date().toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    })

    try {
      setIsSubmitting(true)
      const newTask = await todos.create({
        title: `Untitled Task - ${timestamp}`,
        description: '',
        priority: 'Medium',
        projectId: milestoneId ? undefined : id,
        milestoneId: milestoneId || undefined,
      })
      navigate(`/tasks/${newTask.id}`)
    } catch (error) {
      console.error('Failed to create task:', error)
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleCreateMilestone = async () => {
    if (!id || !milestoneForm.name.trim()) return

    try {
      setIsSubmitting(true)
      await milestones.create(id, milestoneForm)
      closeDialog()
      await loadProject()
    } catch (error) {
      console.error('Failed to create milestone:', error)
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleUpdateMilestone = async () => {
    if (!id || !editingMilestone || !milestoneForm.name.trim()) return

    try {
      setIsSubmitting(true)
      await milestones.update(id, editingMilestone.id, {
        name: milestoneForm.name,
        content: milestoneForm.content,
        dueDate: milestoneForm.dueDate,
        status: milestoneForm.status || editingMilestone.status,
        sortOrder: editingMilestone.sortOrder,
      })
      closeDialog()
      await loadProject()
    } catch (error) {
      console.error('Failed to update milestone:', error)
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleDeleteMilestone = async (milestoneId: string) => {
    if (!id || !window.confirm('Are you sure you want to delete this milestone?')) return

    try {
      await milestones.delete(id, milestoneId)
      await loadProject()
    } catch (error) {
      console.error('Failed to delete milestone:', error)
    }
  }

  const handleDeleteTodo = async (todoId: string) => {
    if (!window.confirm('Are you sure you want to delete this todo?')) return

    try {
      await todos.delete(todoId)
      await loadProject()
    } catch (error) {
      console.error('Failed to delete todo:', error)
    }
  }

  const handleDeleteNote = async (noteId: string) => {
    if (!window.confirm('Are you sure you want to delete this note?')) return

    try {
      await notes.delete(noteId)
      await loadProject()
    } catch (error) {
      console.error('Failed to delete note:', error)
    }
  }

  const handleToggleTodoStatus = async (todo: Todo) => {
    try {
      if (todo.status === 'Completed') {
        // Toggle back to Pending
        await todos.update(todo.id, {
          title: todo.title,
          description: todo.description,
          priority: todo.priority,
          status: 'Pending',
          sortOrder: todo.sortOrder,
        })
      } else {
        // Delete the todo when completing
        await todos.delete(todo.id)
      }
      await loadProject()
    } catch (error) {
      console.error('Failed to update todo:', error)
    }
  }

  const getStatusBadge = (status: ProjectStatus | MilestoneStatus) => {
    const variants: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
      NotStarted: 'secondary',
      InProgress: 'default',
      Completed: 'outline',
      OnHold: 'destructive',
      Cancelled: 'destructive',
    }
    return <Badge variant={variants[status] || 'secondary'}>{status.replace(/([A-Z])/g, ' $1').trim()}</Badge>
  }

  const getPriorityBadge = (priority: TodoPriority) => {
    const variants: Record<TodoPriority, 'default' | 'secondary' | 'destructive' | 'outline'> = {
      Low: 'secondary',
      Medium: 'outline',
      High: 'default',
      Critical: 'destructive',
    }
    return <Badge variant={variants[priority]}>{priority}</Badge>
  }

  const completedTodos = project?.todos.filter(t => t.status === 'Completed').length || 0
  const pendingTodos = project?.todos.filter(t => t.status !== 'Completed').length || 0

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!project) {
    return (
      <div className="flex flex-col items-center justify-center h-64">
        <p className="text-muted-foreground">Project not found</p>
        <Button variant="outline" className="mt-4" onClick={() => navigate('/projects')}>
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
      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-4">
          <Button variant="ghost" size="icon" onClick={() => navigate('/projects')}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <div className="flex items-center gap-3">
              <h1 className="text-2xl font-bold">{project.name}</h1>
              {getStatusBadge(project.status)}
            </div>
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
            <DropdownMenuItem onClick={() => { openDialog('milestone'); setActiveTab('milestones') }}>
              <Target className="h-4 w-4 mr-2 text-purple-500" />
              Milestone
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => { createAndEditTask(null); setActiveTab('tasks') }}>
              <CheckSquare className="h-4 w-4 mr-2 text-emerald-500" />
              Task
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => createAndEditNote(null)}>
              <FileText className="h-4 w-4 mr-2 text-blue-500" />
              Note
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 overflow-x-auto pb-1">
        <TabButton tab="overview" icon={LayoutDashboard} iconColor="text-slate-500" label="Overview" />
        <TabButton tab="milestones" icon={Target} iconColor="text-purple-500" label="Milestones" count={projectMilestones.length} />
        <TabButton tab="notes" icon={StickyNote} iconColor="text-blue-500" label="Notes" count={project.notes.length} />
        <TabButton tab="tasks" icon={CheckSquare} iconColor="text-emerald-500" label="Tasks" count={project.todos.length} />
      </div>

      {/* Tab Content */}
      {activeTab === 'overview' && (
        <div className="space-y-6">
          {/* Stats Cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="rounded-lg border border-border bg-card p-4">
              <div className="text-3xl font-bold text-primary">{projectMilestones.length}</div>
              <div className="text-sm text-muted-foreground">Milestones</div>
            </div>
            <div className="rounded-lg border border-border bg-card p-4">
              <div className="text-3xl font-bold text-emerald-500">{completedTodos}</div>
              <div className="text-sm text-muted-foreground">Completed</div>
            </div>
            <div className="rounded-lg border border-border bg-card p-4">
              <div className="text-3xl font-bold text-amber-500">{pendingTodos}</div>
              <div className="text-sm text-muted-foreground">Pending</div>
            </div>
            <div className="rounded-lg border border-border bg-card p-4">
              <div className="text-3xl font-bold text-blue-500">{project.notes.length}</div>
              <div className="text-sm text-muted-foreground">Notes</div>
            </div>
          </div>

          {/* Success Criteria */}
          {project.successCriteria && (
            <div className="rounded-lg border border-border bg-card p-4">
              <h3 className="text-sm font-semibold text-muted-foreground mb-2">Success Criteria</h3>
              <p className="text-sm whitespace-pre-wrap">{project.successCriteria}</p>
            </div>
          )}

          {/* Tags */}
          {project.tags.length > 0 && (
            <div className="flex items-center gap-2 flex-wrap">
              {project.tags.map((tag) => (
                <Badge key={tag.id} variant="secondary">
                  {tag.name}
                </Badge>
              ))}
            </div>
          )}

          {/* Project Content */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold flex items-center gap-2">
                <FileText className="h-5 w-5" />
                Project Scope
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
                      setContentValue(project.content || '')
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
              <div className="rounded-lg border border-border flex flex-col" style={{ height: 'calc(100vh - 350px)', minHeight: '400px' }}>
                <MarkdownEditor
                  content={contentValue}
                  onChange={setContentValue}
                  placeholder="Write your project scope here..."
                  className="flex-1 h-full"
                />
              </div>
            ) : (
              <div className="rounded-lg border border-border bg-card p-4">
                {project.content ? (
                  <div className="prose prose-sm dark:prose-invert max-w-none whitespace-pre-wrap">
                    {project.content}
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground italic">No project scope yet. Click Edit to add one.</p>
                )}
              </div>
            )}
          </div>

          {/* Recent Tasks Preview */}
          {project.todos.length > 0 && (
            <div className="rounded-lg border border-border bg-card p-4">
              <h4 className="text-sm font-medium text-muted-foreground mb-3">Recent Tasks</h4>
              <div className="space-y-2">
                {project.todos.slice(0, 5).map((todo) => (
                  <div key={todo.id} className="flex items-center gap-2 text-sm">
                    {todo.status === 'Completed' ? (
                      <CheckSquare className="h-3.5 w-3.5 text-emerald-500 flex-shrink-0" />
                    ) : (
                      <div className="h-3.5 w-3.5 rounded border border-muted-foreground flex-shrink-0" />
                    )}
                    <span className={`truncate ${todo.status === 'Completed' ? 'line-through text-muted-foreground' : ''}`}>
                      {todo.title}
                    </span>
                  </div>
                ))}
              </div>
              {project.todos.length > 5 && (
                <button
                  onClick={() => setActiveTab('tasks')}
                  className="text-xs text-primary hover:underline mt-2"
                >
                  View all {project.todos.length} tasks
                </button>
              )}
            </div>
          )}
        </div>
      )}

      {activeTab === 'milestones' && (
        <div className="space-y-4">
          {projectMilestones.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border p-8 text-center">
              <Target className="h-8 w-8 mx-auto text-muted-foreground" />
              <p className="mt-2 text-sm text-muted-foreground">No milestones yet</p>
              <Button variant="outline" className="mt-4" onClick={() => openDialog('milestone')}>
                <Plus className="h-4 w-4 mr-2" />
                Add Milestone
              </Button>
            </div>
          ) : (
            <>
              <div className="flex justify-end">
                <Button variant="outline" size="sm" onClick={() => openDialog('milestone')}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Milestone
                </Button>
              </div>
              <div className="space-y-2">
                {projectMilestones.map((milestone) => (
                  <div
                    key={milestone.id}
                    className="rounded-lg border border-border bg-card hover:shadow-md transition-shadow cursor-pointer"
                    onClick={() => navigate(`/projects/${id}/milestones/${milestone.id}`)}
                  >
                    <div className="flex items-center justify-between p-4">
                      <div className="flex items-center gap-3">
                        <Target className="h-5 w-5 text-muted-foreground" />
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-medium">{milestone.name}</span>
                            {getStatusBadge(milestone.status)}
                          </div>
                          {milestone.dueDate && (
                            <div className="flex items-center gap-1 text-xs text-muted-foreground mt-1">
                              <Calendar className="h-3 w-3" />
                              {new Date(milestone.dueDate).toLocaleDateString()}
                            </div>
                          )}
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <span className="text-sm text-muted-foreground">
                          {milestone.completedTodoCount}/{milestone.todoCount} todos
                        </span>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                            <Button variant="ghost" size="icon" className="h-8 w-8">
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={(e) => { e.stopPropagation(); openEditMilestone(milestone) }}>
                              <Pencil className="h-4 w-4 mr-2 text-amber-500" />
                              Edit
                            </DropdownMenuItem>
                            <DropdownMenuItem onClick={(e) => { e.stopPropagation(); createAndEditTask(milestone.id) }}>
                              <CheckSquare className="h-4 w-4 mr-2 text-emerald-500" />
                              Add Task
                            </DropdownMenuItem>
                            <DropdownMenuItem onClick={(e) => { e.stopPropagation(); createAndEditNote(milestone.id) }}>
                              <FileText className="h-4 w-4 mr-2 text-blue-500" />
                              Add Note
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              className="text-destructive"
                              onClick={(e) => { e.stopPropagation(); handleDeleteMilestone(milestone.id) }}
                            >
                              <Trash2 className="h-4 w-4 mr-2" />
                              Delete
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </>
          )}
        </div>
      )}

      {activeTab === 'notes' && (
        <div className="space-y-4">
          {project.notes.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border p-8 text-center">
              <StickyNote className="h-8 w-8 mx-auto text-muted-foreground" />
              <p className="mt-2 text-sm text-muted-foreground">No notes yet</p>
              <Button variant="outline" className="mt-4" onClick={() => createAndEditNote(null)}>
                <Plus className="h-4 w-4 mr-2" />
                Add Note
              </Button>
            </div>
          ) : (
            <>
              <div className="flex justify-end">
                <Button variant="outline" size="sm" onClick={() => createAndEditNote(null)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Note
                </Button>
              </div>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {project.notes.map((note) => (
                  <div
                    key={note.id}
                    className="rounded-lg border border-border bg-card p-4 hover:shadow-md transition-shadow cursor-pointer min-h-[140px] flex flex-col"
                    onClick={() => navigate(`/notes/${note.id}`)}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <h4 className="font-medium truncate">{note.title}</h4>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-7 w-7 text-destructive shrink-0"
                        onClick={(e) => {
                          e.stopPropagation()
                          handleDeleteNote(note.id)
                        }}
                      >
                        <Trash2 className="h-3 w-3" />
                      </Button>
                    </div>
                    {note.content && (
                      <p className="mt-2 text-sm text-muted-foreground line-clamp-5 flex-1">{note.content}</p>
                    )}
                    <div className="mt-auto pt-2 text-xs text-muted-foreground">
                      {new Date(note.updatedAt || note.createdAt).toLocaleDateString()}
                    </div>
                  </div>
                ))}
              </div>
            </>
          )}
        </div>
      )}

      {activeTab === 'tasks' && (
        <div className="space-y-6">
          {project.todos.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border p-8 text-center">
              <CheckSquare className="h-8 w-8 mx-auto text-muted-foreground" />
              <p className="mt-2 text-sm text-muted-foreground">No tasks yet</p>
              <Button variant="outline" className="mt-4" onClick={() => createAndEditTask(null)}>
                <Plus className="h-4 w-4 mr-2" />
                Add Task
              </Button>
            </div>
          ) : (
            <>
              <div className="flex justify-end">
                <Button variant="outline" size="sm" onClick={() => createAndEditTask(null)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Task
                </Button>
              </div>

              {/* Pending Tasks */}
              {pendingTodos > 0 && (
                <div className="space-y-3">
                  <h3 className="text-sm font-semibold text-muted-foreground flex items-center gap-2">
                    Pending
                    <span className="text-xs px-1.5 py-0.5 rounded-full bg-amber-500/20 text-amber-500">
                      {pendingTodos}
                    </span>
                  </h3>
                  <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                    {project.todos.filter(t => t.status !== 'Completed').map((todo) => (
                      <div
                        key={todo.id}
                        className="rounded-lg border border-border bg-card p-4 hover:shadow-md transition-shadow cursor-pointer min-h-[140px] flex flex-col"
                        onClick={() => navigate(`/tasks/${todo.id}`)}
                      >
                        {/* Header with badges */}
                        <div className="flex items-center gap-2 mb-2 flex-wrap">
                          {getPriorityBadge(todo.priority)}
                          <Badge variant="secondary">{todo.status}</Badge>
                          {todo.dueDate && (
                            <span className="text-xs text-muted-foreground flex items-center gap-1">
                              <Calendar className="h-3 w-3" />
                              {new Date(todo.dueDate).toLocaleDateString()}
                            </span>
                          )}
                        </div>
                        {/* Title and actions */}
                        <div className="flex items-start justify-between gap-2">
                          <div className="flex items-center gap-2 min-w-0">
                            <button
                              onClick={(e) => { e.stopPropagation(); handleToggleTodoStatus(todo) }}
                              className="h-5 w-5 rounded border-2 border-muted-foreground hover:border-primary flex items-center justify-center transition-colors shrink-0"
                              title="Complete task"
                            />
                            <h4 className="font-medium truncate">{todo.title}</h4>
                          </div>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-7 w-7 text-destructive shrink-0"
                            onClick={(e) => { e.stopPropagation(); handleDeleteTodo(todo.id) }}
                          >
                            <Trash2 className="h-3 w-3" />
                          </Button>
                        </div>
                        {/* Description */}
                        {todo.description && (
                          <p className="mt-2 text-sm text-muted-foreground line-clamp-3 flex-1">{todo.description}</p>
                        )}
                        {/* Footer */}
                        <div className="mt-auto pt-2 text-xs text-muted-foreground">
                          {new Date(todo.updatedAt || todo.createdAt).toLocaleDateString()}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Completed Tasks */}
              {completedTodos > 0 && (
                <div className="space-y-3">
                  <h3 className="text-sm font-semibold text-muted-foreground flex items-center gap-2">
                    Completed
                    <span className="text-xs px-1.5 py-0.5 rounded-full bg-emerald-500/20 text-emerald-500">
                      {completedTodos}
                    </span>
                  </h3>
                  <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                    {project.todos.filter(t => t.status === 'Completed').map((todo) => (
                      <div
                        key={todo.id}
                        className="rounded-lg border border-border bg-card/50 p-4 hover:shadow-md transition-shadow cursor-pointer min-h-[140px] flex flex-col opacity-75"
                        onClick={() => navigate(`/tasks/${todo.id}`)}
                      >
                        {/* Header with badges */}
                        <div className="flex items-center gap-2 mb-2 flex-wrap">
                          {getPriorityBadge(todo.priority)}
                          <Badge variant="default" className="bg-emerald-500/20 text-emerald-500 border-emerald-500/30">Completed</Badge>
                        </div>
                        {/* Title and actions */}
                        <div className="flex items-start justify-between gap-2">
                          <div className="flex items-center gap-2 min-w-0">
                            <button
                              onClick={(e) => { e.stopPropagation(); handleToggleTodoStatus(todo) }}
                              className="h-5 w-5 rounded border-2 bg-primary border-primary text-primary-foreground flex items-center justify-center transition-colors shrink-0"
                              title="Mark as pending"
                            >
                              <CheckSquare className="h-3 w-3" />
                            </button>
                            <h4 className="font-medium truncate line-through text-muted-foreground">{todo.title}</h4>
                          </div>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-7 w-7 text-destructive shrink-0"
                            onClick={(e) => { e.stopPropagation(); handleDeleteTodo(todo.id) }}
                          >
                            <Trash2 className="h-3 w-3" />
                          </Button>
                        </div>
                        {/* Description */}
                        {todo.description && (
                          <p className="mt-2 text-sm text-muted-foreground line-clamp-3 flex-1">{todo.description}</p>
                        )}
                        {/* Footer */}
                        <div className="mt-auto pt-2 text-xs text-muted-foreground">
                          {new Date(todo.updatedAt || todo.createdAt).toLocaleDateString()}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* Milestone Dialog */}
      <Dialog open={dialogType === 'milestone'} onOpenChange={closeDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editingMilestone ? 'Edit Milestone' : 'Create Milestone'}</DialogTitle>
            <DialogDescription>
              {editingMilestone ? 'Update milestone details.' : 'Add a new milestone to this project.'}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="milestone-name">Name</Label>
              <Input
                id="milestone-name"
                value={milestoneForm.name}
                onChange={(e) => setMilestoneForm({ ...milestoneForm, name: e.target.value })}
                placeholder="Milestone name"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="milestone-content">Content</Label>
              <Textarea
                id="milestone-content"
                value={milestoneForm.content || ''}
                onChange={(e) => setMilestoneForm({ ...milestoneForm, content: e.target.value })}
                placeholder="Milestone content"
                rows={3}
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="milestone-due-date">Due Date</Label>
                <Input
                  id="milestone-due-date"
                  type="date"
                  value={milestoneForm.dueDate ? new Date(milestoneForm.dueDate).toISOString().split('T')[0] : ''}
                  onChange={(e) => setMilestoneForm({ ...milestoneForm, dueDate: e.target.value ? new Date(e.target.value).toISOString() : undefined })}
                />
              </div>
              {editingMilestone && (
                <div className="space-y-2">
                  <Label htmlFor="milestone-status">Status</Label>
                  <Select
                    value={milestoneForm.status || editingMilestone.status}
                    onValueChange={(value) => setMilestoneForm({ ...milestoneForm, status: value as MilestoneStatus })}
                  >
                    <SelectTrigger>
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
                </div>
              )}
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={closeDialog}>Cancel</Button>
            <Button
              onClick={editingMilestone ? handleUpdateMilestone : handleCreateMilestone}
              disabled={isSubmitting || !milestoneForm.name.trim()}
            >
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              {editingMilestone ? 'Update' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

    </div>
  )
}
