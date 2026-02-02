import { useState, useCallback, useRef } from 'react'
import { useAuthStore } from '@/store/auth'
import type {
  ContentPart,
  ConversationMessage,
  ConversationStreamEvent,
  ResponseStartEvent,
  PartDeltaEvent,
  TokenUsageEvent,
  ResponseErrorEvent,
  ResponseEndEvent,
} from '@/lib/api'

interface UseConversationStreamOptions {
  onResponseStart?: (messageId: string) => void
  onPartDelta?: (content: string, partIndex: number) => void
  onTokenUsage?: (inputTokens: number, outputTokens: number, totalTokens: number) => void
  onResponseEnd?: (message: ConversationMessage) => void
  onError?: (error: string) => void
}

interface StreamingMessage {
  id: string
  role: 'Assistant'
  content: ContentPart[]
  isStreaming: boolean
  inputTokens?: number
  outputTokens?: number
  totalTokens?: number
}

async function fetchWithTokenRefresh(
  url: string,
  options: RequestInit,
  retryOnUnauthorized = true
): Promise<Response> {
  const state = useAuthStore.getState()
  const { shouldRefreshToken, refreshTokens, logout } = state

  // Proactively refresh token if it's about to expire
  if (shouldRefreshToken() && retryOnUnauthorized) {
    const refreshed = await refreshTokens()
    if (!refreshed) {
      logout()
      window.location.href = '/login'
      throw new Error('Session expired')
    }
  }

  // Get potentially updated token after refresh
  const currentToken = useAuthStore.getState().accessToken

  const response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${currentToken}`,
    },
  })

  if (response.status === 401 && retryOnUnauthorized) {
    // Try to refresh the token
    const refreshed = await refreshTokens()

    if (refreshed) {
      // Retry the request with the new token (don't retry again on 401)
      return fetchWithTokenRefresh(url, options, false)
    }

    // Refresh failed - logout and redirect
    logout()
    window.location.href = '/login'
    throw new Error('Session expired')
  }

  return response
}

export function useConversationStream(options: UseConversationStreamOptions = {}) {
  const [streamingMessage, setStreamingMessage] = useState<StreamingMessage | null>(null)
  const [isStreaming, setIsStreaming] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const abortControllerRef = useRef<AbortController | null>(null)

  const sendMessageWithStream = useCallback(async (
    conversationId: string,
    content: ContentPart[]
  ): Promise<ConversationMessage | null> => {
    // Cancel any existing stream
    if (abortControllerRef.current) {
      abortControllerRef.current.abort()
    }

    setError(null)
    setIsStreaming(true)
    setStreamingMessage(null)

    // Create new abort controller for this stream
    const abortController = new AbortController()
    abortControllerRef.current = abortController

    let finalMessage: ConversationMessage | null = null

    try {
      const response = await fetchWithTokenRefresh(
        `/api/v1/conversations/${conversationId}/messages`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Accept': 'text/event-stream'
          },
          body: JSON.stringify({ content }),
          signal: abortController.signal
        }
      )

      if (!response.ok) {
        const errorBody = await response.text()
        throw new Error(`HTTP error! status: ${response.status}, body: ${errorBody}`)
      }

      // Check if we got a streaming response or just JSON
      const contentType = response.headers.get('content-type')
      if (contentType?.includes('application/json')) {
        // Non-streaming response (fallback)
        const message = await response.json() as ConversationMessage
        finalMessage = message
        setIsStreaming(false)
        return message
      }

      if (!response.body) {
        throw new Error('No response body')
      }

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''
      const contentParts: ContentPart[] = []

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n\n')
        buffer = lines.pop() || ''

        for (const line of lines) {
          if (!line.trim()) continue

          // Parse SSE format: "event: EventName\ndata: {...}"
          const dataMatch = line.match(/^data:\s*(.+)$/m)

          if (dataMatch) {
            try {
              const event = JSON.parse(dataMatch[1]) as ConversationStreamEvent

              switch (event.type) {
                case 'response_start': {
                  const startEvent = event as ResponseStartEvent
                  options.onResponseStart?.(startEvent.messageId)

                  // Initialize streaming message
                  setStreamingMessage({
                    id: startEvent.messageId,
                    role: 'Assistant',
                    content: [],
                    isStreaming: true,
                  })
                  break
                }

                case 'part_start': {
                  // A new content part is starting
                  contentParts.push({ type: 'text', text: '' })
                  break
                }

                case 'part_delta': {
                  const deltaEvent = event as PartDeltaEvent
                  const partIndex = deltaEvent.partIndex
                  options.onPartDelta?.(deltaEvent.content, partIndex)

                  // Update the content part
                  if (contentParts[partIndex]) {
                    const part = contentParts[partIndex]
                    if (part.type === 'text') {
                      part.text += deltaEvent.content
                    }
                  }

                  // Update streaming message
                  setStreamingMessage(prev => {
                    if (!prev) return null
                    return {
                      ...prev,
                      content: [...contentParts],
                    }
                  })
                  break
                }

                case 'part_end': {
                  // Content part finished
                  break
                }

                case 'token_usage': {
                  const usageEvent = event as TokenUsageEvent
                  options.onTokenUsage?.(
                    usageEvent.inputTokens,
                    usageEvent.outputTokens,
                    usageEvent.totalTokens
                  )

                  setStreamingMessage(prev => {
                    if (!prev) return null
                    return {
                      ...prev,
                      inputTokens: usageEvent.inputTokens,
                      outputTokens: usageEvent.outputTokens,
                      totalTokens: usageEvent.totalTokens,
                    }
                  })
                  break
                }

                case 'response_error': {
                  const errorEvent = event as ResponseErrorEvent
                  setError(errorEvent.error)
                  options.onError?.(errorEvent.error)
                  setIsStreaming(false)
                  return null
                }

                case 'response_end': {
                  const endEvent = event as ResponseEndEvent
                  finalMessage = endEvent.message
                  options.onResponseEnd?.(endEvent.message)
                  setStreamingMessage(null)
                  setIsStreaming(false)
                  break
                }
              }
            } catch (e) {
              console.error('Failed to parse conversation stream event:', e)
            }
          }
        }
      }
    } catch (err: unknown) {
      if (err instanceof Error && err.name === 'AbortError') {
        // Stream was intentionally cancelled
        return null
      }
      const errorMsg = err instanceof Error ? err.message : 'Stream failed'
      setError(errorMsg)
      options.onError?.(errorMsg)
    } finally {
      setIsStreaming(false)
    }

    return finalMessage
  }, [options])

  const stopStream = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort()
      abortControllerRef.current = null
    }
    setIsStreaming(false)
    setStreamingMessage(null)
  }, [])

  return {
    streamingMessage,
    isStreaming,
    error,
    sendMessageWithStream,
    stopStream
  }
}
