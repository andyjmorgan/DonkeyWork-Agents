import { useCallback } from 'react'
import { Zap, Wrench, Server, MessageSquare } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  Input,
  Label,
  Textarea,
  Switch,
} from '@donkeywork/ui'
import { useEditorStore } from '@/store/editor'
import type { InterfaceConfig } from '@donkeywork/api-client'

interface InterfacesPanelProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const INTERFACE_TYPES = [
  {
    type: 'DirectInterfaceConfig' as const,
    label: 'Direct',
    description: 'Structured JSON input/output API calls',
    icon: Zap,
  },
  {
    type: 'ToolInterfaceConfig' as const,
    label: 'Tool',
    description: 'Available as a tool for custom agent definitions',
    icon: Wrench,
  },
  {
    type: 'McpInterfaceConfig' as const,
    label: 'MCP',
    description: 'Exposed via Model Context Protocol to external clients',
    icon: Server,
  },
  {
    type: 'ChatInterfaceConfig' as const,
    label: 'Navi',
    description: 'Available as a conversational orchestration in Navi',
    icon: MessageSquare,
  },
]

export function InterfacesPanel({ open, onOpenChange }: InterfacesPanelProps) {
  const interfaces = useEditorStore((state) => state.interfaces)
  const setInterfaces = useEditorStore((state) => state.setInterfaces)

  const isEnabled = useCallback((type: string) => {
    return interfaces.some(i => i.type === type)
  }, [interfaces])

  const toggleInterface = useCallback((type: string) => {
    if (isEnabled(type)) {
      const filtered = interfaces.filter(i => i.type !== type)
      setInterfaces(filtered.length > 0 ? filtered : [{ type: 'DirectInterfaceConfig' }])
    } else {
      setInterfaces([...interfaces, { type } as InterfaceConfig])
    }
  }, [interfaces, isEnabled, setInterfaces])

  const handleNameChange = useCallback((name: string) => {
    setInterfaces(interfaces.map(i =>
      i.type === 'DirectInterfaceConfig' ? { ...i, name } : i
    ))
  }, [interfaces, setInterfaces])

  const handleDescriptionChange = useCallback((description: string) => {
    setInterfaces(interfaces.map(i =>
      i.type === 'DirectInterfaceConfig' ? { ...i, description } : i
    ))
  }, [interfaces, setInterfaces])

  const directConfig = interfaces.find(i => i.type === 'DirectInterfaceConfig')

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-xl overflow-y-auto">
        <SheetHeader>
          <SheetTitle>Interfaces</SheetTitle>
          <SheetDescription>
            Choose how this orchestration can be accessed. Multiple interfaces can be enabled simultaneously.
          </SheetDescription>
        </SheetHeader>

        <div className="mt-6 space-y-4">
          {INTERFACE_TYPES.map(({ type, label, description, icon: Icon }) => (
            <div
              key={type}
              className={`flex items-center gap-3 rounded-xl border-2 p-4 transition-all ${
                isEnabled(type)
                  ? 'border-accent bg-accent/5'
                  : 'border-border bg-background hover:border-border/80'
              }`}
            >
              <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-muted">
                <Icon className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="flex-1 min-w-0">
                <div className="font-medium text-sm">{label}</div>
                <div className="text-xs text-muted-foreground">{description}</div>
              </div>
              <Switch
                checked={isEnabled(type)}
                onCheckedChange={() => toggleInterface(type)}
              />
            </div>
          ))}
        </div>

        {directConfig && (
          <div className="mt-6 space-y-4 border-t pt-4">
            <div className="space-y-2">
              <Label htmlFor="interface-name">Display Name</Label>
              <Input
                id="interface-name"
                placeholder="e.g., Data Processor"
                value={directConfig.name ?? ''}
                onChange={(e) => handleNameChange(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="interface-description">Description</Label>
              <Textarea
                id="interface-description"
                placeholder="e.g., Processes incoming data and returns results..."
                value={directConfig.description ?? ''}
                onChange={(e) => handleDescriptionChange(e.target.value)}
                rows={3}
              />
            </div>
          </div>
        )}
      </SheetContent>
    </Sheet>
  )
}
