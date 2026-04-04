import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Clock, CheckCircle2, XCircle, Loader2, ChevronLeft, ChevronRight, Eye, Cpu, Timer, Ban, Pause, Skull } from 'lucide-react'
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
import { agentExecutions, type AgentExecutionSummary } from '@donkeywork/api-client'

const PAGE_SIZE = 20

const TYPE_COLORS: Record<string, string> = {
  conversation: 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20',
  delegate: 'bg-purple-500/10 text-purple-400 border-purple-500/20',
  agent: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
}

function getStatusIcon(status: string) {
  switch (status.toLowerCase()) {
    case 'completed':
      return <CheckCircle2 className="h-4 w-4 text-green-600" />
    case 'failed':
      return <XCircle className="h-4 w-4 text-destructive" />
    case 'running':
      return <Loader2 className="h-4 w-4 animate-spin text-blue-600" />
    case 'cancelled':
      return <Ban className="h-4 w-4 text-amber-500" />
    case 'idle':
      return <Pause className="h-4 w-4 text-cyan-500" />
    case 'stale':
      return <Skull className="h-4 w-4 text-orange-500" />
    default:
      return <Clock className="h-4 w-4 text-muted-foreground" />
  }
}

function getStatusVariant(status: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status.toLowerCase()) {
    case 'completed':
      return 'outline'
    case 'failed':
      return 'destructive'
    case 'running':
      return 'secondary'
    case 'idle':
      return 'secondary'
    case 'stale':
      return 'destructive'
    default:
      return 'default'
  }
}

function formatDuration(ms?: number) {
  if (ms == null) return '-'
  if (ms < 1000) return `${ms}ms`
  if (ms < 60000) return `${(ms / 1000).toFixed(1)}s`
  return `${(ms / 60000).toFixed(1)}m`
}

function formatTokens(input?: number, output?: number) {
  const total = (input ?? 0) + (output ?? 0)
  if (total === 0) return '-'
  if (total < 1000) return `${total}`
  return `${(total / 1000).toFixed(1)}k`
}

export function AgentExecutionsPage() {
  const navigate = useNavigate()
  const [items, setItems] = useState<AgentExecutionSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(0)
  const [totalCount, setTotalCount] = useState(0)

  useEffect(() => {
    loadExecutions()
  }, [page])

  const loadExecutions = async () => {
    try {
      setLoading(true)
      const response = await agentExecutions.list(page * PAGE_SIZE, PAGE_SIZE)
      setItems(response.items)
      setTotalCount(response.totalCount)
    } catch (error) {
      console.error('Failed to load agent executions:', error)
    } finally {
      setLoading(false)
    }
  }

  const totalPages = Math.ceil(totalCount / PAGE_SIZE)
  const canGoBack = page > 0
  const canGoForward = page < totalPages - 1

  if (loading && items.length === 0) {
    return (
      <div className="p-4 md:p-6 lg:p-8">
        <h1 className="text-2xl font-bold mb-6">Agent Executions</h1>
        <div className="flex h-32 items-center justify-center">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      </div>
    )
  }

  return (
    <div className="p-4 md:p-6 lg:p-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Agent Executions</h1>
        <div className="text-sm text-muted-foreground">
          {totalCount} total
        </div>
      </div>

      {items.length === 0 ? (
        <div className="rounded-lg border border-dashed border-border p-8 text-center">
          <Cpu className="mx-auto h-12 w-12 text-muted-foreground/50" />
          <h3 className="mt-4 text-lg font-medium">No agent executions yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Start a conversation to see agent execution traces here.
          </p>
        </div>
      ) : (
        <>
          {/* Mobile view */}
          <div className="space-y-3 md:hidden">
            {items.map((exec) => (
              <div
                key={exec.id}
                className="rounded-lg border border-border bg-card p-4 space-y-2 cursor-pointer hover:bg-accent/50 transition-colors"
                onClick={() => navigate(`/agent-executions/${exec.id}`)}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      {getStatusIcon(exec.status)}
                      <Badge variant={getStatusVariant(exec.status)}>{exec.status}</Badge>
                      <span className={`rounded px-1.5 py-0.5 text-[10px] uppercase tracking-wider font-semibold border ${TYPE_COLORS[exec.agentType.toLowerCase()] ?? TYPE_COLORS.agent}`}>
                        {exec.agentType}
                      </span>
                    </div>
                    <div className="text-sm font-medium truncate">{exec.label}</div>
                    <div className="text-xs text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {new Date(exec.startedAt).toLocaleString()}
                    </div>
                    <div className="flex items-center gap-3 text-xs text-muted-foreground">
                      {exec.durationMs != null && (
                        <span className="flex items-center gap-1">
                          <Timer className="h-3 w-3" />
                          {formatDuration(exec.durationMs)}
                        </span>
                      )}
                      <span className="flex items-center gap-1">
                        <Cpu className="h-3 w-3" />
                        {formatTokens(exec.inputTokensUsed, exec.outputTokensUsed)}
                      </span>
                    </div>
                  </div>
                  <span className="shrink-0 inline-flex items-center justify-center" aria-hidden="true">
                    <Eye className="h-4 w-4" />
                  </span>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop view */}
          <div className="hidden md:block rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Status</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Label</TableHead>
                  <TableHead>Model</TableHead>
                  <TableHead>Started</TableHead>
                  <TableHead>Duration</TableHead>
                  <TableHead>Tokens</TableHead>
                  <TableHead className="w-[80px]" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((exec) => (
                  <TableRow
                    key={exec.id}
                    className="cursor-pointer"
                    onClick={() => navigate(`/agent-executions/${exec.id}`)}
                  >
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {getStatusIcon(exec.status)}
                        <Badge variant={getStatusVariant(exec.status)}>{exec.status}</Badge>
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className={`rounded px-1.5 py-0.5 text-[10px] uppercase tracking-wider font-semibold border ${TYPE_COLORS[exec.agentType.toLowerCase()] ?? TYPE_COLORS.agent}`}>
                        {exec.agentType}
                      </span>
                    </TableCell>
                    <TableCell className="font-medium max-w-[200px] truncate">{exec.label}</TableCell>
                    <TableCell className="text-muted-foreground text-xs font-mono">{exec.modelId ?? '-'}</TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(exec.startedAt).toLocaleString()}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {formatDuration(exec.durationMs)}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {formatTokens(exec.inputTokensUsed, exec.outputTokensUsed)}
                    </TableCell>
                    <TableCell>
                      <span className="inline-flex h-9 w-9 items-center justify-center rounded-md">
                        <Eye className="h-4 w-4" aria-hidden="true" />
                      </span>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-4">
              <p className="text-sm text-muted-foreground">
                Showing {page * PAGE_SIZE + 1}-{Math.min((page + 1) * PAGE_SIZE, totalCount)} of {totalCount}
              </p>
              <div className="flex items-center gap-2">
                <Button variant="outline" size="sm" onClick={() => setPage(p => p - 1)} disabled={!canGoBack}>
                  <ChevronLeft className="h-4 w-4" />
                  Previous
                </Button>
                <Button variant="outline" size="sm" onClick={() => setPage(p => p + 1)} disabled={!canGoForward}>
                  Next
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
