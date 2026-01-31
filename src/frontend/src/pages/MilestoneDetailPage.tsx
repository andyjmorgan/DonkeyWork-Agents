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
  FolderKanban,
  ChevronRight,
  Pencil,
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
  type Todo,
  type CreateTodoRequest,
  type MilestoneStatus,
  type TodoStatus,
  type TodoPriority,
} from '@/lib/api'

export function MilestoneDetailPage() {
  const { projectId, milestoneId } = useParams<{ projectId: string; milestoneId: string }>()
  const navigate = useNavigate()

  const [isLoading, setIsLoading] = useState(true)
  const [project, setProject] = useState<ProjectDetails | null>(null)
  const [milestone, setMilestone] = useState<MilestoneDetails | null>(null)

  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [isTodoDialogOpen, setIsTodoDialogOpen] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const [editForm, setEditForm] = useState({
    name: '',
    description: '',
    dueDate: '',
    status: 'NotStarted' as MilestoneStatus,
  })

  const [todoForm, setTodoForm] = useState<CreateTodoRequest>({
    title: '',
    description: '',
    priority: 'Medium',
  })

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
    } catch (error) {
      console.error('Failed to load data:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const openEditDialog = () => {
    if (!milestone) return
    setEditForm({
      name: milestone.name,
      description: milestone.description || '',
      dueDate: milestone.dueDate || '',
      status: milestone.status,
    })
    setIsEditDialogOpen(true)
  }

  const handleUpdateMilestone = async () => {
    if (!projectId || !milestone || !editForm.name.trim()) return

    try {
      setIsSubmitting(true)
      await milestones.update(projectId, milestone.id, {
        name: editForm.name,
        description: editForm.description || undefined,
        dueDate: editForm.dueDate || undefined,
        status: editForm.status,
        sortOrder: milestone.sortOrder,
      })
      setIsEditDialogOpen(false)
      await loadData()
    } catch (error) {
      console.error('Failed to update milestone:', error)
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleCreateTodo = async () => {
    if (!milestoneId || !todoForm.title.trim()) return

    try {
      setIsSubmitting(true)
      await todos.create({
        ...todoForm,
        milestoneId,
      })
      setIsTodoDialogOpen(false)
      setTodoForm({ title: '', description: '', priority: 'Medium' })
      await loadData()
    } catch (error) {
      console.error('Failed to create todo:', error)
    } finally {
      setIsSubmitting(false)
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
      setIsSubmitting(true)
      const newNote = await notes.create({
        title: `Untitled Note - ${timestamp}`,
        content: '',
        milestoneId,
      })
      navigate(`/notes/${newNote.id}`)
    } catch (error) {
      console.error('Failed to create note:', error)
    } finally {
      setIsSubmitting(false)
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
      })
      await loadData()
    } catch (error) {
      console.error('Failed to update todo:', error)
    }
  }

  const handleDeleteTodo = async (todoId: string) => {
    if (!window.confirm('Are you sure you want to delete this todo?')) return

    try {
      await todos.delete(todoId)
      await loadData()
    } catch (error) {
      console.error('Failed to delete todo:', error)
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

  const getStatusBadge = (status: MilestoneStatus) => {
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
        <Button variant="outline" className="mt-4" onClick={() => navigate('/projects')}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Projects
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb Header */}
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-2 min-w-0">
          <Button variant="ghost" size="icon" onClick={() => navigate(`/projects/${projectId}`)}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div className="flex items-center gap-2 text-sm text-muted-foreground min-w-0">
            <button
              onClick={() => navigate(`/projects/${projectId}`)}
              className="flex items-center gap-1.5 hover:text-foreground transition-colors"
            >
              <FolderKanban className="h-4 w-4 shrink-0" />
              <span className="truncate max-w-[200px]">{project.name}</span>
            </button>
            <ChevronRight className="h-4 w-4 shrink-0" />
            <span className="flex items-center gap-1.5 text-foreground font-medium">
              <Target className="h-4 w-4 shrink-0" />
              <span className="truncate">{milestone.name}</span>
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
            <DropdownMenuItem onClick={() => setIsTodoDialogOpen(true)}>
              <CheckSquare className="h-4 w-4 mr-2" />
              Todo
            </DropdownMenuItem>
            <DropdownMenuItem onClick={createAndEditNote}>
              <FileText className="h-4 w-4 mr-2" />
              Note
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Milestone Info */}
      <div className="rounded-lg border border-border bg-card p-4">
        <div className="flex items-start justify-between gap-4">
          <div className="space-y-2">
            <div className="flex items-center gap-3">
              <h1 className="text-xl font-bold">{milestone.name}</h1>
              {getStatusBadge(milestone.status)}
            </div>
            {milestone.dueDate && (
              <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                <Calendar className="h-4 w-4" />
                Due: {new Date(milestone.dueDate).toLocaleDateString()}
              </div>
            )}
            {milestone.description && (
              <p className="text-sm text-muted-foreground">{milestone.description}</p>
            )}
          </div>
          <Button variant="ghost" size="icon" onClick={openEditDialog}>
            <Pencil className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Todos */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold flex items-center gap-2">
            <CheckSquare className="h-5 w-5" />
            Todos ({milestone.todos.length})
          </h2>
        </div>

        {milestone.todos.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border p-8 text-center">
            <CheckSquare className="h-8 w-8 mx-auto text-muted-foreground" />
            <p className="mt-2 text-sm text-muted-foreground">No todos yet</p>
            <Button variant="outline" className="mt-4" onClick={() => setIsTodoDialogOpen(true)}>
              <Plus className="h-4 w-4 mr-2" />
              Add Todo
            </Button>
          </div>
        ) : (
          <div className="space-y-2">
            {milestone.todos.map((todo) => (
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
        )}
      </div>

      {/* Notes */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold flex items-center gap-2">
            <FileText className="h-5 w-5" />
            Notes ({milestone.notes.length})
          </h2>
        </div>

        {milestone.notes.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border p-8 text-center">
            <FileText className="h-8 w-8 mx-auto text-muted-foreground" />
            <p className="mt-2 text-sm text-muted-foreground">No notes yet</p>
            <Button variant="outline" className="mt-4" onClick={createAndEditNote}>
              <Plus className="h-4 w-4 mr-2" />
              Add Note
            </Button>
          </div>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {milestone.notes.map((note) => (
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
        )}
      </div>

      {/* Edit Milestone Dialog */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Milestone</DialogTitle>
            <DialogDescription>Update milestone details.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="edit-name">Name</Label>
              <Input
                id="edit-name"
                value={editForm.name}
                onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                placeholder="Milestone name"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="edit-description">Description</Label>
              <Textarea
                id="edit-description"
                value={editForm.description}
                onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                placeholder="Milestone description"
                rows={3}
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-due-date">Due Date</Label>
                <Input
                  id="edit-due-date"
                  type="date"
                  value={editForm.dueDate ? new Date(editForm.dueDate).toISOString().split('T')[0] : ''}
                  onChange={(e) => setEditForm({ ...editForm, dueDate: e.target.value ? new Date(e.target.value).toISOString() : '' })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-status">Status</Label>
                <Select
                  value={editForm.status}
                  onValueChange={(value) => setEditForm({ ...editForm, status: value as MilestoneStatus })}
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
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsEditDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleUpdateMilestone} disabled={isSubmitting || !editForm.name.trim()}>
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              Update
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Todo Dialog */}
      <Dialog open={isTodoDialogOpen} onOpenChange={setIsTodoDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Create Todo</DialogTitle>
            <DialogDescription>Add a new todo to this milestone.</DialogDescription>
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
            <Button variant="outline" onClick={() => setIsTodoDialogOpen(false)}>Cancel</Button>
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
