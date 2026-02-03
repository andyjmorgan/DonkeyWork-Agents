import { useCallback } from 'react'
import { MessageSquare, HelpCircle } from 'lucide-react'
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
import type { ChatInterfaceConfig } from '@/lib/api'

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
