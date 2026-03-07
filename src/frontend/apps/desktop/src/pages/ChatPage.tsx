import { useState, useCallback } from 'react'
import { AgentChatPanel, ChatConfigProvider, type ChatConfig } from '@donkeywork/chat'

const defaultChatConfig: ChatConfig = {
  renderJson: (data, opts) => (
    <pre className={`text-xs text-muted-foreground whitespace-pre-wrap break-words font-mono leading-relaxed ${opts.className ?? ''}`}>
      {JSON.stringify(data, null, 2)}
    </pre>
  ),
}

interface ChatPageProps {
  conversationId?: string
}

export function ChatPage({ conversationId }: ChatPageProps) {
  const [chatKey, setChatKey] = useState(() => conversationId ?? `new-${Date.now()}`)

  const onConversationCreated = useCallback((_id: string) => {
    // Don't update key — AgentChatPanel manages its own conversation state internally.
    // Changing the key would remount the component and kill the active WebSocket.
  }, [])

  const onReset = useCallback(() => {
    setChatKey(`new-${Date.now()}`)
  }, [])

  return (
    <ChatConfigProvider config={defaultChatConfig}>
      <div className="flex h-full flex-col overflow-hidden">
        <AgentChatPanel
          key={chatKey}
          conversationId={conversationId}
          onConversationCreated={onConversationCreated}
          onReset={onReset}
        />
      </div>
    </ChatConfigProvider>
  )
}
