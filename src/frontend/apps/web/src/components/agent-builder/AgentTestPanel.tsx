import { useState, useRef, useEffect } from 'react'
import { Play, Square, X, ChevronDown, ChevronRight, Loader2 } from 'lucide-react'
import { Button, Textarea, Badge } from '@donkeywork/ui'
import { useAgentTestStream, type AgentTestEvent } from '@/hooks/useAgentTestStream'
import { useAgentBuilderStore } from '@/store/agentBuilder'

interface AgentTestPanelProps {
  open: boolean
  onClose: () => void
}

export function AgentTestPanel({ open, onClose }: AgentTestPanelProps) {
  const [input, setInput] = useState('')
  const serializeToContract = useAgentBuilderStore((s) => s.serializeToContract)
  const { events, isStreaming, error, streamedText, startTest, stopTest } = useAgentTestStream()
  const outputRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (outputRef.current) {
      outputRef.current.scrollTop = outputRef.current.scrollHeight
    }
  }, [events, streamedText])

  const handleRun = () => {
    if (!input.trim()) return
    const contract = serializeToContract()
    startTest(contract, input.trim())
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
      e.preventDefault()
      handleRun()
    }
  }

  if (!open) return null

  return (
    <div className="fixed inset-y-0 right-0 z-50 flex w-full max-w-md flex-col border-l border-border bg-card shadow-xl">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-border px-4 py-3">
        <h2 className="text-sm font-semibold">Test Agent</h2>
        <Button variant="ghost" size="icon" className="h-7 w-7" onClick={onClose}>
          <X className="h-4 w-4" />
        </Button>
      </div>

      {/* Input */}
      <div className="border-b border-border p-4 space-y-3">
        <Textarea
          placeholder="Enter a test message..."
          rows={3}
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          className="text-sm"
        />
        <div className="flex items-center gap-2">
          {isStreaming ? (
            <Button size="sm" variant="destructive" onClick={stopTest}>
              <Square className="h-3 w-3" />
              Stop
            </Button>
          ) : (
            <Button size="sm" onClick={handleRun} disabled={!input.trim()}>
              <Play className="h-3 w-3" />
              Run
            </Button>
          )}
          <span className="text-xs text-muted-foreground">
            {isStreaming ? 'Streaming...' : 'Cmd+Enter to run'}
          </span>
        </div>
      </div>

      {/* Output */}
      <div ref={outputRef} className="flex-1 overflow-y-auto p-4 space-y-3">
        {events.length === 0 && !error && (
          <p className="text-sm text-muted-foreground text-center py-8">
            Run a test to see streaming output here
          </p>
        )}

        {events.map((evt, i) => (
          <EventBlock key={i} event={evt} />
        ))}

        {isStreaming && (
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <Loader2 className="h-3 w-3 animate-spin" />
            Streaming...
          </div>
        )}

        {error && (
          <div className="rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
            {error}
          </div>
        )}
      </div>
    </div>
  )
}

function EventBlock({ event }: { event: AgentTestEvent }) {
  const [expanded, setExpanded] = useState(false)

  switch (event.eventType) {
    case 'message':
      return (
        <div className="text-sm whitespace-pre-wrap">
          {event.text as string}
        </div>
      )

    case 'thinking':
      return (
        <CollapsibleBlock
          label="Thinking"
          variant="thinking"
          expanded={expanded}
          onToggle={() => setExpanded(!expanded)}
        >
          <pre className="text-xs whitespace-pre-wrap text-muted-foreground">
            {event.text as string}
          </pre>
        </CollapsibleBlock>
      )

    case 'tool_use':
      return (
        <CollapsibleBlock
          label={`Tool: ${event.name as string}`}
          variant="tool"
          expanded={expanded}
          onToggle={() => setExpanded(!expanded)}
        >
          <pre className="text-xs whitespace-pre-wrap overflow-x-auto">
            {typeof event.input === 'string' ? event.input : JSON.stringify(event.input, null, 2)}
          </pre>
        </CollapsibleBlock>
      )

    case 'tool_result':
      return (
        <CollapsibleBlock
          label="Tool Result"
          variant="tool"
          expanded={expanded}
          onToggle={() => setExpanded(!expanded)}
        >
          <pre className="text-xs whitespace-pre-wrap overflow-x-auto">
            {typeof event.result === 'string' ? event.result : JSON.stringify(event.result, null, 2)}
          </pre>
        </CollapsibleBlock>
      )

    case 'complete':
      return (
        <div className="flex items-center gap-2 pt-2">
          <Badge variant="secondary" className="text-xs">Complete</Badge>
        </div>
      )

    case 'error':
      return null // Handled by parent error display

    case 'turn_start':
    case 'turn_end':
    case 'queue_status':
      return null // Don't render control events

    default:
      return (
        <CollapsibleBlock
          label={event.eventType}
          variant="default"
          expanded={expanded}
          onToggle={() => setExpanded(!expanded)}
        >
          <pre className="text-xs whitespace-pre-wrap overflow-x-auto">
            {JSON.stringify(event, null, 2)}
          </pre>
        </CollapsibleBlock>
      )
  }
}

function CollapsibleBlock({
  label,
  variant,
  expanded,
  onToggle,
  children,
}: {
  label: string
  variant: 'thinking' | 'tool' | 'default'
  expanded: boolean
  onToggle: () => void
  children: React.ReactNode
}) {
  const borderColor = {
    thinking: 'border-amber-500/30',
    tool: 'border-blue-500/30',
    default: 'border-border',
  }[variant]

  return (
    <div className={`rounded-md border ${borderColor} text-sm`}>
      <button
        className="flex w-full items-center gap-2 px-3 py-2 text-xs text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
        onClick={onToggle}
      >
        {expanded ? <ChevronDown className="h-3 w-3" /> : <ChevronRight className="h-3 w-3" />}
        {label}
      </button>
      {expanded && <div className="px-3 pb-3">{children}</div>}
    </div>
  )
}
