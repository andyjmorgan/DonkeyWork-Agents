import { AgentChatPanel } from '@/components/agent-chat/AgentChatPanel'

export function AgentChatPage() {
  return (
    <div className="flex h-full flex-col overflow-hidden -m-4 md:-m-6">
      <AgentChatPanel />
    </div>
  )
}
