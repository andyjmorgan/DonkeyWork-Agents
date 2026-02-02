import { useState, useEffect } from 'react'
import { Play, Loader2, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { useExecutionStream } from '@/hooks/useExecutionStream'
import { StreamingOutput } from './StreamingOutput'
import { ChatInterface } from './ChatInterface'
import type { JSONSchema, OrchestrationInterfaces, ChatInterfaceConfig } from '@/lib/api'

interface TestPanelProps {
  orchestrationId: string
  inputSchema?: JSONSchema
  interfaces?: OrchestrationInterfaces
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

export function TestPanel({ orchestrationId, inputSchema, interfaces }: TestPanelProps) {
  const [input, setInput] = useState('{\n  \n}')
  const [hasLoadedSchema, setHasLoadedSchema] = useState(false)
  const [output, setOutput] = useState<any>(null)

  const { events, isStreaming, error, startStream } = useExecutionStream({
    onComplete: (result) => setOutput(result),
    onError: (err) => console.error('Execution error:', err)
  })

  // Check if chat interface is enabled
  const isChatEnabled = interfaces?.chat?.enabled === true
  const chatConfig: ChatInterfaceConfig | undefined = interfaces?.chat

  // Generate template from schema when available (only for non-chat mode)
  useEffect(() => {
    if (!isChatEnabled && inputSchema && !hasLoadedSchema) {
      const example = generateExampleFromSchema(inputSchema)
      setInput(JSON.stringify(example, null, 2))
      setHasLoadedSchema(true)
    }
  }, [inputSchema, hasLoadedSchema, isChatEnabled])

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
      startStream(orchestrationId, parsedInput, true)
    } catch (e) {
      alert('Invalid JSON input')
    }
  }

  // Render chat interface when chat is enabled
  if (isChatEnabled) {
    return (
      <div className="flex h-full flex-col">
        <ChatInterface
          orchestrationId={orchestrationId}
          chatConfig={chatConfig}
        />
      </div>
    )
  }

  // Render regular test interface when chat is not enabled
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
              Test Orchestration
            </>
          )}
        </Button>
      </div>

      <div className="flex-1 overflow-hidden">
        <Label className="mb-2 block">Output</Label>
        <StreamingOutput
          events={events}
          output={output}
          error={error}
          isStreaming={isStreaming}
        />
      </div>
    </div>
  )
}
