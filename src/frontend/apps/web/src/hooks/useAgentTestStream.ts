import { useState, useCallback, useRef } from 'react'
import { fetchWithAuth } from '@donkeywork/api-client'
import type {
  AgentContractV1,
  ContentBox,
  TextBox,
  ThinkingBox,
  WebSearchResult,
  AgentCompleteReason,
} from '@donkeywork/api-client'

export function useAgentTestStream() {
  const [boxes, setBoxes] = useState<ContentBox[]>([])
  const [isStreaming, setIsStreaming] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const abortControllerRef = useRef<AbortController | null>(null)
  const agentGroupIndexRef = useRef<Map<string, number>>(new Map())

  function appendOrMerge(
    agentKey: string,
    boxType: string,
    newBox: ContentBox,
    appender?: (existing: ContentBox) => ContentBox,
  ) {
    setBoxes((prev) => {
      const agentIdx = agentGroupIndexRef.current.get(agentKey)

      if (agentIdx !== undefined) {
        const newBoxes = [...prev]
        const host = newBoxes[agentIdx]

        const updateInner = (inner: ContentBox[]): ContentBox[] => {
          const last = inner[inner.length - 1]
          if (last?.type === boxType && appender) {
            return [...inner.slice(0, -1), appender(last)]
          }
          return [...inner, newBox]
        }

        if (host?.type === 'tool_use' && host.subAgent?.agentKey === agentKey) {
          newBoxes[agentIdx] = {
            ...host,
            subAgent: { ...host.subAgent, boxes: updateInner(host.subAgent.boxes) },
          }
        } else if (host?.type === 'agent_group' && host.agentKey === agentKey) {
          newBoxes[agentIdx] = { ...host, boxes: updateInner(host.boxes) }
        }
        return newBoxes
      }

      // Top-level event
      const last = prev[prev.length - 1]
      if (last?.type === boxType && appender) {
        return [...prev.slice(0, -1), appender(last)]
      }
      return [...prev, newBox]
    })
  }

  function handleEvent(data: Record<string, unknown>) {
    const eventType = (data.eventType as string) ?? ''
    const agentKey = (data.agentKey as string) ?? ''

    switch (eventType) {
      case 'message': {
        const t = (data.text as string) ?? ''
        appendOrMerge(
          agentKey,
          'text',
          { type: 'text', text: t },
          (prev) => ({ ...prev, text: (prev as TextBox).text + t }),
        )
        break
      }

      case 'thinking': {
        const t = (data.text as string) ?? ''
        appendOrMerge(
          agentKey,
          'thinking',
          { type: 'thinking', text: t },
          (prev) => ({ ...prev, text: (prev as ThinkingBox).text + t }),
        )
        break
      }

      case 'tool_use':
        appendOrMerge(agentKey, '', {
          type: 'tool_use',
          toolName: (data.toolName as string) ?? '',
          displayName: (data.displayName as string) ?? undefined,
          toolUseId: (data.toolUseId as string) ?? '',
          arguments: (data.arguments as string) ?? undefined,
        })
        break

      case 'tool_result': {
        const resultToolId = (data.toolUseId as string) ?? ''
        const mergeResult = (bs: ContentBox[]): ContentBox[] =>
          bs.map((b) => {
            if (b.type === 'tool_use' && b.toolUseId === resultToolId) {
              return {
                ...b,
                result: (data.result as string) ?? '',
                success: (data.success as boolean) ?? true,
                durationMs: (data.durationMs as number) ?? 0,
              }
            }
            if (b.type === 'tool_use' && b.subAgent) {
              return { ...b, subAgent: { ...b.subAgent, boxes: mergeResult(b.subAgent.boxes) } }
            }
            if (b.type === 'agent_group') {
              return { ...b, boxes: mergeResult(b.boxes) }
            }
            return b
          })
        setBoxes((prev) => mergeResult(prev))
        break
      }

      case 'tool_complete': {
        const toolUseId = (data.toolUseId as string) ?? ''
        const markComplete = (bs: ContentBox[]): ContentBox[] =>
          bs.map((b) => {
            if (b.type === 'tool_use' && b.toolUseId === toolUseId) {
              if (b.subAgent) return b
              return { ...b, isComplete: true }
            }
            if (b.type === 'tool_use' && b.subAgent) {
              return { ...b, subAgent: { ...b.subAgent, boxes: markComplete(b.subAgent.boxes) } }
            }
            if (b.type === 'agent_group') {
              return { ...b, boxes: markComplete(b.boxes) }
            }
            return b
          })
        setBoxes((prev) => markComplete(prev))
        break
      }

      case 'web_search': {
        const wsQuery = (data.query as string) ?? undefined
        appendOrMerge(agentKey, '', {
          type: 'tool_use',
          toolName: 'web_search',
          displayName: wsQuery ? `Searching: ${wsQuery}` : 'Web Search',
          toolUseId: (data.toolUseId as string) ?? '',
          arguments: wsQuery ? JSON.stringify({ query: wsQuery }) : undefined,
        })
        break
      }

      case 'web_search_complete': {
        const wsToolId = (data.toolUseId as string) ?? ''
        const wsResults = (data.results as WebSearchResult[]) ?? []
        const markSearchDone = (bs: ContentBox[]): ContentBox[] =>
          bs.map((b) => {
            if (b.type === 'tool_use' && b.toolUseId === wsToolId) {
              return { ...b, isComplete: true, success: true, webSearchResults: wsResults }
            }
            if (b.type === 'tool_use' && b.subAgent) {
              return { ...b, subAgent: { ...b.subAgent, boxes: markSearchDone(b.subAgent.boxes) } }
            }
            if (b.type === 'agent_group') {
              return { ...b, boxes: markSearchDone(b.boxes) }
            }
            return b
          })
        setBoxes((prev) => markSearchDone(prev))
        break
      }

      case 'agent_spawn': {
        const spawnedKey = (data.spawnedAgentKey as string) ?? ''
        const agentType = (data.agentType as string) ?? ''
        const label = (data.label as string) || undefined
        setBoxes((prev) => {
          const newBoxes = [...prev]
          for (let i = newBoxes.length - 1; i >= 0; i--) {
            const b = newBoxes[i]
            if (b.type === 'tool_use' && !b.subAgent) {
              newBoxes[i] = {
                ...b,
                subAgent: {
                  type: 'agent_group',
                  agentKey: spawnedKey,
                  agentType,
                  label,
                  boxes: [],
                },
              }
              agentGroupIndexRef.current.set(spawnedKey, i)
              return newBoxes
            }
          }
          const newIdx = newBoxes.length
          agentGroupIndexRef.current.set(spawnedKey, newIdx)
          return [
            ...newBoxes,
            {
              type: 'agent_group' as const,
              agentKey: spawnedKey,
              agentType,
              label,
              boxes: [],
            },
          ]
        })
        break
      }

      case 'agent_complete': {
        const reason = (data.reason as AgentCompleteReason) ?? 'completed'
        setBoxes((prev) =>
          prev.map((b) => {
            if (b.type === 'tool_use' && b.subAgent?.agentKey === agentKey) {
              return {
                ...b,
                isComplete: true,
                completeReason: reason,
                subAgent: { ...b.subAgent, isComplete: true, completeReason: reason },
              }
            }
            if (b.type === 'agent_group' && b.agentKey === agentKey) {
              return { ...b, isComplete: true, completeReason: reason }
            }
            return b
          }),
        )
        break
      }

      case 'agent_result_data': {
        const subKey = (data.subAgentKey as string) ?? ''
        const resultText = (data.text as string) ?? ''
        const citations =
          (data.citations as Array<{ title: string; url: string; citedText: string }>) ?? []
        if (resultText) {
          const targetKey = agentGroupIndexRef.current.has(subKey) ? subKey : agentKey
          appendOrMerge(
            targetKey,
            'text',
            { type: 'text', text: resultText },
            (prev) => ({ ...prev, text: (prev as TextBox).text + resultText }),
          )
        }
        for (const c of citations) {
          const targetKey = agentGroupIndexRef.current.has(subKey) ? subKey : agentKey
          appendOrMerge(targetKey, '', {
            type: 'citation',
            title: c.title ?? '',
            url: c.url ?? '',
            citedText: c.citedText ?? '',
          })
        }
        break
      }

      case 'citation':
        appendOrMerge(agentKey, '', {
          type: 'citation',
          title: (data.title as string) ?? '',
          url: (data.url as string) ?? '',
          citedText: (data.citedText as string) ?? '',
        })
        break

      case 'usage':
        appendOrMerge(agentKey, '', {
          type: 'usage',
          inputTokens: (data.inputTokens as number) ?? 0,
          outputTokens: (data.outputTokens as number) ?? 0,
          webSearchRequests: (data.webSearchRequests as number) ?? 0,
          contextWindowLimit: (data.contextWindowLimit as number) ?? 0,
          maxOutputTokens: (data.maxOutputTokens as number) ?? 0,
        })
        break

      case 'complete':
        setIsStreaming(false)
        break

      case 'error':
        setError((data.error as string) || 'Unknown error')
        setIsStreaming(false)
        break

      // Ignore control events
      case 'turn_start':
      case 'turn_end':
      case 'queue_status':
      case 'progress':
      case 'cancelled':
      case 'mcp_server_status':
      case 'sandbox_status':
        break
    }
  }

  const startTest = useCallback(
    async (contract: AgentContractV1, input: string) => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort()
      }

      setBoxes([])
      setError(null)
      setIsStreaming(true)
      agentGroupIndexRef.current = new Map()

      const abortController = new AbortController()
      abortControllerRef.current = abortController

      try {
        const response = await fetchWithAuth('/api/v1/agent-test', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Accept: 'text/event-stream',
          },
          body: JSON.stringify({ contract, input }),
          signal: abortController.signal,
        })

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
                const event = JSON.parse(dataMatch[1]) as Record<string, unknown>
                handleEvent(event)
              } catch (e) {
                console.error('Failed to parse SSE event:', e)
              }
            }
          }
        }

        // Process remaining buffer after stream ends
        if (buffer.trim()) {
          const dataMatch = buffer.match(/^data:\s*(.+)$/m)
          if (dataMatch) {
            try {
              const event = JSON.parse(dataMatch[1]) as Record<string, unknown>
              handleEvent(event)
            } catch (e) {
              console.error('Failed to parse final SSE event:', e)
            }
          }
        }
      } catch (err: unknown) {
        if (err instanceof Error && err.name === 'AbortError') {
          return
        }
        const errorMsg = err instanceof Error ? err.message : 'Stream failed'
        setError(errorMsg)
      } finally {
        setIsStreaming(false)
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [],
  )

  const stopTest = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort()
      abortControllerRef.current = null
    }
    setIsStreaming(false)
  }, [])

  return {
    boxes,
    isStreaming,
    error,
    startTest,
    stopTest,
  }
}
