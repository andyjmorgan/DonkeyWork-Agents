import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Loader2, Save, Trash2, ChevronRight, FolderKanban, CheckSquare, Calendar, Target } from 'lucide-react'
import {
  Button,
  Input,
  Label,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@donkeywork/ui'
import { MarkdownEditor } from '@/components/editor/MarkdownEditor'
import { MarkdownViewer } from '@/components/editor/MarkdownViewer'
import { tasks, projects, milestones, type Task, type ProjectDetails, type MilestoneDetails, type TaskPriority, type TaskStatus } from '@/lib/api'

export function TaskEditorPage() {
  const { taskId } = useParams<{ taskId: string }>()
  const navigate = useNavigate()
  const isNewTask = taskId === 'new'

  const [isLoading, setIsLoading] = useState(!isNewTask)
  const [isSaving, setIsSaving] = useState(false)
  const [task, setTask] = useState<Task | null>(null)
  const [project, setProject] = useState<ProjectDetails | null>(null)
  const [milestone, setMilestone] = useState<MilestoneDetails | null>(null)

  // Form fields
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [summary, setSummary] = useState('')
  const [priority, setPriority] = useState<TaskPriority>('Medium')
  const [status, setStatus] = useState<TaskStatus>('Pending')
  const [dueDate, setDueDate] = useState('')
  const [completionNotes, setCompletionNotes] = useState('')

  // Active tab
  const [activeTab, setActiveTab] = useState('summary')

  useEffect(() => {
    if (taskId && !isNewTask) {
      loadTask()
    }
  }, [taskId])

  const loadTask = async () => {
    if (!taskId || isNewTask) return

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

      // Load project if task belongs to one
      if (taskData.projectId) {
        const projectData = await projects.get(taskData.projectId)
        setProject(projectData)

        // Load milestone if task belongs to one
        if (taskData.milestoneId) {
          const milestoneData = await milestones.get(taskData.projectId, taskData.milestoneId)
          setMilestone(milestoneData)
        }
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
        const newTask = await tasks.create({
          title,
          description,
          summary,
          priority,
          status,
          dueDate: dueDate || undefined,
        })
        navigate(`/tasks/${newTask.id}`, { replace: true })
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
      await tasks.delete(task.id)
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

  const isTerminal = status === 'Completed' || status === 'Cancelled'

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
                  <span className="truncate max-w-[60px] sm:max-w-[100px] md:max-w-[150px]">{project.name}</span>
                </button>
                <ChevronRight className="h-4 w-4 shrink-0" />
                {milestone && (
                  <>
                    <button
                      onClick={() => navigate(`/workspace/${project.id}/milestones/${milestone.id}`)}
                      className="flex items-center gap-1.5 hover:text-foreground transition-colors min-w-0"
                    >
                      <Target className="h-4 w-4 shrink-0 text-purple-500" />
                      <span className="truncate max-w-[60px] sm:max-w-[100px] md:max-w-[150px]">{milestone.name}</span>
                    </button>
                    <ChevronRight className="h-4 w-4 shrink-0" />
                  </>
                )}
                <span className="flex items-center gap-1.5 text-foreground font-medium min-w-0">
                  <CheckSquare className="h-4 w-4 shrink-0 text-emerald-500" />
                  <span className="truncate max-w-[60px] sm:max-w-[100px] md:max-w-[150px]">{isNewTask ? 'New Task' : task?.title}</span>
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
        className="min-h-[calc(100vh-600px)]"
      />

      {/* Tabs for Summary and Completion Notes */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="summary">Summary</TabsTrigger>
          <TabsTrigger value="completion-notes">
            {isTerminal ? (status === 'Completed' ? 'Completion Notes' : 'Cancellation Reason') : 'Completion Notes'}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="summary" className="mt-4">
          {summary ? (
            <div className="rounded-lg border border-border bg-card p-4">
              <MarkdownViewer content={summary} className="min-h-[100px]" />
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-8 text-center">
              <p className="text-sm text-muted-foreground">
                No summary yet
              </p>
            </div>
          )}
        </TabsContent>

        <TabsContent value="completion-notes" className="mt-4">
          {completionNotes ? (
            <div className="rounded-lg border border-border bg-card p-4">
              <MarkdownViewer content={completionNotes} className="min-h-[100px]" />
              {task?.completedAt && (
                <p className="text-xs text-muted-foreground mt-4 pt-3 border-t border-border">
                  {status === 'Completed' ? 'Completed' : 'Cancelled'} {new Date(task.completedAt).toLocaleDateString()}
                </p>
              )}
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-8 text-center">
              <p className="text-sm text-muted-foreground">
                {isTerminal
                  ? 'Completion notes are required — add them above to save'
                  : 'No completion notes yet — will be added when the task is completed'}
              </p>
            </div>
          )}
        </TabsContent>
      </Tabs>
    </div>
  )
}
