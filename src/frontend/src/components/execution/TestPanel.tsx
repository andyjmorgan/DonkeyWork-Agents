import { useState, useEffect } from 'react'
import { Play, Loader2, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useExecutionStream } from '@/hooks/useExecutionStream'
import { StreamingOutput } from './StreamingOutput'
import { ExecutionLogs } from './ExecutionLogs'
import type { JSONSchema } from '@/lib/api'

interface TestPanelProps {
  agentId: string
  inputSchema?: JSONSchema
}

// Generate example JSON from schema
function generateExampleFromSchema(schema: JSONSchema): any {
  if (!schema || schema.type !== 'object') {
    return {}
  }

  const example: any = {}
  const properties = schema.properties || {}

  for (const [key, prop] of Object.entries(properties)) {
    const propSchema = prop as any

    // Generate example values based on type
    if (propSchema.type === 'string') {
      // Check for default value first
      if (propSchema.default !== undefined) {
        example[key] = propSchema.default
      } else if (propSchema.format === 'email') {
        example[key] = 'user@example.com'
      } else if (propSchema.enum && propSchema.enum.length > 0) {
        example[key] = propSchema.enum[0]
      } else if (propSchema.pattern) {
        // Try to generate a matching example
        if (propSchema.pattern.includes('ORD-')) {
          example[key] = 'ORD-123456'
        } else if (propSchema.pattern.includes('+')) {
          example[key] = '+1234567890'
        } else {
          example[key] = propSchema.description || 'example'
        }
      } else {
        example[key] = propSchema.description || 'Enter value here'
      }
    } else if (propSchema.type === 'number' || propSchema.type === 'integer') {
      const min = propSchema.minimum || propSchema.minLength || 0
      const max = propSchema.maximum || propSchema.maxLength || 100
      example[key] = Math.floor((min + max) / 2)
    } else if (propSchema.type === 'boolean') {
      example[key] = propSchema.default ?? false
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

export function TestPanel({ agentId, inputSchema }: TestPanelProps) {
  const [input, setInput] = useState('{\n  \n}')
  const [hasLoadedSchema, setHasLoadedSchema] = useState(false)
  const [output, setOutput] = useState<any>(null)
  const [activeTab, setActiveTab] = useState('stream')

  const { events, isStreaming, error, executionId, startStream } = useExecutionStream({
    onComplete: (result) => setOutput(result),
    onError: (err) => console.error('Execution error:', err)
  })

  // Generate template from schema when available
  useEffect(() => {
    if (inputSchema && !hasLoadedSchema) {
      const example = generateExampleFromSchema(inputSchema)
      setInput(JSON.stringify(example, null, 2))
      setHasLoadedSchema(true)
    }
  }, [inputSchema, hasLoadedSchema])

  const handleResetTemplate = () => {
    if (inputSchema) {
      const example = generateExampleFromSchema(inputSchema)
      setInput(JSON.stringify(example, null, 2))
    }
  }

  const handleTest = () => {
    try {
      const parsedInput = JSON.parse(input)
      setOutput(null)
      startStream(agentId, parsedInput, true)
    } catch (e) {
      alert('Invalid JSON input')
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
          rows={12}
          placeholder='{\n  "input": "Hello"\n}'
        />
        <Button
          onClick={handleTest}
          disabled={isStreaming}
          className="w-full"
        >
          {isStreaming ? (
            <>
              <Loader2 className="h-4 w-4 animate-spin" />
              Running...
            </>
          ) : (
            <>
              <Play className="h-4 w-4" />
              Test Agent
            </>
          )}
        </Button>
      </div>

      <div className="flex-1 overflow-hidden">
        <Tabs value={activeTab} onValueChange={setActiveTab} className="h-full flex flex-col">
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="stream">Stream</TabsTrigger>
            <TabsTrigger value="logs">Logs</TabsTrigger>
          </TabsList>
          <TabsContent value="stream" className="flex-1 overflow-hidden mt-2">
            <StreamingOutput
              events={events}
              output={output}
              error={error}
              isStreaming={isStreaming}
            />
          </TabsContent>
          <TabsContent value="logs" className="flex-1 overflow-hidden mt-2">
            <ExecutionLogs executionId={executionId} />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
