import { useCallback } from 'react'
import { Zap } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  Input,
  Label,
  Textarea,
} from '@donkeywork/ui'
import { useEditorStore } from '@/store/editor'

interface InterfacesPanelProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function InterfacesPanel({ open, onOpenChange }: InterfacesPanelProps) {
  const interfaceConfig = useEditorStore((state) => state.interface)
  const setInterface = useEditorStore((state) => state.setInterface)

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
            Configure how users and systems will interact with this orchestration.
          </SheetDescription>
        </SheetHeader>

        <div className="mt-6 space-y-6">
          {/* Interface Type (read-only) */}
          <div className="space-y-3">
            <Label>Interface Type</Label>
            <div className="flex items-start gap-3 rounded-lg border border-accent bg-accent/5 p-3">
              <div className="flex-1 space-y-1">
                <div className="flex items-center gap-2">
                  <Zap className="h-4 w-4" />
                  <span className="font-medium text-sm">Direct</span>
                </div>
                <p className="text-xs text-muted-foreground">
                  Structured JSON input/output API calls
                </p>
              </div>
            </div>
          </div>

          {/* Configuration */}
          <div className="space-y-4 border-t pt-4">
            <div className="space-y-2">
              <Label htmlFor="interface-name">Display Name</Label>
              <Input
                id="interface-name"
                placeholder="e.g., Data Processor"
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
                placeholder="e.g., Processes incoming data and returns results..."
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
