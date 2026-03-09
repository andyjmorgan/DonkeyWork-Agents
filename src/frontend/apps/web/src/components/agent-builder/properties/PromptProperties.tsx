import { useState, useEffect } from 'react'
import { useAgentBuilderStore } from '@/store/agentBuilder'
import { prompts as promptsApi, type PromptDetails } from '@donkeywork/api-client'
import { FileText } from 'lucide-react'

interface PromptPropertiesProps {
  nodeId: string
}

export function PromptProperties({ nodeId }: PromptPropertiesProps) {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[nodeId])
  const [promptDetails, setPromptDetails] = useState<PromptDetails | null>(null)

  const promptName = (config?.promptName as string) || 'Unknown Prompt'
  const promptId = (config?.promptId as string) || ''

  useEffect(() => {
    if (promptId) {
      promptsApi.get(promptId).then(setPromptDetails).catch(console.error)
    }
  }, [promptId])

  if (!config) return null

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 rounded-lg border border-border p-4">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-emerald-500 to-green-600 shadow-lg shadow-emerald-500/25">
          <FileText className="h-5 w-5 text-white" />
        </div>
        <div>
          <div className="font-medium">{promptName}</div>
          <div className="text-xs text-muted-foreground font-mono">{promptId}</div>
        </div>
      </div>
      {promptDetails?.content && (
        <div className="space-y-2">
          <div className="text-sm font-medium text-muted-foreground">Content Preview</div>
          <div className="rounded-lg border border-border bg-muted/50 p-3 text-sm font-mono whitespace-pre-wrap max-h-[300px] overflow-y-auto">
            {promptDetails.content}
          </div>
        </div>
      )}
      <p className="text-xs text-muted-foreground">
        This prompt is connected to the agent. Remove the node from the canvas to disconnect it.
      </p>
    </div>
  )
}
