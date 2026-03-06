import { useState, useEffect, useMemo, useRef, useLayoutEffect } from 'react'
import { useEditorStore } from '@/store/editor'
import { nodeTypes, models, type NodeConfigSchema, type NodeFieldSchema, type ModelDefinition } from '@/lib/api'
import { FieldRenderer } from './FieldRenderer'
import {
  Input,
  Label,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@donkeywork/ui'
import { Loader2 } from 'lucide-react'

interface SchemaPropertiesPanelProps {
  nodeId: string
  nodeType: string
  /** Optional provider for filtering credentials (e.g., for Model nodes) */
  credentialProvider?: string
  /** Optional model ID for filtering fields by supportedBy */
  modelId?: string
}

export function SchemaPropertiesPanel({
  nodeId,
  nodeType,
  credentialProvider,
  modelId,
}: SchemaPropertiesPanelProps) {
  const [schema, setSchema] = useState<NodeConfigSchema | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedModel, setSelectedModel] = useState<ModelDefinition | null>(null)

  const nodeConfigurations = useEditorStore((state) => state.nodeConfigurations)
  const updateNodeConfig = useEditorStore((state) => state.updateNodeConfig)
  const getReachablePredecessors = useEditorStore((state) => state.getReachablePredecessors)

  const config = nodeConfigurations[nodeId] as unknown as Record<string, unknown> | undefined
  const predecessors = useMemo(() => getReachablePredecessors(nodeId), [nodeId, getReachablePredecessors])
  const prevNodeTypeRef = useRef(nodeType)
  const prevModelIdRef = useRef(modelId)

  // Fetch schema on mount and nodeType change
  useEffect(() => {
    let isMounted = true

    nodeTypes.getSchema(nodeType)
      .then((fetchedSchema) => {
        if (!isMounted) return
        if (fetchedSchema) {
          setSchema(fetchedSchema)
          setError(null)
        } else {
          setError(`No schema found for node type: ${nodeType}`)
        }
      })
      .catch((err) => {
        if (!isMounted) return
        console.error('Failed to fetch schema:', err)
        setError('Failed to load configuration schema')
      })
      .finally(() => {
        if (isMounted) {
          setLoading(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [nodeType])

  // Set loading state synchronously when nodeType changes
  useLayoutEffect(() => {
    if (prevNodeTypeRef.current !== nodeType) {
      setLoading(true)
      setError(null)
      prevNodeTypeRef.current = nodeType
    }
  }, [nodeType])

  // Fetch model definition when modelId changes (for Model nodes)
  useEffect(() => {
    let isMounted = true

    if (modelId) {
      models.get(modelId)
        .then((model) => {
          if (isMounted) setSelectedModel(model)
        })
        .catch((err) => {
          console.error('Failed to fetch model:', err)
          if (isMounted) setSelectedModel(null)
        })
    }

    return () => {
      isMounted = false
    }
  }, [modelId])

  // Clear selected model synchronously when modelId becomes null
  useLayoutEffect(() => {
    if (prevModelIdRef.current !== modelId && !modelId) {
      setSelectedModel(null)
    }
    prevModelIdRef.current = modelId
  }, [modelId])

  const handleFieldChange = (fieldName: string, value: unknown) => {
    updateNodeConfig(nodeId, { [fieldName]: value })
  }

  // Check if a field should be visible based on reliesUpon
  const isFieldVisible = (field: NodeFieldSchema): boolean => {
    if (!field.reliesUpon) return true

    const dependentValue = config?.[field.reliesUpon.fieldName]
    return dependentValue === field.reliesUpon.value
  }

  // Group fields by tab
  const fieldsByTab = useMemo(() => {
    if (!schema) return {}

    const groups: Record<string, NodeFieldSchema[]> = {}

    for (const field of schema.fields) {
      // Skip 'name' field - we render it separately at the top
      if (field.name === 'name') continue

      const tab = field.tab || 'General'
      if (!groups[tab]) {
        groups[tab] = []
      }
      groups[tab].push(field)
    }

    // Sort fields within each tab by order
    for (const tab of Object.keys(groups)) {
      groups[tab].sort((a, b) => a.order - b.order)
    }

    return groups
  }, [schema])

  // Get sorted tabs
  const sortedTabs = useMemo(() => {
    if (!schema) return []

    const tabNames = Object.keys(fieldsByTab)
    if (tabNames.length === 0) return []

    // If there's a schema with tabs, use their order
    if (schema.tabs.length > 0) {
      const tabOrder = new Map(schema.tabs.map((t) => [t.name, t.order]))
      return tabNames.sort((a, b) => {
        const orderA = tabOrder.get(a) ?? 999
        const orderB = tabOrder.get(b) ?? 999
        return orderA - orderB
      })
    }

    // Default: General first, then alphabetically
    return tabNames.sort((a, b) => {
      if (a === 'General') return -1
      if (b === 'General') return 1
      return a.localeCompare(b)
    })
  }, [schema, fieldsByTab])

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
        No configuration schema available for this node type.
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Name field - always shown at top */}
      <div className="space-y-2">
        <Label className="text-sm font-medium">
          Name
          <span className="text-destructive ml-1">*</span>
        </Label>
        <Input
          value={String(config?.name ?? '')}
          onChange={(e) => handleFieldChange('name', e.target.value)}
          placeholder="Enter node name"
        />
        <p className="text-xs text-muted-foreground">
          Unique identifier for this node (lowercase, underscores allowed)
        </p>
      </div>

      {/* Tabbed fields */}
      {sortedTabs.length === 1 ? (
        // Single tab - no tab UI needed
        <div className="space-y-4">
          {fieldsByTab[sortedTabs[0]]?.map((field) =>
            isFieldVisible(field) ? (
              <FieldRenderer
                key={field.name}
                field={field}
                value={config?.[field.name]}
                onChange={(value) => handleFieldChange(field.name, value)}
                predecessors={predecessors}
                credentialProvider={credentialProvider}
                modelId={modelId}
                modelMaxOutputTokens={selectedModel?.max_output_tokens}
              />
            ) : null
          )}
        </div>
      ) : sortedTabs.length > 1 ? (
        // Multiple tabs
        <Tabs defaultValue={sortedTabs[0]} className="w-full">
          <TabsList className="w-full">
            {sortedTabs.map((tab) => (
              <TabsTrigger key={tab} value={tab} className="flex-1">
                {tab}
              </TabsTrigger>
            ))}
          </TabsList>
          {sortedTabs.map((tab) => (
            <TabsContent key={tab} value={tab} className="space-y-4 mt-4">
              {fieldsByTab[tab]?.map((field) =>
                isFieldVisible(field) ? (
                  <FieldRenderer
                    key={field.name}
                    field={field}
                    value={config?.[field.name]}
                    onChange={(value) => handleFieldChange(field.name, value)}
                    predecessors={predecessors}
                    credentialProvider={credentialProvider}
                    modelId={modelId}
                    modelMaxOutputTokens={selectedModel?.max_output_tokens}
                  />
                ) : null
              )}
            </TabsContent>
          ))}
        </Tabs>
      ) : null}
    </div>
  )
}
