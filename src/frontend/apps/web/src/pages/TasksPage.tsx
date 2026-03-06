import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Loader2, CheckSquare } from 'lucide-react'
import { Button } from '@donkeywork/ui'
import { ContentCard } from '@/components/workspace/ContentCard'
import { tasks, type Task, type TaskStatus } from '@donkeywork/api-client'

export function TasksPage() {
  const navigate = useNavigate()
  const [isLoading, setIsLoading] = useState(true)
  const [tasksList, setTasksList] = useState<Task[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)

  useEffect(() => {
    loadTasks()
  }, [])

  const loadTasks = async () => {
    try {
      setIsLoading(true)
      const data = await tasks.listStandalone()
      setTasksList(data)
    } catch (error) {
      console.error('Failed to load tasks:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleToggleComplete = async (task: Task) => {
    const newStatus: TaskStatus = task.status === 'Completed' ? 'Pending' : 'Completed'
    try {
      await tasks.update(task.id, {
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
      await tasks.delete(taskId)
      await loadTasks()
    } catch (error) {
      console.error('Failed to delete task:', error)
    } finally {
      setDeletingId(null)
    }
  }

  const pendingTasks = tasksList.filter(t => t.status !== 'Completed')
  const completedTasks = tasksList.filter(t => t.status === 'Completed')

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
        <div className="space-y-6">
          {/* Pending Tasks */}
          {pendingTasks.length > 0 && (
            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-muted-foreground flex items-center gap-2">
                Pending
                <span className="text-xs px-1.5 py-0.5 rounded-full bg-amber-500/20 text-amber-500">
                  {pendingTasks.length}
                </span>
              </h3>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {pendingTasks.map((task) => (
                  <ContentCard
                    key={task.id}
                    title={task.title}
                    content={task.description}
                    onClick={() => navigate(`/tasks/${task.id}`)}
                    onDelete={() => handleDelete(task.id, task.title)}
                    isDeleting={deletingId === task.id}
                    date={task.updatedAt || task.createdAt}
                    status={task.status}
                    priority={task.priority}
                    dueDate={task.dueDate}
                    onToggleComplete={() => handleToggleComplete(task)}
                    isCompleted={false}
                  />
                ))}
              </div>
            </div>
          )}

          {/* Completed Tasks */}
          {completedTasks.length > 0 && (
            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-muted-foreground flex items-center gap-2">
                Completed
                <span className="text-xs px-1.5 py-0.5 rounded-full bg-emerald-500/20 text-emerald-500">
                  {completedTasks.length}
                </span>
              </h3>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {completedTasks.map((task) => (
                  <ContentCard
                    key={task.id}
                    title={task.title}
                    content={task.description}
                    onClick={() => navigate(`/tasks/${task.id}`)}
                    onDelete={() => handleDelete(task.id, task.title)}
                    isDeleting={deletingId === task.id}
                    date={task.updatedAt || task.createdAt}
                    status={task.status}
                    priority={task.priority}
                    dueDate={task.dueDate}
                    onToggleComplete={() => handleToggleComplete(task)}
                    isCompleted={true}
                  />
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
