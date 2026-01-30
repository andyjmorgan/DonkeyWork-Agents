import { useState, useCallback, useRef } from 'react'
import { useAuthStore } from '@/store/auth'
import type { ExecutionEvent } from '@/lib/api'

interface UseExecutionStreamOptions {
  onEvent?: (event: ExecutionEvent) => void
  onComplete?: (output: any) => void
  onError?: (error: string) => void
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

export function useExecutionStream(options: UseExecutionStreamOptions = {}) {
  const [events, setEvents] = useState<ExecutionEvent[]>([])
  const [isStreaming, setIsStreaming] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [executionId, setExecutionId] = useState<string | null>(null)
  const abortControllerRef = useRef<AbortController | null>(null)
  const executionIdRef = useRef<string | null>(null)

  const startStream = useCallback(async (agentId: string, input: any, isTest = false) => {
    // Cancel any existing stream
    if (abortControllerRef.current) {
      abortControllerRef.current.abort()
    }

    setEvents([])
    setError(null)
    setExecutionId(null)
    executionIdRef.current = null
    setIsStreaming(true)

    // Create new abort controller for this stream
    const abortController = new AbortController()
    abortControllerRef.current = abortController

    try {
      const endpoint = isTest ? 'test' : 'execute'

      const response = await fetchWithTokenRefresh(
        `/api/v1/agents/${agentId}/${endpoint}`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Accept': 'text/event-stream'
          },
          body: JSON.stringify({ input }),
          signal: abortController.signal
        }
      )

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      if (!response.body) {
        throw new Error('No response body')
      }

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''

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
              const event = JSON.parse(dataMatch[1]) as ExecutionEvent
              setEvents(prev => [...prev, event])
              options.onEvent?.(event)

              // Track execution ID from first event
              if (event.executionId && !executionIdRef.current) {
                executionIdRef.current = event.executionId
                setExecutionId(event.executionId)
              }

              if (event.type === 'execution_completed') {
                options.onComplete?.((event as any).output)
                setIsStreaming(false)
              } else if (event.type === 'execution_failed') {
                const errorMsg = (event as any).errorMessage || 'Execution failed'
                options.onError?.(errorMsg)
                setError(errorMsg)
                setIsStreaming(false)
              }
            } catch (e) {
              console.error('Failed to parse event:', e)
            }
          }
        }
      }
    } catch (err: any) {
      if (err.name === 'AbortError') {
        // Stream was intentionally cancelled
        return
      }
      const errorMsg = err.message || 'Stream failed'
      setError(errorMsg)
      setIsStreaming(false)
      options.onError?.(errorMsg)
    }
  }, [options])

  const stopStream = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort()
      abortControllerRef.current = null
    }
    setIsStreaming(false)
  }, [])

  return {
    events,
    isStreaming,
    error,
    executionId,
    startStream,
    stopStream
  }
}
