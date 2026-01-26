import { useEffect, useRef } from 'react'
import { CheckCircle2, XCircle, Loader2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import type { ExecutionEvent } from '@/lib/api'

interface StreamingOutputProps {
  events: ExecutionEvent[]
  output?: any
  error?: string | null
  isStreaming: boolean
}

export function StreamingOutput({ events, output, isStreaming }: StreamingOutputProps) {
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [events])

  if (events.length === 0 && !isStreaming) {
    return (
      <div className="flex h-full items-center justify-center text-muted-foreground">
        Click "Test Agent" to run an execution
      </div>
    )
  }

  return (
    <div className="flex h-full flex-col gap-2 overflow-y-auto rounded-lg border border-border bg-muted/20 p-4">
      {events.map((event, i) => (
        <div key={i} className="space-y-1">
          {event.type === 'execution_started' && (
            <div className="flex items-center gap-2 text-sm">
              <Badge variant="outline">Started</Badge>
              <span className="text-muted-foreground">
                {new Date(event.timestamp).toLocaleTimeString()}
              </span>
            </div>
          )}

          {event.type === 'node_started' && (
            <div className="flex items-center gap-2 text-sm">
              <Badge variant="secondary">{(event as any).nodeType || 'node'}</Badge>
              <span className="text-muted-foreground">{(event as any).nodeId}</span>
            </div>
          )}

          {event.type === 'token_delta' && (
            <span className="text-sm">{(event as any).delta}</span>
          )}

          {event.type === 'node_completed' && (
            <div className="flex items-center gap-2 text-sm text-green-600">
              <CheckCircle2 className="h-4 w-4" />
              <span>Node completed: {(event as any).nodeId}</span>
            </div>
          )}

          {event.type === 'execution_completed' && (
            <div className="rounded-md border border-green-600/20 bg-green-600/10 p-3">
              <div className="flex items-center gap-2 text-sm font-medium text-green-600">
                <CheckCircle2 className="h-4 w-4" />
                Execution completed
              </div>
              {output && (
                <pre className="mt-2 text-sm">{JSON.stringify(output, null, 2)}</pre>
              )}
            </div>
          )}

          {event.type === 'execution_failed' && (
            <div className="rounded-md border border-destructive/20 bg-destructive/10 p-3">
              <div className="flex items-center gap-2 text-sm font-medium text-destructive">
                <XCircle className="h-4 w-4" />
                Execution failed
              </div>
              <p className="mt-1 text-sm text-destructive">{(event as any).errorMessage}</p>
            </div>
          )}
        </div>
      ))}

      {isStreaming && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Streaming...
        </div>
      )}

      <div ref={bottomRef} />
    </div>
  )
}
