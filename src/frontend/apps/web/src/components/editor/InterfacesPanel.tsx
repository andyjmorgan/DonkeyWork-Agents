import { Zap, Wrench, Server, MessageSquare } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  Switch,
} from '@donkeywork/ui'
import { useEditorStore } from '@/store/editor'

interface InterfacesPanelProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const INTERFACE_TYPES = [
  { key: 'directEnabled' as const, label: 'Direct', description: 'Structured JSON input/output API calls', icon: Zap },
  { key: 'toolEnabled' as const, label: 'Tool', description: 'Available as a tool for custom agent definitions', icon: Wrench },
  { key: 'mcpEnabled' as const, label: 'MCP', description: 'Exposed via Model Context Protocol to external clients', icon: Server },
  { key: 'naviEnabled' as const, label: 'Navi', description: 'Available as a conversational orchestration in Navi', icon: MessageSquare },
]

export function InterfacesPanel({ open, onOpenChange }: InterfacesPanelProps) {
  const directEnabled = useEditorStore((s) => s.directEnabled)
  const toolEnabled = useEditorStore((s) => s.toolEnabled)
  const mcpEnabled = useEditorStore((s) => s.mcpEnabled)
  const naviEnabled = useEditorStore((s) => s.naviEnabled)
  const setInterfaceFlags = useEditorStore((s) => s.setInterfaceFlags)

  const flags = { directEnabled, toolEnabled, mcpEnabled, naviEnabled }

  const toggle = (key: keyof typeof flags) => {
    setInterfaceFlags({ ...flags, [key]: !flags[key] })
  }

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
          {INTERFACE_TYPES.map(({ key, label, description, icon: Icon }) => (
            <div
              key={key}
              className={`flex items-center gap-3 rounded-xl border-2 p-4 transition-all ${
                flags[key]
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
              <Switch checked={flags[key]} onCheckedChange={() => toggle(key)} />
            </div>
          ))}
        </div>
      </SheetContent>
    </Sheet>
  )
}
