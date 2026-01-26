import { useEditorStore } from '@/store/editor'
import { StartNodeProperties } from './properties/StartNodeProperties'
import { ModelNodeProperties } from './properties/ModelNodeProperties'
import { EndNodeProperties } from './properties/EndNodeProperties'
import { ActionNodeProperties } from './properties/ActionNodeProperties'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'

export function PropertiesPanel() {
  const selectedNodeId = useEditorStore((state) => state.selectedNodeId)
  const isPropertiesOpen = useEditorStore((state) => state.isPropertiesOpen)
  const nodes = useEditorStore((state) => state.nodes)
  const selectNode = useEditorStore((state) => state.selectNode)

  // Find the selected node
  const selectedNode = selectedNodeId
    ? nodes.find(n => n.id === selectedNodeId)
    : null

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      selectNode(null)
    }
  }

  const getNodeTitle = () => {
    if (!selectedNode) return 'Properties'

    switch (selectedNode.type) {
      case 'start':
        return 'Start Node Properties'
      case 'model':
        return 'Model Node Properties'
      case 'end':
        return 'End Node Properties'
      case 'action':
        return selectedNode.data?.displayName || 'Action Node Properties'
      default:
        return 'Node Properties'
    }
  }

  const renderNodeProperties = () => {
    if (!selectedNode) return null

    switch (selectedNode.type) {
      case 'start':
        return <StartNodeProperties nodeId={selectedNode.id} />
      case 'model':
        return <ModelNodeProperties nodeId={selectedNode.id} />
      case 'end':
        return <EndNodeProperties nodeId={selectedNode.id} />
      case 'action':
        return <ActionNodeProperties nodeId={selectedNode.id} />
      default:
        return (
          <div className="p-4 text-sm text-muted-foreground">
            Unknown node type: {selectedNode.type}
          </div>
        )
    }
  }

  return (
    <Sheet open={isPropertiesOpen} onOpenChange={handleOpenChange}>
      <SheetContent side="right" className="w-full sm:w-[600px] lg:w-[700px] overflow-y-auto">
        <SheetHeader>
          <SheetTitle>{getNodeTitle()}</SheetTitle>
        </SheetHeader>
        <div className="mt-6">
          {renderNodeProperties()}
        </div>
      </SheetContent>
    </Sheet>
  )
}
