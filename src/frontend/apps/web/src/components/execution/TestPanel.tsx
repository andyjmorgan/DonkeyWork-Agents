import { useState, useMemo, useLayoutEffect, useRef } from 'react'
import { Play, Loader2, RefreshCw, CheckCircle2, XCircle } from 'lucide-react'
import { Button, Label, Textarea } from '@donkeywork/ui'
import { executions, type JSONSchema } from '@donkeywork/api-client'

interface TestPanelProps {
  orchestrationId: string
  inputSchema?: JSONSchema
}

// Generate example JSON from schema
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
  return <DirectTestPanel orchestrationId={orchestrationId} inputSchema={inputSchema} />
}

// Direct JSON test interface (non-streaming)
function DirectTestPanel({ orchestrationId, inputSchema }: { orchestrationId: string; inputSchema?: JSONSchema }) {
  // Initialize input from schema if available
  const initialInput = useMemo(() => {
    if (inputSchema) {
      return JSON.stringify(generateExampleFromSchema(inputSchema), null, 2)
    }
    return '{\n  \n}'
  }, [inputSchema])

  const [input, setInput] = useState(initialInput)
  const [output, setOutput] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isExecuting, setIsExecuting] = useState(false)
  const [executionStatus, setExecutionStatus] = useState<'Completed' | 'Failed' | null>(null)

  // Update input when schema changes (for subsequent schema loads)
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

  const handleTest = async () => {
    let parsedInput: unknown
    try {
      parsedInput = JSON.parse(input)
    } catch {
      setError('Invalid JSON input')
      return
    }

    setIsExecuting(true)
    setOutput(null)
    setError(null)
    setExecutionStatus(null)

    try {
      const result = await executions.test(orchestrationId, parsedInput)

      if (result.status === 'Completed') {
        setExecutionStatus('Completed')
        // Output is a JSON string from backend, format it nicely
        if (result.output) {
          try {
            const parsed = typeof result.output === 'string' ? JSON.parse(result.output) : result.output
            setOutput(JSON.stringify(parsed, null, 2))
          } catch {
            setOutput(String(result.output))
          }
        } else {
          setOutput('(no output)')
        }
      } else if (result.status === 'Failed') {
        setExecutionStatus('Failed')
        setError(result.error || 'Execution failed')
      } else {
        setExecutionStatus('Failed')
        setError(`Unexpected status: ${result.status}`)
      }
    } catch (err) {
      setExecutionStatus('Failed')
      setError(err instanceof Error ? err.message : 'Execution failed')
    } finally {
      setIsExecuting(false)
    }
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
              disabled={isExecuting}
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
          rows={12}
          placeholder='{\n  "input": "Hello"\n}'
        />
        <Button
          onClick={handleTest}
          disabled={isExecuting}
          className="w-full"
        >
          {isExecuting ? (
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
      </div>

      <div className="flex-1 overflow-hidden">
        <Label className="mb-2 block">Output</Label>
        <div className="h-full rounded-lg border border-border bg-muted/30 overflow-auto">
          {/* Loading state */}
          {isExecuting && (
            <div className="flex items-center justify-center h-full p-4">
              <div className="flex items-center gap-2 text-muted-foreground">
                <Loader2 className="h-5 w-5 animate-spin" />
                <span>Executing orchestration...</span>
              </div>
            </div>
          )}

          {/* Success output */}
          {!isExecuting && executionStatus === 'Completed' && output && (
            <div className="p-4">
              <div className="flex items-center gap-2 mb-3 text-sm text-emerald-600 dark:text-emerald-400">
                <CheckCircle2 className="h-4 w-4" />
                <span>Execution completed</span>
              </div>
              <pre className="text-sm font-mono whitespace-pre-wrap break-all">{output}</pre>
            </div>
          )}

          {/* Error output */}
          {!isExecuting && error && (
            <div className="p-4">
              <div className="flex items-center gap-2 mb-3 text-sm text-destructive">
                <XCircle className="h-4 w-4" />
                <span>Execution failed</span>
              </div>
              <div className="p-3 rounded-md bg-destructive/10 text-sm text-destructive">
                {error}
              </div>
            </div>
          )}

          {/* Empty state */}
          {!isExecuting && !output && !error && (
            <div className="flex items-center justify-center h-full p-4">
              <p className="text-sm text-muted-foreground">
                Click "Test Orchestration" to see results
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
