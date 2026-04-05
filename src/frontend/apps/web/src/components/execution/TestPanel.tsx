import { useState, useMemo, useLayoutEffect, useRef } from 'react'
import { Play, Loader2, RefreshCw, ScrollText, StopCircle } from 'lucide-react'
import { Button, Label, Textarea } from '@donkeywork/ui'
import type { JSONSchema } from '@donkeywork/api-client'
import { useExecutionStream } from '@/hooks/useExecutionStream'
import { StreamingOutput } from './StreamingOutput'
import { ExecutionDetailDialog } from './ExecutionDetailDialog'

interface TestPanelProps {
  orchestrationId: string
  inputSchema?: JSONSchema
}

function generateExampleFromSchema(schema: JSONSchema): Record<string, unknown> {
  if (!schema || schema.type !== 'object') {
    return {}
  }

  const example: Record<string, unknown> = {}
  const properties = schema.properties || {}

  for (const [key, prop] of Object.entries(properties)) {
    const propSchema = prop as Record<string, unknown>

    if (propSchema.type === 'string') {
      if (propSchema.default !== undefined) {
        example[key] = propSchema.default
      } else if (propSchema.format === 'email') {
        example[key] = 'user@example.com'
      } else if (Array.isArray(propSchema.enum) && propSchema.enum.length > 0) {
        example[key] = propSchema.enum[0]
      } else {
        example[key] = (propSchema.description as string) || 'Enter value here'
      }
    } else if (propSchema.type === 'number' || propSchema.type === 'integer') {
      const min = (propSchema.minimum as number) || 0
      const max = (propSchema.maximum as number) || 100
      example[key] = Math.floor((min + max) / 2)
    } else if (propSchema.type === 'boolean') {
      example[key] = (propSchema.default as boolean) ?? false
    } else if (propSchema.type === 'array') {
      example[key] = []
    } else if (propSchema.type === 'object') {
      example[key] = {}
    } else {
      example[key] = ''
    }
  }

  return example
}

export function TestPanel({ orchestrationId, inputSchema }: TestPanelProps) {
  const [showExecutionLog, setShowExecutionLog] = useState(false)
  const [completedOutput, setCompletedOutput] = useState<unknown>(undefined)

  const initialInput = useMemo(() => {
    if (inputSchema) {
      return JSON.stringify(generateExampleFromSchema(inputSchema), null, 2)
    }
    return '{\n  \n}'
  }, [inputSchema])

  const [input, setInput] = useState(initialInput)

  const { events, isStreaming, error, executionId, startStream, stopStream } = useExecutionStream({
    onComplete: (output) => setCompletedOutput(output),
    onError: () => {},
  })

  const prevInputSchemaRef = useRef(inputSchema)
  useLayoutEffect(() => {
    if (inputSchema && prevInputSchemaRef.current !== inputSchema) {
      const example = generateExampleFromSchema(inputSchema)
      setInput(JSON.stringify(example, null, 2))
    }
    prevInputSchemaRef.current = inputSchema
  }, [inputSchema])

  const handleResetTemplate = () => {
    if (inputSchema) {
      const example = generateExampleFromSchema(inputSchema)
      setInput(JSON.stringify(example, null, 2))
    }
  }

  const handleTest = () => {
    let parsedInput: unknown
    try {
      parsedInput = JSON.parse(input)
    } catch {
      return
    }

    setCompletedOutput(undefined)
    startStream(orchestrationId, parsedInput, true)
  }

  return (
    <div className="flex h-full flex-col gap-4">
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label>Input (JSON)</Label>
          {inputSchema && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleResetTemplate}
              disabled={isStreaming}
              className="h-7 text-xs"
            >
              <RefreshCw className="h-3 w-3 mr-1" />
              Reset Template
            </Button>
          )}
        </div>
        <Textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          className="font-mono text-sm"
          rows={8}
          placeholder='{\n  "input": "Hello"\n}'
        />
        <div className="flex gap-2">
          <Button
            onClick={handleTest}
            disabled={isStreaming}
            className="flex-1"
          >
            {isStreaming ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Running...
              </>
            ) : (
              <>
                <Play className="h-4 w-4" />
                Test Orchestration
              </>
            )}
          </Button>
          {isStreaming && (
            <Button variant="destructive" size="icon" onClick={stopStream}>
              <StopCircle className="h-4 w-4" />
            </Button>
          )}
        </div>
      </div>

      <div className="flex-1 overflow-hidden">
        <div className="flex items-center justify-between mb-2">
          <Label>Output</Label>
          {executionId && !isStreaming && (
            <Button
              variant="outline"
              size="sm"
              className="h-7 text-xs"
              onClick={() => setShowExecutionLog(true)}
            >
              <ScrollText className="h-3 w-3 mr-1" />
              Execution Log
            </Button>
          )}
        </div>
        <div className="h-[calc(100%-2rem)]">
          {error && !isStreaming && events.length === 0 ? (
            <div className="rounded-lg border border-destructive/20 bg-destructive/10 p-4 text-sm text-destructive">
              {error}
            </div>
          ) : (
            <StreamingOutput
              events={events}
              output={completedOutput}
              error={error}
              isStreaming={isStreaming}
            />
          )}
        </div>
      </div>

      {executionId && (
        <ExecutionDetailDialog
          executionId={executionId}
          open={showExecutionLog}
          onOpenChange={setShowExecutionLog}
        />
      )}
    </div>
  )
}
