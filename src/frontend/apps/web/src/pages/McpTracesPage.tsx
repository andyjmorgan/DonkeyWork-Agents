import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Clock, CheckCircle2, XCircle, Loader2, ChevronLeft, ChevronRight, Eye, Server, Timer, Globe, RefreshCw } from 'lucide-react'
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
import { mcpTraces, type McpTraceSummary } from '@donkeywork/api-client'

const PAGE_SIZE = 20

const METHOD_COLORS: Record<string, string> = {
  'initialize': 'bg-green-500/10 text-green-400 border-green-500/20',
  'tools/list': 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20',
  'tools/call': 'bg-purple-500/10 text-purple-400 border-purple-500/20',
  'notifications/initialized': 'bg-amber-500/10 text-amber-400 border-amber-500/20',
}

function getMethodColor(method: string) {
  return METHOD_COLORS[method] ?? 'bg-slate-500/10 text-slate-400 border-slate-500/20'
}

function getStatusIcon(isSuccess: boolean, httpStatusCode: number) {
  if (isSuccess) return <CheckCircle2 className="h-4 w-4 text-green-600" />
  if (httpStatusCode === 401 || httpStatusCode === 403) return <XCircle className="h-4 w-4 text-amber-500" />
  return <XCircle className="h-4 w-4 text-destructive" />
}

function getStatusVariant(isSuccess: boolean): 'outline' | 'destructive' {
  return isSuccess ? 'outline' : 'destructive'
}

function formatDuration(ms?: number) {
  if (ms == null) return '-'
  if (ms < 1000) return `${ms}ms`
  if (ms < 60000) return `${(ms / 1000).toFixed(1)}s`
  return `${(ms / 60000).toFixed(1)}m`
}

function formatHttpStatus(code: number) {
  if (code >= 200 && code < 300) return 'text-green-500'
  if (code >= 400 && code < 500) return 'text-amber-500'
  return 'text-destructive'
}

export function McpTracesPage() {
  const navigate = useNavigate()
  const [items, setItems] = useState<McpTraceSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(0)
  const [totalCount, setTotalCount] = useState(0)

  useEffect(() => {
    loadTraces()
  }, [page])

  const loadTraces = async () => {
    try {
      setLoading(true)
      const response = await mcpTraces.list(page * PAGE_SIZE, PAGE_SIZE)
      setItems(response.items)
      setTotalCount(response.totalCount)
    } catch (error) {
      console.error('Failed to load MCP traces:', error)
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
        <h1 className="text-2xl font-bold mb-6">MCP Traces</h1>
        <div className="flex h-32 items-center justify-center">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      </div>
    )
  }

  return (
    <div className="p-4 md:p-6 lg:p-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">MCP Traces</h1>
        <div className="flex items-center gap-3">
          <Button variant="outline" size="sm" onClick={loadTraces} disabled={loading}>
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          <div className="text-sm text-muted-foreground">
            {totalCount} total
          </div>
        </div>
      </div>

      {items.length === 0 ? (
        <div className="rounded-lg border border-dashed border-border p-8 text-center">
          <Server className="mx-auto h-12 w-12 text-muted-foreground/50" />
          <h3 className="mt-4 text-lg font-medium">No MCP traces yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            MCP client traffic will appear here when clients connect to your server.
          </p>
        </div>
      ) : (
        <>
          {/* Mobile view */}
          <div className="space-y-3 md:hidden">
            {items.map((trace) => (
              <div
                key={trace.id}
                className="rounded-lg border border-border bg-card p-4 space-y-2 cursor-pointer hover:bg-accent/50 transition-colors"
                onClick={() => navigate(`/mcp-traces/${trace.id}`)}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      {getStatusIcon(trace.isSuccess, trace.httpStatusCode)}
                      <Badge variant={getStatusVariant(trace.isSuccess)}>
                        {trace.isSuccess ? 'OK' : 'Error'}
                      </Badge>
                      <span className={`rounded px-1.5 py-0.5 text-[10px] uppercase tracking-wider font-semibold border ${getMethodColor(trace.method)}`}>
                        {trace.method}
                      </span>
                    </div>
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <span className={`font-mono font-semibold ${formatHttpStatus(trace.httpStatusCode)}`}>
                        {trace.httpStatusCode}
                      </span>
                    </div>
                    <div className="text-xs text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {new Date(trace.startedAt).toLocaleString()}
                    </div>
                    <div className="flex items-center gap-3 text-xs text-muted-foreground">
                      {trace.durationMs != null && (
                        <span className="flex items-center gap-1">
                          <Timer className="h-3 w-3" />
                          {formatDuration(trace.durationMs)}
                        </span>
                      )}
                      {trace.clientIpAddress && (
                        <span className="flex items-center gap-1">
                          <Globe className="h-3 w-3" />
                          {trace.clientIpAddress}
                        </span>
                      )}
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
                  <TableHead>Method</TableHead>
                  <TableHead>HTTP</TableHead>
                  <TableHead>Started</TableHead>
                  <TableHead>Duration</TableHead>
                  <TableHead>Client IP</TableHead>
                  <TableHead className="w-[80px]" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((trace) => (
                  <TableRow
                    key={trace.id}
                    className="cursor-pointer"
                    onClick={() => navigate(`/mcp-traces/${trace.id}`)}
                  >
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {getStatusIcon(trace.isSuccess, trace.httpStatusCode)}
                        <Badge variant={getStatusVariant(trace.isSuccess)}>
                          {trace.isSuccess ? 'OK' : 'Error'}
                        </Badge>
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className={`rounded px-1.5 py-0.5 text-[10px] uppercase tracking-wider font-semibold border ${getMethodColor(trace.method)}`}>
                        {trace.method}
                      </span>
                    </TableCell>
                    <TableCell>
                      <span className={`font-mono font-semibold ${formatHttpStatus(trace.httpStatusCode)}`}>
                        {trace.httpStatusCode}
                      </span>
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(trace.startedAt).toLocaleString()}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {formatDuration(trace.durationMs)}
                    </TableCell>
                    <TableCell className="text-muted-foreground font-mono text-xs">
                      {trace.clientIpAddress ?? '-'}
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
