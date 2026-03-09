import { useState, useCallback, useRef } from 'react'
import { fetchWithAuth } from '@donkeywork/api-client'
import type { AgentContractV1 } from '@donkeywork/api-client'

export interface AgentTestEvent {
  agentKey: string
  eventType: string
  [key: string]: unknown
}

export function useAgentTestStream() {
  const [events, setEvents] = useState<AgentTestEvent[]>([])
  const [isStreaming, setIsStreaming] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [streamedText, setStreamedText] = useState('')
  const abortControllerRef = useRef<AbortController | null>(null)

  const startTest = useCallback(async (contract: AgentContractV1, input: string) => {
    // Cancel any existing stream
    if (abortControllerRef.current) {
      abortControllerRef.current.abort()
    }

    setEvents([])
    setError(null)
    setStreamedText('')
    setIsStreaming(true)

    const abortController = new AbortController()
    abortControllerRef.current = abortController

    try {
      const response = await fetchWithAuth(
        '/api/v1/agent-test',
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Accept': 'text/event-stream',
          },
          body: JSON.stringify({ contract, input }),
          signal: abortController.signal,
        },
      )

      if (!response.ok) {
        const text = await response.text()
        throw new Error(`HTTP ${response.status}: ${text}`)
      }

      if (!response.body) {
        throw new Error('No response body')
      }

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''
      let accumulated = ''

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n\n')
        buffer = lines.pop() || ''

        for (const line of lines) {
          if (!line.trim()) continue

          const dataMatch = line.match(/^data:\s*(.+)$/m)
          if (dataMatch) {
            try {
              const event = JSON.parse(dataMatch[1]) as AgentTestEvent
              setEvents((prev) => [...prev, event])

              // Accumulate message text
              if (event.eventType === 'message' && 'text' in event) {
                accumulated += event.text as string
                setStreamedText(accumulated)
              }

              // Stop on complete or error
              if (event.eventType === 'complete') {
                setIsStreaming(false)
              } else if (event.eventType === 'error') {
                setError((event.error as string) || 'Unknown error')
                setIsStreaming(false)
              }
            } catch (e) {
              console.error('Failed to parse SSE event:', e)
            }
          }
        }
      }
    } catch (err: unknown) {
      if (err instanceof Error && err.name === 'AbortError') {
        return
      }
      const errorMsg = err instanceof Error ? err.message : 'Stream failed'
      setError(errorMsg)
      setIsStreaming(false)
    }
  }, [])

  const stopTest = useCallback(() => {
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
    streamedText,
    startTest,
    stopTest,
  }
}
