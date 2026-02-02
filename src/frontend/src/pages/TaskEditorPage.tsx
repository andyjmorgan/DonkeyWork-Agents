import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Loader2, Save, Trash2, ChevronRight, FolderKanban, CheckSquare, Calendar } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { MarkdownEditor } from '@/components/editor/MarkdownEditor'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { todos, projects, type Todo, type ProjectDetails, type TodoPriority, type TodoStatus } from '@/lib/api'

export function TaskEditorPage() {
  const { taskId } = useParams<{ taskId: string }>()
  const navigate = useNavigate()
  const isNewTask = taskId === 'new'

  const [isLoading, setIsLoading] = useState(!isNewTask)
  const [isSaving, setIsSaving] = useState(false)
  const [task, setTask] = useState<Todo | null>(null)
  const [project, setProject] = useState<ProjectDetails | null>(null)

  // Form fields
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<TodoPriority>('Medium')
  const [status, setStatus] = useState<TodoStatus>('Pending')
  const [dueDate, setDueDate] = useState('')

  useEffect(() => {
    if (taskId && !isNewTask) {
      loadTask()
    }
  }, [taskId])

  const loadTask = async () => {
    if (!taskId || isNewTask) return

    try {
      setIsLoading(true)
      const taskData = await todos.get(taskId)
      setTask(taskData)
      setTitle(taskData.title)
      setDescription(taskData.description || '')
      setPriority(taskData.priority)
      setStatus(taskData.status)
      setDueDate(taskData.dueDate || '')

      // Load project if task belongs to one
      if (taskData.projectId) {
        const projectData = await projects.get(taskData.projectId)
        setProject(projectData)
      }
    } catch (error) {
      console.error('Failed to load task:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleSave = async () => {
    if (!title.trim()) return

    try {
      setIsSaving(true)

      if (isNewTask) {
        // Create new task
        const newTask = await todos.create({
          title,
          description,
          priority,
          status,
          dueDate: dueDate || undefined,
        })
        // Navigate to the created task
        navigate(`/tasks/${newTask.id}`, { replace: true })
      } else if (task) {
        // Update existing task
        await todos.update(task.id, {
          title,
          description,
          priority,
          status,
          dueDate: dueDate || undefined,
          sortOrder: task.sortOrder,
          projectId: task.projectId,
          milestoneId: task.milestoneId,
        })
        // Navigate back
        handleBack()
      }
    } catch (error) {
      console.error('Failed to save task:', error)
    } finally {
      setIsSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!task || !window.confirm('Are you sure you want to delete this task?')) return

    try {
      await todos.delete(task.id)
      handleBack()
    } catch (error) {
      console.error('Failed to delete task:', error)
    }
  }

  const handleBack = () => {
    if (task?.milestoneId && project) {
      navigate(`/workspace/${project.id}/milestones/${task.milestoneId}`)
    } else if (project) {
      navigate(`/workspace/${project.id}`)
    } else {
      navigate('/tasks')
    }
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!isNewTask && !task) {
    return (
      <div className="flex flex-col items-center justify-center h-64">
        <p className="text-muted-foreground">Task not found</p>
        <Button variant="outline" className="mt-4" onClick={() => navigate('/tasks')}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Tasks
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb Header */}
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-2 min-w-0">
          <Button variant="ghost" size="icon" onClick={handleBack}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div className="flex items-center gap-2 text-sm text-muted-foreground min-w-0">
            {project ? (
              <>
                <button
                  onClick={() => navigate(`/workspace/${project.id}`)}
                  className="flex items-center gap-1.5 hover:text-foreground transition-colors min-w-0"
                >
                  <FolderKanban className="h-4 w-4 shrink-0" />
                  <span className="truncate max-w-[80px] sm:max-w-[120px] md:max-w-[200px]">{project.name}</span>
                </button>
                <ChevronRight className="h-4 w-4 shrink-0" />
                <span className="flex items-center gap-1.5 text-foreground font-medium min-w-0">
                  <CheckSquare className="h-4 w-4 shrink-0 text-emerald-500" />
                  <span className="truncate max-w-[80px] sm:max-w-[120px] md:max-w-[200px]">{isNewTask ? 'New Task' : task?.title}</span>
                </span>
              </>
            ) : (
              <>
                <button
                  onClick={() => navigate('/tasks')}
                  className="flex items-center gap-1.5 hover:text-foreground transition-colors"
                >
                  <CheckSquare className="h-4 w-4 shrink-0 text-emerald-500" />
                  <span>Tasks</span>
                </button>
                <ChevronRight className="h-4 w-4 shrink-0" />
                <span className="flex items-center gap-1.5 text-foreground font-medium min-w-0">
                  <CheckSquare className="h-4 w-4 shrink-0 text-emerald-500" />
                  <span className="truncate max-w-[100px] sm:max-w-[150px] md:max-w-[250px]">{isNewTask ? 'New Task' : task?.title}</span>
                </span>
              </>
            )}
          </div>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          {!isNewTask && (
            <Button
              variant="ghost"
              size="icon"
              className="text-destructive"
              onClick={handleDelete}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          )}
          <Button onClick={handleSave} disabled={isSaving || !title.trim()}>
            {isSaving && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
            <Save className="h-4 w-4 mr-2" />
            {isNewTask ? 'Create' : 'Save'}
          </Button>
        </div>
      </div>

      {/* Task Details Card */}
      <div className="rounded-lg border border-border bg-card p-4">
        <div className="flex flex-wrap items-center gap-3">
          {/* Priority */}
          <div className="flex items-center gap-2">
            <Label className="text-sm text-muted-foreground">Priority:</Label>
            <Select value={priority} onValueChange={(v) => setPriority(v as TodoPriority)}>
              <SelectTrigger className="w-[120px] h-8">
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

          {/* Status */}
          <div className="flex items-center gap-2">
            <Label className="text-sm text-muted-foreground">Status:</Label>
            <Select value={status} onValueChange={(v) => setStatus(v as TodoStatus)}>
              <SelectTrigger className="w-[130px] h-8">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Pending">Pending</SelectItem>
                <SelectItem value="InProgress">In Progress</SelectItem>
                <SelectItem value="Completed">Completed</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Due Date */}
          <div className="flex items-center gap-2">
            <Calendar className="h-4 w-4 text-muted-foreground" />
            <Input
              type="date"
              className="w-[150px] h-8"
              value={dueDate ? new Date(dueDate).toISOString().split('T')[0] : ''}
              onChange={(e) => setDueDate(e.target.value ? new Date(e.target.value).toISOString() : '')}
            />
          </div>

        </div>
      </div>

      {/* Task Editor */}
      <div className="space-y-4">
        <Input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Task title"
          className="text-xl font-semibold border-0 px-0 focus-visible:ring-0 focus-visible:ring-offset-0"
        />
        <MarkdownEditor
          content={description}
          onChange={setDescription}
          placeholder="Add a description for this task..."
          className="min-h-[calc(100vh-400px)]"
        />
      </div>
    </div>
  )
}
