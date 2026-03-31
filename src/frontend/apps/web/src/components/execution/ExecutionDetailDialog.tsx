import { useState, useEffect } from 'react'
import {
  Clock,
  CheckCircle2,
  XCircle,
  Loader2,
  Zap,
} from 'lucide-react'
import {
  Badge,
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@donkeywork/ui'
import { JsonViewer } from '@/components/ui/json-viewer'
import { executions, type NodeExecution } from '@donkeywork/api-client'

interface ExecutionDetailDialogProps {
  executionId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

function formatDuration(ms: number): string {
  if (ms < 1000) return `${ms}ms`
  return `${(ms / 1000).toFixed(2)}s`
}

function getStatusVariant(status: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status) {
    case 'Completed': return 'default'
    case 'Failed': return 'destructive'
    case 'Running': return 'secondary'
    default: return 'outline'
  }
}

function getStatusIcon(status: string) {
  switch (status) {
    case 'Completed': return <CheckCircle2 className="h-4 w-4 text-green-600" />
    case 'Failed': return <XCircle className="h-4 w-4 text-destructive" />
    case 'Running': return <Loader2 className="h-4 w-4 animate-spin text-blue-500" />
    default: return <Clock className="h-4 w-4 text-muted-foreground" />
  }
}

export function ExecutionDetailDialog({ executionId, open, onOpenChange }: ExecutionDetailDialogProps) {
  const [nodeExecutions, setNodeExecutions] = useState<NodeExecution[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!open || !executionId) return

    setLoading(true)
    executions.getNodeExecutions(executionId)
      .then(response => setNodeExecutions(response.nodeExecutions))
      .catch(() => setNodeExecutions([]))
      .finally(() => setLoading(false))
  }, [executionId, open])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Execution Log</DialogTitle>
        </DialogHeader>

        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : nodeExecutions.length === 0 ? (
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
                    <Badge variant="outline" className="text-xs">
                      {node.nodeType}
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
      </DialogContent>
    </Dialog>
  )
}
