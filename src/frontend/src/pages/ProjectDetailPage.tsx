import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Loader2,
  Edit,
  Trash2,
  Plus,
  Target,
  CheckSquare,
  FileText,
  Calendar,
  Flag,
  MoreHorizontal,
  ChevronDown,
  ChevronRight,
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
  type MilestoneDetails,
  type MilestoneSummary,
  type Todo,
  type Note,
  type CreateMilestoneRequest,
  type CreateTodoRequest,
  type CreateNoteRequest,
  ProjectStatus,
  MilestoneStatus,
  TodoStatus,
  TodoPriority,
} from '@/lib/api'

type DialogType = 'milestone' | 'todo' | 'note' | null

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [isLoading, setIsLoading] = useState(true)
  const [project, setProject] = useState<ProjectDetails | null>(null)
  const [projectMilestones, setProjectMilestones] = useState<MilestoneSummary[]>([])
  const [expandedMilestones, setExpandedMilestones] = useState<Set<string>>(new Set())

  const [dialogType, setDialogType] = useState<DialogType>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [selectedMilestoneId, setSelectedMilestoneId] = useState<string | null>(null)

  // Form states
  const [milestoneForm, setMilestoneForm] = useState<CreateMilestoneRequest>({
    name: '',
    description: '',
  })
  const [todoForm, setTodoForm] = useState<CreateTodoRequest>({
    title: '',
    description: '',
    priority: TodoPriority.Medium,
  })
  const [noteForm, setNoteForm] = useState<CreateNoteRequest>({
    title: '',
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
    } catch (error) {
      console.error('Failed to load project:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const toggleMilestone = (milestoneId: string) => {
    setExpandedMilestones((prev) => {
      const next = new Set(prev)
      if (next.has(milestoneId)) {
        next.delete(milestoneId)
      } else {
        next.add(milestoneId)
      }
      return next
    })
  }

  const openDialog = (type: DialogType, milestoneId: string | null = null) => {
    setDialogType(type)
    setSelectedMilestoneId(milestoneId)
  }

  const closeDialog = () => {
    setDialogType(null)
    setSelectedMilestoneId(null)
    setMilestoneForm({ name: '', description: '' })
    setTodoForm({ title: '', description: '', priority: TodoPriority.Medium })
    setNoteForm({ title: '', content: '' })
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

  const handleCreateNote = async () => {
    if (!id || !noteForm.title.trim()) return

    try {
      setIsSubmitting(true)
      await notes.create({
        ...noteForm,
        projectId: selectedMilestoneId ? undefined : id,
        milestoneId: selectedMilestoneId || undefined,
      })
      closeDialog()
      await loadProject()
    } catch (error) {
      console.error('Failed to create note:', error)
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
      const newStatus = todo.status === TodoStatus.Completed ? TodoStatus.NotStarted : TodoStatus.Completed
      await todos.update(todo.id, {
        title: todo.title,
        description: todo.description,
        priority: todo.priority,
        status: newStatus,
        completionNotes: newStatus === TodoStatus.Completed ? todo.completionNotes : undefined,
      })
      await loadProject()
    } catch (error) {
      console.error('Failed to update todo:', error)
    }
  }

  const getStatusBadge = (status: ProjectStatus | MilestoneStatus) => {
    const variants: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
      [ProjectStatus.NotStarted]: 'secondary',
      [ProjectStatus.InProgress]: 'default',
      [ProjectStatus.Completed]: 'outline',
      [ProjectStatus.OnHold]: 'destructive',
      [MilestoneStatus.NotStarted]: 'secondary',
      [MilestoneStatus.InProgress]: 'default',
      [MilestoneStatus.Completed]: 'outline',
    }
    return <Badge variant={variants[status] || 'secondary'}>{status.replace(/([A-Z])/g, ' $1').trim()}</Badge>
  }

  const getPriorityBadge = (priority: TodoPriority) => {
    const variants: Record<TodoPriority, 'default' | 'secondary' | 'destructive' | 'outline'> = {
      [TodoPriority.Low]: 'secondary',
      [TodoPriority.Medium]: 'outline',
      [TodoPriority.High]: 'default',
      [TodoPriority.Critical]: 'destructive',
    }
    return <Badge variant={variants[priority]}>{priority}</Badge>
  }

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
            <DropdownMenuItem onClick={() => openDialog('milestone')}>
              <Target className="h-4 w-4 mr-2" />
              Milestone
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => openDialog('todo')}>
              <CheckSquare className="h-4 w-4 mr-2" />
              Todo
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => openDialog('note')}>
              <FileText className="h-4 w-4 mr-2" />
              Note
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
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

      {/* Milestones */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold flex items-center gap-2">
            <Target className="h-5 w-5" />
            Milestones ({projectMilestones.length})
          </h2>
        </div>

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
          <div className="space-y-2">
            {projectMilestones.map((milestone) => (
              <div key={milestone.id} className="rounded-lg border border-border bg-card">
                <div
                  className="flex items-center justify-between p-4 cursor-pointer hover:bg-muted/50"
                  onClick={() => toggleMilestone(milestone.id)}
                >
                  <div className="flex items-center gap-3">
                    {expandedMilestones.has(milestone.id) ? (
                      <ChevronDown className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    )}
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
                      {milestone.todoCount} todos, {milestone.noteCount} notes
                    </span>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                        <Button variant="ghost" size="icon" className="h-8 w-8">
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={(e) => { e.stopPropagation(); openDialog('todo', milestone.id) }}>
                          <CheckSquare className="h-4 w-4 mr-2" />
                          Add Todo
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={(e) => { e.stopPropagation(); openDialog('note', milestone.id) }}>
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
                {expandedMilestones.has(milestone.id) && milestone.description && (
                  <div className="px-4 pb-4 pt-0 border-t border-border">
                    <p className="text-sm text-muted-foreground mt-3">{milestone.description}</p>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Project Todos */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold flex items-center gap-2">
            <CheckSquare className="h-5 w-5" />
            Todos ({project.todos.length})
          </h2>
        </div>

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
          <div className="space-y-2">
            {project.todos.map((todo) => (
              <div
                key={todo.id}
                className="flex items-center gap-3 rounded-lg border border-border bg-card p-3"
              >
                <button
                  onClick={() => handleToggleTodoStatus(todo)}
                  className={`h-5 w-5 rounded border-2 flex items-center justify-center transition-colors ${
                    todo.status === TodoStatus.Completed
                      ? 'bg-primary border-primary text-primary-foreground'
                      : 'border-muted-foreground hover:border-primary'
                  }`}
                >
                  {todo.status === TodoStatus.Completed && (
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
        )}
      </div>

      {/* Project Notes */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold flex items-center gap-2">
            <FileText className="h-5 w-5" />
            Notes ({project.notes.length})
          </h2>
        </div>

        {project.notes.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border p-8 text-center">
            <FileText className="h-8 w-8 mx-auto text-muted-foreground" />
            <p className="mt-2 text-sm text-muted-foreground">No notes yet</p>
            <Button variant="outline" className="mt-4" onClick={() => openDialog('note')}>
              <Plus className="h-4 w-4 mr-2" />
              Add Note
            </Button>
          </div>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {project.notes.map((note) => (
              <div key={note.id} className="rounded-lg border border-border bg-card p-4">
                <div className="flex items-start justify-between gap-2">
                  <h4 className="font-medium truncate">{note.title}</h4>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7 text-destructive shrink-0"
                    onClick={() => handleDeleteNote(note.id)}
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
        )}
      </div>

      {/* Milestone Dialog */}
      <Dialog open={dialogType === 'milestone'} onOpenChange={closeDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create Milestone</DialogTitle>
            <DialogDescription>Add a new milestone to this project.</DialogDescription>
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
            <div className="space-y-2">
              <Label htmlFor="milestone-due-date">Due Date</Label>
              <Input
                id="milestone-due-date"
                type="date"
                value={milestoneForm.dueDate ? new Date(milestoneForm.dueDate).toISOString().split('T')[0] : ''}
                onChange={(e) => setMilestoneForm({ ...milestoneForm, dueDate: e.target.value ? new Date(e.target.value).toISOString() : undefined })}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={closeDialog}>Cancel</Button>
            <Button onClick={handleCreateMilestone} disabled={isSubmitting || !milestoneForm.name.trim()}>
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Todo Dialog */}
      <Dialog open={dialogType === 'todo'} onOpenChange={closeDialog}>
        <DialogContent>
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
              <Label htmlFor="todo-description">Description</Label>
              <Textarea
                id="todo-description"
                value={todoForm.description || ''}
                onChange={(e) => setTodoForm({ ...todoForm, description: e.target.value })}
                placeholder="Todo description (supports markdown)"
                rows={3}
              />
            </div>
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

      {/* Note Dialog */}
      <Dialog open={dialogType === 'note'} onOpenChange={closeDialog}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Create Note</DialogTitle>
            <DialogDescription>
              Add a new note to {selectedMilestoneId ? 'this milestone' : 'this project'}.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="note-title">Title</Label>
              <Input
                id="note-title"
                value={noteForm.title}
                onChange={(e) => setNoteForm({ ...noteForm, title: e.target.value })}
                placeholder="Note title"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="note-content">Content</Label>
              <Textarea
                id="note-content"
                value={noteForm.content || ''}
                onChange={(e) => setNoteForm({ ...noteForm, content: e.target.value })}
                placeholder="Write your note here (supports markdown)"
                rows={10}
                className="font-mono text-sm"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={closeDialog}>Cancel</Button>
            <Button onClick={handleCreateNote} disabled={isSubmitting || !noteForm.title.trim()}>
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
