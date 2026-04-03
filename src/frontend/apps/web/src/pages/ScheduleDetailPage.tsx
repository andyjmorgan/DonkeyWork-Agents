import { useState, useEffect, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Loader2, ArrowLeft, Zap, Play, Pause, Trash2, Clock, CheckCircle2, XCircle, AlertCircle } from 'lucide-react'
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
} from '@donkeywork/ui'
import {
  schedules,
  type ScheduledJobDetail,
  type ScheduledJobExecution,
  type ScheduleExecutionStatus,
} from '@donkeywork/api-client'

const executionStatusConfig: Record<ScheduleExecutionStatus, { label: string; color: string; icon: typeof CheckCircle2 }> = {
  Running: { label: 'Running', color: 'border-blue-500/30 text-blue-400', icon: Loader2 },
  Succeeded: { label: 'Succeeded', color: 'border-green-500/30 text-green-400', icon: CheckCircle2 },
  Failed: { label: 'Failed', color: 'border-red-500/30 text-red-400', icon: XCircle },
  Cancelled: { label: 'Cancelled', color: 'border-yellow-500/30 text-yellow-400', icon: AlertCircle },
}

function formatLocalTime(utcString?: string): string {
  if (!utcString) return '—'
  return new Date(utcString).toLocaleString()
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

function formatDuration(start: string, end?: string): string {
  if (!end) return 'Running...'
  const ms = new Date(end).getTime() - new Date(start).getTime()
  if (ms < 1000) return `${ms}ms`
  if (ms < 60000) return `${(ms / 1000).toFixed(1)}s`
  return `${Math.floor(ms / 60000)}m ${Math.round((ms % 60000) / 1000)}s`
}

export function ScheduleDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [isLoading, setIsLoading] = useState(true)
  const [schedule, setSchedule] = useState<ScheduledJobDetail | null>(null)
  const [executions, setExecutions] = useState<ScheduledJobExecution[]>([])
  const [togglingEnabled, setTogglingEnabled] = useState(false)

  const load = useCallback(async () => {
    if (!id) return
    try {
      const [detail, executionData] = await Promise.all([
        schedules.get(id),
        schedules.listExecutions(id),
      ])
      setSchedule(detail)
      setExecutions(executionData.items)
    } catch (error) {
      console.error('Failed to load schedule:', error)
    } finally {
      setIsLoading(false)
    }
  }, [id])

  useEffect(() => {
    load()
  }, [load])

  const handleToggle = async () => {
    if (!schedule) return
    try {
      setTogglingEnabled(true)
      if (schedule.isEnabled) {
        await schedules.disable(schedule.id)
      } else {
        await schedules.enable(schedule.id)
      }
      await load()
    } finally {
      setTogglingEnabled(false)
    }
  }

  const handleTrigger = async () => {
    if (!schedule) return
    if (!window.confirm(`Manually trigger "${schedule.name}" to run now?`)) return
    await schedules.trigger(schedule.id)
    await load()
  }

  const handleDelete = async () => {
    if (!schedule) return
    if (!window.confirm(`Delete "${schedule.name}" and all its execution history?`)) return
    await schedules.delete(schedule.id)
    navigate('/schedules')
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!schedule) {
    return (
      <div className="flex h-64 flex-col items-center justify-center gap-2">
        <p className="text-muted-foreground">Schedule not found</p>
        <Button variant="outline" onClick={() => navigate('/schedules')}>Back to Schedules</Button>
      </div>
    )
  }

  const isCompletedOneOff = schedule.scheduleMode === 'OneOff' && !schedule.isEnabled

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate('/schedules')}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold">{schedule.name}</h1>
          {schedule.description && (
            <p className="text-muted-foreground">{schedule.description}</p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={handleTrigger} disabled={!schedule.isEnabled}>
            <Zap className="h-4 w-4" />
            Run Now
          </Button>
          {!isCompletedOneOff && (
            <Button variant="outline" size="sm" onClick={handleToggle} disabled={togglingEnabled}>
              {schedule.isEnabled ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
              {schedule.isEnabled ? 'Pause' : 'Enable'}
            </Button>
          )}
          <Button variant="outline" size="sm" onClick={handleDelete}>
            <Trash2 className="h-4 w-4 text-destructive" />
          </Button>
        </div>
      </div>

      {/* Schedule info */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div className="rounded-xl border border-border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wider">Schedule</p>
          <p className="mt-1 font-medium">
            {schedule.scheduleMode === 'Recurring' && schedule.cronExpression
              ? formatCron(schedule.cronExpression)
              : schedule.runAtUtc
                ? formatLocalTime(schedule.runAtUtc)
                : '—'}
          </p>
          {schedule.cronExpression && (
            <p className="mt-1 text-xs font-mono text-muted-foreground">{schedule.cronExpression}</p>
          )}
        </div>
        <div className="rounded-xl border border-border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wider">Next Run</p>
          <p className="mt-1 font-medium">{formatLocalTime(schedule.nextFireTimeUtc)}</p>
        </div>
        <div className="rounded-xl border border-border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wider">Target</p>
          <p className="mt-1 font-medium">{schedule.targetType}</p>
          {schedule.targetName && <p className="text-xs text-muted-foreground">{schedule.targetName}</p>}
        </div>
      </div>

      {/* Prompt */}
      {schedule.payload && (
        <div className="rounded-xl border border-border p-4">
          <p className="text-xs text-muted-foreground uppercase tracking-wider mb-2">Prompt</p>
          <p className="text-sm whitespace-pre-wrap">{schedule.payload.userPrompt}</p>
        </div>
      )}

      {/* Execution History */}
      <div>
        <h2 className="text-lg font-semibold mb-4">Execution History</h2>
        {executions.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border p-8 text-center">
            <Clock className="mx-auto h-8 w-8 text-muted-foreground" />
            <p className="mt-2 text-sm text-muted-foreground">No executions yet</p>
          </div>
        ) : (
          <div className="rounded-xl border border-border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Status</TableHead>
                  <TableHead>Started</TableHead>
                  <TableHead>Duration</TableHead>
                  <TableHead>Trigger</TableHead>
                  <TableHead>Details</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {executions.map((exec) => {
                  const config = executionStatusConfig[exec.status]
                  const StatusIcon = config.icon
                  return (
                    <TableRow key={exec.id}>
                      <TableCell>
                        <Badge variant="outline" className={config.color}>
                          <StatusIcon className={`h-3 w-3 mr-1 ${exec.status === 'Running' ? 'animate-spin' : ''}`} />
                          {config.label}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {formatLocalTime(exec.startedAtUtc)}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {formatDuration(exec.startedAtUtc, exec.completedAtUtc)}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {exec.triggerSource}
                      </TableCell>
                      <TableCell className="text-sm text-destructive max-w-xs truncate">
                        {exec.errorDetails}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </div>
        )}
      </div>
    </div>
  )
}
