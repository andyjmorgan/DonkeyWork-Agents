import { useState, useEffect } from 'react'
import { useAgentBuilderStore } from '@/store/agentBuilder'
import { models, type ModelDefinition } from '@donkeywork/api-client'
import {
  Label,
  Input,
  Slider,
  Switch,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  RadioGroup,
  RadioGroupItem,
} from '@donkeywork/ui'

interface ModelPropertiesProps {
  nodeId: string
}

export function ModelProperties({ nodeId }: ModelPropertiesProps) {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[nodeId])
  const updateNodeConfig = useAgentBuilderStore((s) => s.updateNodeConfig)
  const isReadOnly = useAgentBuilderStore((s) => s.isReadOnly)
  const [allModels, setAllModels] = useState<ModelDefinition[]>([])

  useEffect(() => {
    models
      .list()
      .then((data) => setAllModels(data.filter((m) => m.mode === 'Chat')))
      .catch(console.error)
  }, [])

  if (!config) return null

  const update = (field: string, value: unknown) => {
    updateNodeConfig(nodeId, { [field]: value })
  }

  const modelId = (config.modelId as string) || ''
  const maxTokens = (config.maxTokens as number) || 4096
  const reasoningEffort = (config.reasoningEffort as string) || ''
  const stream = (config.stream as boolean) ?? true
  const webSearch = (config.webSearch as boolean) ?? false
  const webFetch = (config.webFetch as boolean) ?? false

  // Group models by provider
  const grouped = allModels.reduce(
    (acc, m) => {
      if (!acc[m.provider]) acc[m.provider] = []
      acc[m.provider].push(m)
      return acc
    },
    {} as Record<string, ModelDefinition[]>
  )

  return (
    <div className="space-y-6">
      {/* Model Select */}
      <div className="space-y-2">
        <Label>Model</Label>
        <Select value={modelId} onValueChange={(v) => update('modelId', v)} disabled={isReadOnly}>
          <SelectTrigger>
            <SelectValue placeholder="Select a model" />
          </SelectTrigger>
          <SelectContent>
            {Object.entries(grouped)
              .sort(([a], [b]) => a.localeCompare(b))
              .map(([provider, providerModels]) => (
                <div key={provider}>
                  <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">{provider}</div>
                  {[...providerModels]
                    .sort((a, b) => a.name.localeCompare(b.name))
                    .map((m) => (
                      <SelectItem key={m.id} value={m.id}>
                        {m.name}
                      </SelectItem>
                    ))}
                </div>
              ))}
          </SelectContent>
        </Select>
      </div>

      {/* Max Tokens */}
      <div className="space-y-2">
        <Label>Max Tokens</Label>
        <div className="flex items-center gap-4">
          <Slider
            value={[maxTokens]}
            onValueChange={([v]) => update('maxTokens', v)}
            min={256}
            max={32768}
            step={256}
            className="flex-1"
            disabled={isReadOnly}
          />
          <Input
            type="number"
            value={maxTokens}
            onChange={(e) => update('maxTokens', Number(e.target.value))}
            className="w-24"
            disabled={isReadOnly}
          />
        </div>
      </div>

      {/* Reasoning Effort */}
      <div className="space-y-2">
        <Label>Reasoning Effort</Label>
        <RadioGroup
          value={reasoningEffort}
          onValueChange={(v) => update('reasoningEffort', v)}
          disabled={isReadOnly}
          className="flex items-center gap-4"
        >
          <div className="flex items-center gap-1.5">
            <RadioGroupItem value="" id="reasoning-none" />
            <Label htmlFor="reasoning-none" className="font-normal">None</Label>
          </div>
          <div className="flex items-center gap-1.5">
            <RadioGroupItem value="Low" id="reasoning-low" />
            <Label htmlFor="reasoning-low" className="font-normal">Low</Label>
          </div>
          <div className="flex items-center gap-1.5">
            <RadioGroupItem value="Medium" id="reasoning-medium" />
            <Label htmlFor="reasoning-medium" className="font-normal">Medium</Label>
          </div>
          <div className="flex items-center gap-1.5">
            <RadioGroupItem value="High" id="reasoning-high" />
            <Label htmlFor="reasoning-high" className="font-normal">High</Label>
          </div>
        </RadioGroup>
        <p className="text-xs text-muted-foreground">Extended thinking level for models that support it</p>
      </div>

      {/* Stream */}
      <div className="flex items-center justify-between">
        <Label>Stream</Label>
        <Switch checked={stream} onCheckedChange={(v) => update('stream', v)} disabled={isReadOnly} />
      </div>

      {/* Web Search */}
      <div className="flex items-center justify-between">
        <div>
          <Label>Web Search</Label>
          <p className="text-xs text-muted-foreground">Allow the agent to search the web</p>
        </div>
        <Switch checked={webSearch} onCheckedChange={(v) => update('webSearch', v)} disabled={isReadOnly} />
      </div>

      {/* Web Fetch */}
      <div className="flex items-center justify-between">
        <div>
          <Label>Web Fetch</Label>
          <p className="text-xs text-muted-foreground">Allow the agent to fetch web pages</p>
        </div>
        <Switch checked={webFetch} onCheckedChange={(v) => update('webFetch', v)} disabled={isReadOnly} />
      </div>

    </div>
  )
}
