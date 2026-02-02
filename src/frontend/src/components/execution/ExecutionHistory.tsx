import { useState, useEffect } from 'react'
import { Clock, CheckCircle2, XCircle, Loader2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { executions, type OrchestrationExecution } from '@/lib/api'

interface ExecutionHistoryProps {
  orchestrationId: string
}

export function ExecutionHistory({ orchestrationId }: ExecutionHistoryProps) {
  const [items, setItems] = useState<OrchestrationExecution[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadExecutions()
  }, [orchestrationId])

  const loadExecutions = async () => {
    try {
      setLoading(true)
      const response = await executions.list(orchestrationId)
      setItems(response.executions)
    } catch (error) {
      console.error('Failed to load executions:', error)
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="flex h-32 items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (items.length === 0) {
    return (
      <div className="flex h-32 items-center justify-center text-sm text-muted-foreground">
        No executions yet
      </div>
    )
  }

  return (
    <div className="space-y-2">
      {items.map((execution) => (
        <div
          key={execution.id}
          className="rounded-lg border border-border bg-card p-3 space-y-2"
        >
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              {execution.status === 'Completed' && (
                <CheckCircle2 className="h-4 w-4 text-green-600" />
              )}
              {execution.status === 'Failed' && (
                <XCircle className="h-4 w-4 text-destructive" />
              )}
              {execution.status === 'Running' && (
                <Loader2 className="h-4 w-4 animate-spin text-blue-600" />
              )}
              <Badge variant={
                execution.status === 'Completed' ? 'outline' :
                execution.status === 'Failed' ? 'destructive' :
                'secondary'
              }>
                {execution.status}
              </Badge>
            </div>
            <div className="flex items-center gap-1 text-xs text-muted-foreground">
              <Clock className="h-3 w-3" />
              {new Date(execution.startedAt).toLocaleString()}
            </div>
          </div>

          {execution.totalTokensUsed && (
            <div className="text-xs text-muted-foreground">
              Tokens: {execution.totalTokensUsed.toLocaleString()}
            </div>
          )}

          {execution.errorMessage && (
            <div className="text-xs text-destructive">
              {execution.errorMessage}
            </div>
          )}
        </div>
      ))}
    </div>
  )
}
