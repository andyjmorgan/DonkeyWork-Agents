import { useState, useEffect, useMemo } from 'react'
import { Play, Flag, Globe, Mail, Database, File, Zap, FileText, Clock, Brain, Volume2 } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useEditorStore } from '@/store/editor'
import { models, nodeTypes, type ModelDefinition, type NodeTypeInfo } from '@donkeywork/api-client'
import { OpenAIIcon } from '@/components/icons/OpenAIIcon'
import { AnthropicIcon } from '@/components/icons/AnthropicIcon'
import { GoogleIcon } from '@/components/icons/GoogleIcon'
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@donkeywork/ui'

// Icon mapping for node types
const iconMap: Record<string, LucideIcon> = {
  play: Play,
  flag: Flag,
  globe: Globe,
  mail: Mail,
  database: Database,
  file: File,
  zap: Zap,
  'file-text': FileText,
  clock: Clock,
  brain: Brain,
  'volume-2': Volume2,
}

// Color classes for nodes
const colorClasses: Record<string, { border: string; iconContainer: string; iconColor: string }> = {
  green: {
    border: 'border-green-500/30 bg-green-500/5 hover:border-green-500/50 hover:bg-green-500/10',
    iconContainer: 'bg-gradient-to-br from-green-500 to-emerald-600 shadow-lg shadow-green-500/25',
    iconColor: 'text-white',
  },
  orange: {
    border: 'border-orange-500/30 bg-orange-500/5 hover:border-orange-500/50 hover:bg-orange-500/10',
    iconContainer: 'bg-gradient-to-br from-orange-500 to-red-500 shadow-lg shadow-orange-500/25',
    iconColor: 'text-white',
  },
  blue: {
    border: 'border-blue-500/30 bg-blue-500/5 hover:border-blue-500/50 hover:bg-blue-500/10',
    iconContainer: 'bg-gradient-to-br from-blue-500 to-indigo-600 shadow-lg shadow-blue-500/25',
    iconColor: 'text-white',
  },
  amber: {
    border: 'border-amber-500/30 bg-amber-500/5 hover:border-amber-500/50 hover:bg-amber-500/10',
    iconContainer: 'bg-gradient-to-br from-yellow-500 to-amber-600 shadow-lg shadow-yellow-500/25',
    iconColor: 'text-white',
  },
  purple: {
    border: 'border-purple-500/30 bg-purple-500/5 hover:border-purple-500/50 hover:bg-purple-500/10',
    iconContainer: 'bg-gradient-to-br from-purple-500 to-fuchsia-600 shadow-lg shadow-purple-500/25',
    iconColor: 'text-white',
  },
  cyan: {
    border: 'border-cyan-500/30 bg-cyan-500/5 hover:border-cyan-500/50 hover:bg-cyan-500/10',
    iconContainer: 'bg-gradient-to-br from-cyan-500 to-teal-600 shadow-lg shadow-cyan-500/25',
    iconColor: 'text-white',
  },
  pink: {
    border: 'border-pink-500/30 bg-pink-500/5 hover:border-pink-500/50 hover:bg-pink-500/10',
    iconContainer: 'bg-gradient-to-br from-pink-500 to-rose-600 shadow-lg shadow-pink-500/25',
    iconColor: 'text-white',
  },
  emerald: {
    border: 'border-emerald-500/30 bg-emerald-500/5 hover:border-emerald-500/50 hover:bg-emerald-500/10',
    iconContainer: 'bg-gradient-to-br from-emerald-500 to-green-600 shadow-lg shadow-emerald-500/25',
    iconColor: 'text-white',
  },
  violet: {
    border: 'border-violet-500/30 bg-violet-500/5 hover:border-violet-500/50 hover:bg-violet-500/10',
    iconContainer: 'bg-gradient-to-br from-violet-500 to-purple-600 shadow-lg shadow-violet-500/25',
    iconColor: 'text-white',
  },
}

export function NodePalette() {
  const { nodes } = useEditorStore()
  const [allModels, setAllModels] = useState<ModelDefinition[]>([])
  const [allNodeTypes, setAllNodeTypes] = useState<NodeTypeInfo[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    Promise.all([
      models.list(),
      nodeTypes.list().catch(() => [] as NodeTypeInfo[]) // Fallback to empty if API not available
    ])
      .then(([modelsData, nodeTypesData]) => {
        setAllModels(modelsData)
        setAllNodeTypes(nodeTypesData)
      })
      .catch((error) => {
        console.error('Failed to load data:', error)
      })
      .finally(() => setLoading(false))
  }, [])

  // Group node types by category (excluding Model category since we show models separately)
  const nodeTypesByCategory = useMemo(() => {
    return allNodeTypes
      .filter(nt => nt.category !== 'AI' && nt.category !== 'Audio') // Don't show Model/Audio in generic categories - they have dedicated sections
      .reduce((acc, nt) => {
        if (!acc[nt.category]) {
          acc[nt.category] = []
        }
        acc[nt.category].push(nt)
        return acc
      }, {} as Record<string, NodeTypeInfo[]>)
  }, [allNodeTypes])

  // Group chat models by provider
  const modelsByProvider = allModels
    .filter(model => model.mode === 'Chat')
    .reduce((acc, model) => {
      if (!acc[model.provider]) {
        acc[model.provider] = []
      }
      acc[model.provider].push(model)
      return acc
    }, {} as Record<string, ModelDefinition[]>)

  // Group audio generation models by provider
  const ttsModelsByProvider = allModels
    .filter(model => model.mode === 'AudioGeneration')
    .reduce((acc, model) => {
      if (!acc[model.provider]) {
        acc[model.provider] = []
      }
      acc[model.provider].push(model)
      return acc
    }, {} as Record<string, ModelDefinition[]>)

  const hasStartNode = nodes.some(n => n.data?.nodeType === 'Start')
  const hasEndNode = nodes.some(n => n.data?.nodeType === 'End')

  const handleDragStart = (event: React.DragEvent, nodeType: string, data?: Record<string, unknown>) => {
    event.dataTransfer.effectAllowed = 'move'
    event.dataTransfer.setData('application/reactflow', nodeType)

    if (data) {
      event.dataTransfer.setData('application/json', JSON.stringify(data))
    }
  }

  const getColorClasses = (color?: string) => {
    return colorClasses[color || 'violet'] || colorClasses.violet
  }

  const getIcon = (iconName?: string): LucideIcon => {
    return iconMap[iconName || 'zap'] || Zap
  }

  const getProviderIcon = (provider: string, useWhite = false) => {
    const colorClass = useWhite ? 'text-white' : 'text-blue-500'
    switch (provider) {
      case 'OpenAi':
        return <OpenAIIcon className={cn('h-3.5 w-3.5', colorClass)} />
      case 'Anthropic':
        return <AnthropicIcon className={cn('h-3.5 w-3.5', colorClass)} />
      case 'Google':
        return <GoogleIcon className={cn('h-3.5 w-3.5', colorClass)} />
      default:
        return <OpenAIIcon className={cn('h-3.5 w-3.5', colorClass)} />
    }
  }

  const renderNodeTypeItem = (nodeType: NodeTypeInfo) => {
    const isDisabled =
      (nodeType.type === 'Start' && hasStartNode) ||
      (nodeType.type === 'End' && hasEndNode)

    const Icon = getIcon(nodeType.icon)
    const colors = getColorClasses(nodeType.color)

    return (
      <div
        key={nodeType.type}
        draggable={!isDisabled}
        onDragStart={(e) => handleDragStart(e, 'schemaNode', {
          nodeType: nodeType.type,
          displayName: nodeType.displayName,
          icon: nodeType.icon,
          color: nodeType.color,
          hasInputHandle: nodeType.hasInputHandle,
          hasOutputHandle: nodeType.hasOutputHandle,
          canDelete: nodeType.canDelete,
        })}
        className={cn(
          'flex cursor-move items-center gap-3 rounded-xl border-2 p-3 transition-all',
          colors.border,
          isDisabled && 'cursor-not-allowed opacity-40'
        )}
        title={nodeType.description}
      >
        <div className={cn('flex h-8 w-8 shrink-0 items-center justify-center rounded-lg', colors.iconContainer)}>
          <Icon className={cn('h-4 w-4', colors.iconColor)} />
        </div>
        <div className="min-w-0 flex-1">
          <div className="text-sm font-medium">{nodeType.displayName}</div>
          <div className="truncate text-xs text-muted-foreground">
            {nodeType.description}
          </div>
        </div>
      </div>
    )
  }

  // Category display order
  const categoryOrder = ['Core', 'HTTP', 'Utility', 'Timing']

  const sortedCategories = Object.keys(nodeTypesByCategory).sort((a, b) => {
    const aIndex = categoryOrder.indexOf(a)
    const bIndex = categoryOrder.indexOf(b)
    if (aIndex === -1 && bIndex === -1) return a.localeCompare(b)
    if (aIndex === -1) return 1
    if (bIndex === -1) return -1
    return aIndex - bIndex
  })

  return (
    <div className="h-full overflow-y-auto">
      <Accordion type="multiple" defaultValue={['models', 'tts', ...sortedCategories]} className="space-y-2">
        {/* Models Section - Always show first */}
        <AccordionItem value="models">
          <AccordionTrigger className="text-xs font-semibold uppercase tracking-wider text-muted-foreground hover:no-underline px-2">
            Models
          </AccordionTrigger>
          <AccordionContent className="px-2 pb-4">
            {loading ? (
              <div className="text-sm text-muted-foreground">Loading models...</div>
            ) : (
              <div className="space-y-4">
                {Object.entries(modelsByProvider)
                  .sort(([a], [b]) => a.localeCompare(b))
                  .map(([provider, providerModels]) => (
                  <div key={provider}>
                    <div className="mb-2 flex items-center gap-2">
                      <div className="flex h-4 w-4 items-center justify-center">
                        {getProviderIcon(provider)}
                      </div>
                      <h4 className="text-xs font-medium text-muted-foreground">{provider}</h4>
                    </div>
                    <div className="space-y-1.5">
                      {[...providerModels].sort((a, b) => a.name.localeCompare(b.name)).map((model) => {
                        const colors = getColorClasses('blue')
                        return (
                          <div
                            key={model.id}
                            draggable
                            onDragStart={(e) =>
                              handleDragStart(e, 'schemaNode', {
                                nodeType: 'MultimodalChatModel',
                                displayName: model.name,
                                icon: 'brain',
                                color: 'blue',
                                hasInputHandle: true,
                                hasOutputHandle: true,
                                canDelete: true,
                                provider: model.provider,
                                modelId: model.id
                              })
                            }
                            className={cn(
                              'flex cursor-move items-center gap-2.5 rounded-xl border-2 p-2.5 transition-all',
                              colors.border
                            )}
                            title={`Add ${model.name} node`}
                          >
                            <div className={cn('flex h-7 w-7 shrink-0 items-center justify-center rounded-md', colors.iconContainer)}>
                              {getProviderIcon(model.provider, true)}
                            </div>
                            <div className="min-w-0 flex-1">
                              <div className="truncate text-sm font-medium">{model.name}</div>
                            </div>
                          </div>
                        )
                      })}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </AccordionContent>
        </AccordionItem>

        {/* Text to Speech Section */}
        {Object.keys(ttsModelsByProvider).length > 0 && (
          <AccordionItem value="tts">
            <AccordionTrigger className="text-xs font-semibold uppercase tracking-wider text-muted-foreground hover:no-underline px-2">
              Text to Speech
            </AccordionTrigger>
            <AccordionContent className="px-2 pb-4">
              <div className="space-y-4">
                {Object.entries(ttsModelsByProvider)
                  .sort(([a], [b]) => a.localeCompare(b))
                  .map(([provider, providerModels]) => (
                  <div key={provider}>
                    <div className="mb-2 flex items-center gap-2">
                      <div className="flex h-4 w-4 items-center justify-center">
                        {getProviderIcon(provider)}
                      </div>
                      <h4 className="text-xs font-medium text-muted-foreground">{provider}</h4>
                    </div>
                    <div className="space-y-1.5">
                      {[...providerModels].sort((a, b) => a.name.localeCompare(b.name)).map((model) => {
                        const colors = getColorClasses('pink')
                        const ttsNodeType = model.provider === 'Google' ? 'GeminiTextToSpeech' : 'TextToSpeech'
                        return (
                          <div
                            key={model.id}
                            draggable
                            onDragStart={(e) =>
                              handleDragStart(e, 'schemaNode', {
                                nodeType: ttsNodeType,
                                displayName: model.name,
                                icon: 'volume-2',
                                color: 'pink',
                                hasInputHandle: true,
                                hasOutputHandle: true,
                                canDelete: true,
                                provider: model.provider,
                                modelId: model.id
                              })
                            }
                            className={cn(
                              'flex cursor-move items-center gap-2.5 rounded-xl border-2 p-2.5 transition-all',
                              colors.border
                            )}
                            title={`Add ${model.name} node`}
                          >
                            <div className={cn('flex h-7 w-7 shrink-0 items-center justify-center rounded-md', colors.iconContainer)}>
                              <Volume2 className="h-3.5 w-3.5 text-white" />
                            </div>
                            <div className="min-w-0 flex-1">
                              <div className="truncate text-sm font-medium">{model.name}</div>
                            </div>
                          </div>
                        )
                      })}
                    </div>
                  </div>
                ))}
              </div>
            </AccordionContent>
          </AccordionItem>
        )}

        {/* Node Type Categories */}
        {sortedCategories.map((category) => (
          <AccordionItem key={category} value={category}>
            <AccordionTrigger className="text-xs font-semibold uppercase tracking-wider text-muted-foreground hover:no-underline px-2">
              {category}
            </AccordionTrigger>
            <AccordionContent className="px-2 pb-4">
              <div className="space-y-2">
                {nodeTypesByCategory[category]
                  .sort((a, b) => a.displayName.localeCompare(b.displayName))
                  .map(renderNodeTypeItem)}
              </div>
            </AccordionContent>
          </AccordionItem>
        ))}
      </Accordion>
    </div>
  )
}
