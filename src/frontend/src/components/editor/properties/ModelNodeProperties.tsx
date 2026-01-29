import { useEffect, useState } from 'react'
import { useEditorStore, type ModelNodeConfig } from '@/store/editor'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { FormField } from '@/components/ui/form-field'
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
import { Plus, Pencil, Check } from 'lucide-react'

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
  const [isEditingName, setIsEditingName] = useState(false)
  const [editedName, setEditedName] = useState('')

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
          Model Node - Calls an LLM with the provided configuration
        </p>
      </div>

      <div className="space-y-4">
        {/* Provider (read-only) */}
        <FormField
          label="Provider"
          description="Set when dragging from palette"
        >
          <div className="rounded-md border border-input bg-background px-3 py-2 text-sm">
            {config.provider}
          </div>
        </FormField>

        {/* Model (read-only) */}
        <FormField
          label="Model"
          description="Set when dragging from palette"
        >
          <div className="rounded-md border border-input bg-background px-3 py-2 text-sm">
            {availableModels.find(m => m.id === config.modelId)?.name || config.modelId || 'Not set'}
          </div>
        </FormField>

        {/* Credential dropdown */}
        <FormField
          label="Credential"
          description={availableCredentials.length === 0 ? `No credentials found for ${config.provider}. Add one to get started.` : undefined}
        >
          <Select
            value={config.credentialId || ''}
            onValueChange={handleCredentialChange}
          >
            <SelectTrigger className="bg-background">
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
        </FormField>

        {/* System Prompt */}
        <FormField
          label="System Prompt"
          htmlFor="systemPrompt"
          description="Instructions that define the AI's behavior and role"
        >
          <Textarea
            id="systemPrompt"
            value={config.systemPrompt || ''}
            onChange={handleSystemPromptChange}
            placeholder="You are a helpful assistant..."
            rows={4}
            className="bg-background"
          />
        </FormField>

        {/* User Message */}
        <FormField
          label="User Message Template"
          htmlFor="userMessage"
          description="Use {{...}} for variables (e.g., {{Input.message}})"
        >
          <Textarea
            id="userMessage"
            value={config.userMessage || ''}
            onChange={handleUserMessageChange}
            placeholder="{{Input}}"
            rows={4}
            className="bg-background"
          />
        </FormField>

        {/* Advanced Settings */}
        <div className="space-y-4 rounded-lg border border-border/50 bg-muted/30 p-4">
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
            <p className="text-xs text-muted-foreground">
              Controls randomness. Lower is more focused, higher is more creative.
            </p>
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
              className="bg-background"
            />
            <p className="text-xs text-muted-foreground">
              Maximum length of the response. Leave empty for model default.
            </p>
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
            <p className="text-xs text-muted-foreground">
              Nucleus sampling. Consider tokens with top_p probability mass.
            </p>
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
