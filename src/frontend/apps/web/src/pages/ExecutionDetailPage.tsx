import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Clock,
  CheckCircle2,
  XCircle,
  Loader2,
  Play,
  Pause,
  AlertCircle,
  Zap,
  Timer,
} from 'lucide-react'
import {
  Button,
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@donkeywork/ui'
import { JsonViewer } from '@/components/ui/json-viewer'
import { executions, orchestrations, type OrchestrationExecution, type NodeExecution, type Orchestration } from '@/lib/api'

export function ExecutionDetailPage() {
  const { executionId } = useParams<{ executionId: string }>()
  const navigate = useNavigate()
  const [execution, setExecution] = useState<OrchestrationExecution | null>(null)
  const [nodeExecutions, setNodeExecutions] = useState<NodeExecution[]>([])
  const [orchestration, setOrchestration] = useState<Orchestration | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (executionId) {
      loadExecution()
    }
  }, [executionId])

  const loadExecution = async () => {
    if (!executionId) return

    try {
      setLoading(true)
      setError(null)

      // Load execution details
      const exec = await executions.get(executionId)
      setExecution(exec)

      // Load node executions
      const nodeExecResponse = await executions.getNodeExecutions(executionId)
      setNodeExecutions(nodeExecResponse.nodeExecutions)

      // Load orchestration info
      try {
        const orchestrationData = await orchestrations.get(exec.orchestrationId)
        setOrchestration(orchestrationData)
      } catch {
        // Orchestration might have been deleted
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load execution')
    } finally {
      setLoading(false)
    }
  }

  const getStatusIcon = (status: string, size = 'h-4 w-4') => {
    switch (status) {
      case 'Completed':
        return <CheckCircle2 className={`${size} text-green-600`} />
      case 'Failed':
        return <XCircle className={`${size} text-destructive`} />
      case 'Running':
      case 'Pending':
        return <Loader2 className={`${size} animate-spin text-blue-600`} />
      default:
        return <Clock className={`${size} text-muted-foreground`} />
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

  const getNodeTypeIcon = (nodeType: string) => {
    switch (nodeType) {
      case 'start':
        return <Play className="h-4 w-4 text-green-600" />
      case 'end':
        return <Pause className="h-4 w-4 text-red-600" />
      case 'model':
        return <Zap className="h-4 w-4 text-yellow-600" />
      case 'action':
        return <AlertCircle className="h-4 w-4 text-blue-600" />
      default:
        return <Clock className="h-4 w-4 text-muted-foreground" />
    }
  }

  const formatDuration = (durationMs?: number) => {
    if (!durationMs) return '-'
    if (durationMs < 1000) return `${durationMs}ms`
    if (durationMs < 60000) return `${(durationMs / 1000).toFixed(2)}s`
    return `${(durationMs / 60000).toFixed(2)}m`
  }

  if (loading) {
    return (
      <div className="p-4 md:p-6 lg:p-8">
        <div className="flex h-64 items-center justify-center">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      </div>
    )
  }

  if (error || !execution) {
    return (
      <div className="p-4 md:p-6 lg:p-8">
        <Button variant="ghost" onClick={() => navigate('/executions')} className="mb-4">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Executions
        </Button>
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-8 text-center">
          <XCircle className="mx-auto h-12 w-12 text-destructive" />
          <h3 className="mt-4 text-lg font-medium">Failed to load execution</h3>
          <p className="mt-2 text-sm text-muted-foreground">{error || 'Execution not found'}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="p-4 md:p-6 lg:p-8 space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate('/executions')}>
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold">Execution Details</h1>
          <p className="text-sm text-muted-foreground font-mono">{execution.id}</p>
        </div>
        <div className="flex items-center gap-2">
          {getStatusIcon(execution.status, 'h-5 w-5')}
          <Badge variant={getStatusVariant(execution.status)} className="text-sm">
            {execution.status}
          </Badge>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Orchestration</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-lg font-semibold">
              {orchestration?.name || execution.orchestrationId.slice(0, 8)}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Started</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-lg font-semibold">
              {new Date(execution.startedAt).toLocaleString()}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Duration</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-lg font-semibold flex items-center gap-2">
              <Timer className="h-4 w-4 text-muted-foreground" />
              {execution.completedAt
                ? formatDuration(new Date(execution.completedAt).getTime() - new Date(execution.startedAt).getTime())
                : 'Running...'}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Tokens Used</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-lg font-semibold flex items-center gap-2">
              <Zap className="h-4 w-4 text-yellow-600" />
              {execution.totalTokensUsed?.toLocaleString() || '-'}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Error Message */}
      {execution.errorMessage && (
        <Card className="border-destructive/50 bg-destructive/10">
          <CardHeader className="pb-2">
            <CardTitle className="text-destructive flex items-center gap-2">
              <XCircle className="h-5 w-5" />
              Error
            </CardTitle>
          </CardHeader>
          <CardContent>
            <pre className="text-sm text-destructive whitespace-pre-wrap">{execution.errorMessage}</pre>
          </CardContent>
        </Card>
      )}

      {/* Input/Output */}
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Input</CardTitle>
          </CardHeader>
          <CardContent>
            <JsonViewer data={execution.input} className="max-h-64" />
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Output</CardTitle>
          </CardHeader>
          <CardContent>
            {execution.output ? (
              <JsonViewer data={execution.output} className="max-h-64" />
            ) : (
              <div className="text-sm text-muted-foreground">No output available</div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Node Executions */}
      <Card>
        <CardHeader>
          <CardTitle>Node Execution Trace</CardTitle>
          <CardDescription>
            {nodeExecutions.length} node{nodeExecutions.length !== 1 ? 's' : ''} executed
          </CardDescription>
        </CardHeader>
        <CardContent>
          {nodeExecutions.length === 0 ? (
            <div className="text-sm text-muted-foreground text-center py-8">
              No node execution data available
            </div>
          ) : (
            <Accordion type="multiple" defaultValue={nodeExecutions.map(n => n.id)} className="space-y-2">
              {nodeExecutions.map((node, index) => (
                <AccordionItem
                  key={node.id}
                  value={node.id}
                  className="border rounded-lg px-4"
                >
                  <AccordionTrigger className="hover:no-underline">
                    <div className="flex items-center gap-3 flex-1">
                      <span className="text-xs text-muted-foreground font-mono w-6">
                        {index + 1}
                      </span>
                      {getNodeTypeIcon(node.nodeType)}
                      <Badge variant="outline" className="text-xs">
                        {node.nodeType}
                        {node.actionType && `:${node.actionType}`}
                      </Badge>
                      <span className="font-medium">{node.nodeName || node.nodeId.slice(0, 8)}</span>
                      <div className="flex items-center gap-2 ml-auto mr-4">
                        {getStatusIcon(node.status)}
                        <Badge variant={getStatusVariant(node.status)} className="text-xs">
                          {node.status}
                        </Badge>
                        {node.durationMs !== undefined && (
                          <span className="text-xs text-muted-foreground">
                            {formatDuration(node.durationMs)}
                          </span>
                        )}
                        {node.tokensUsed && (
                          <span className="text-xs text-muted-foreground flex items-center gap-1">
                            <Zap className="h-3 w-3" />
                            {node.tokensUsed}
                          </span>
                        )}
                      </div>
                    </div>
                  </AccordionTrigger>
                  <AccordionContent className="space-y-4 pt-4">
                    {node.errorMessage && (
                      <div className="rounded-md bg-destructive/10 border border-destructive/50 p-3">
                        <div className="text-sm font-medium text-destructive mb-1">Error</div>
                        <pre className="text-xs text-destructive whitespace-pre-wrap">
                          {node.errorMessage}
                        </pre>
                      </div>
                    )}

                    <div className="flex items-center gap-6 text-xs">
                      <div className="flex items-center gap-2">
                        <Clock className="h-3 w-3 text-muted-foreground" />
                        <span className="text-muted-foreground">Started:</span>
                        <span className="font-mono">{new Date(node.startedAt).toLocaleString()}</span>
                      </div>
                      {node.completedAt && (
                        <div className="flex items-center gap-2">
                          <CheckCircle2 className="h-3 w-3 text-green-600" />
                          <span className="text-muted-foreground">Completed:</span>
                          <span className="font-mono">{new Date(node.completedAt).toLocaleString()}</span>
                          {node.durationMs !== undefined && (
                            <span className="text-muted-foreground">({formatDuration(node.durationMs)})</span>
                          )}
                        </div>
                      )}
                    </div>

                    <div className="grid gap-4 lg:grid-cols-2">
                      <div>
                        <div className="text-sm font-medium mb-2">Input</div>
                        {node.input ? (
                          <JsonViewer data={node.input} className="max-h-48" collapsed={1} />
                        ) : (
                          <div className="text-xs text-muted-foreground bg-muted p-3 rounded-md">No input data</div>
                        )}
                      </div>
                      <div>
                        <div className="text-sm font-medium mb-2">Output</div>
                        {node.output ? (
                          <JsonViewer data={node.output} className="max-h-48" collapsed={1} />
                        ) : (
                          <div className="text-xs text-muted-foreground bg-muted p-3 rounded-md">No output data</div>
                        )}
                      </div>
                    </div>

                    {node.fullResponse && (
                      <div>
                        <div className="text-sm font-medium mb-2">Full Response</div>
                        <pre className="text-xs bg-muted p-3 rounded-md overflow-x-auto max-h-48 whitespace-pre-wrap">
                          {node.fullResponse}
                        </pre>
                      </div>
                    )}
                  </AccordionContent>
                </AccordionItem>
              ))}
            </Accordion>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
