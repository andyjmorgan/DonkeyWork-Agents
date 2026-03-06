import { useState, useCallback } from 'react'
import { AgentChatPanel, ChatConfigProvider, type ChatConfig } from '@donkeywork/chat'

const defaultChatConfig: ChatConfig = {
  renderJson: (data, opts) => (
    <pre className={`text-xs text-muted-foreground whitespace-pre-wrap break-words font-mono leading-relaxed ${opts.className ?? ''}`}>
      {JSON.stringify(data, null, 2)}
    </pre>
  ),
}

export function ChatPage() {
  const [conversationId, setConversationId] = useState<string | undefined>()

  const onConversationCreated = useCallback((id: string) => {
    setConversationId(id)
  }, [])

  const onReset = useCallback(() => {
    setConversationId(undefined)
  }, [])

  return (
    <ChatConfigProvider config={defaultChatConfig}>
      <div className="flex h-full flex-col overflow-hidden">
        <AgentChatPanel
          key={conversationId ?? 'new'}
          conversationId={conversationId}
          onConversationCreated={onConversationCreated}
          onReset={onReset}
        />
      </div>
    </ChatConfigProvider>
  )
}
