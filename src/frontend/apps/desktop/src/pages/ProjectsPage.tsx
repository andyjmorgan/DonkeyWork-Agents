import { useState, useEffect } from 'react'
import {
  Plus,
  Loader2,
  Trash2,
  FolderOpen,
  Target,
  CheckSquare,
  RefreshCw,
} from 'lucide-react'
import {
  Button,
  Badge,
  Progress,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@donkeywork/ui'
import {
  projects,
  type ProjectSummary,
  type ProjectStatus,
  type CreateProjectRequest,
} from '@donkeywork/api-client'
import { ProjectDetailPage } from './ProjectDetailPage'
import { MilestoneDetailPage } from './MilestoneDetailPage'

interface NavigateParams {
  page: string
  projectId?: string
  milestoneId?: string
}

const statusVariants: Record<ProjectStatus, 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning' | 'pending' | 'inProgress' | 'muted'> = {
  NotStarted: 'pending',
  InProgress: 'inProgress',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'destructive',
}

const statusLabels: Record<ProjectStatus, string> = {
  NotStarted: 'Not Started',
  InProgress: 'In Progress',
  OnHold: 'On Hold',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
}

type SubPage =
  | { view: 'list' }
  | { view: 'project-detail'; projectId: string }
  | { view: 'milestone-detail'; projectId: string; milestoneId: string }

export function ProjectsPage() {
  const [subPage, setSubPage] = useState<SubPage>({ view: 'list' })

  const handleNavigate = (params: NavigateParams) => {
    if (params.page === 'projects' || params.page === 'list') {
      setSubPage({ view: 'list' })
    } else if (params.page === 'project-detail' && params.projectId) {
      setSubPage({ view: 'project-detail', projectId: params.projectId })
    } else if (params.page === 'milestone-detail' && params.projectId && params.milestoneId) {
      setSubPage({ view: 'milestone-detail', projectId: params.projectId, milestoneId: params.milestoneId })
    }
  }

  if (subPage.view === 'project-detail') {
    return (
      <ProjectDetailPage
        projectId={subPage.projectId}
        onNavigate={handleNavigate}
      />
    )
  }

  if (subPage.view === 'milestone-detail') {
    return (
      <MilestoneDetailPage
        projectId={subPage.projectId}
        milestoneId={subPage.milestoneId}
        onNavigate={handleNavigate}
      />
    )
  }

  return <ProjectListView onNavigate={handleNavigate} />
}

function ProjectListView({ onNavigate }: { onNavigate: (params: NavigateParams) => void }) {
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [projectsList, setProjectsList] = useState<ProjectSummary[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [newProject, setNewProject] = useState<CreateProjectRequest>({
    name: '',
    status: 'NotStarted',
  })

  useEffect(() => {
    loadProjects()
  }, [])

  const loadProjects = async () => {
    try {
      setIsLoading(true)
      const data = await projects.list()
      setProjectsList(data)
    } catch (error) {
      console.error('Failed to load projects:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleRefresh = async () => {
    try {
      setIsRefreshing(true)
      const data = await projects.list()
      setProjectsList(data)
    } catch (error) {
      console.error('Failed to refresh projects:', error)
    } finally {
      setIsRefreshing(false)
    }
  }

  const handleCreate = async () => {
    if (!newProject.name.trim()) return

    try {
      setIsCreating(true)
      const created = await projects.create(newProject)
      setIsDialogOpen(false)
      setNewProject({ name: '', status: 'NotStarted' })
      onNavigate({ page: 'project-detail', projectId: created.id })
    } catch (error) {
      console.error('Failed to create project:', error)
    } finally {
      setIsCreating(false)
    }
  }

  const handleDelete = async (projectId: string, projectName: string) => {
    if (!window.confirm(`Are you sure you want to delete "${projectName}"? This will also delete all milestones, tasks, and notes associated with this project.`)) {
      return
    }

    try {
      setDeletingId(projectId)
      await projects.delete(projectId)
      await loadProjects()
    } catch (error) {
      console.error('Failed to delete project:', error)
    } finally {
      setDeletingId(null)
    }
  }

  const getProgress = (project: ProjectSummary) => {
    if (project.taskCount === 0) return 0
    return Math.round((project.completedTaskCount / project.taskCount) * 100)
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="space-y-6 p-6 overflow-y-auto h-full">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Projects</h1>
          <p className="text-muted-foreground text-sm">
            Manage your projects, milestones, and track progress
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="icon"
            onClick={handleRefresh}
            disabled={isRefreshing}
          >
            <RefreshCw className={`h-4 w-4 ${isRefreshing ? 'animate-spin' : ''}`} />
          </Button>
          <Button onClick={() => setIsDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-1" />
            New Project
          </Button>
        </div>
      </div>

      {projectsList.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <FolderOpen className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No projects yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Get started by creating your first project
          </p>
          <Button className="mt-4" onClick={() => setIsDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-1" />
            Create Project
          </Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {projectsList.map((project) => (
            <div
              key={project.id}
              className="rounded-lg border border-border bg-card p-4 space-y-3 cursor-pointer hover:shadow-md hover:border-accent/30 transition-all"
              onClick={() => onNavigate({ page: 'project-detail', projectId: project.id })}
            >
              <div className="flex items-start justify-between gap-2">
                <div className="space-y-1 min-w-0 flex-1">
                  <div className="font-medium text-base truncate">{project.name}</div>
                </div>
                <Badge variant={statusVariants[project.status]}>
                  {statusLabels[project.status]}
                </Badge>
              </div>

              <div className="flex items-center gap-4 text-sm text-muted-foreground">
                <span className="flex items-center gap-1">
                  <Target className="h-4 w-4 text-purple-500" />
                  {project.milestoneCount} milestones
                </span>
                <span className="flex items-center gap-1">
                  <CheckSquare className="h-4 w-4 text-emerald-500" />
                  {project.completedTaskCount}/{project.taskCount} tasks
                </span>
              </div>

              {project.taskCount > 0 && (
                <div className="space-y-1">
                  <Progress value={getProgress(project)} className="h-1.5" />
                  <div className="text-xs text-muted-foreground text-right">
                    {getProgress(project)}% complete
                  </div>
                </div>
              )}

              <div className="flex items-center justify-between">
                <span className="text-xs text-muted-foreground">
                  Created {new Date(project.createdAt).toLocaleDateString()}
                </span>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 text-destructive hover:text-destructive"
                  onClick={(e) => {
                    e.stopPropagation()
                    handleDelete(project.id, project.name)
                  }}
                  disabled={deletingId === project.id}
                >
                  {deletingId === project.id ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <Trash2 className="h-3.5 w-3.5" />
                  )}
                </Button>
              </div>

              {/* Tags */}
              {project.tags.length > 0 && (
                <div className="flex items-center gap-1 flex-wrap">
                  {project.tags.map((tag) => (
                    <Badge key={tag.id} variant="secondary" className="text-[10px] px-1.5 py-0">
                      {tag.name}
                    </Badge>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Create Project Dialog */}
      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create New Project</DialogTitle>
            <DialogDescription>
              Add a new project to organize your work
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                value={newProject.name}
                onChange={(e) => setNewProject({ ...newProject, name: e.target.value })}
                placeholder="Project name"
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && newProject.name.trim()) handleCreate()
                }}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="status">Status</Label>
              <Select
                value={newProject.status}
                onValueChange={(value) => setNewProject({ ...newProject, status: value as ProjectStatus })}
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
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={isCreating || !newProject.name.trim()}>
              {isCreating ? <Loader2 className="h-4 w-4 animate-spin mr-1" /> : null}
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
