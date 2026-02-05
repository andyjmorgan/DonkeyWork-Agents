import { useState, useCallback } from 'react'
import { Menu } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ChatInterface } from '@/components/execution/ChatInterface'
import { AgentSelector } from '@/components/chat/AgentSelector'
import { ConversationSidebar } from '@/components/chat/ConversationSidebar'
import { conversations, type ChatEnabledOrchestration, type ConversationSummary } from '@/lib/api'

export function ChatPage() {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [selectedAgent, setSelectedAgent] = useState<ChatEnabledOrchestration | null>(null)
  const [activeConversationId, setActiveConversationId] = useState<string | undefined>()
  const [refreshTrigger, setRefreshTrigger] = useState(0)

  const handleAgentSelect = useCallback(async (agent: ChatEnabledOrchestration) => {
    setSelectedAgent(agent)
    // Create a new conversation with the selected agent
    try {
      const newConversation = await conversations.create({
        orchestrationId: agent.id,
      })
      setActiveConversationId(newConversation.id)
      setRefreshTrigger(prev => prev + 1)
    } catch (err) {
      console.error('Failed to create conversation:', err)
    }
  }, [])

  const handleConversationSelect = useCallback((conversation: ConversationSummary) => {
    setActiveConversationId(conversation.id)
    // Update selected agent to match the conversation's orchestration
    setSelectedAgent({
      id: conversation.orchestrationId,
      name: conversation.orchestrationName,
    })
  }, [])

  const handleNewChat = useCallback(() => {
    setActiveConversationId(undefined)
    // Keep the selected agent so user can start a new chat with them
  }, [])

  const handleConversationCreated = useCallback((conversationId: string) => {
    setActiveConversationId(conversationId)
    setRefreshTrigger(prev => prev + 1)
  }, [])

  const handleConversationDeleted = useCallback(() => {
    setActiveConversationId(undefined)
    setRefreshTrigger(prev => prev + 1)
  }, [])

  return (
    <div className="flex h-[calc(100vh-3.5rem)] overflow-hidden">
      {/* Conversation sidebar */}
      <ConversationSidebar
        activeConversationId={activeConversationId}
        onConversationSelect={handleConversationSelect}
        onConversationDeleted={handleConversationDeleted}
        refreshTrigger={refreshTrigger}
        open={sidebarOpen}
        onClose={() => setSidebarOpen(false)}
      />

      {/* Main chat area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Chat header */}
        <div className="flex items-center gap-3 border-b border-border px-4 py-3">
          <Button
            variant="ghost"
            size="icon"
            className="md:hidden"
            onClick={() => setSidebarOpen(true)}
          >
            <Menu className="h-5 w-5" />
          </Button>

          <AgentSelector
            selectedAgentId={selectedAgent?.id}
            selectedAgentName={selectedAgent?.name}
            onAgentSelect={handleAgentSelect}
            onNewChat={handleNewChat}
          />
        </div>

        {/* Chat interface */}
        <div className="flex-1 overflow-hidden">
          {selectedAgent ? (
            <ChatInterface
              key={activeConversationId || 'new'}
              orchestrationId={selectedAgent.id}
              conversationId={activeConversationId}
              onConversationCreated={handleConversationCreated}
            />
          ) : (
            <div className="flex h-full items-center justify-center">
              <div className="text-center text-muted-foreground">
                <p className="text-lg font-medium">Select an agent to start chatting</p>
                <p className="text-sm mt-1">
                  Choose an agent from the dropdown above to begin a conversation
                </p>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
