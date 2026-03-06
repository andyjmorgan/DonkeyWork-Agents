import { useCallback } from 'react'
import { MessageSquare, Zap } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  Input,
  Label,
  Textarea,
  RadioGroup,
  RadioGroupItem,
} from '@donkeywork/ui'
import { useEditorStore } from '@/store/editor'
import type { InterfaceConfig, InterfaceType } from '@/lib/api'

interface InterfacesPanelProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const interfaceOptions: Array<{
  type: InterfaceType
  label: string
  description: string
  icon: React.ReactNode
}> = [
  {
    type: 'DirectInterfaceConfig',
    label: 'Direct',
    description: 'Simple JSON input/output API calls',
    icon: <Zap className="h-4 w-4" />,
  },
  {
    type: 'ChatInterfaceConfig',
    label: 'Chat',
    description: 'Conversational interface with message history',
    icon: <MessageSquare className="h-4 w-4" />,
  },
]

export function InterfacesPanel({ open, onOpenChange }: InterfacesPanelProps) {
  const interfaceConfig = useEditorStore((state) => state.interface)
  const setInterface = useEditorStore((state) => state.setInterface)

  const handleTypeChange = useCallback((type: InterfaceType) => {
    // Preserve name/description when switching types
    setInterface({
      type,
      name: interfaceConfig.name,
      description: interfaceConfig.description,
    } as InterfaceConfig)
  }, [interfaceConfig, setInterface])

  const handleNameChange = useCallback((name: string) => {
    setInterface({
      ...interfaceConfig,
      name,
    })
  }, [interfaceConfig, setInterface])

  const handleDescriptionChange = useCallback((description: string) => {
    setInterface({
      ...interfaceConfig,
      description,
    })
  }, [interfaceConfig, setInterface])

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-xl overflow-y-auto">
        <SheetHeader>
          <SheetTitle>Interface</SheetTitle>
          <SheetDescription>
            Choose how users and systems will interact with this orchestration.
          </SheetDescription>
        </SheetHeader>

        <div className="mt-6 space-y-6">
          {/* Interface Type Selection */}
          <div className="space-y-3">
            <Label>Interface Type</Label>
            <RadioGroup
              value={interfaceConfig.type}
              onValueChange={(value) => handleTypeChange(value as InterfaceType)}
              className="space-y-2"
            >
              {interfaceOptions.map((option) => (
                <label
                  key={option.type}
                  className={`flex items-start gap-3 rounded-lg border p-3 cursor-pointer transition-colors hover:bg-muted/50 ${
                    interfaceConfig.type === option.type
                      ? 'border-accent bg-accent/5'
                      : 'border-border'
                  }`}
                >
                  <RadioGroupItem value={option.type} className="mt-0.5" />
                  <div className="flex-1 space-y-1">
                    <div className="flex items-center gap-2">
                      {option.icon}
                      <span className="font-medium text-sm">{option.label}</span>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {option.description}
                    </p>
                  </div>
                </label>
              ))}
            </RadioGroup>
          </div>

          {/* Common Configuration */}
          <div className="space-y-4 border-t pt-4">
            <div className="space-y-2">
              <Label htmlFor="interface-name">Display Name</Label>
              <Input
                id="interface-name"
                placeholder="e.g., Customer Support Bot"
                value={interfaceConfig.name ?? ''}
                onChange={(e) => handleNameChange(e.target.value)}
              />
              <p className="text-xs text-muted-foreground">
                Optional name shown to users interacting with this interface
              </p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="interface-description">Description</Label>
              <Textarea
                id="interface-description"
                placeholder="e.g., I can help answer your questions..."
                value={interfaceConfig.description ?? ''}
                onChange={(e) => handleDescriptionChange(e.target.value)}
                rows={3}
              />
              <p className="text-xs text-muted-foreground">
                Optional description explaining what this orchestration does
              </p>
            </div>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
