import { useState, useEffect } from 'react'
import { ArrowLeft, Loader2, Save, Trash2, Calendar, CheckSquare, ChevronRight } from 'lucide-react'
import {
  Button,
  Input,
  Label,
  Textarea,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@donkeywork/ui'
import { MarkdownEditor } from '@donkeywork/editor'
import { tasks, type Task, type TaskPriority, type TaskStatus } from '@donkeywork/api-client'

interface TaskEditorPageProps {
  /** Task ID to edit, or null for creating a new task */
  taskId: string | null
  /** Callback to navigate back to the tasks list */
  onBack: () => void
}

export function TaskEditorPage({ taskId, onBack }: TaskEditorPageProps) {
  const isNewTask = taskId === null

  const [isLoading, setIsLoading] = useState(!isNewTask)
  const [isSaving, setIsSaving] = useState(false)
  const [task, setTask] = useState<Task | null>(null)

  // Form fields
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [summary, setSummary] = useState('')
  const [priority, setPriority] = useState<TaskPriority>('Medium')
  const [status, setStatus] = useState<TaskStatus>('Pending')
  const [dueDate, setDueDate] = useState('')
  const [completionNotes, setCompletionNotes] = useState('')

  useEffect(() => {
    if (taskId) {
      loadTask()
    }
  }, [taskId])

  const loadTask = async () => {
    if (!taskId) return

    try {
      setIsLoading(true)
      const taskData = await tasks.get(taskId)
      setTask(taskData)
      setTitle(taskData.title)
      setDescription(taskData.description || '')
      setSummary(taskData.summary || '')
      setPriority(taskData.priority)
      setStatus(taskData.status)
      setDueDate(taskData.dueDate || '')
      setCompletionNotes(taskData.completionNotes || '')
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
        await tasks.create({
          title,
          description,
          summary,
          priority,
          status,
          dueDate: dueDate || undefined,
        })
        onBack()
      } else if (task) {
        await tasks.update(task.id, {
          title,
          description,
          summary,
          priority,
          status,
          completionNotes: (status === 'Completed' || status === 'Cancelled') ? completionNotes || undefined : undefined,
          dueDate: dueDate || undefined,
          sortOrder: task.sortOrder,
          projectId: task.projectId,
          milestoneId: task.milestoneId,
        })
        onBack()
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
      await tasks.delete(task.id)
      onBack()
    } catch (error) {
      console.error('Failed to delete task:', error)
    }
  }

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!isNewTask && !task) {
    return (
      <div className="flex flex-col items-center justify-center h-full">
        <p className="text-muted-foreground">Task not found</p>
        <Button variant="outline" className="mt-4" onClick={onBack}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Tasks
        </Button>
      </div>
    )
  }

  const isTerminal = status === 'Completed' || status === 'Cancelled'

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <div className="flex-1 overflow-y-auto p-6 space-y-6">
        {/* Breadcrumb Header */}
        <div className="flex items-center justify-between gap-4">
          <div className="flex items-center gap-2 min-w-0">
            <Button variant="ghost" size="icon" onClick={onBack}>
              <ArrowLeft className="h-5 w-5" />
            </Button>
            <div className="flex items-center gap-2 text-sm text-muted-foreground min-w-0">
              <button
                onClick={onBack}
                className="flex items-center gap-1.5 hover:text-foreground transition-colors"
              >
                <CheckSquare className="h-4 w-4 shrink-0 text-emerald-500" />
                <span>Tasks</span>
              </button>
              <ChevronRight className="h-4 w-4 shrink-0" />
              <span className="flex items-center gap-1.5 text-foreground font-medium min-w-0">
                <CheckSquare className="h-4 w-4 shrink-0 text-emerald-500" />
                <span className="truncate max-w-[150px] md:max-w-[250px]">
                  {isNewTask ? 'New Task' : task?.title}
                </span>
              </span>
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
            <Button
              onClick={handleSave}
              disabled={isSaving || !title.trim() || (isTerminal && !completionNotes.trim())}
            >
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
              <Select value={priority} onValueChange={(v) => setPriority(v as TaskPriority)}>
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
              <Select value={status} onValueChange={(v) => setStatus(v as TaskStatus)}>
                <SelectTrigger className="w-[130px] h-8">
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

        {/* Title */}
        <Input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Task title"
          className="text-xl font-semibold border-0 px-0 focus-visible:ring-0 focus-visible:ring-offset-0"
        />

        {/* Description */}
        <MarkdownEditor
          content={description}
          onChange={setDescription}
          placeholder="Add a description for this task..."
          className="min-h-[200px]"
        />

        {/* Summary (read-only display) */}
        {summary && (
          <div className="space-y-2">
            <Label className="text-sm text-muted-foreground">Summary</Label>
            <div className="rounded-lg border border-border bg-card p-4">
              <p className="text-sm text-foreground whitespace-pre-wrap">{summary}</p>
            </div>
          </div>
        )}

        {/* Completion Notes */}
        {isTerminal && (
          <div className="space-y-2">
            <Label className="text-sm text-muted-foreground">
              {status === 'Completed' ? 'Completion Notes' : 'Cancellation Reason'}
              <span className="text-destructive ml-1">*</span>
            </Label>
            <Textarea
              value={completionNotes}
              onChange={(e) => setCompletionNotes(e.target.value)}
              placeholder={
                status === 'Completed'
                  ? 'Describe what was accomplished...'
                  : 'Describe why this task was cancelled...'
              }
              rows={4}
            />
            {!completionNotes.trim() && (
              <p className="text-xs text-destructive">
                {status === 'Completed' ? 'Completion notes are' : 'A cancellation reason is'} required to save
              </p>
            )}
          </div>
        )}

        {/* Completed At timestamp */}
        {task?.completedAt && (
          <p className="text-xs text-muted-foreground">
            {status === 'Completed' ? 'Completed' : 'Cancelled'}{' '}
            {new Date(task.completedAt).toLocaleDateString()}
          </p>
        )}
      </div>
    </div>
  )
}
