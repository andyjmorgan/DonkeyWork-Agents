import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2, CheckSquare, Circle, CheckCircle2, Clock, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
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
import { todos, type Todo, type TodoStatus, type TodoPriority, type CreateTodoRequest } from '@/lib/api'

const statusIcons: Record<TodoStatus, React.ReactNode> = {
  Pending: <Circle className="h-4 w-4 text-gray-400" />,
  InProgress: <Clock className="h-4 w-4 text-blue-500" />,
  Completed: <CheckCircle2 className="h-4 w-4 text-green-500" />,
  Cancelled: <AlertCircle className="h-4 w-4 text-red-500" />,
}

const priorityColors: Record<TodoPriority, string> = {
  Low: 'bg-gray-500',
  Medium: 'bg-blue-500',
  High: 'bg-orange-500',
  Critical: 'bg-red-500',
}

export function TodosPage() {
  const [isLoading, setIsLoading] = useState(true)
  const [todosList, setTodosList] = useState<Todo[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [editingTodo, setEditingTodo] = useState<Todo | null>(null)
  const [formData, setFormData] = useState<CreateTodoRequest>({
    title: '',
    description: '',
    status: 'Pending',
    priority: 'Medium',
  })

  useEffect(() => {
    loadTodos()
  }, [])

  const loadTodos = async () => {
    try {
      setIsLoading(true)
      const data = await todos.listStandalone()
      setTodosList(data)
    } catch (error) {
      console.error('Failed to load todos:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleCreate = async () => {
    if (!formData.title.trim()) return

    try {
      setIsCreating(true)
      await todos.create(formData)
      setIsDialogOpen(false)
      setFormData({ title: '', description: '', status: 'Pending', priority: 'Medium' })
      await loadTodos()
    } catch (error) {
      console.error('Failed to create todo:', error)
    } finally {
      setIsCreating(false)
    }
  }

  const handleUpdate = async () => {
    if (!editingTodo || !formData.title.trim()) return

    try {
      setIsCreating(true)
      await todos.update(editingTodo.id, {
        title: formData.title,
        description: formData.description,
        status: formData.status as TodoStatus,
        priority: formData.priority as TodoPriority,
        sortOrder: editingTodo.sortOrder,
      })
      setEditingTodo(null)
      setFormData({ title: '', description: '', status: 'Pending', priority: 'Medium' })
      await loadTodos()
    } catch (error) {
      console.error('Failed to update todo:', error)
    } finally {
      setIsCreating(false)
    }
  }

  const handleToggleComplete = async (todo: Todo) => {
    const newStatus: TodoStatus = todo.status === 'Completed' ? 'Pending' : 'Completed'
    try {
      await todos.update(todo.id, {
        ...todo,
        status: newStatus,
      })
      await loadTodos()
    } catch (error) {
      console.error('Failed to update todo:', error)
    }
  }

  const handleDelete = async (todoId: string, todoTitle: string) => {
    if (!window.confirm(`Are you sure you want to delete "${todoTitle}"?`)) {
      return
    }

    try {
      setDeletingId(todoId)
      await todos.delete(todoId)
      await loadTodos()
    } catch (error) {
      console.error('Failed to delete todo:', error)
    } finally {
      setDeletingId(null)
    }
  }

  const openEditDialog = (todo: Todo) => {
    setEditingTodo(todo)
    setFormData({
      title: todo.title,
      description: todo.description,
      status: todo.status,
      priority: todo.priority,
    })
  }

  const closeDialog = () => {
    setIsDialogOpen(false)
    setEditingTodo(null)
    setFormData({ title: '', description: '', status: 'Pending', priority: 'Medium' })
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Todos</h1>
          <p className="text-muted-foreground">
            Manage your standalone todo items
          </p>
        </div>
        <Button onClick={() => setIsDialogOpen(true)}>
          <Plus className="h-4 w-4" />
          <span className="hidden sm:inline">New Todo</span>
        </Button>
      </div>

      {todosList.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <CheckSquare className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No todos yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Create your first todo to get started
          </p>
          <Button className="mt-4" onClick={() => setIsDialogOpen(true)}>
            <Plus className="h-4 w-4" />
            Create Todo
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile view */}
          <div className="space-y-3 md:hidden">
            {todosList.map((todo) => (
              <div key={todo.id} className="rounded-lg border border-border bg-card p-4 space-y-2">
                <div className="flex items-start gap-3">
                  <button
                    onClick={() => handleToggleComplete(todo)}
                    className="mt-1 shrink-0"
                  >
                    {statusIcons[todo.status]}
                  </button>
                  <div className="flex-1 min-w-0">
                    <div className={`font-medium ${todo.status === 'Completed' ? 'line-through text-muted-foreground' : ''}`}>
                      {todo.title}
                    </div>
                    {todo.description && (
                      <div className="text-sm text-muted-foreground line-clamp-2 mt-1">
                        {todo.description}
                      </div>
                    )}
                    <div className="flex items-center gap-2 mt-2">
                      <Badge variant="outline" className={`${priorityColors[todo.priority]} text-white border-0 text-xs`}>
                        {todo.priority}
                      </Badge>
                      {todo.dueDate && (
                        <span className="text-xs text-muted-foreground">
                          Due: {new Date(todo.dueDate).toLocaleDateString()}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => openEditDialog(todo)}
                    >
                      <Edit className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive"
                      onClick={() => handleDelete(todo.id, todo.title)}
                      disabled={deletingId === todo.id}
                    >
                      {deletingId === todo.id ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
                    </Button>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop view */}
          <div className="hidden md:block rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[50px]">Status</TableHead>
                  <TableHead>Title</TableHead>
                  <TableHead>Priority</TableHead>
                  <TableHead>Due Date</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {todosList.map((todo) => (
                  <TableRow key={todo.id}>
                    <TableCell>
                      <button onClick={() => handleToggleComplete(todo)}>
                        {statusIcons[todo.status]}
                      </button>
                    </TableCell>
                    <TableCell>
                      <div className={todo.status === 'Completed' ? 'line-through text-muted-foreground' : ''}>
                        <div className="font-medium">{todo.title}</div>
                        {todo.description && (
                          <div className="text-sm text-muted-foreground line-clamp-1">
                            {todo.description}
                          </div>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className={`${priorityColors[todo.priority]} text-white border-0`}>
                        {todo.priority}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {todo.dueDate ? new Date(todo.dueDate).toLocaleDateString() : '—'}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(todo.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8"
                          onClick={() => openEditDialog(todo)}
                        >
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-destructive hover:text-destructive"
                          onClick={() => handleDelete(todo.id, todo.title)}
                          disabled={deletingId === todo.id}
                        >
                          {deletingId === todo.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Trash2 className="h-4 w-4" />
                          )}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </>
      )}

      {/* Create/Edit Todo Dialog */}
      <Dialog open={isDialogOpen || !!editingTodo} onOpenChange={closeDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editingTodo ? 'Edit Todo' : 'Create New Todo'}</DialogTitle>
            <DialogDescription>
              {editingTodo ? 'Update your todo item' : 'Add a new todo to your list'}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="title">Title</Label>
              <Input
                id="title"
                value={formData.title}
                onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                placeholder="Todo title"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={formData.description || ''}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                placeholder="Description (supports markdown)"
                rows={3}
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="status">Status</Label>
                <Select
                  value={formData.status}
                  onValueChange={(value) => setFormData({ ...formData, status: value as TodoStatus })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Pending">Pending</SelectItem>
                    <SelectItem value="InProgress">In Progress</SelectItem>
                    <SelectItem value="Completed">Completed</SelectItem>
                    <SelectItem value="Cancelled">Cancelled</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="priority">Priority</Label>
                <Select
                  value={formData.priority}
                  onValueChange={(value) => setFormData({ ...formData, priority: value as TodoPriority })}
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
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={closeDialog}>
              Cancel
            </Button>
            <Button
              onClick={editingTodo ? handleUpdate : handleCreate}
              disabled={isCreating || !formData.title.trim()}
            >
              {isCreating ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
              {editingTodo ? 'Update' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
