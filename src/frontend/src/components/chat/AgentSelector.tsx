import { useState, useEffect } from 'react'
import { Bot, ChevronDown, Plus, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from '@/components/ui/dropdown-menu'
import { orchestrations, type ChatEnabledOrchestration } from '@/lib/api'

interface AgentSelectorProps {
  selectedAgentId?: string
  selectedAgentName?: string
  onAgentSelect: (agent: ChatEnabledOrchestration) => void
  onNewChat: () => void
}

export function AgentSelector({
  selectedAgentId,
  selectedAgentName,
  onAgentSelect,
  onNewChat,
}: AgentSelectorProps) {
  const [agents, setAgents] = useState<ChatEnabledOrchestration[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    loadAgents()
  }, [])

  const loadAgents = async () => {
    try {
      setIsLoading(true)
      const data = await orchestrations.listChatEnabled()
      setAgents(data)
    } catch (err) {
      console.error('Failed to load chat-enabled agents:', err)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="flex items-center gap-2">
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" className="min-w-[200px] justify-between">
            <div className="flex items-center gap-2">
              <Bot className="h-4 w-4 text-cyan-500" />
              <span className="truncate">
                {selectedAgentName || 'Select an agent'}
              </span>
            </div>
            <ChevronDown className="h-4 w-4 opacity-50" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-[250px]">
          {isLoading ? (
            <div className="flex items-center justify-center py-4">
              <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
            </div>
          ) : agents.length === 0 ? (
            <div className="px-2 py-4 text-center text-sm text-muted-foreground">
              No chat-enabled agents found.
              <br />
              Enable Chat interface on an orchestration.
            </div>
          ) : (
            <>
              {agents.map((agent) => (
                <DropdownMenuItem
                  key={agent.id}
                  onClick={() => onAgentSelect(agent)}
                  className={agent.id === selectedAgentId ? 'bg-accent' : ''}
                >
                  <div className="flex flex-col gap-0.5">
                    <span className="font-medium">{agent.name}</span>
                    {agent.description && (
                      <span className="text-xs text-muted-foreground line-clamp-1">
                        {agent.description}
                      </span>
                    )}
                  </div>
                </DropdownMenuItem>
              ))}
            </>
          )}
          {agents.length > 0 && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={onNewChat}>
                <Plus className="h-4 w-4 mr-2" />
                New Chat
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>

      <Button variant="outline" size="icon" onClick={onNewChat} title="New Chat">
        <Plus className="h-4 w-4" />
      </Button>
    </div>
  )
}
