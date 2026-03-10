import { useState, useRef, useEffect } from 'react'
import { Play, Square, X, Loader2 } from 'lucide-react'
import { Button, Textarea } from '@donkeywork/ui'
import { BoxList } from '@donkeywork/chat'
import { useAgentTestStream } from '@/hooks/useAgentTestStream'
import { useAgentBuilderStore } from '@/store/agentBuilder'

interface AgentTestPanelProps {
  open: boolean
  onClose: () => void
}

export function AgentTestPanel({ open, onClose }: AgentTestPanelProps) {
  const [input, setInput] = useState('')
  const serializeToContract = useAgentBuilderStore((s) => s.serializeToContract)
  const { boxes, isStreaming, error, startTest, stopTest } = useAgentTestStream()
  const outputRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (outputRef.current) {
      outputRef.current.scrollTop = outputRef.current.scrollHeight
    }
  }, [boxes])

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
      <div ref={outputRef} className="flex-1 overflow-y-auto p-4">
        {boxes.length === 0 && !isStreaming && !error && (
          <p className="text-sm text-muted-foreground text-center py-8">
            Run a test to see streaming output here
          </p>
        )}

        <BoxList boxes={boxes} isStreaming={isStreaming} />

        {isStreaming && (
          <div className="flex items-center gap-2 text-xs text-muted-foreground mt-3">
            <Loader2 className="h-3 w-3 animate-spin" />
            Streaming...
          </div>
        )}

        {error && (
          <div className="rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive mt-3">
            {error}
          </div>
        )}
      </div>
    </div>
  )
}
