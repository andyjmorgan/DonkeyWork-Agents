import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Clock, CheckCircle2, XCircle, Loader2, ChevronLeft, ChevronRight, Eye, PlayCircle } from 'lucide-react'
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
import { executions, agents, type AgentExecution, type Agent } from '@/lib/api'

const PAGE_SIZE = 20

export function ExecutionsPage() {
  const navigate = useNavigate()
  const [items, setItems] = useState<AgentExecution[]>([])
  const [agentMap, setAgentMap] = useState<Record<string, Agent>>({})
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(0)
  const [totalCount, setTotalCount] = useState(0)

  useEffect(() => {
    loadExecutions()
    loadAgents()
  }, [page])

  const loadExecutions = async () => {
    try {
      setLoading(true)
      const response = await executions.list(undefined, page * PAGE_SIZE, PAGE_SIZE)
      setItems(response.executions)
      setTotalCount(response.totalCount)
    } catch (error) {
      console.error('Failed to load executions:', error)
    } finally {
      setLoading(false)
    }
  }

  const loadAgents = async () => {
    try {
      const agentList = await agents.list()
      const map: Record<string, Agent> = {}
      agentList.forEach(a => { map[a.id] = a })
      setAgentMap(map)
    } catch (error) {
      console.error('Failed to load agents:', error)
    }
  }

  const totalPages = Math.ceil(totalCount / PAGE_SIZE)
  const canGoBack = page > 0
  const canGoForward = page < totalPages - 1

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Completed':
        return <CheckCircle2 className="h-4 w-4 text-green-600" />
      case 'Failed':
        return <XCircle className="h-4 w-4 text-destructive" />
      case 'Running':
      case 'Pending':
        return <Loader2 className="h-4 w-4 animate-spin text-blue-600" />
      default:
        return <Clock className="h-4 w-4 text-muted-foreground" />
    }
  }

  const getStatusVariant = (status: string): 'default' | 'secondary' | 'destructive' | 'outline' => {
    switch (status) {
      case 'Completed':
        return 'outline'
      case 'Failed':
        return 'destructive'
      case 'Running':
      case 'Pending':
        return 'secondary'
      default:
        return 'default'
    }
  }

  const formatDuration = (startedAt: string, completedAt?: string) => {
    if (!completedAt) return '-'
    const start = new Date(startedAt).getTime()
    const end = new Date(completedAt).getTime()
    const durationMs = end - start
    if (durationMs < 1000) return `${durationMs}ms`
    if (durationMs < 60000) return `${(durationMs / 1000).toFixed(1)}s`
    return `${(durationMs / 60000).toFixed(1)}m`
  }

  if (loading && items.length === 0) {
    return (
      <div className="p-4 md:p-6 lg:p-8">
        <h1 className="text-2xl font-bold mb-6">Executions</h1>
        <div className="flex h-32 items-center justify-center">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      </div>
    )
  }

  return (
    <div className="p-4 md:p-6 lg:p-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Executions</h1>
        <div className="text-sm text-muted-foreground">
          {totalCount} total execution{totalCount !== 1 ? 's' : ''}
        </div>
      </div>

      {items.length === 0 ? (
        <div className="rounded-lg border border-dashed border-border p-8 text-center">
          <PlayCircle className="mx-auto h-12 w-12 text-muted-foreground/50" />
          <h3 className="mt-4 text-lg font-medium">No executions yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Run an agent to see execution history here.
          </p>
        </div>
      ) : (
        <>
          {/* Mobile view - card layout */}
          <div className="space-y-3 md:hidden">
            {items.map((execution) => (
              <div
                key={execution.id}
                className="rounded-lg border border-border bg-card p-4 space-y-2 cursor-pointer hover:bg-accent/50 transition-colors"
                onClick={() => navigate(`/executions/${execution.id}`)}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      {getStatusIcon(execution.status)}
                      <Badge variant={getStatusVariant(execution.status)}>
                        {execution.status}
                      </Badge>
                    </div>
                    <div className="text-sm">
                      <span className="text-muted-foreground">Agent: </span>
                      <span className="font-medium">
                        {agentMap[execution.agentId]?.name || execution.agentId.slice(0, 8)}
                      </span>
                    </div>
                    <div className="text-xs text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {new Date(execution.startedAt).toLocaleString()}
                    </div>
                  </div>
                  <Button variant="ghost" size="icon" className="shrink-0">
                    <Eye className="h-4 w-4" />
                  </Button>
                </div>
                {execution.errorMessage && (
                  <div className="text-xs text-destructive truncate">
                    {execution.errorMessage}
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Desktop view - table layout */}
          <div className="hidden md:block rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Status</TableHead>
                  <TableHead>Agent</TableHead>
                  <TableHead>Started</TableHead>
                  <TableHead>Duration</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((execution) => (
                  <TableRow
                    key={execution.id}
                    className="cursor-pointer"
                    onClick={() => navigate(`/executions/${execution.id}`)}
                  >
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {getStatusIcon(execution.status)}
                        <Badge variant={getStatusVariant(execution.status)}>
                          {execution.status}
                        </Badge>
                      </div>
                    </TableCell>
                    <TableCell className="font-medium">
                      {agentMap[execution.agentId]?.name || execution.agentId.slice(0, 8)}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(execution.startedAt).toLocaleString()}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {formatDuration(execution.startedAt, execution.completedAt)}
                    </TableCell>
                    <TableCell>
                      <Button variant="ghost" size="icon">
                        <Eye className="h-4 w-4" />
                      </Button>
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
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage(p => p - 1)}
                  disabled={!canGoBack}
                >
                  <ChevronLeft className="h-4 w-4" />
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage(p => p + 1)}
                  disabled={!canGoForward}
                >
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
