import { useCallback } from 'react'
import { MessageSquare, Webhook, Bot, Wrench, HelpCircle } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet'
import { Switch } from '@/components/ui/switch'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { useEditorStore } from '@/store/editor'
import type {
  ChatInterfaceConfig,
  McpInterfaceConfig,
  A2aInterfaceConfig,
  WebhookInterfaceConfig
} from '@/lib/api'

interface InterfacesPanelProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function InterfacesPanel({ open, onOpenChange }: InterfacesPanelProps) {
  const interfaces = useEditorStore((state) => state.interfaces)
  const updateInterface = useEditorStore((state) => state.updateInterface)

  // Helper to update a specific interface
  const updateChatConfig = useCallback((updates: Partial<ChatInterfaceConfig>) => {
    const current = interfaces?.chat ?? { enabled: false }
    updateInterface('chat', { ...current, ...updates })
  }, [interfaces, updateInterface])

  const updateMcpConfig = useCallback((updates: Partial<McpInterfaceConfig>) => {
    const current = interfaces?.mcp ?? { enabled: false }
    updateInterface('mcp', { ...current, ...updates })
  }, [interfaces, updateInterface])

  const updateA2aConfig = useCallback((updates: Partial<A2aInterfaceConfig>) => {
    const current = interfaces?.a2a ?? { enabled: false }
    updateInterface('a2a', { ...current, ...updates })
  }, [interfaces, updateInterface])

  const updateWebhookConfig = useCallback((updates: Partial<WebhookInterfaceConfig>) => {
    const current = interfaces?.webhook ?? { enabled: false }
    updateInterface('webhook', { ...current, ...updates })
  }, [interfaces, updateInterface])

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-xl overflow-y-auto">
        <SheetHeader>
          <SheetTitle>Interfaces</SheetTitle>
          <SheetDescription>
            Configure how users and other systems can interact with this orchestration.
            Enable the interfaces you want to expose.
          </SheetDescription>
        </SheetHeader>

        <div className="mt-6 space-y-4">
          {/* Chat Interface */}
          <InterfaceCard
            icon={<MessageSquare className="h-5 w-5 text-green-500" />}
            title="Chat Interface"
            description="Allow users to interact via a conversational chat UI"
            enabled={interfaces?.chat?.enabled ?? false}
            onToggle={(enabled) => updateChatConfig({ enabled })}
            helpText="When enabled, users can start conversations with this orchestration using a chat interface. The orchestration will process messages and stream responses back."
          >
            {interfaces?.chat?.enabled && (
              <div className="space-y-4 pt-4 border-t">
                <div className="space-y-2">
                  <Label htmlFor="chat-name">Display Name</Label>
                  <Input
                    id="chat-name"
                    placeholder="e.g., Customer Support Bot"
                    value={interfaces?.chat?.name ?? ''}
                    onChange={(e) => updateChatConfig({ name: e.target.value })}
                  />
                  <p className="text-xs text-muted-foreground">
                    The name shown to users in the chat interface
                  </p>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="chat-description">Description</Label>
                  <Textarea
                    id="chat-description"
                    placeholder="e.g., I can help answer your questions..."
                    value={interfaces?.chat?.description ?? ''}
                    onChange={(e) => updateChatConfig({ description: e.target.value })}
                    rows={2}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="chat-welcome">Welcome Message</Label>
                  <Textarea
                    id="chat-welcome"
                    placeholder="e.g., Hello! How can I help you today?"
                    value={interfaces?.chat?.welcomeMessage ?? ''}
                    onChange={(e) => updateChatConfig({ welcomeMessage: e.target.value })}
                    rows={2}
                  />
                  <p className="text-xs text-muted-foreground">
                    Shown when users start a new conversation
                  </p>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="chat-system">System Prompt</Label>
                  <Textarea
                    id="chat-system"
                    placeholder="e.g., You are a helpful assistant..."
                    value={interfaces?.chat?.systemPrompt ?? ''}
                    onChange={(e) => updateChatConfig({ systemPrompt: e.target.value })}
                    rows={4}
                  />
                  <p className="text-xs text-muted-foreground">
                    Instructions for the AI model (optional, may override node-level prompts)
                  </p>
                </div>
              </div>
            )}
          </InterfaceCard>

          {/* MCP Interface */}
          <InterfaceCard
            icon={<Wrench className="h-5 w-5 text-blue-500" />}
            title="MCP (Model Context Protocol)"
            description="Expose as a tool for AI assistants via MCP"
            enabled={interfaces?.mcp?.enabled ?? false}
            onToggle={(enabled) => updateMcpConfig({ enabled })}
            helpText="MCP allows AI assistants like Claude to use this orchestration as a tool. The orchestration becomes callable with its input schema."
          >
            {interfaces?.mcp?.enabled && (
              <div className="space-y-4 pt-4 border-t">
                <div className="space-y-2">
                  <Label htmlFor="mcp-name">Tool Name</Label>
                  <Input
                    id="mcp-name"
                    placeholder="e.g., search_knowledge_base"
                    value={interfaces?.mcp?.name ?? ''}
                    onChange={(e) => updateMcpConfig({ name: e.target.value })}
                  />
                  <p className="text-xs text-muted-foreground">
                    The name AI assistants will use to call this tool (lowercase, underscores)
                  </p>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="mcp-description">Tool Description</Label>
                  <Textarea
                    id="mcp-description"
                    placeholder="e.g., Searches the knowledge base for relevant information..."
                    value={interfaces?.mcp?.description ?? ''}
                    onChange={(e) => updateMcpConfig({ description: e.target.value })}
                    rows={3}
                  />
                  <p className="text-xs text-muted-foreground">
                    Helps AI assistants understand when to use this tool
                  </p>
                </div>
              </div>
            )}
          </InterfaceCard>

          {/* A2A Interface */}
          <InterfaceCard
            icon={<Bot className="h-5 w-5 text-purple-500" />}
            title="A2A (Agent-to-Agent)"
            description="Allow other AI agents to discover and call this orchestration"
            enabled={interfaces?.a2a?.enabled ?? false}
            onToggle={(enabled) => updateA2aConfig({ enabled })}
            helpText="A2A enables agent-to-agent communication following the Google A2A protocol. Other agents can discover and interact with this orchestration."
          >
            {interfaces?.a2a?.enabled && (
              <div className="space-y-4 pt-4 border-t">
                <div className="space-y-2">
                  <Label htmlFor="a2a-name">Agent Name</Label>
                  <Input
                    id="a2a-name"
                    placeholder="e.g., Data Analysis Agent"
                    value={interfaces?.a2a?.name ?? ''}
                    onChange={(e) => updateA2aConfig({ name: e.target.value })}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="a2a-description">Agent Description</Label>
                  <Textarea
                    id="a2a-description"
                    placeholder="e.g., Analyzes data and generates reports..."
                    value={interfaces?.a2a?.description ?? ''}
                    onChange={(e) => updateA2aConfig({ description: e.target.value })}
                    rows={3}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="a2a-agent-id">Agent ID (optional)</Label>
                  <Input
                    id="a2a-agent-id"
                    placeholder="e.g., data-analysis-agent-v1"
                    value={interfaces?.a2a?.agentId ?? ''}
                    onChange={(e) => updateA2aConfig({ agentId: e.target.value })}
                  />
                  <p className="text-xs text-muted-foreground">
                    Unique identifier for A2A discovery (auto-generated if empty)
                  </p>
                </div>
              </div>
            )}
          </InterfaceCard>

          {/* Webhook Interface */}
          <InterfaceCard
            icon={<Webhook className="h-5 w-5 text-orange-500" />}
            title="Webhook"
            description="Trigger this orchestration via HTTP webhooks"
            enabled={interfaces?.webhook?.enabled ?? false}
            onToggle={(enabled) => updateWebhookConfig({ enabled })}
            helpText="Webhooks allow external services to trigger this orchestration via HTTP POST requests. Useful for integrations with Zapier, n8n, or custom applications."
          >
            {interfaces?.webhook?.enabled && (
              <div className="space-y-4 pt-4 border-t">
                <div className="space-y-2">
                  <Label htmlFor="webhook-name">Webhook Name</Label>
                  <Input
                    id="webhook-name"
                    placeholder="e.g., Order Processing Webhook"
                    value={interfaces?.webhook?.name ?? ''}
                    onChange={(e) => updateWebhookConfig({ name: e.target.value })}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="webhook-description">Description</Label>
                  <Textarea
                    id="webhook-description"
                    placeholder="e.g., Processes incoming order events..."
                    value={interfaces?.webhook?.description ?? ''}
                    onChange={(e) => updateWebhookConfig({ description: e.target.value })}
                    rows={2}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Require Signature</Label>
                    <p className="text-xs text-muted-foreground">
                      Validate webhook signatures for security
                    </p>
                  </div>
                  <Switch
                    checked={interfaces?.webhook?.requireSignature ?? false}
                    onCheckedChange={(checked) => updateWebhookConfig({ requireSignature: checked })}
                  />
                </div>
              </div>
            )}
          </InterfaceCard>
        </div>
      </SheetContent>
    </Sheet>
  )
}

interface InterfaceCardProps {
  icon: React.ReactNode
  title: string
  description: string
  enabled: boolean
  onToggle: (enabled: boolean) => void
  helpText?: string
  children?: React.ReactNode
}

function InterfaceCard({
  icon,
  title,
  description,
  enabled,
  onToggle,
  helpText,
  children
}: InterfaceCardProps) {
  return (
    <Card className={enabled ? 'border-accent/50' : ''}>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between gap-4">
          <div className="flex items-start gap-3">
            <div className="mt-0.5">{icon}</div>
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <CardTitle className="text-base">{title}</CardTitle>
                {helpText && (
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <HelpCircle className="h-4 w-4 text-muted-foreground cursor-help" />
                      </TooltipTrigger>
                      <TooltipContent side="top" className="max-w-xs">
                        <p>{helpText}</p>
                      </TooltipContent>
                    </Tooltip>
                  </TooltipProvider>
                )}
              </div>
              <CardDescription>{description}</CardDescription>
            </div>
          </div>
          <Switch checked={enabled} onCheckedChange={onToggle} />
        </div>
      </CardHeader>
      {children && <CardContent>{children}</CardContent>}
    </Card>
  )
}
