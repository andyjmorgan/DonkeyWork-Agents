import { useState, useRef, useEffect, useCallback } from 'react'
import { Send, Loader2, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { parseMarkdown } from '@/lib/markdown'
import { conversations, type ContentPart, type ConversationMessage } from '@/lib/api'
import { useConversationStream } from '@/hooks/useConversationStream'

// Internal message type for UI display
interface DisplayMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  timestamp: Date
  isStreaming?: boolean
}

interface ChatInterfaceProps {
  orchestrationId: string
  conversationId?: string
  onConversationCreated?: (conversationId: string) => void
}

// Helper to extract text from content parts
function extractTextFromContent(content: ContentPart[]): string {
  return content
    .filter((part): part is { type: 'text'; text: string } => part.type === 'text')
    .map(part => part.text)
    .join('')
}

// Helper to convert API message to display message
function apiMessageToDisplay(msg: ConversationMessage): DisplayMessage {
  return {
    id: msg.id,
    role: msg.role === 'User' ? 'user' : 'assistant',
    content: extractTextFromContent(msg.content),
    timestamp: new Date(msg.createdAt),
  }
}

export function ChatInterface({
  orchestrationId,
  conversationId: initialConversationId,
  onConversationCreated
}: ChatInterfaceProps) {
  const [messages, setMessages] = useState<DisplayMessage[]>([])
  const [inputValue, setInputValue] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [conversationId, setConversationId] = useState<string | null>(initialConversationId || null)
  const [error, setError] = useState<string | null>(null)
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  // Use the conversation stream hook
  const {
    streamingMessage,
    isStreaming,
    error: streamError,
    sendMessageWithStream,
  } = useConversationStream({
    onError: (err) => setError(err),
  })

  // Load existing conversation if we have an ID
  useEffect(() => {
    if (initialConversationId) {
      setConversationId(initialConversationId)
      loadConversation(initialConversationId)
    }
  }, [initialConversationId])

  const loadConversation = async (id: string) => {
    try {
      const conversation = await conversations.get(id)
      const displayMessages = conversation.messages.map(apiMessageToDisplay)
      setMessages(displayMessages)
    } catch (err) {
      console.error('Failed to load conversation:', err)
      setError('Failed to load conversation history')
    }
  }

  // Scroll to bottom when messages change or streaming updates
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, streamingMessage])

  // Auto-resize textarea
  const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setInputValue(e.target.value)
    // Reset height to auto to get the correct scrollHeight
    e.target.style.height = 'auto'
    // Set height to scrollHeight, but cap at 200px
    e.target.style.height = `${Math.min(e.target.scrollHeight, 200)}px`
  }

  const handleSend = useCallback(async () => {
    if (!inputValue.trim() || isLoading || isStreaming) return

    const messageText = inputValue.trim()
    setInputValue('')
    setError(null)

    // Reset textarea height
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto'
    }

    // Add user message to display immediately
    const userDisplayMessage: DisplayMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: messageText,
      timestamp: new Date(),
    }
    setMessages(prev => [...prev, userDisplayMessage])

    setIsLoading(true)

    try {
      let activeConversationId = conversationId

      // Create conversation if this is the first message
      if (!activeConversationId) {
        const newConversation = await conversations.create({
          orchestrationId,
          title: messageText.substring(0, 50) + (messageText.length > 50 ? '...' : ''),
        })
        activeConversationId = newConversation.id
        setConversationId(activeConversationId)
        onConversationCreated?.(activeConversationId)
      }

      // Create content parts for the message
      const content: ContentPart[] = [{ type: 'text', text: messageText }]

      // Send message with streaming
      const assistantMessage = await sendMessageWithStream(activeConversationId, content)

      // Add the final assistant message to display
      if (assistantMessage) {
        const assistantDisplayMessage: DisplayMessage = {
          id: assistantMessage.id,
          role: 'assistant',
          content: extractTextFromContent(assistantMessage.content),
          timestamp: new Date(assistantMessage.createdAt),
        }
        setMessages(prev => [...prev, assistantDisplayMessage])
      }
    } catch (err) {
      console.error('Failed to send message:', err)
      setError(err instanceof Error ? err.message : 'Failed to send message')
    } finally {
      setIsLoading(false)
    }
  }, [inputValue, isLoading, isStreaming, conversationId, orchestrationId, sendMessageWithStream, onConversationCreated])

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    // Send on Enter without Shift
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  }

  // Get streaming content for display
  const streamingContent = streamingMessage?.content
    .filter((part): part is { type: 'text'; text: string } => part.type === 'text')
    .map(part => part.text)
    .join('') || ''

  const displayError = error || streamError

  return (
    <div className="flex h-full flex-col">
      {/* Error banner */}
      {displayError && (
        <div className="border-b border-destructive/20 bg-destructive/10 px-4 py-2">
          <div className="flex items-center gap-2 text-sm text-destructive">
            <AlertCircle className="h-4 w-4" />
            <span>{displayError}</span>
          </div>
        </div>
      )}

      {/* Messages area */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.length === 0 && !isStreaming && (
          <div className="flex h-full items-center justify-center">
            <div className="text-center text-muted-foreground">
              <p>Send a message to start the conversation</p>
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
                <p className="mt-1 text-xs text-user-chat-foreground/70">
                  {formatTime(message.timestamp)}
                </p>
              </div>
            ) : (
              <div className="max-w-[90%]">
                <div
                  className="prose prose-sm dark:prose-invert max-w-none text-foreground/80"
                  dangerouslySetInnerHTML={{
                    __html: parseMarkdown(message.content),
                  }}
                />
                <p className="mt-1 text-xs text-muted-foreground">
                  {formatTime(message.timestamp)}
                </p>
              </div>
            )}
          </div>
        ))}

        {/* Streaming message */}
        {isStreaming && streamingMessage && (
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

        {/* Loading indicator (when not streaming yet) */}
        {isLoading && !isStreaming && (
          <div className="flex justify-start">
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span>Thinking...</span>
            </div>
          </div>
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
            disabled={isLoading || isStreaming}
          />
          <Button
            onClick={handleSend}
            disabled={!inputValue.trim() || isLoading || isStreaming}
            size="icon"
            className="h-[44px] w-[44px] shrink-0"
          >
            {isLoading || isStreaming ? (
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
