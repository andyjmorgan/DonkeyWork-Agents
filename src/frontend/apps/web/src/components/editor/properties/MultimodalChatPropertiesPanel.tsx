import { useState, useMemo, useCallback, useEffect } from 'react'
import { useEditorStore, type NodeConfig } from '@/store/editor'
import { multimodalChat, models, type NodeConfigSchema, type NodeFieldSchema, type ModelDefinition } from '@donkeywork/api-client'
import { FieldRenderer } from './FieldRenderer'
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  FormField,
  Label,
  Switch,
} from '@donkeywork/ui'
import { Loader2, Settings, Sliders, Brain, Pencil, Check } from 'lucide-react'

interface MultimodalChatPropertiesPanelProps {
  nodeId: string
}

// Icon mapping for tabs
const tabIcons: Record<string, React.ComponentType<{ className?: string }>> = {
  'settings': Settings,
  'sliders': Sliders,
  'brain': Brain,
}

/**
 * Helper to get a nested value from config using dot notation path.
 * e.g., getNestedValue(config, "providerConfig.thinkingBudget")
 */
function getNestedValue(obj: Record<string, unknown> | undefined, path: string): unknown {
  if (!obj) return undefined
  const parts = path.split('.')
  let current: unknown = obj
  for (const part of parts) {
    if (current === null || current === undefined || typeof current !== 'object') {
      return undefined
    }
    current = (current as Record<string, unknown>)[part]
  }
  return current
}

/**
 * Helper to set a nested value in config using dot notation path.
 * Returns a new config object with the nested value set.
 */
function setNestedValue(obj: Record<string, unknown>, path: string, value: unknown): Record<string, unknown> {
  const parts = path.split('.')
  if (parts.length === 1) {
    return { ...obj, [path]: value }
  }

  const result = { ...obj }
  let current: Record<string, unknown> = result

  for (let i = 0; i < parts.length - 1; i++) {
    const part = parts[i]
    if (current[part] === undefined || current[part] === null || typeof current[part] !== 'object') {
      current[part] = {}
    } else {
      current[part] = { ...(current[part] as Record<string, unknown>) }
    }
    current = current[part] as Record<string, unknown>
  }

  current[parts[parts.length - 1]] = value
  return result
}

/**
 * Custom hook for fetching schema data.
 * Returns schema data for the given provider and model.
 */
function useSchemaData(provider: string | undefined, modelId: string | undefined) {
  const [schema, setSchema] = useState<NodeConfigSchema | null>(null)
  const [modelInfo, setModelInfo] = useState<ModelDefinition | null>(null)
  const [fetchKey, setFetchKey] = useState<string>('')

  // Derive loading and error from sync state
  const loading = provider !== undefined && fetchKey !== `${provider}-${modelId}`
  const error = !provider ? 'Provider not set' : null

  useEffect(() => {
    if (!provider) return

    const currentKey = `${provider}-${modelId}`
    if (fetchKey === currentKey) return

    let cancelled = false

    Promise.all([
      multimodalChat.getSchema(provider),
      modelId ? models.get(modelId).catch(() => null) : Promise.resolve(null)
    ])
      .then(([fetchedSchema, fetchedModel]) => {
        if (cancelled) return
        setSchema(fetchedSchema ?? null)
        setModelInfo(fetchedModel ?? null)
        setFetchKey(currentKey)
      })
      .catch((err) => {
        if (cancelled) return
        console.error('Failed to fetch schema:', err)
        setSchema(null)
        setModelInfo(null)
        setFetchKey(currentKey)
      })

    return () => {
      cancelled = true
    }
  }, [provider, modelId, fetchKey])

  return { schema, modelInfo, loading, error }
}

export function MultimodalChatPropertiesPanel({ nodeId }: MultimodalChatPropertiesPanelProps) {
  const [isEditingName, setIsEditingName] = useState(false)
  const [editedName, setEditedName] = useState('')
  const [activeTab, setActiveTab] = useState<string>('Basic')

  const nodeConfigurations = useEditorStore((state) => state.nodeConfigurations)
  const updateNodeConfig = useEditorStore((state) => state.updateNodeConfig)
  const getReachablePredecessors = useEditorStore((state) => state.getReachablePredecessors)

  const config = nodeConfigurations[nodeId] as NodeConfig | undefined
  const predecessors = useMemo(() => getReachablePredecessors(nodeId), [nodeId, getReachablePredecessors])

  const provider = config?.provider as string | undefined
  const modelId = config?.modelId as string | undefined

  // Fetch schema based on provider
  const { schema, modelInfo, loading, error } = useSchemaData(provider, modelId)

  // Get config value helper - handles nested paths
  const getConfigValue = useCallback((fieldName: string): unknown => {
    return getNestedValue(config as Record<string, unknown>, fieldName)
  }, [config])

  // Update config value helper - handles nested paths
  const handleFieldChange = useCallback((fieldName: string, value: unknown) => {
    if (!config) return

    if (fieldName.includes('.')) {
      // Nested path - build the full config update
      const updatedConfig = setNestedValue(config as Record<string, unknown>, fieldName, value)
      // Only send the changed top-level key
      const topLevelKey = fieldName.split('.')[0]
      updateNodeConfig(nodeId, { [topLevelKey]: updatedConfig[topLevelKey] } as Partial<NodeConfig>)
    } else {
      updateNodeConfig(nodeId, { [fieldName]: value } as Partial<NodeConfig>)
    }
  }, [nodeId, config, updateNodeConfig])

  // Check if a field should be visible based on reliesUpon and supportedBy
  const isFieldVisible = useCallback((field: NodeFieldSchema): boolean => {
    // Check reliesUpon dependency
    if (field.reliesUpon) {
      const dependentValue = getConfigValue(field.reliesUpon.fieldName)
      if (dependentValue !== field.reliesUpon.value) {
        return false
      }
    }

    // Check supportedBy - if specified, only show if modelId is in the array
    if (field.supportedBy && field.supportedBy.length > 0 && modelId) {
      if (!field.supportedBy.includes(modelId)) {
        return false
      }
    }

    return true
  }, [getConfigValue, modelId])

  // Build dependency groups: map parent field names to their dependent fields
  const dependencyGroups = useMemo(() => {
    if (!schema) return new Map<string, NodeFieldSchema[]>()

    const groups = new Map<string, NodeFieldSchema[]>()
    const fields = schema.fields ?? []

    for (const field of fields) {
      if (field.reliesUpon) {
        const parentName = field.reliesUpon.fieldName
        if (!groups.has(parentName)) {
          groups.set(parentName, [])
        }
        groups.get(parentName)!.push(field)
      }
    }

    return groups
  }, [schema])

  // Get set of field names that are dependent fields (should not render standalone)
  const dependentFieldNames = useMemo(() => {
    const names = new Set<string>()
    dependencyGroups.forEach((dependents) => {
      dependents.forEach((field) => names.add(field.name))
    })
    return names
  }, [dependencyGroups])

  // Group fields by tab and then by group
  const fieldsByTab = useMemo(() => {
    if (!schema) return {}

    const result: Record<string, { ungrouped: NodeFieldSchema[], groups: Record<string, NodeFieldSchema[]> }> = {}

    // Initialize tabs (handle null/undefined tabs array)
    const tabs = schema.tabs ?? []
    for (const tab of tabs) {
      result[tab.name] = { ungrouped: [], groups: {} }
    }

    // Add fields to their tabs and groups (handle null/undefined fields array)
    // Skip fields that are dependent fields (they'll be rendered in dependency cards)
    const fields = schema.fields ?? []
    for (const field of fields) {
      // Skip 'name', 'provider', 'modelId' fields - we render them separately
      if (field.name === 'name' || field.name === 'provider' || field.name === 'modelId') continue

      // Skip dependent fields - they'll be rendered inside their parent's card
      if (dependentFieldNames.has(field.name)) continue

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

    // Sort fields within each tab/group by order
    for (const tabName of Object.keys(result)) {
      result[tabName].ungrouped.sort((a, b) => a.order - b.order)
      for (const groupName of Object.keys(result[tabName].groups)) {
        result[tabName].groups[groupName].sort((a, b) => a.order - b.order)
      }
    }

    return result
  }, [schema, dependentFieldNames])

  // Get sorted tabs
  const sortedTabs = useMemo(() => {
    if (!schema) return []

    const tabNames = Object.keys(fieldsByTab)
    if (tabNames.length === 0) return []

    // If there's a schema with tabs, use their order
    const schemaTabs = schema.tabs ?? []
    if (schemaTabs.length > 0) {
      const tabOrder = new Map(schemaTabs.map((t) => [t.name, t.order]))
      return tabNames.sort((a, b) => {
        const orderA = tabOrder.get(a) ?? 999
        const orderB = tabOrder.get(b) ?? 999
        return orderA - orderB
      })
    }

    // Default: Basic first, then alphabetically
    return tabNames.sort((a, b) => {
      if (a === 'Basic') return -1
      if (b === 'Basic') return 1
      return a.localeCompare(b)
    })
  }, [schema, fieldsByTab])

  // Name editing handlers
  const handleStartEdit = () => {
    setEditedName(config?.name || '')
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

  // Render a single field
  const renderField = (field: NodeFieldSchema) => {
    if (!isFieldVisible(field)) return null

    const value = getConfigValue(field.name)

    // Render immutable fields as read-only
    if (field.immutable) {
      return (
        <FormField
          key={field.name}
          label={field.label}
          description={field.description}
        >
          <div className="rounded-xl border border-input bg-muted/50 px-3 py-2.5 text-sm text-muted-foreground">
            {String(value ?? field.default ?? 'Not set')}
          </div>
        </FormField>
      )
    }

    return (
      <FieldRenderer
        key={field.name}
        field={field}
        value={value}
        onChange={(newValue) => handleFieldChange(field.name, newValue)}
        predecessors={predecessors}
        credentialProvider={provider}
        modelId={modelId}
        modelMaxOutputTokens={modelInfo?.max_output_tokens}
      />
    )
  }

  // Render a dependency card for a parent toggle field with its dependent fields
  const renderDependencyCard = (parentField: NodeFieldSchema, dependentFields: NodeFieldSchema[]) => {
    const parentValue = getConfigValue(parentField.name)
    const isEnabled = (parentValue as boolean) ?? (parentField.default as boolean) ?? false

    // Filter dependent fields that match the enabled state
    const visibleDependents = dependentFields.filter(field => {
      if (!field.reliesUpon) return false
      return parentValue === field.reliesUpon.value
    })

    return (
      <div
        key={`dependency-${parentField.name}`}
        className="rounded-xl border border-border bg-muted/30 overflow-hidden"
      >
        {/* Header with toggle */}
        <div className="flex items-center justify-between p-4 border-b border-border/50">
          <div className="space-y-0.5">
            <Label className="text-sm font-medium">{parentField.label}</Label>
            {parentField.description && (
              <p className="text-xs text-muted-foreground">{parentField.description}</p>
            )}
          </div>
          <Switch
            checked={isEnabled}
            onCheckedChange={(checked: boolean) => handleFieldChange(parentField.name, checked)}
          />
        </div>

        {/* Dependent fields - shown when enabled */}
        {isEnabled && visibleDependents.length > 0 && (
          <div className="p-4 space-y-4 bg-background/50">
            {visibleDependents.map(renderField)}
          </div>
        )}
      </div>
    )
  }

  // Check if a field is a parent field that has dependencies
  const isParentField = useCallback((field: NodeFieldSchema): boolean => {
    return dependencyGroups.has(field.name)
  }, [dependencyGroups])

  // Render a single field, or if it's a parent field, render it as a dependency card
  const renderFieldOrDependencyCard = (field: NodeFieldSchema) => {
    if (isParentField(field)) {
      const dependents = dependencyGroups.get(field.name) || []
      return renderDependencyCard(field, dependents)
    }
    return renderField(field)
  }

  // Render a group of fields
  const renderGroup = (groupName: string, fields: NodeFieldSchema[]) => {
    const visibleFields = fields.filter(isFieldVisible)
    if (visibleFields.length === 0) return null

    return (
      <div key={groupName} className="space-y-4 rounded-xl border border-border bg-muted/30 p-4">
        <h4 className="text-sm font-medium text-foreground">{groupName}</h4>
        {visibleFields.map(renderFieldOrDependencyCard)}
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
        {visibleUngrouped.map(renderFieldOrDependencyCard)}
        {groupNames.map(groupName => renderGroup(groupName, tabContent.groups[groupName]))}
      </div>
    )
  }

  if (!config) {
    return <div className="p-4 text-sm text-muted-foreground">No configuration found</div>
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-sm text-destructive py-4">
        {error}
      </div>
    )
  }

  if (!schema) {
    return (
      <div className="text-sm text-muted-foreground py-4">
        No configuration schema available for this provider.
      </div>
    )
  }

  return (
    <div className="flex h-full flex-col gap-4 overflow-y-auto p-4">
      {/* Header with editable name */}
      <div className="space-y-2">
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
          Multimodal Chat Model - Calls an LLM with the provided configuration
        </p>
      </div>

      {/* Provider and Model (read-only/immutable) */}
      <div className="space-y-4">
        <FormField
          label="Provider"
          description="Set when dragging from palette"
        >
          <div className="rounded-xl border border-input bg-muted/50 px-3 py-2.5 text-sm text-muted-foreground">
            {provider}
          </div>
        </FormField>

        <FormField
          label="Model"
          description="Set when dragging from palette"
        >
          <div className="rounded-xl border border-input bg-muted/50 px-3 py-2.5 text-sm text-muted-foreground">
            {modelInfo?.name || modelId || 'Not set'}
          </div>
        </FormField>
      </div>

      {/* Schema-driven tabs */}
      {sortedTabs.length > 0 ? (
        sortedTabs.length === 1 ? (
          // Single tab - no tab UI needed
          <div className="space-y-4">
            {renderTabContent(sortedTabs[0])}
          </div>
        ) : (
          // Multiple tabs
          <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1">
            <TabsList className="grid w-full" style={{ gridTemplateColumns: `repeat(${sortedTabs.length}, 1fr)` }}>
              {sortedTabs.map(tabName => {
                const tabDef = (schema.tabs ?? []).find(t => t.name === tabName)
                const IconComponent = tabDef?.icon ? tabIcons[tabDef.icon] : null
                return (
                  <TabsTrigger key={tabName} value={tabName} className="gap-2">
                    {IconComponent && <IconComponent className="h-4 w-4" />}
                    {tabName}
                  </TabsTrigger>
                )
              })}
            </TabsList>
            {sortedTabs.map(tabName => (
              <TabsContent key={tabName} value={tabName} className="mt-4">
                {renderTabContent(tabName)}
              </TabsContent>
            ))}
          </Tabs>
        )
      ) : (
        <div className="text-sm text-muted-foreground">
          No configuration fields available.
        </div>
      )}
    </div>
  )
}
