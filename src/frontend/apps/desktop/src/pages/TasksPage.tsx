import { useState, useEffect, useMemo } from 'react'
import {
  Plus,
  Loader2,
  CheckSquare,
  Trash2,
  Calendar,
} from 'lucide-react'
import {
  Button,
  Badge,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@donkeywork/ui'
import { tasks, type Task, type TaskStatus, type TaskPriority } from '@donkeywork/api-client'
import { TaskEditorPage } from './TaskEditorPage'

const priorityColors: Record<TaskPriority, string> = {
  Critical: 'bg-red-500/20 text-red-400 border-red-500/30',
  High: 'bg-orange-500/20 text-orange-400 border-orange-500/30',
  Medium: 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30',
  Low: 'bg-gray-500/20 text-gray-400 border-gray-500/30',
}

const statusBadgeVariant: Record<TaskStatus, 'pending' | 'inProgress' | 'success' | 'destructive'> = {
  Pending: 'pending',
  InProgress: 'inProgress',
  Completed: 'success',
  Cancelled: 'destructive',
}

const statusLabels: Record<TaskStatus, string> = {
  Pending: 'Pending',
  InProgress: 'In Progress',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
}

type StatusFilter = 'All' | TaskStatus
type PriorityFilter = 'All' | TaskPriority

export function TasksPage() {
  const [view, setView] = useState<{ mode: 'list' } | { mode: 'editor'; taskId: string | null }>({ mode: 'list' })
  const [isLoading, setIsLoading] = useState(true)
  const [tasksList, setTasksList] = useState<Task[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('All')
  const [priorityFilter, setPriorityFilter] = useState<PriorityFilter>('All')

  useEffect(() => {
    loadTasks()
  }, [])

  const loadTasks = async () => {
    try {
      setIsLoading(true)
      const data = await tasks.list()
      setTasksList(data)
    } catch (error) {
      console.error('Failed to load tasks:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const filteredTasks = useMemo(() => {
    let items = tasksList

    if (statusFilter !== 'All') {
      items = items.filter((task) => task.status === statusFilter)
    }

    if (priorityFilter !== 'All') {
      items = items.filter((task) => task.priority === priorityFilter)
    }

    return items
  }, [tasksList, statusFilter, priorityFilter])

  const handleDelete = async (e: React.MouseEvent, taskId: string, taskTitle: string) => {
    e.stopPropagation()
    if (!window.confirm(`Are you sure you want to delete "${taskTitle}"?`)) {
      return
    }

    try {
      setDeletingId(taskId)
      await tasks.delete(taskId)
      await loadTasks()
    } catch (error) {
      console.error('Failed to delete task:', error)
    } finally {
      setDeletingId(null)
    }
  }

  const handleNavigateToEditor = (taskId: string | null) => {
    setView({ mode: 'editor', taskId })
  }

  const handleBackToList = () => {
    setView({ mode: 'list' })
    loadTasks()
  }

  // Show editor view
  if (view.mode === 'editor') {
    return (
      <TaskEditorPage
        taskId={view.taskId}
        onBack={handleBackToList}
      />
    )
  }

  // Show loading state
  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <div className="flex-1 overflow-y-auto p-6 space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">Tasks</h1>
            <p className="text-muted-foreground text-sm">
              Manage and track your tasks
            </p>
          </div>
          <Button onClick={() => handleNavigateToEditor(null)}>
            <Plus className="h-4 w-4" />
            New Task
          </Button>
        </div>

        {/* Filters */}
        <div className="flex items-center gap-3">
          <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as StatusFilter)}>
            <SelectTrigger className="w-[150px]">
              <SelectValue placeholder="All Statuses" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="All">All Statuses</SelectItem>
              <SelectItem value="Pending">Pending</SelectItem>
              <SelectItem value="InProgress">In Progress</SelectItem>
              <SelectItem value="Completed">Completed</SelectItem>
              <SelectItem value="Cancelled">Cancelled</SelectItem>
            </SelectContent>
          </Select>
          <Select value={priorityFilter} onValueChange={(v) => setPriorityFilter(v as PriorityFilter)}>
            <SelectTrigger className="w-[150px]">
              <SelectValue placeholder="All Priorities" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="All">All Priorities</SelectItem>
              <SelectItem value="Critical">Critical</SelectItem>
              <SelectItem value="High">High</SelectItem>
              <SelectItem value="Medium">Medium</SelectItem>
              <SelectItem value="Low">Low</SelectItem>
            </SelectContent>
          </Select>
          {(statusFilter !== 'All' || priorityFilter !== 'All') && (
            <Button
              variant="ghost"
              size="sm"
              className="text-xs text-muted-foreground"
              onClick={() => {
                setStatusFilter('All')
                setPriorityFilter('All')
              }}
            >
              Clear filters
            </Button>
          )}
        </div>

        {/* Content */}
        {filteredTasks.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
            <div className="rounded-full bg-muted p-4">
              <CheckSquare className="h-8 w-8 text-muted-foreground" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">
              {tasksList.length === 0 ? 'No tasks yet' : 'No matching tasks'}
            </h3>
            <p className="mt-2 text-sm text-muted-foreground">
              {tasksList.length === 0
                ? 'Create your first task to get started'
                : 'Try adjusting your filters'}
            </p>
            {tasksList.length === 0 && (
              <Button className="mt-4" onClick={() => handleNavigateToEditor(null)}>
                <Plus className="h-4 w-4" />
                Create Task
              </Button>
            )}
          </div>
        ) : (
          <div className="rounded-lg border border-border overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead>Title</TableHead>
                  <TableHead className="w-[120px]">Status</TableHead>
                  <TableHead className="w-[100px]">Priority</TableHead>
                  <TableHead className="w-[140px]">Due Date</TableHead>
                  <TableHead className="w-[60px]" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredTasks.map((task) => (
                  <TableRow
                    key={task.id}
                    className="cursor-pointer group"
                    onClick={() => handleNavigateToEditor(task.id)}
                  >
                    <TableCell>
                      <span className={`font-medium ${task.status === 'Completed' ? 'line-through text-muted-foreground' : ''}`}>
                        {task.title}
                      </span>
                    </TableCell>
                    <TableCell>
                      <Badge variant={statusBadgeVariant[task.status]}>
                        {statusLabels[task.status]}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <span className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium ${priorityColors[task.priority]}`}>
                        {task.priority}
                      </span>
                    </TableCell>
                    <TableCell>
                      {task.dueDate ? (
                        <span className="text-sm text-muted-foreground flex items-center gap-1.5">
                          <Calendar className="h-3.5 w-3.5" />
                          {new Date(task.dueDate).toLocaleDateString()}
                        </span>
                      ) : (
                        <span className="text-sm text-muted-foreground/50">--</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-7 w-7 text-destructive opacity-0 group-hover:opacity-100 transition-opacity"
                        onClick={(e) => handleDelete(e, task.id, task.title)}
                        disabled={deletingId === task.id}
                      >
                        {deletingId === task.id ? (
                          <Loader2 className="h-3.5 w-3.5 animate-spin" />
                        ) : (
                          <Trash2 className="h-3.5 w-3.5" />
                        )}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </div>
    </div>
  )
}
