import { useEditorStore, type StartNodeConfig } from '@/store/editor'
import { useThemeStore } from '@/store/theme'
import { Label } from '@/components/ui/label'
import Editor from '@monaco-editor/react'

interface StartNodePropertiesProps {
  nodeId: string
}

export function StartNodeProperties({ nodeId }: StartNodePropertiesProps) {
  const theme = useThemeStore((state) => state.theme)
  const config = useEditorStore((state) => state.nodeConfigurations[nodeId]) as StartNodeConfig
  const updateNodeConfig = useEditorStore((state) => state.updateNodeConfig)

  if (!config) {
    return <div className="p-4 text-sm text-muted-foreground">No configuration found</div>
  }

  const handleSchemaChange = (value: string | undefined) => {
    if (!value) return
    try {
      const parsed = JSON.parse(value)
      updateNodeConfig(nodeId, { inputSchema: parsed })
    } catch (error) {
      // Invalid JSON, don't update
      console.warn('Invalid JSON schema:', error)
    }
  }

  return (
    <div className="flex h-full flex-col gap-4 p-4">
      <div className="space-y-2">
        <h3 className="text-sm font-semibold">Start Node</h3>
        <p className="text-xs text-muted-foreground">
          Entry point - validates input against schema
        </p>
      </div>

      <div className="space-y-4">
        {/* Input Schema editor */}
        <div className="flex-1 space-y-2">
          <Label>Input Schema</Label>
          <div className="rounded-xl border border-border overflow-hidden">
            <Editor
              height="400px"
              language="json"
              theme={theme === 'dark' ? 'vs-dark' : 'light'}
              value={JSON.stringify(config.inputSchema, null, 2)}
              onChange={handleSchemaChange}
              options={{
                minimap: { enabled: false },
                fontSize: 12,
                lineNumbers: 'on',
                scrollBeyondLastLine: false,
                wordWrap: 'on',
                wrappingIndent: 'indent',
                formatOnPaste: true,
                formatOnType: true,
              }}
            />
          </div>
          <p className="text-xs text-muted-foreground">
            JSON Schema defining the expected input format
          </p>
        </div>
      </div>
    </div>
  )
}
