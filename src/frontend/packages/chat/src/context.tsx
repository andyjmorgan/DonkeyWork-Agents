import { createContext, useContext } from 'react'
import type { ReactNode } from 'react'

export interface ChatConfig {
  renderJson: (data: unknown, opts: { collapsed?: number; className?: string }) => ReactNode
}

const defaultConfig: ChatConfig = {
  renderJson: (data, opts) => (
    <pre className={`text-xs text-muted-foreground whitespace-pre-wrap break-words font-mono leading-relaxed ${opts.className ?? ''}`}>
      {JSON.stringify(data, null, 2)}
    </pre>
  ),
}

const ChatConfigContext = createContext<ChatConfig>(defaultConfig)

export function ChatConfigProvider({
  config,
  children,
}: {
  config: ChatConfig
  children: ReactNode
}) {
  return (
    <ChatConfigContext.Provider value={config}>
      {children}
    </ChatConfigContext.Provider>
  )
}

export function useChatConfig(): ChatConfig {
  return useContext(ChatConfigContext)
}
