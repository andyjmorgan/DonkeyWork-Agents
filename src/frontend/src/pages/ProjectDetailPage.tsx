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
  type CreateTodoRequest,
  type ProjectStatus,
  type MilestoneStatus,
  type TodoStatus,
  type TodoPriority,
} from '@/lib/api'

type DialogType = 'milestone' | 'todo' | null
type TabType = 'overview' | 'milestones' | 'notes' | 'tasks'

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [isLoading, setIsLoading] = useState(true)
  const [project, setProject] = useState<ProjectDetails | null>(null)
  const [projectMilestones, setProjectMilestones] = useState<MilestoneSummary[]>([])
  const [activeTab, setActiveTab] = useState<TabType>('overview')

  // Project body editing
  const [isEditingBody, setIsEditingBody] = useState(false)
  const [bodyContent, setBodyContent] = useState('')
  const [isSavingBody, setIsSavingBody] = useState(false)

  const [dialogType, setDialogType] = useState<DialogType>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [selectedMilestoneId, setSelectedMilestoneId] = useState<string | null>(null)
  const [editingMilestone, setEditingMilestone] = useState<MilestoneSummary | null>(null)

  // Form states
  const [milestoneForm, setMilestoneForm] = useState<CreateMilestoneRequest>({
    name: '',
    description: '',
  })
  const [todoForm, setTodoForm] = useState<CreateTodoRequest>({
    title: '',
    description: '',
    priority: 'Medium',
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
      setBodyContent(projectData.body || '')
    } catch (error) {
      console.error('Failed to load project:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleSaveBody = async () => {
    if (!id || !project) return

    try {
      setIsSavingBody(true)
      await projects.update(id, {
        name: project.name,
        description: project.description,
        status: project.status,
        successCriteria: project.successCriteria,
        body: bodyContent,
      })
      setProject({ ...project, body: bodyContent })
      setIsEditingBody(false)
    } catch (error) {
      console.error('Failed to save project body:', error)
    } finally {
      setIsSavingBody(false)
    }
  }

  const openDialog = (type: DialogType, milestoneId: string | null = null) => {
    setDialogType(type)
    setSelectedMilestoneId(milestoneId)
  }

  const closeDialog = () => {
    setDialogType(null)
    setSelectedMilestoneId(null)
    setEditingMilestone(null)
    setMilestoneForm({ name: '', description: '' })
    setTodoForm({ title: '', description: '', priority: 'Medium' })
  }

  const openEditMilestone = (milestone: MilestoneSummary) => {
    setEditingMilestone(milestone)
    setMilestoneForm({
      name: milestone.name,
      description: milestone.description,
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
        description: milestoneForm.description,
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

  const handleCreateTodo = async () => {
    if (!id || !todoForm.title.trim()) return

    try {
      setIsSubmitting(true)
      await todos.create({
        ...todoForm,
        projectId: selectedMilestoneId ? undefined : id,
        milestoneId: selectedMilestoneId || undefined,
      })
      closeDialog()
      await loadProject()
    } catch (error) {
      console.error('Failed to create todo:', error)
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
      const newStatus: TodoStatus = todo.status === 'Completed' ? 'Pending' : 'Completed'
      await todos.update(todo.id, {
        title: todo.title,
        description: todo.description,
        priority: todo.priority,
        status: newStatus,
        sortOrder: todo.sortOrder,
        completionNotes: newStatus === 'Completed' ? todo.completionNotes : undefined,
      })
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

  const TabButton = ({ tab, icon: Icon, label, count }: { tab: TabType; icon: React.ElementType; label: string; count?: number }) => (
    <button
      onClick={() => setActiveTab(tab)}
      className={`flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-lg transition-colors ${
        activeTab === tab
          ? 'bg-primary/10 text-primary border border-primary/30'
          : 'text-muted-foreground hover:bg-muted'
      }`}
    >
      <Icon className="h-4 w-4" />
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
            {project.description && (
              <p className="mt-1 text-muted-foreground">{project.description}</p>
            )}
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
              <Target className="h-4 w-4 mr-2" />
              Milestone
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => { openDialog('todo'); setActiveTab('tasks') }}>
              <CheckSquare className="h-4 w-4 mr-2" />
              Todo
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => createAndEditNote(null)}>
              <FileText className="h-4 w-4 mr-2" />
              Note
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 overflow-x-auto pb-1">
        <TabButton tab="overview" icon={LayoutDashboard} label="Overview" />
        <TabButton tab="milestones" icon={Target} label="Milestones" count={projectMilestones.length} />
        <TabButton tab="notes" icon={StickyNote} label="Notes" count={project.notes.length} />
        <TabButton tab="tasks" icon={CheckSquare} label="Tasks" count={project.todos.length} />
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

          {/* Project Body/Description */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold flex items-center gap-2">
                <FileText className="h-5 w-5" />
                Description
              </h2>
              {!isEditingBody ? (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setIsEditingBody(true)}
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
                      setBodyContent(project.body || '')
                      setIsEditingBody(false)
                    }}
                  >
                    Cancel
                  </Button>
                  <Button
                    size="sm"
                    onClick={handleSaveBody}
                    disabled={isSavingBody}
                  >
                    {isSavingBody && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
                    <Save className="h-4 w-4 mr-2" />
                    Save
                  </Button>
                </div>
              )}
            </div>
            {isEditingBody ? (
              <Textarea
                value={bodyContent}
                onChange={(e) => setBodyContent(e.target.value)}
                placeholder="Write your project description here (supports markdown)..."
                className="min-h-[300px] resize-none font-mono text-sm"
              />
            ) : (
              <div className="rounded-lg border border-border bg-card p-4">
                {project.body ? (
                  <div className="prose prose-sm dark:prose-invert max-w-none whitespace-pre-wrap">
                    {project.body}
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground italic">No description yet. Click Edit to add one.</p>
                )}
              </div>
            )}
          </div>

          {/* Recent Items Preview */}
          <div className="grid md:grid-cols-2 gap-4">
            {/* Recent Milestones */}
            {projectMilestones.length > 0 && (
              <div className="rounded-lg border border-border bg-card p-4">
                <h4 className="text-sm font-medium text-muted-foreground mb-3">Recent Milestones</h4>
                <div className="space-y-2">
                  {projectMilestones.slice(0, 3).map((milestone) => (
                    <div
                      key={milestone.id}
                      className="flex items-center gap-2 text-sm cursor-pointer hover:text-primary transition-colors"
                      onClick={() => navigate(`/projects/${id}/milestones/${milestone.id}`)}
                    >
                      <Target className="h-3.5 w-3.5 text-muted-foreground flex-shrink-0" />
                      <span className="truncate">{milestone.name}</span>
                      <span className="text-xs text-muted-foreground ml-auto">
                        {milestone.completedTodoCount}/{milestone.todoCount}
                      </span>
                    </div>
                  ))}
                </div>
                {projectMilestones.length > 3 && (
                  <button
                    onClick={() => setActiveTab('milestones')}
                    className="text-xs text-primary hover:underline mt-2"
                  >
                    View all {projectMilestones.length} milestones
                  </button>
                )}
              </div>
            )}

            {/* Recent Tasks */}
            {project.todos.length > 0 && (
              <div className="rounded-lg border border-border bg-card p-4">
                <h4 className="text-sm font-medium text-muted-foreground mb-3">Recent Tasks</h4>
                <div className="space-y-2">
                  {project.todos.slice(0, 3).map((todo) => (
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
                {project.todos.length > 3 && (
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
                              <Pencil className="h-4 w-4 mr-2" />
                              Edit
                            </DropdownMenuItem>
                            <DropdownMenuItem onClick={(e) => { e.stopPropagation(); openDialog('todo', milestone.id) }}>
                              <CheckSquare className="h-4 w-4 mr-2" />
                              Add Todo
                            </DropdownMenuItem>
                            <DropdownMenuItem onClick={(e) => { e.stopPropagation(); createAndEditNote(milestone.id) }}>
                              <FileText className="h-4 w-4 mr-2" />
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
                    className="rounded-lg border border-border bg-card p-4 hover:shadow-md transition-shadow cursor-pointer"
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
                      <p className="mt-2 text-sm text-muted-foreground line-clamp-3">{note.content}</p>
                    )}
                  </div>
                ))}
              </div>
            </>
          )}
        </div>
      )}

      {activeTab === 'tasks' && (
        <div className="space-y-4">
          {project.todos.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border p-8 text-center">
              <CheckSquare className="h-8 w-8 mx-auto text-muted-foreground" />
              <p className="mt-2 text-sm text-muted-foreground">No todos yet</p>
              <Button variant="outline" className="mt-4" onClick={() => openDialog('todo')}>
                <Plus className="h-4 w-4 mr-2" />
                Add Todo
              </Button>
            </div>
          ) : (
            <>
              <div className="flex justify-end">
                <Button variant="outline" size="sm" onClick={() => openDialog('todo')}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Todo
                </Button>
              </div>
              <div className="space-y-2">
                {project.todos.map((todo) => (
                  <div
                    key={todo.id}
                    className="flex items-center gap-3 rounded-lg border border-border bg-card p-3"
                  >
                    <button
                      onClick={() => handleToggleTodoStatus(todo)}
                      className={`h-5 w-5 rounded border-2 flex items-center justify-center transition-colors ${
                        todo.status === 'Completed'
                          ? 'bg-primary border-primary text-primary-foreground'
                          : 'border-muted-foreground hover:border-primary'
                      }`}
                    >
                      {todo.status === 'Completed' && (
                        <CheckSquare className="h-3 w-3" />
                      )}
                    </button>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span
                          className={`font-medium ${
                            todo.status === 'Completed' ? 'line-through text-muted-foreground' : ''
                          }`}
                        >
                          {todo.title}
                        </span>
                        {getPriorityBadge(todo.priority)}
                      </div>
                      {todo.description && (
                        <p className="text-sm text-muted-foreground truncate">{todo.description}</p>
                      )}
                    </div>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive"
                      onClick={() => handleDeleteTodo(todo.id)}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                ))}
              </div>
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
              <Label htmlFor="milestone-description">Description</Label>
              <Textarea
                id="milestone-description"
                value={milestoneForm.description || ''}
                onChange={(e) => setMilestoneForm({ ...milestoneForm, description: e.target.value })}
                placeholder="Milestone description"
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

      {/* Todo Dialog */}
      <Dialog open={dialogType === 'todo'} onOpenChange={closeDialog}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Create Todo</DialogTitle>
            <DialogDescription>
              Add a new todo to {selectedMilestoneId ? 'this milestone' : 'this project'}.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="todo-title">Title</Label>
              <Input
                id="todo-title"
                value={todoForm.title}
                onChange={(e) => setTodoForm({ ...todoForm, title: e.target.value })}
                placeholder="Todo title"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="todo-description">Description (Markdown)</Label>
              <Textarea
                id="todo-description"
                value={todoForm.description || ''}
                onChange={(e) => setTodoForm({ ...todoForm, description: e.target.value })}
                placeholder="Write your todo description here (supports markdown)"
                rows={8}
                className="font-mono text-sm"
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="todo-priority">Priority</Label>
                <Select
                  value={todoForm.priority}
                  onValueChange={(value) => setTodoForm({ ...todoForm, priority: value as TodoPriority })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Low">Low</SelectItem>
                    <SelectItem value="Medium">Medium</SelectItem>
                    <SelectItem value="High">High</SelectItem>
                    <SelectItem value="Critical">Critical</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="todo-due-date">Due Date</Label>
                <Input
                  id="todo-due-date"
                  type="date"
                  value={todoForm.dueDate ? new Date(todoForm.dueDate).toISOString().split('T')[0] : ''}
                  onChange={(e) => setTodoForm({ ...todoForm, dueDate: e.target.value ? new Date(e.target.value).toISOString() : undefined })}
                />
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={closeDialog}>Cancel</Button>
            <Button onClick={handleCreateTodo} disabled={isSubmitting || !todoForm.title.trim()}>
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
