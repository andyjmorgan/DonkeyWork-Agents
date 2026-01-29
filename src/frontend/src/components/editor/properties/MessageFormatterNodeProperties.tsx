import { useMemo, useState } from 'react'
import { useEditorStore, type MessageFormatterNodeConfig, type StartNodeConfig } from '@/store/editor'
import { FormField } from '@/components/ui/form-field'
import { ScribanEditor } from './ScribanEditor'
import { Pencil, Check } from 'lucide-react'

interface MessageFormatterNodePropertiesProps {
  nodeId: string
}

export function MessageFormatterNodeProperties({ nodeId }: MessageFormatterNodePropertiesProps) {
  const config = useEditorStore((state) => state.nodeConfigurations[nodeId]) as MessageFormatterNodeConfig
  const updateNodeConfig = useEditorStore((state) => state.updateNodeConfig)
  const getReachablePredecessors = useEditorStore((state) => state.getReachablePredecessors)
  const nodes = useEditorStore((state) => state.nodes)
  const nodeConfigurations = useEditorStore((state) => state.nodeConfigurations)

  const [isEditingName, setIsEditingName] = useState(false)
  const [editedName, setEditedName] = useState('')

  // Get reachable predecessors for the reference section
  const allPredecessors = getReachablePredecessors(nodeId)
  const predecessors = useMemo(() =>
    allPredecessors.filter(p => p.nodeType !== 'start'),
    [allPredecessors]
  )

  // Get input schema properties from the start node
  const inputProperties = useMemo(() => {
    const startNode = nodes.find(n => n.type === 'start')
    if (!startNode) return []

    const startConfig = nodeConfigurations[startNode.id] as StartNodeConfig | undefined
    if (!startConfig?.inputSchema?.properties) return []

    return Object.keys(startConfig.inputSchema.properties)
  }, [nodes, nodeConfigurations])

  if (!config) {
    return <div className="p-4 text-sm text-muted-foreground">No configuration found</div>
  }

  const handleStartEdit = () => {
    setEditedName(config.name)
    setIsEditingName(true)
  }

  const handleSaveName = () => {
    if (editedName.trim()) {
      updateNodeConfig(nodeId, { name: editedName.trim() })
    }
    setIsEditingName(false)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSaveName()
    } else if (e.key === 'Escape') {
      setIsEditingName(false)
    }
  }

  const handleTemplateChange = (value: string) => {
    updateNodeConfig(nodeId, { template: value })
  }

  return (
    <div className="flex h-full flex-col gap-4 overflow-y-auto p-4">
      <div className="space-y-2">
        {/* Editable title */}
        <div className="flex items-center gap-2">
          {isEditingName ? (
            <>
              <input
                type="text"
                value={editedName}
                onChange={(e) => setEditedName(e.target.value)}
                onKeyDown={handleKeyDown}
                onBlur={handleSaveName}
                autoFocus
                className="flex-1 bg-transparent text-sm font-semibold outline-none border-b border-primary"
              />
              <button
                onClick={handleSaveName}
                className="p-1 hover:bg-muted rounded"
              >
                <Check className="h-3.5 w-3.5" />
              </button>
            </>
          ) : (
            <>
              <h3 className="text-sm font-semibold">{config.name}</h3>
              <button
                onClick={handleStartEdit}
                className="p-1 hover:bg-muted rounded opacity-50 hover:opacity-100 transition-opacity"
              >
                <Pencil className="h-3 w-3" />
              </button>
            </>
          )}
        </div>
        <p className="text-xs text-muted-foreground">
          Message Formatter - Format messages using Scriban templates
        </p>
      </div>

      <div className="space-y-4">
        {/* Template editor */}
        <FormField
          label="Template"
          description={`Use {{...}} for Scriban expressions. Type Steps. for autocomplete.`}
        >
          <ScribanEditor
            nodeId={nodeId}
            value={config.template || ''}
            onChange={handleTemplateChange}
            height="300px"
          />
        </FormField>

        {/* Available variables reference */}
        <div className="space-y-2 rounded-lg border border-border p-4">
          <h4 className="text-sm font-medium">Available Variables</h4>
          <div className="space-y-1 text-xs text-muted-foreground">
            <div><code className="bg-muted px-1 rounded">Input</code> - Input data</div>
            {inputProperties.length > 0 && (
              <div className="ml-4 space-y-1">
                {inputProperties.map(prop => (
                  <div key={prop}>
                    <code className="bg-muted px-1 rounded">Input.{prop}</code>
                  </div>
                ))}
              </div>
            )}
            <div><code className="bg-muted px-1 rounded">ExecutionId</code> - Execution ID</div>
            <div><code className="bg-muted px-1 rounded">UserId</code> - User ID</div>
            {predecessors.length > 0 && (
              <>
                <div className="mt-2 font-medium text-foreground">Previous Nodes:</div>
                {predecessors.map(pred => (
                  <div key={pred.nodeId}>
                    <code className="bg-muted px-1 rounded">Steps.{pred.nodeName}</code> - {pred.nodeType}
                  </div>
                ))}
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
