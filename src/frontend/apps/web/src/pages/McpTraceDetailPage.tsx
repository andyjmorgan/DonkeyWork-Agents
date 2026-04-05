import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Clock,
  CheckCircle2,
  XCircle,
  Loader2,
  Timer,
  Globe,
  AlertTriangle,
  Server,
  Hash,
  User,
  ChevronDown,
} from 'lucide-react'
import {
  Button,
  Badge,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@donkeywork/ui'
import { JsonViewer } from '@/components/ui/json-viewer'
import { mcpTraces, type McpTraceDetail } from '@donkeywork/api-client'

const METHOD_COLORS: Record<string, string> = {
  'initialize': 'bg-green-500/10 text-green-400 border-green-500/20',
  'tools/list': 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20',
  'tools/call': 'bg-purple-500/10 text-purple-400 border-purple-500/20',
  'notifications/initialized': 'bg-amber-500/10 text-amber-400 border-amber-500/20',
}

function getMethodColor(method: string) {
  return METHOD_COLORS[method] ?? 'bg-slate-500/10 text-slate-400 border-slate-500/20'
}

function formatDuration(ms?: number) {
  if (ms == null) return '-'
  if (ms < 1000) return `${ms}ms`
  if (ms < 60000) return `${(ms / 1000).toFixed(2)}s`
  return `${(ms / 60000).toFixed(2)}m`
}

function formatHttpStatus(code: number) {
  if (code >= 200 && code < 300) return 'text-green-500'
  if (code >= 400 && code < 500) return 'text-amber-500'
  return 'text-destructive'
}

function tryParseJson(str: string): unknown {
  try {
    return JSON.parse(str)
  } catch {
    return str
  }
}

function CollapsibleSection({ title, defaultOpen = true, children }: { title: string; defaultOpen?: boolean; children: React.ReactNode }) {
  const [open, setOpen] = useState(defaultOpen)
  return (
    <Card>
      <CardHeader
        className="cursor-pointer select-none"
        onClick={() => setOpen(!open)}
      >
        <div className="flex items-center gap-2">
          <ChevronDown className={`h-4 w-4 transition-transform ${open ? '' : '-rotate-90'}`} />
          <CardTitle className="text-base">{title}</CardTitle>
        </div>
      </CardHeader>
      {open && <CardContent>{children}</CardContent>}
    </Card>
  )
}

export function McpTraceDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [trace, setTrace] = useState<McpTraceDetail | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    loadTrace()
  }, [id])

  const loadTrace = async () => {
    try {
      setLoading(true)
      const data = await mcpTraces.get(id!)
      setTrace(data)
    } catch (error) {
      console.error('Failed to load MCP trace:', error)
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="p-4 md:p-6 lg:p-8">
        <div className="flex h-32 items-center justify-center">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      </div>
    )
  }

  if (!trace) {
    return (
      <div className="p-4 md:p-6 lg:p-8">
        <Button variant="ghost" size="sm" onClick={() => navigate('/mcp-traces')}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to MCP Traces
        </Button>
        <div className="mt-8 text-center text-muted-foreground">Trace not found.</div>
      </div>
    )
  }

  return (
    <div className="p-4 md:p-6 lg:p-8 space-y-6">
      {/* Header */}
      <div>
        <Button variant="ghost" size="sm" onClick={() => navigate('/mcp-traces')} className="mb-4">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to MCP Traces
        </Button>
        <div className="flex items-center gap-3 flex-wrap">
          <span className={`rounded px-2 py-1 text-xs uppercase tracking-wider font-semibold border ${getMethodColor(trace.method)}`}>
            {trace.method}
          </span>
          {trace.isSuccess ? (
            <Badge variant="outline" className="gap-1">
              <CheckCircle2 className="h-3 w-3 text-green-600" />
              Success
            </Badge>
          ) : (
            <Badge variant="destructive" className="gap-1">
              <XCircle className="h-3 w-3" />
              Error
            </Badge>
          )}
          <span className={`font-mono font-semibold text-sm ${formatHttpStatus(trace.httpStatusCode)}`}>
            HTTP {trace.httpStatusCode}
          </span>
        </div>
        <p className="mt-1 text-xs text-muted-foreground font-mono">{trace.id}</p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        <Card>
          <CardContent className="pt-4 pb-4">
            <div className="flex items-center gap-2 text-xs text-muted-foreground mb-1">
              <Server className="h-3 w-3" />
              Method
            </div>
            <p className="text-sm font-medium font-mono">{trace.method}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4 pb-4">
            <div className="flex items-center gap-2 text-xs text-muted-foreground mb-1">
              <Hash className="h-3 w-3" />
              HTTP Status
            </div>
            <p className={`text-sm font-medium font-mono ${formatHttpStatus(trace.httpStatusCode)}`}>
              {trace.httpStatusCode}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4 pb-4">
            <div className="flex items-center gap-2 text-xs text-muted-foreground mb-1">
              <Timer className="h-3 w-3" />
              Duration
            </div>
            <p className="text-sm font-medium">{formatDuration(trace.durationMs)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4 pb-4">
            <div className="flex items-center gap-2 text-xs text-muted-foreground mb-1">
              <Clock className="h-3 w-3" />
              Started
            </div>
            <p className="text-sm font-medium">{new Date(trace.startedAt).toLocaleString()}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4 pb-4">
            <div className="flex items-center gap-2 text-xs text-muted-foreground mb-1">
              <Globe className="h-3 w-3" />
              Client IP
            </div>
            <p className="text-sm font-medium font-mono">{trace.clientIpAddress ?? '-'}</p>
          </CardContent>
        </Card>
      </div>

      {/* Error message */}
      {trace.errorMessage && (
        <Card className="border-destructive/50 bg-destructive/5">
          <CardContent className="pt-4 pb-4">
            <div className="flex items-start gap-2">
              <AlertTriangle className="h-4 w-4 text-destructive shrink-0 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-destructive">Error</p>
                <p className="text-sm text-destructive/80 mt-1">{trace.errorMessage}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Request */}
      <CollapsibleSection title="Request">
        <JsonViewer data={tryParseJson(trace.requestBody)} collapsed={2} className="max-h-96" />
      </CollapsibleSection>

      {/* Response */}
      {trace.responseBody && (
        <CollapsibleSection title="Response">
          <JsonViewer data={tryParseJson(trace.responseBody)} collapsed={2} className="max-h-96" />
        </CollapsibleSection>
      )}

      {/* Metadata */}
      <CollapsibleSection title="Metadata" defaultOpen={false}>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
          {trace.jsonRpcId && (
            <div>
              <span className="text-muted-foreground">JSON-RPC ID</span>
              <p className="font-mono">{trace.jsonRpcId}</p>
            </div>
          )}
          {trace.userId && (
            <div>
              <span className="text-muted-foreground flex items-center gap-1">
                <User className="h-3 w-3" />
                User ID
              </span>
              <p className="font-mono text-xs">{trace.userId}</p>
            </div>
          )}
          {trace.userAgent && (
            <div className="md:col-span-2">
              <span className="text-muted-foreground">User Agent</span>
              <p className="font-mono text-xs break-all">{trace.userAgent}</p>
            </div>
          )}
          {trace.completedAt && (
            <div>
              <span className="text-muted-foreground">Completed</span>
              <p>{new Date(trace.completedAt).toLocaleString()}</p>
            </div>
          )}
          <div>
            <span className="text-muted-foreground">Created</span>
            <p>{new Date(trace.createdAt).toLocaleString()}</p>
          </div>
        </div>
      </CollapsibleSection>
    </div>
  )
}
