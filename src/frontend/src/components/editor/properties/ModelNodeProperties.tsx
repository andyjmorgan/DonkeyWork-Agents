import { useEffect, useState } from 'react'
import { useEditorStore, type ModelNodeConfig } from '@/store/editor'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Slider } from '@/components/ui/slider'
import { models, credentials } from '@/lib/api'
import type { ModelDefinition, CredentialSummary } from '@/lib/api'
import { CreateCredentialDialog } from '@/components/credentials/CreateCredentialDialog'
import { Plus } from 'lucide-react'

interface ModelNodePropertiesProps {
  nodeId: string
}

export function ModelNodeProperties({ nodeId }: ModelNodePropertiesProps) {
  const config = useEditorStore((state) => state.nodeConfigurations[nodeId]) as ModelNodeConfig
  const updateNodeConfig = useEditorStore((state) => state.updateNodeConfig)

  const [allModels, setAllModels] = useState<ModelDefinition[]>([])
  const [allCredentials, setAllCredentials] = useState<CredentialSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)

  // Fetch models and credentials
  useEffect(() => {
    Promise.all([
      models.list(),
      credentials.list()
    ]).then(([modelsData, credsData]) => {
      setAllModels(modelsData)
      setAllCredentials(credsData)
      setLoading(false)
    }).catch((error) => {
      console.error('Failed to load models/credentials:', error)
      setLoading(false)
    })
  }, [])

  if (!config) {
    return <div className="p-4 text-sm text-muted-foreground">No configuration found</div>
  }

  if (loading) {
    return <div className="p-4 text-sm text-muted-foreground">Loading...</div>
  }

  // Filter models by provider
  const availableModels = allModels.filter(m => m.provider === config.provider)

  // Filter credentials by provider
  const availableCredentials = allCredentials.filter(c => c.provider === config.provider)

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    updateNodeConfig(nodeId, { name: e.target.value })
  }

  const handleCredentialChange = (value: string) => {
    if (value === '__create_new__') {
      setIsCreateDialogOpen(true)
    } else {
      updateNodeConfig(nodeId, { credentialId: value })
    }
  }

  const handleSystemPromptChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    updateNodeConfig(nodeId, { systemPrompt: e.target.value })
  }

  const handleUserMessageChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    updateNodeConfig(nodeId, { userMessage: e.target.value })
  }

  const handleTemperatureChange = (value: number[]) => {
    updateNodeConfig(nodeId, { temperature: value[0] })
  }

  const handleMaxTokensChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = parseInt(e.target.value)
    updateNodeConfig(nodeId, { maxTokens: isNaN(val) ? undefined : val })
  }

  const handleTopPChange = (value: number[]) => {
    updateNodeConfig(nodeId, { topP: value[0] })
  }

  const handleCredentialCreated = async (credentialId: string) => {
    // Refetch credentials to include the new one
    try {
      const credsData = await credentials.list()
      setAllCredentials(credsData)
      // Automatically select the newly created credential
      updateNodeConfig(nodeId, { credentialId })
    } catch (error) {
      console.error('Failed to refresh credentials:', error)
    }
  }

  return (
    <div className="flex h-full flex-col gap-4 overflow-y-auto p-4">
      <div className="space-y-2">
        <h3 className="text-sm font-semibold">Model Node</h3>
        <p className="text-xs text-muted-foreground">
          Calls an LLM with the provided configuration
        </p>
      </div>

      <div className="space-y-4">
        {/* Name field */}
        <div className="space-y-2">
          <Label htmlFor="name">Name</Label>
          <Input
            id="name"
            value={config.name}
            onChange={handleNameChange}
            placeholder="model"
          />
        </div>

        {/* Provider (read-only) */}
        <div className="space-y-2">
          <Label>Provider</Label>
          <div className="rounded-md border border-input bg-muted/50 px-3 py-2 text-sm">
            {config.provider}
          </div>
          <p className="text-xs text-muted-foreground">
            Set when dragging from palette
          </p>
        </div>

        {/* Model (read-only) */}
        <div className="space-y-2">
          <Label>Model</Label>
          <div className="rounded-md border border-input bg-muted/50 px-3 py-2 text-sm">
            {availableModels.find(m => m.id === config.modelId)?.name || config.modelId || 'Not set'}
          </div>
          <p className="text-xs text-muted-foreground">
            Set when dragging from palette
          </p>
        </div>

        {/* Credential dropdown */}
        <div className="space-y-2">
          <Label>Credential</Label>
          <Select
            value={config.credentialId || ''}
            onValueChange={handleCredentialChange}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select credential" />
            </SelectTrigger>
            <SelectContent>
              {availableCredentials.map(cred => (
                <SelectItem key={cred.id} value={cred.id}>
                  {cred.name}
                </SelectItem>
              ))}
              <SelectItem value="__create_new__" className="text-blue-500">
                <div className="flex items-center gap-2">
                  <Plus className="h-3.5 w-3.5" />
                  <span>Add New Credential</span>
                </div>
              </SelectItem>
            </SelectContent>
          </Select>
          {availableCredentials.length === 0 && (
            <p className="text-xs text-muted-foreground">
              No credentials found for {config.provider}. Add one to get started.
            </p>
          )}
        </div>

        {/* System Prompt */}
        <div className="space-y-2">
          <Label htmlFor="systemPrompt">System Prompt</Label>
          <Textarea
            id="systemPrompt"
            value={config.systemPrompt || ''}
            onChange={handleSystemPromptChange}
            placeholder="You are a helpful assistant..."
            rows={4}
          />
        </div>

        {/* User Message */}
        <div className="space-y-2">
          <Label htmlFor="userMessage">User Message Template</Label>
          <Textarea
            id="userMessage"
            value={config.userMessage || ''}
            onChange={handleUserMessageChange}
            placeholder="{{input}}"
            rows={4}
          />
          <p className="text-xs text-muted-foreground">
            Use {'{{'}...{'}'} for variables (e.g., {'{{input}}'})
          </p>
        </div>

        {/* Advanced Settings */}
        <div className="space-y-4 rounded-lg border border-border p-4">
          <h4 className="text-sm font-medium">Advanced Settings</h4>

          {/* Temperature */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>Temperature</Label>
              <span className="text-sm text-muted-foreground">
                {config.temperature?.toFixed(2) || '1.00'}
              </span>
            </div>
            <Slider
              value={[config.temperature || 1]}
              onValueChange={handleTemperatureChange}
              min={0}
              max={2}
              step={0.01}
            />
          </div>

          {/* Max Tokens */}
          <div className="space-y-2">
            <Label htmlFor="maxTokens">Max Tokens</Label>
            <Input
              id="maxTokens"
              type="number"
              value={config.maxTokens || ''}
              onChange={handleMaxTokensChange}
              placeholder="Auto"
            />
          </div>

          {/* Top P */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>Top P</Label>
              <span className="text-sm text-muted-foreground">
                {config.topP?.toFixed(2) || '1.00'}
              </span>
            </div>
            <Slider
              value={[config.topP || 1]}
              onValueChange={handleTopPChange}
              min={0}
              max={1}
              step={0.01}
            />
          </div>
        </div>
      </div>

      <CreateCredentialDialog
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen}
        onCreated={handleCredentialCreated}
        defaultProvider={config.provider}
      />
    </div>
  )
}
