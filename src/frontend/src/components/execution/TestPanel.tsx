import { useState, useEffect, useRef, useCallback } from 'react'
import { Play, Loader2, RefreshCw, Send, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { useExecutionStream } from '@/hooks/useExecutionStream'
import { StreamingOutput } from './StreamingOutput'
import { parseMarkdown } from '@/lib/markdown'
import type { JSONSchema, InterfaceConfig } from '@/lib/api'

interface TestPanelProps {
  orchestrationId: string
  inputSchema?: JSONSchema
  interfaceConfig?: InterfaceConfig
}

interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
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

export function TestPanel({ orchestrationId, inputSchema, interfaceConfig }: TestPanelProps) {
  const isChatMode = interfaceConfig?.type === 'ChatInterfaceConfig'

  if (isChatMode) {
    return <ChatTestPanel orchestrationId={orchestrationId} />
  }

  return <DirectTestPanel orchestrationId={orchestrationId} inputSchema={inputSchema} />
}

// Chat-style test interface
function ChatTestPanel({ orchestrationId }: { orchestrationId: string }) {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [inputValue, setInputValue] = useState('')
  const [streamingContent, setStreamingContent] = useState('')
  const streamingContentRef = useRef('')
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  const { isStreaming, error, startChatStream } = useExecutionStream({
    onToken: (token) => {
      streamingContentRef.current += token
      setStreamingContent(streamingContentRef.current)
    },
    onComplete: (result) => {
      // Add assistant message - prefer streamed content over final result
      const finalContent = streamingContentRef.current
      const fallbackContent = typeof result === 'string' ? result : JSON.stringify(result, null, 2)
      setMessages(prev => [...prev, {
        id: crypto.randomUUID(),
        role: 'assistant',
        content: finalContent || fallbackContent
      }])
      streamingContentRef.current = ''
      setStreamingContent('')
    },
    onError: (err) => {
      console.error('Execution error:', err)
      streamingContentRef.current = ''
      setStreamingContent('')
    }
  })

  // Scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, streamingContent])

  const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setInputValue(e.target.value)
    e.target.style.height = 'auto'
    e.target.style.height = `${Math.min(e.target.scrollHeight, 200)}px`
  }

  const handleSend = useCallback(() => {
    if (!inputValue.trim() || isStreaming) return

    const messageText = inputValue.trim()
    setInputValue('')
    streamingContentRef.current = ''
    setStreamingContent('')

    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto'
    }

    // Build messages array including history
    const newUserMessage = { role: 'user' as const, content: messageText }
    const allMessages = [...messages, newUserMessage]

    // Add user message to display
    setMessages(allMessages)

    // Execute with full conversation history via chat endpoint
    startChatStream(orchestrationId, allMessages, true)
  }, [inputValue, isStreaming, orchestrationId, startChatStream, messages])

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  const handleClear = () => {
    setMessages([])
    setStreamingContent('')
  }

  return (
    <div className="flex h-full flex-col">
      {/* Header with clear button */}
      <div className="flex items-center justify-between border-b border-border px-4 py-2">
        <span className="text-sm text-muted-foreground">Playground</span>
        <Button
          variant="ghost"
          size="sm"
          onClick={handleClear}
          disabled={isStreaming || messages.length === 0}
        >
          <Trash2 className="h-4 w-4" />
          <span className="ml-1">Clear</span>
        </Button>
      </div>

      {/* Messages area */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.length === 0 && !isStreaming && (
          <div className="flex h-full items-center justify-center">
            <div className="text-center text-muted-foreground">
              <p>Send a message to test the orchestration</p>
            </div>
          </div>
        )}

        {messages.map((message) => (
          <div
            key={message.id}
            className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}
          >
            {message.role === 'user' ? (
              <div className="max-w-[80%] rounded-2xl px-4 py-2 bg-user-chat text-user-chat-foreground">
                <p className="text-sm whitespace-pre-wrap">{message.content}</p>
              </div>
            ) : (
              <div className="max-w-[90%]">
                <div
                  className="prose prose-sm dark:prose-invert max-w-none text-foreground/80"
                  dangerouslySetInnerHTML={{
                    __html: parseMarkdown(message.content),
                  }}
                />
              </div>
            )}
          </div>
        ))}

        {/* Streaming response */}
        {isStreaming && (
          <div className="flex justify-start">
            <div className="max-w-[90%]">
              {streamingContent ? (
                <div
                  className="prose prose-sm dark:prose-invert max-w-none text-foreground/80"
                  dangerouslySetInnerHTML={{
                    __html: parseMarkdown(streamingContent),
                  }}
                />
              ) : (
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  <span>Thinking...</span>
                </div>
              )}
            </div>
          </div>
        )}

        {error && (
          <div className="text-sm text-destructive">{error}</div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Input area */}
      <div className="border-t border-border bg-background p-4">
        <div className="flex gap-2">
          <Textarea
            ref={textareaRef}
            value={inputValue}
            onChange={handleInputChange}
            onKeyDown={handleKeyDown}
            placeholder="Type a message..."
            className="min-h-[44px] max-h-[200px] resize-none"
            rows={1}
            disabled={isStreaming}
          />
          <Button
            onClick={handleSend}
            disabled={!inputValue.trim() || isStreaming}
            size="icon"
            className="h-[44px] w-[44px] shrink-0"
          >
            {isStreaming ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Send className="h-4 w-4" />
            )}
          </Button>
        </div>
        <p className="mt-2 text-xs text-muted-foreground">
          Press Enter to send, Shift+Enter for new line
        </p>
      </div>
    </div>
  )
}

// Direct JSON test interface
function DirectTestPanel({ orchestrationId, inputSchema }: { orchestrationId: string; inputSchema?: JSONSchema }) {
  const [input, setInput] = useState('{\n  \n}')
  const [hasLoadedSchema, setHasLoadedSchema] = useState(false)
  const [output, setOutput] = useState<unknown>(null)

  const { events, isStreaming, error, startStream } = useExecutionStream({
    onComplete: (result) => setOutput(result),
    onError: (err) => console.error('Execution error:', err)
  })

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
      startStream(orchestrationId, parsedInput, true)
    } catch {
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
