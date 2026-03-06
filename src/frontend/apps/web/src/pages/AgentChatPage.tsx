import { useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { AgentChatPanel } from '@donkeywork/chat'

export function AgentChatPage() {
  const { conversationId } = useParams<{ conversationId?: string }>()
  const navigate = useNavigate()

  const onConversationCreated = useCallback((id: string) => {
    window.history.replaceState(null, '', `/agent-chat/${id}`)
  }, [])

  const onReset = useCallback(() => {
    navigate('/agent-chat', { replace: true })
  }, [navigate])

  return (
    <div className="flex h-full flex-col overflow-hidden -m-4 md:-m-6">
      <AgentChatPanel
        key={conversationId ?? "new"}
        conversationId={conversationId}
        onConversationCreated={onConversationCreated}
        onReset={onReset}
      />
    </div>
  )
}
