import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Loader2, Trash2, Calendar, RefreshCw, Play, Pause, Zap, Clock } from 'lucide-react'
import cronstrue from 'cronstrue'
import {
  Button,
  Badge,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
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
  Textarea,
} from '@donkeywork/ui'
import {
  schedules,
  agentDefinitions,
  type ScheduledJobSummary,
  type ScheduleMode,
  type ScheduleJobType,
  type ScheduleTargetType,
  type CreateScheduleRequest,
  type AgentDefinitionSummary,
} from '@donkeywork/api-client'

const jobTypeLabels: Record<ScheduleJobType, string> = {
  AgentInvocation: 'Agent',
  Reminder: 'Reminder',
  Maintenance: 'Maintenance',
  Cleanup: 'Cleanup',
  Archival: 'Archival',
  ReportGeneration: 'Report',
  WorkflowExecution: 'Workflow',
}

const targetTypeLabels: Record<ScheduleTargetType, string> = {
  Navi: 'Navi',
  CustomAgent: 'Custom Agent',
  Orchestration: 'Orchestration',
}

function formatCron(cron: string): string {
  try {
    const parts = cron.trim().split(/\s+/)
    const displayCron = parts.length >= 6 ? parts.slice(1, 6).join(' ') : cron
    return cronstrue.toString(displayCron)
  } catch {
    return cron
  }
}

function formatLocalTime(utcString?: string): string {
  if (!utcString) return '—'
  return new Date(utcString).toLocaleString()
}

function getScheduleDisplayText(item: ScheduledJobSummary): string {
  if (item.scheduleMode === 'Recurring' && item.cronExpression) {
    return formatCron(item.cronExpression)
  }
  if (item.scheduleMode === 'OneOff' && item.runAtUtc) {
    return formatLocalTime(item.runAtUtc)
  }
  return '—'
}

function getStatusBadge(item: ScheduledJobSummary) {
  if (item.scheduleMode === 'OneOff' && !item.isEnabled) {
    return <Badge variant="outline" className="border-green-500/30 text-green-400">Completed</Badge>
  }
  if (!item.isEnabled) {
    return <Badge variant="outline" className="border-yellow-500/30 text-yellow-400">Paused</Badge>
  }
  return <Badge variant="outline" className="border-emerald-500/30 text-emerald-400">Active</Badge>
}

export function SchedulesPage() {
  const navigate = useNavigate()
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [schedulesList, setSchedulesList] = useState<ScheduledJobSummary[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [togglingId, setTogglingId] = useState<string | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [agentList, setAgentList] = useState<AgentDefinitionSummary[]>([])
  const [newSchedule, setNewSchedule] = useState<CreateScheduleRequest>({
    name: '',
    scheduleMode: 'Recurring',
    jobType: 'AgentInvocation',
    targetType: 'Navi',
    userPrompt: '',
  })

  const loadSchedules = useCallback(async () => {
    try {
      const [data, agents] = await Promise.all([
        schedules.list(),
        agentDefinitions.list(),
      ])
      setSchedulesList(data.items)
      setAgentList(agents)
    } catch (error) {
      console.error('Failed to load schedules:', error)
    }
  }, [])

  useEffect(() => {
    loadSchedules().finally(() => setIsLoading(false))
  }, [loadSchedules])

  const handleRefresh = async () => {
    try {
      setIsRefreshing(true)
      await loadSchedules()
    } finally {
      setIsRefreshing(false)
    }
  }

  const handleCreate = async () => {
    if (!newSchedule.name.trim() || !newSchedule.userPrompt.trim()) return

    try {
      setIsCreating(true)
      const created = await schedules.create(newSchedule)
      setIsDialogOpen(false)
      setNewSchedule({
        name: '',
        scheduleMode: 'Recurring',
        jobType: 'AgentInvocation',
        targetType: 'Navi',
        userPrompt: '',
      })
      navigate(`/schedules/${created.id}`)
    } catch (error) {
      console.error('Failed to create schedule:', error)
    } finally {
      setIsCreating(false)
    }
  }

  const handleDelete = async (id: string, name: string) => {
    if (!window.confirm(`Are you sure you want to delete "${name}"? This will also delete all execution history.`)) {
      return
    }

    try {
      setDeletingId(id)
      await schedules.delete(id)
      await loadSchedules()
    } catch (error) {
      console.error('Failed to delete schedule:', error)
    } finally {
      setDeletingId(null)
    }
  }

  const handleToggle = async (item: ScheduledJobSummary) => {
    try {
      setTogglingId(item.id)
      if (item.isEnabled) {
        await schedules.disable(item.id)
      } else {
        await schedules.enable(item.id)
      }
      await loadSchedules()
    } catch (error) {
      console.error('Failed to toggle schedule:', error)
    } finally {
      setTogglingId(null)
    }
  }

  const handleTrigger = async (id: string, name: string) => {
    if (!window.confirm(`Manually trigger "${name}" to run now?`)) return

    try {
      await schedules.trigger(id)
      await loadSchedules()
    } catch (error) {
      console.error('Failed to trigger schedule:', error)
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
          <h1 className="text-2xl font-bold">Schedules</h1>
          <p className="text-muted-foreground">
            Manage recurring and one-off scheduled agent jobs
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
            <Plus className="h-4 w-4" />
            <span className="hidden sm:inline">New Schedule</span>
          </Button>
        </div>
      </div>

      {schedulesList.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <Calendar className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No schedules yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Create a schedule to run agents on a timer or at a specific time
          </p>
          <Button className="mt-4" onClick={() => setIsDialogOpen(true)}>
            <Plus className="h-4 w-4" />
            Create Schedule
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile cards */}
          <div className="grid grid-cols-1 gap-4 md:hidden">
            {schedulesList.map((item) => (
              <div
                key={item.id}
                className="cursor-pointer rounded-xl border border-border p-4 hover:border-accent/30"
                onClick={() => navigate(`/schedules/${item.id}`)}
              >
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <h3 className="font-medium">{item.name}</h3>
                    <p className="text-xs text-muted-foreground">{getScheduleDisplayText(item)}</p>
                  </div>
                  {getStatusBadge(item)}
                </div>
                <div className="mt-3 flex items-center gap-2 text-xs text-muted-foreground">
                  <Badge variant="outline" className="text-xs">{jobTypeLabels[item.jobType]}</Badge>
                  <span>{targetTypeLabels[item.targetType]}</span>
                  {item.nextFireTimeUtc && (
                    <span className="flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {formatLocalTime(item.nextFireTimeUtc)}
                    </span>
                  )}
                </div>
              </div>
            ))}
          </div>

          {/* Desktop table */}
          <div className="hidden md:block rounded-xl border border-border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Schedule</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Target</TableHead>
                  <TableHead>Next Run</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {schedulesList.map((item) => (
                  <TableRow
                    key={item.id}
                    className="cursor-pointer"
                    onClick={() => navigate(`/schedules/${item.id}`)}
                  >
                    <TableCell className="font-medium">{item.name}</TableCell>
                    <TableCell className="text-muted-foreground text-sm">
                      {getScheduleDisplayText(item)}
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className="text-xs">{jobTypeLabels[item.jobType]}</Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {targetTypeLabels[item.targetType]}
                    </TableCell>
                    <TableCell className="text-muted-foreground text-sm">
                      {formatLocalTime(item.nextFireTimeUtc)}
                    </TableCell>
                    <TableCell>{getStatusBadge(item)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-1" onClick={(e) => e.stopPropagation()}>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleTrigger(item.id, item.name)}
                          disabled={!item.isEnabled}
                          title="Run now"
                        >
                          <Zap className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleToggle(item)}
                          disabled={togglingId === item.id || (item.scheduleMode === 'OneOff' && !item.isEnabled)}
                          title={item.isEnabled ? 'Pause' : 'Enable'}
                        >
                          {item.isEnabled ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleDelete(item.id, item.name)}
                          disabled={deletingId === item.id}
                        >
                          <Trash2 className="h-4 w-4 text-destructive" />
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

      {/* Create Dialog */}
      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Create Schedule</DialogTitle>
            <DialogDescription>
              Schedule an agent to run at a specific time or on a recurring basis
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>Name</Label>
              <Input
                value={newSchedule.name}
                onChange={(e) => setNewSchedule({ ...newSchedule, name: e.target.value })}
                placeholder="Daily market report"
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Mode</Label>
                <Select
                  value={newSchedule.scheduleMode}
                  onValueChange={(v) => setNewSchedule({ ...newSchedule, scheduleMode: v as ScheduleMode })}
                >
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Recurring">Recurring</SelectItem>
                    <SelectItem value="OneOff">One-off</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Type</Label>
                <Select
                  value={newSchedule.jobType}
                  onValueChange={(v) => setNewSchedule({ ...newSchedule, jobType: v as ScheduleJobType })}
                >
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="AgentInvocation">Agent Invocation</SelectItem>
                    <SelectItem value="Reminder">Reminder</SelectItem>
                    <SelectItem value="ReportGeneration">Report Generation</SelectItem>
                    <SelectItem value="WorkflowExecution">Workflow</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            {newSchedule.scheduleMode === 'Recurring' ? (
              <div>
                <Label>Cron Expression</Label>
                <Input
                  value={newSchedule.cronExpression ?? ''}
                  onChange={(e) => setNewSchedule({ ...newSchedule, cronExpression: e.target.value })}
                  placeholder="0 0 8 ? * MON-FRI"
                />
                {newSchedule.cronExpression && (
                  <p className="mt-1 text-xs text-muted-foreground">
                    {formatCron(newSchedule.cronExpression)}
                  </p>
                )}
              </div>
            ) : (
              <div>
                <Label>Run At</Label>
                <Input
                  type="datetime-local"
                  value={newSchedule.runAtUtc?.toString().slice(0, 16) ?? ''}
                  onChange={(e) => setNewSchedule({ ...newSchedule, runAtUtc: e.target.value ? new Date(e.target.value).toISOString() : undefined })}
                />
              </div>
            )}
            <div>
              <Label>Target</Label>
              <Select
                value={newSchedule.targetType}
                onValueChange={(v) => setNewSchedule({ ...newSchedule, targetType: v as ScheduleTargetType, targetAgentDefinitionId: undefined })}
              >
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Navi">Navi</SelectItem>
                  <SelectItem value="CustomAgent">Custom Agent</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {newSchedule.targetType === 'CustomAgent' && (
              <div>
                <Label>Agent</Label>
                <Select
                  value={newSchedule.targetAgentDefinitionId ?? ''}
                  onValueChange={(v) => setNewSchedule({ ...newSchedule, targetAgentDefinitionId: v })}
                >
                  <SelectTrigger><SelectValue placeholder="Select an agent" /></SelectTrigger>
                  <SelectContent>
                    {agentList.map((agent) => (
                      <SelectItem key={agent.id} value={agent.id}>{agent.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {agentList.length === 0 && (
                  <p className="mt-1 text-xs text-muted-foreground">No agent definitions found</p>
                )}
              </div>
            )}
            <div>
              <Label>Prompt</Label>
              <Textarea
                value={newSchedule.userPrompt}
                onChange={(e) => setNewSchedule({ ...newSchedule, userPrompt: e.target.value })}
                placeholder="Generate a daily market report covering..."
                rows={4}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsDialogOpen(false)}>Cancel</Button>
            <Button
              onClick={handleCreate}
              disabled={isCreating || !newSchedule.name.trim() || !newSchedule.userPrompt.trim()}
            >
              {isCreating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
