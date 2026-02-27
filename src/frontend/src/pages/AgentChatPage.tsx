import { useParams } from 'react-router-dom'
import { AgentChatPanel } from '@/components/agent-chat/AgentChatPanel'

export function AgentChatPage() {
  const { conversationId } = useParams<{ conversationId?: string }>()

  return (
    <div className="flex h-full flex-col overflow-hidden -m-4 md:-m-6">
      <AgentChatPanel key={conversationId ?? "new"} conversationId={conversationId} />
    </div>
  )
}
