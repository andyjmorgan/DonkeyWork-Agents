import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
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
import { todos, type Todo, type TodoStatus, type TodoPriority } from '@/lib/api'

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

export function TasksPage() {
  const navigate = useNavigate()
  const [isLoading, setIsLoading] = useState(true)
  const [tasksList, setTasksList] = useState<Todo[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)

  useEffect(() => {
    loadTasks()
  }, [])

  const loadTasks = async () => {
    try {
      setIsLoading(true)
      const data = await todos.listStandalone()
      setTasksList(data)
    } catch (error) {
      console.error('Failed to load tasks:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleToggleComplete = async (task: Todo) => {
    const newStatus: TodoStatus = task.status === 'Completed' ? 'Pending' : 'Completed'
    try {
      await todos.update(task.id, {
        ...task,
        status: newStatus,
      })
      await loadTasks()
    } catch (error) {
      console.error('Failed to update task:', error)
    }
  }

  const handleDelete = async (taskId: string, taskTitle: string) => {
    if (!window.confirm(`Are you sure you want to delete "${taskTitle}"?`)) {
      return
    }

    try {
      setDeletingId(taskId)
      await todos.delete(taskId)
      await loadTasks()
    } catch (error) {
      console.error('Failed to delete task:', error)
    } finally {
      setDeletingId(null)
    }
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
          <h1 className="text-2xl font-bold">Tasks</h1>
          <p className="text-muted-foreground">
            Manage your standalone tasks
          </p>
        </div>
        <Button onClick={() => navigate('/tasks/new')}>
          <Plus className="h-4 w-4" />
          <span className="hidden sm:inline">New Task</span>
        </Button>
      </div>

      {tasksList.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <CheckSquare className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No tasks yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Create your first task to get started
          </p>
          <Button className="mt-4" onClick={() => navigate('/tasks/new')}>
            <Plus className="h-4 w-4" />
            Create Task
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile view */}
          <div className="space-y-3 md:hidden">
            {tasksList.map((task) => (
              <div
                key={task.id}
                className="rounded-lg border border-border bg-card p-4 space-y-2 cursor-pointer hover:border-accent/50 transition-colors"
                onClick={() => navigate(`/tasks/${task.id}`)}
              >
                <div className="flex items-start gap-3">
                  <button
                    onClick={(e) => { e.stopPropagation(); handleToggleComplete(task) }}
                    className="mt-1 shrink-0"
                  >
                    {statusIcons[task.status]}
                  </button>
                  <div className="flex-1 min-w-0">
                    <div className={`font-medium ${task.status === 'Completed' ? 'line-through text-muted-foreground' : ''}`}>
                      {task.title}
                    </div>
                    {task.description && (
                      <div className="text-sm text-muted-foreground line-clamp-2 mt-1">
                        {task.description}
                      </div>
                    )}
                    <div className="flex items-center gap-2 mt-2">
                      <Badge variant="outline" className={`${priorityColors[task.priority]} text-white border-0 text-xs`}>
                        {task.priority}
                      </Badge>
                      {task.dueDate && (
                        <span className="text-xs text-muted-foreground">
                          Due: {new Date(task.dueDate).toLocaleDateString()}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={(e) => { e.stopPropagation(); navigate(`/tasks/${task.id}`) }}
                    >
                      <Edit className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive"
                      onClick={(e) => { e.stopPropagation(); handleDelete(task.id, task.title) }}
                      disabled={deletingId === task.id}
                    >
                      {deletingId === task.id ? (
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
                {tasksList.map((task) => (
                  <TableRow
                    key={task.id}
                    className="cursor-pointer"
                    onClick={() => navigate(`/tasks/${task.id}`)}
                  >
                    <TableCell>
                      <button onClick={(e) => { e.stopPropagation(); handleToggleComplete(task) }}>
                        {statusIcons[task.status]}
                      </button>
                    </TableCell>
                    <TableCell>
                      <div className={task.status === 'Completed' ? 'line-through text-muted-foreground' : ''}>
                        <div className="font-medium">{task.title}</div>
                        {task.description && (
                          <div className="text-sm text-muted-foreground line-clamp-1">
                            {task.description}
                          </div>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className={`${priorityColors[task.priority]} text-white border-0`}>
                        {task.priority}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {task.dueDate ? new Date(task.dueDate).toLocaleDateString() : '—'}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(task.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8"
                          onClick={(e) => { e.stopPropagation(); navigate(`/tasks/${task.id}`) }}
                        >
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-destructive hover:text-destructive"
                          onClick={(e) => { e.stopPropagation(); handleDelete(task.id, task.title) }}
                          disabled={deletingId === task.id}
                        >
                          {deletingId === task.id ? (
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
    </div>
  )
}
