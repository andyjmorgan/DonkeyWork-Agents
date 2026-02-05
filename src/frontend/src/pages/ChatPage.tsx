import { useState, useCallback, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { ChatInterface } from '@/components/execution/ChatInterface'
import { AgentSelector } from '@/components/chat/AgentSelector'
import { conversations, type ChatEnabledOrchestration } from '@/lib/api'

export function ChatPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [selectedAgent, setSelectedAgent] = useState<ChatEnabledOrchestration | null>(null)
  const [activeConversationId, setActiveConversationId] = useState<string | undefined>()

  // Load conversation from URL query parameter
  useEffect(() => {
    const conversationId = searchParams.get('conversation')
    if (conversationId && conversationId !== activeConversationId) {
      // Load the conversation to get orchestration info
      conversations.get(conversationId).then((conversation) => {
        setActiveConversationId(conversationId)
        setSelectedAgent({
          id: conversation.orchestrationId,
          name: conversation.orchestrationName,
        })
        // Clear the query param after loading
        setSearchParams({}, { replace: true })
      }).catch((err) => {
        console.error('Failed to load conversation from URL:', err)
        setSearchParams({}, { replace: true })
      })
    }
  }, [searchParams, activeConversationId, setSearchParams])

  const handleAgentSelect = useCallback(async (agent: ChatEnabledOrchestration) => {
    setSelectedAgent(agent)
    // Create a new conversation with the selected agent
    try {
      const newConversation = await conversations.create({
        orchestrationId: agent.id,
      })
      setActiveConversationId(newConversation.id)
    } catch (err) {
      console.error('Failed to create conversation:', err)
    }
  }, [])

  const handleConversationCreated = useCallback((conversationId: string) => {
    setActiveConversationId(conversationId)
  }, [])

  return (
    <div className="flex h-full flex-col overflow-hidden -m-4 md:-m-6">
      {/* Chat header */}
      <div className="flex items-center gap-3 border-b border-border px-4 py-3">
        <AgentSelector
          selectedAgentId={selectedAgent?.id}
          selectedAgentName={selectedAgent?.name}
          onAgentSelect={handleAgentSelect}
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
  )
}
