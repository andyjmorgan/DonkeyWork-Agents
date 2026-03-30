import { useEffect, useState, useLayoutEffect, useRef } from 'react'
import { AlertCircle, Info, AlertTriangle, Bug } from 'lucide-react'
import { Badge } from '@donkeywork/ui'
import { executions, type ExecutionLog } from '@donkeywork/api-client'

interface ExecutionLogsProps {
  executionId: string | null
}

export function ExecutionLogs({ executionId }: ExecutionLogsProps) {
  const [logs, setLogs] = useState<ExecutionLog[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const prevExecutionIdRef = useRef(executionId)

  useLayoutEffect(() => {
    if (prevExecutionIdRef.current !== executionId && !executionId) {
      setLogs([])
    }
    prevExecutionIdRef.current = executionId
  }, [executionId])

  useEffect(() => {
    if (!executionId) {
      return
    }

    let isMounted = true
    setLoading(true)
    setError(null)

    executions.getLogs(executionId)
      .then((response) => {
        if (isMounted) {
          setLogs(response.logs)
          setLoading(false)
        }
      })
      .catch((err) => {
        if (isMounted) {
          setError(err.message || 'Failed to load logs')
          setLoading(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [executionId])

  if (!executionId) {
    return (
      <div className="flex h-full items-center justify-center text-muted-foreground">
        Run an execution to view logs
      </div>
    )
  }

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center text-muted-foreground">
        Loading logs...
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex h-full items-center justify-center text-destructive">
        {error}
      </div>
    )
  }

  if (logs.length === 0) {
    return (
      <div className="flex h-full items-center justify-center text-muted-foreground">
        No logs available
      </div>
    )
  }

  const getLogLevelIcon = (level: string) => {
    switch (level.toLowerCase()) {
      case 'error':
      case 'critical':
        return <AlertCircle className="h-4 w-4 text-destructive" />
      case 'warning':
        return <AlertTriangle className="h-4 w-4 text-yellow-500" />
      case 'debug':
        return <Bug className="h-4 w-4 text-blue-500" />
      default:
        return <Info className="h-4 w-4 text-muted-foreground" />
    }
  }

  const getLogLevelColor = (level: string) => {
    switch (level.toLowerCase()) {
      case 'error':
      case 'critical':
        return 'destructive'
      case 'warning':
        return 'outline'
      case 'debug':
        return 'secondary'
      default:
        return 'default'
    }
  }

  return (
    <div className="flex h-full flex-col gap-2 overflow-y-auto rounded-lg border border-border bg-muted/20 p-4">
      {logs.map((log) => (
        <div key={log.id} className="space-y-1 rounded-md border border-border bg-card p-3">
          <div className="flex items-center gap-2">
            {getLogLevelIcon(log.logLevel)}
            <Badge variant={getLogLevelColor(log.logLevel) as 'default' | 'secondary' | 'destructive' | 'outline'}>{log.logLevel}</Badge>
            {log.nodeId && (
              <Badge variant="outline" className="text-xs">{log.nodeId}</Badge>
            )}
            <span className="text-xs text-muted-foreground">
              {new Date(log.createdAt).toLocaleTimeString()}
            </span>
            <span className="text-xs text-muted-foreground">
              {log.source}
            </span>
          </div>
          <p className="text-sm">{log.message}</p>
          {log.details && (
            <pre className="mt-2 rounded-md bg-muted p-2 text-xs overflow-x-auto">
              {JSON.stringify(JSON.parse(log.details), null, 2)}
            </pre>
          )}
        </div>
      ))}
    </div>
  )
}
