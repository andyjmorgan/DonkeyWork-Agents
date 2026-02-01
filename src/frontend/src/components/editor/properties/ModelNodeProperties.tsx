import { useEffect, useState, useMemo, useCallback } from 'react'
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
import { Switch } from '@/components/ui/switch'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { models, credentials } from '@/lib/api'
import type { ModelDefinition, CredentialSummary, ModelConfigSchema, ConfigFieldSchema } from '@/lib/api'
import { CreateCredentialDialog } from '@/components/credentials/CreateCredentialDialog'
import { Plus, Pencil, Check, Trash2, Settings, Brain, Sliders } from 'lucide-react'
import { ScribanEditor } from './ScribanEditor'

interface ModelNodePropertiesProps {
  nodeId: string
}

// Icon mapping for tabs
const tabIcons: Record<string, React.ComponentType<{ className?: string }>> = {
  'settings': Settings,
  'sliders': Sliders,
  'brain': Brain,
}

export function ModelNodeProperties({ nodeId }: ModelNodePropertiesProps) {
  const config = useEditorStore((state) => state.nodeConfigurations[nodeId]) as ModelNodeConfig
  const updateNodeConfig = useEditorStore((state) => state.updateNodeConfig)

  const [allModels, setAllModels] = useState<ModelDefinition[]>([])
  const [allCredentials, setAllCredentials] = useState<CredentialSummary[]>([])
  const [schema, setSchema] = useState<ModelConfigSchema | null>(null)
  const [loading, setLoading] = useState(true)
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [isEditingName, setIsEditingName] = useState(false)
  const [editedName, setEditedName] = useState('')
  const [activeTab, setActiveTab] = useState<string>('Basic')

  // Fetch models, credentials, and schema
  useEffect(() => {
    const fetchData = async () => {
      try {
        const [modelsData, credsData] = await Promise.all([
          models.list(),
          credentials.list()
        ])
        setAllModels(modelsData)
        setAllCredentials(credsData)

        // Fetch schema for the specific model if we have a modelId
        if (config?.modelId) {
          try {
            const schemaData = await models.getConfigSchema(config.modelId)
            setSchema(schemaData)
          } catch (error) {
            console.error('Failed to load model schema:', error)
          }
        }
      } catch (error) {
        console.error('Failed to load models/credentials:', error)
      } finally {
        setLoading(false)
      }
    }
    fetchData()
  }, [config?.modelId])

  // Get config value helper
  const getConfigValue = useCallback((fieldName: string): unknown => {
    return config?.config?.[fieldName]
  }, [config?.config])

  // Update config value helper
  const updateConfigValue = useCallback((fieldName: string, value: unknown) => {
    updateNodeConfig(nodeId, {
      config: {
        ...config?.config,
        [fieldName]: value
      }
    } as Partial<ModelNodeConfig>)
  }, [nodeId, config?.config, updateNodeConfig])

  // Check if a field should be visible based on reliesUpon
  const isFieldVisible = useCallback((field: ConfigFieldSchema): boolean => {
    if (!field.reliesUpon) return true
    const parentValue = getConfigValue(field.reliesUpon.fieldName)
    return parentValue === field.reliesUpon.value
  }, [getConfigValue])

  // Filter credentials by provider
  const availableCredentials = useMemo(() => {
    return allCredentials.filter(c => c.provider === config?.provider)
  }, [allCredentials, config?.provider])

  // Group fields by tab and then by group
  const fieldsByTab = useMemo(() => {
    if (!schema) return {}

    const result: Record<string, { ungrouped: ConfigFieldSchema[], groups: Record<string, ConfigFieldSchema[]> }> = {}

    // Initialize tabs
    for (const tab of schema.tabs) {
      result[tab.name] = { ungrouped: [], groups: {} }
    }

    // Add fields to their tabs and groups
    for (const field of schema.fields) {
      const tabName = field.tab || 'Basic'
      if (!result[tabName]) {
        result[tabName] = { ungrouped: [], groups: {} }
      }

      if (field.group) {
        if (!result[tabName].groups[field.group]) {
          result[tabName].groups[field.group] = []
        }
        result[tabName].groups[field.group].push(field)
      } else {
        result[tabName].ungrouped.push(field)
      }
    }

    return result
  }, [schema])

  if (!config) {
    return <div className="p-4 text-sm text-muted-foreground">No configuration found</div>
  }

  if (loading) {
    return <div className="p-4 text-sm text-muted-foreground">Loading...</div>
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

  const handleCredentialCreated = async (credentialId: string) => {
    try {
      const credsData = await credentials.list()
      setAllCredentials(credsData)
      updateConfigValue('credentialId', credentialId)
    } catch (error) {
      console.error('Failed to refresh credentials:', error)
    }
  }

  // Render a single field based on its schema
  const renderField = (field: ConfigFieldSchema) => {
    if (!isFieldVisible(field)) return null

    const value = getConfigValue(field.name)

    switch (field.controlType) {
      case 'Credential':
        return (
          <FormField
            key={field.name}
            label={field.label}
            description={availableCredentials.length === 0 ? `No credentials found for ${config.provider}. Add one to get started.` : field.description}
          >
            <Select
              value={(value as string) || ''}
              onValueChange={(v) => {
                if (v === '__create_new__') {
                  setIsCreateDialogOpen(true)
                } else {
                  updateConfigValue(field.name, v)
                }
              }}
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
        )

      case 'TextArea':
        // Check if this is an array type (like systemPrompts or userMessages)
        if (field.propertyType.endsWith('[]')) {
          return (
            <StringArrayEditor
              key={field.name}
              nodeId={nodeId}
              label={field.label}
              description={field.description}
              value={(value as string[]) || []}
              onChange={(newValue) => updateConfigValue(field.name, newValue)}
              placeholder={field.name === 'systemPrompts' ? 'You are a helpful assistant...' : '{{Input.message}}'}
              resolvable={field.resolvable}
            />
          )
        }
        // Use ScribanEditor for resolvable single text fields
        if (field.resolvable) {
          return (
            <FormField
              key={field.name}
              label={field.label}
              description={field.description}
            >
              <ScribanEditor
                nodeId={nodeId}
                value={(value as string) || ''}
                onChange={(newValue) => updateConfigValue(field.name, newValue)}
                height="120px"
                placeholder={field.description}
              />
            </FormField>
          )
        }
        return (
          <FormField
            key={field.name}
            label={field.label}
            htmlFor={field.name}
            description={field.description}
          >
            <Textarea
              id={field.name}
              value={(value as string) || ''}
              onChange={(e) => updateConfigValue(field.name, e.target.value)}
              placeholder={field.description}
              rows={4}
              className="bg-background"
            />
          </FormField>
        )

      case 'Slider':
        return (
          <div key={field.name} className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>{field.label}</Label>
              <span className="text-sm text-muted-foreground">
                {((value as number) ?? field.default ?? 0).toFixed(2)}
              </span>
            </div>
            <Slider
              value={[(value as number) ?? (field.default as number) ?? 0]}
              onValueChange={(v) => updateConfigValue(field.name, v[0])}
              min={field.min ?? 0}
              max={field.max ?? 1}
              step={field.step ?? 0.01}
            />
            {field.description && (
              <p className="text-xs text-muted-foreground">{field.description}</p>
            )}
          </div>
        )

      case 'NumberInput':
        return (
          <FormField
            key={field.name}
            label={field.label}
            htmlFor={field.name}
            description={field.description}
          >
            <Input
              id={field.name}
              type="number"
              value={(value as number) ?? ''}
              onChange={(e) => {
                const val = parseFloat(e.target.value)
                updateConfigValue(field.name, isNaN(val) ? undefined : val)
              }}
              placeholder={field.default?.toString() || 'Auto'}
              min={field.min}
              max={field.max}
              className="bg-background"
            />
          </FormField>
        )

      case 'Toggle':
        return (
          <div key={field.name} className="flex items-center justify-between py-2">
            <div className="space-y-0.5">
              <Label>{field.label}</Label>
              {field.description && (
                <p className="text-xs text-muted-foreground">{field.description}</p>
              )}
            </div>
            <Switch
              checked={(value as boolean) ?? (field.default as boolean) ?? false}
              onCheckedChange={(checked: boolean) => updateConfigValue(field.name, checked)}
            />
          </div>
        )

      case 'Select':
        return (
          <FormField
            key={field.name}
            label={field.label}
            description={field.description}
          >
            <Select
              value={(value as string) || (field.default as string) || ''}
              onValueChange={(v) => updateConfigValue(field.name, v)}
            >
              <SelectTrigger className="bg-background">
                <SelectValue placeholder={`Select ${field.label.toLowerCase()}`} />
              </SelectTrigger>
              <SelectContent>
                {field.options?.map(option => (
                  <SelectItem key={option} value={option}>
                    {option}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </FormField>
        )

      case 'TextInput':
      default:
        return (
          <FormField
            key={field.name}
            label={field.label}
            htmlFor={field.name}
            description={field.description}
          >
            <Input
              id={field.name}
              value={(value as string) || ''}
              onChange={(e) => updateConfigValue(field.name, e.target.value)}
              placeholder={field.description}
              className="bg-background"
            />
          </FormField>
        )
    }
  }

  // Render a group of fields
  const renderGroup = (groupName: string, fields: ConfigFieldSchema[]) => {
    const visibleFields = fields.filter(isFieldVisible)
    if (visibleFields.length === 0) return null

    return (
      <div key={groupName} className="space-y-4 rounded-xl border border-border bg-muted/30 p-4">
        <h4 className="text-sm font-medium text-foreground">{groupName}</h4>
        {visibleFields.map(renderField)}
      </div>
    )
  }

  // Render tab content
  const renderTabContent = (tabName: string) => {
    const tabContent = fieldsByTab[tabName]
    if (!tabContent) return null

    const visibleUngrouped = tabContent.ungrouped.filter(isFieldVisible)
    const groupNames = Object.keys(tabContent.groups)

    return (
      <div className="space-y-4">
        {visibleUngrouped.map(renderField)}
        {groupNames.map(groupName => renderGroup(groupName, tabContent.groups[groupName]))}
      </div>
    )
  }

  // Get model display info
  const availableModels = allModels.filter(m => m.provider === config.provider)
  const selectedModel = availableModels.find(m => m.id === config.modelId)

  // Get sorted tabs
  const sortedTabs = schema?.tabs?.slice().sort((a, b) => a.order - b.order) || []

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
                className="flex-1 bg-transparent text-sm font-semibold outline-none border-b-2 border-accent"
              />
              <button
                onClick={handleSaveName}
                className="p-1 hover:bg-accent/20 rounded-lg transition-colors"
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

      {/* Provider and Model (read-only) */}
      <div className="space-y-4">
        <FormField
          label="Provider"
          description="Set when dragging from palette"
        >
          <div className="rounded-xl border border-input bg-muted/50 px-3 py-2.5 text-sm text-muted-foreground">
            {config.provider}
          </div>
        </FormField>

        <FormField
          label="Model"
          description="Set when dragging from palette"
        >
          <div className="rounded-xl border border-input bg-muted/50 px-3 py-2.5 text-sm text-muted-foreground">
            {selectedModel?.name || config.modelId || 'Not set'}
          </div>
        </FormField>
      </div>

      {/* Schema-driven tabs */}
      {schema && sortedTabs.length > 0 ? (
        <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1">
          <TabsList className="grid w-full" style={{ gridTemplateColumns: `repeat(${sortedTabs.length}, 1fr)` }}>
            {sortedTabs.map(tab => {
              const IconComponent = tab.icon ? tabIcons[tab.icon] : null
              return (
                <TabsTrigger key={tab.name} value={tab.name} className="gap-2">
                  {IconComponent && <IconComponent className="h-4 w-4" />}
                  {tab.name}
                </TabsTrigger>
              )
            })}
          </TabsList>
          {sortedTabs.map(tab => (
            <TabsContent key={tab.name} value={tab.name} className="mt-4">
              {renderTabContent(tab.name)}
            </TabsContent>
          ))}
        </Tabs>
      ) : (
        /* Fallback: hardcoded fields when no schema available */
        <FallbackFields
          nodeId={nodeId}
          config={config}
          availableCredentials={availableCredentials}
          onCreateCredential={() => setIsCreateDialogOpen(true)}
          updateConfigValue={updateConfigValue}
        />
      )}

      <CreateCredentialDialog
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen}
        onCreated={handleCredentialCreated}
        defaultProvider={config.provider}
      />
    </div>
  )
}

// String array editor component for system prompts and user messages
interface StringArrayEditorProps {
  nodeId: string
  label: string
  description?: string
  value: string[]
  onChange: (value: string[]) => void
  placeholder?: string
  resolvable?: boolean
}

function StringArrayEditor({ nodeId, label, description, value, onChange, placeholder, resolvable }: StringArrayEditorProps) {
  const addItem = () => {
    onChange([...value, ''])
  }

  const removeItem = (index: number) => {
    onChange(value.filter((_, i) => i !== index))
  }

  const updateItem = (index: number, newValue: string) => {
    const updated = [...value]
    updated[index] = newValue
    onChange(updated)
  }

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <Label>{label}</Label>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={addItem}
          className="h-7 px-2 text-xs"
        >
          <Plus className="h-3.5 w-3.5 mr-1" />
          Add
        </Button>
      </div>
      {description && (
        <p className="text-xs text-muted-foreground">{description}</p>
      )}
      {value.length === 0 ? (
        <div
          className="rounded-xl border border-dashed border-border bg-muted/30 p-4 text-center text-sm text-muted-foreground cursor-pointer hover:border-accent/50 transition-colors"
          onClick={addItem}
        >
          Click to add {label.toLowerCase()}
        </div>
      ) : (
        <div className="space-y-2">
          {value.map((item, index) => (
            <div key={index} className="flex gap-2">
              {resolvable ? (
                <div className="flex-1">
                  <ScribanEditor
                    nodeId={nodeId}
                    value={item}
                    onChange={(newValue) => updateItem(index, newValue)}
                    height="100px"
                    placeholder={placeholder}
                  />
                </div>
              ) : (
                <Textarea
                  value={item}
                  onChange={(e) => updateItem(index, e.target.value)}
                  placeholder={placeholder}
                  rows={3}
                  className="flex-1 bg-background"
                />
              )}
              <Button
                type="button"
                variant="ghost"
                size="icon"
                onClick={() => removeItem(index)}
                className="h-8 w-8 shrink-0 text-muted-foreground hover:text-destructive"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// Fallback fields when schema is not available
interface FallbackFieldsProps {
  nodeId: string
  config: ModelNodeConfig
  availableCredentials: CredentialSummary[]
  onCreateCredential: () => void
  updateConfigValue: (fieldName: string, value: unknown) => void
}

function FallbackFields({ nodeId, config, availableCredentials, onCreateCredential, updateConfigValue }: FallbackFieldsProps) {
  return (
    <div className="space-y-4">
      {/* Credential dropdown */}
      <FormField
        label="Credential"
        description={availableCredentials.length === 0 ? `No credentials found for ${config.provider}. Add one to get started.` : undefined}
      >
        <Select
          value={(config.config?.credentialId as string) || ''}
          onValueChange={(v) => {
            if (v === '__create_new__') {
              onCreateCredential()
            } else {
              updateConfigValue('credentialId', v)
            }
          }}
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

      {/* System Prompts */}
      <StringArrayEditor
        nodeId={nodeId}
        label="System Prompts"
        description="Instructions that define the AI's behavior and role"
        value={(config.config?.systemPrompts as string[]) || []}
        onChange={(value) => updateConfigValue('systemPrompts', value)}
        placeholder="You are a helpful assistant..."
        resolvable={true}
      />

      {/* User Messages */}
      <StringArrayEditor
        nodeId={nodeId}
        label="User Messages"
        description="Use {{...}} for variables (e.g., {{Input.message}})"
        value={(config.config?.userMessages as string[]) || []}
        onChange={(value) => updateConfigValue('userMessages', value)}
        placeholder="{{Input.message}}"
        resolvable={true}
      />

      {/* Advanced Settings */}
      <div className="space-y-4 rounded-xl border border-border bg-muted/30 p-4">
        <h4 className="text-sm font-medium text-foreground">Advanced Settings</h4>

        {/* Temperature */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label>Temperature</Label>
            <span className="text-sm text-muted-foreground">
              {((config.config?.temperature as number) ?? 1).toFixed(2)}
            </span>
          </div>
          <Slider
            value={[(config.config?.temperature as number) ?? 1]}
            onValueChange={(v) => updateConfigValue('temperature', v[0])}
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
          <Label htmlFor="maxOutputTokens">Max Output Tokens</Label>
          <Input
            id="maxOutputTokens"
            type="number"
            value={(config.config?.maxOutputTokens as number) ?? ''}
            onChange={(e) => {
              const val = parseInt(e.target.value)
              updateConfigValue('maxOutputTokens', isNaN(val) ? undefined : val)
            }}
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
              {((config.config?.topP as number) ?? 1).toFixed(2)}
            </span>
          </div>
          <Slider
            value={[(config.config?.topP as number) ?? 1]}
            onValueChange={(v) => updateConfigValue('topP', v[0])}
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
  )
}
