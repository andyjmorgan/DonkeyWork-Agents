import { useState } from 'react'
import { useEditorStore, type MessageFormatterNodeConfig } from '@/store/editor'
import { FormField } from '@/components/ui/form-field'
import { ScribanEditor } from './ScribanEditor'
import { Pencil, Check } from 'lucide-react'

interface MessageFormatterNodePropertiesProps {
  nodeId: string
}

export function MessageFormatterNodeProperties({ nodeId }: MessageFormatterNodePropertiesProps) {
  const config = useEditorStore((state) => state.nodeConfigurations[nodeId]) as MessageFormatterNodeConfig
  const updateNodeConfig = useEditorStore((state) => state.updateNodeConfig)

  const [isEditingName, setIsEditingName] = useState(false)
  const [editedName, setEditedName] = useState('')

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
        {/* Template editor - ScribanEditor provides autocomplete for variables */}
        <FormField
          label="Template"
          description="Use {{...}} for Scriban expressions. Type {{ for autocomplete."
        >
          <ScribanEditor
            nodeId={nodeId}
            value={config.template || ''}
            onChange={handleTemplateChange}
            height="300px"
          />
        </FormField>
      </div>
    </div>
  )
}
