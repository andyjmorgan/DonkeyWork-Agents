import { useState, useEffect } from 'react'
import { Play, Flag, Globe, Mail, Database, File, Zap, FileText, Clock } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useEditorStore } from '@/store/editor'
import { models, type ModelDefinition } from '@/lib/api'
import { OpenAIIcon } from '@/components/icons/OpenAIIcon'
import { AnthropicIcon } from '@/components/icons/AnthropicIcon'
import { GoogleIcon } from '@/components/icons/GoogleIcon'
import { useActions } from '@/hooks/useActions'
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion'

interface CoreNode {
  type: 'start' | 'end'
  label: string
  icon: typeof Play
  description: string
  color: string
}

interface UtilityNode {
  type: 'messageFormatter'
  label: string
  icon: typeof FileText
  description: string
  color: string
}

const coreNodes: CoreNode[] = [
  {
    type: 'end',
    label: 'End',
    icon: Flag,
    description: 'Output and completion',
    color: 'orange'
  },
  {
    type: 'start',
    label: 'Start',
    icon: Play,
    description: 'Entry point - validates input',
    color: 'green'
  }
]

const utilityNodes: UtilityNode[] = [
  {
    type: 'messageFormatter',
    label: 'Message Formatter',
    icon: FileText,
    description: 'Format messages with templates',
    color: 'amber'
  }
]

export function NodePalette() {
  const { nodes } = useEditorStore()
  const [allModels, setAllModels] = useState<ModelDefinition[]>([])
  const [loading, setLoading] = useState(true)
  const { actions, actionsByCategory } = useActions()

  // Fetch models from backend
  useEffect(() => {
    models.list()
      .then(setAllModels)
      .catch((error) => {
        console.error('Failed to load models:', error)
      })
      .finally(() => setLoading(false))
  }, [])

  // Group models by provider
  const modelsByProvider = allModels.reduce((acc, model) => {
    if (!acc[model.provider]) {
      acc[model.provider] = []
    }
    acc[model.provider].push(model)
    return acc
  }, {} as Record<string, ModelDefinition[]>)

  // Check if Start/End nodes exist
  const hasStartNode = nodes.some(n => n.type === 'start')
  const hasEndNode = nodes.some(n => n.type === 'end')

  const handleDragStart = (event: React.DragEvent, nodeType: string, data?: any) => {
    event.dataTransfer.effectAllowed = 'move'
    event.dataTransfer.setData('application/reactflow', nodeType)

    // Store additional data for model nodes
    if (data) {
      event.dataTransfer.setData('application/json', JSON.stringify(data))
    }
  }

  const getColorClasses = (color: string) => {
    if (color === 'green') return 'border-green-500/30 bg-green-500/5 hover:border-green-500/50 hover:bg-green-500/10'
    if (color === 'orange') return 'border-orange-500/30 bg-orange-500/5 hover:border-orange-500/50 hover:bg-orange-500/10'
    if (color === 'blue') return 'border-blue-500/30 bg-blue-500/5 hover:border-blue-500/50 hover:bg-blue-500/10'
    if (color === 'amber') return 'border-amber-500/30 bg-amber-500/5 hover:border-amber-500/50 hover:bg-amber-500/10'
    return 'border-border bg-muted/50 hover:bg-muted'
  }

  const getIconContainerClasses = (color: string) => {
    if (color === 'green') return 'bg-gradient-to-br from-green-500 to-emerald-600 shadow-lg shadow-green-500/25'
    if (color === 'orange') return 'bg-gradient-to-br from-orange-500 to-red-500 shadow-lg shadow-orange-500/25'
    if (color === 'blue') return 'bg-gradient-to-br from-blue-500 to-indigo-600 shadow-lg shadow-blue-500/25'
    if (color === 'amber') return 'bg-gradient-to-br from-yellow-500 to-amber-600 shadow-lg shadow-yellow-500/25'
    return 'bg-muted'
  }

  const getIconColorClasses = (color: string) => {
    // With gradient backgrounds, icons should be white
    if (color === 'green' || color === 'orange' || color === 'blue' || color === 'amber') return 'text-white'
    return 'text-muted-foreground'
  }

  const getProviderIcon = (provider: string, useWhite = false) => {
    const colorClass = useWhite ? 'text-white' : 'text-blue-500'
    switch (provider) {
      case 'OpenAI':
        return <OpenAIIcon className={cn('h-3.5 w-3.5', colorClass)} />
      case 'Anthropic':
        return <AnthropicIcon className={cn('h-3.5 w-3.5', colorClass)} />
      case 'Google':
        return <GoogleIcon className={cn('h-3.5 w-3.5', colorClass)} />
      default:
        return <OpenAIIcon className={cn('h-3.5 w-3.5', colorClass)} />
    }
  }

  const getActionIcon = (iconName?: string) => {
    switch (iconName) {
      case 'globe':
        return Globe
      case 'mail':
        return Mail
      case 'database':
        return Database
      case 'file':
        return File
      case 'clock':
        return Clock
      default:
        return Zap
    }
  }

  const getActionColorClasses = (iconName?: string) => {
    switch (iconName) {
      case 'globe':
        return 'border-purple-500/30 bg-purple-500/5 hover:border-purple-500/50 hover:bg-purple-500/10'
      case 'clock':
        return 'border-cyan-500/30 bg-cyan-500/5 hover:border-cyan-500/50 hover:bg-cyan-500/10'
      case 'mail':
        return 'border-pink-500/30 bg-pink-500/5 hover:border-pink-500/50 hover:bg-pink-500/10'
      case 'database':
        return 'border-emerald-500/30 bg-emerald-500/5 hover:border-emerald-500/50 hover:bg-emerald-500/10'
      case 'file':
        return 'border-orange-500/30 bg-orange-500/5 hover:border-orange-500/50 hover:bg-orange-500/10'
      default:
        return 'border-violet-500/30 bg-violet-500/5 hover:border-violet-500/50 hover:bg-violet-500/10'
    }
  }

  const getActionIconContainerClasses = (iconName?: string) => {
    switch (iconName) {
      case 'globe':
        return 'bg-gradient-to-br from-purple-500 to-fuchsia-600 shadow-lg shadow-purple-500/25'
      case 'clock':
        return 'bg-gradient-to-br from-cyan-500 to-teal-600 shadow-lg shadow-cyan-500/25'
      case 'mail':
        return 'bg-gradient-to-br from-pink-500 to-rose-600 shadow-lg shadow-pink-500/25'
      case 'database':
        return 'bg-gradient-to-br from-emerald-500 to-green-600 shadow-lg shadow-emerald-500/25'
      case 'file':
        return 'bg-gradient-to-br from-orange-500 to-amber-600 shadow-lg shadow-orange-500/25'
      default:
        return 'bg-gradient-to-br from-violet-500 to-purple-600 shadow-lg shadow-violet-500/25'
    }
  }

  const getActionIconColorClass = () => {
    // All action icons use white since they're on gradient backgrounds
    return 'text-white'
  }

  return (
    <div className="h-full overflow-y-auto">
      <Accordion type="multiple" defaultValue={['actions', 'core-nodes', 'models', 'utilities']} className="space-y-2">
        {/* Actions Section */}
        <AccordionItem value="actions">
          <AccordionTrigger className="text-xs font-semibold uppercase tracking-wider text-muted-foreground hover:no-underline px-2">
            Actions
          </AccordionTrigger>
          <AccordionContent className="px-2 pb-4">
            {actions.length === 0 ? (
              <div className="text-sm text-muted-foreground">No actions available</div>
            ) : (
              <div className="space-y-4">
                {Object.entries(actionsByCategory)
                  .sort(([a], [b]) => a.localeCompare(b))
                  .map(([category, categoryActions]) => (
                  <div key={category}>
                    <h4 className="mb-2 text-xs font-medium text-muted-foreground">{category}</h4>
                    <div className="space-y-1.5">
                      {[...categoryActions].sort((a, b) => a.displayName.localeCompare(b.displayName)).map((action) => {
                        const Icon = getActionIcon(action.icon)
                        return (
                          <div
                            key={action.actionType}
                            draggable
                            onDragStart={(e) =>
                              handleDragStart(e, 'action', {
                                actionType: action.actionType,
                                displayName: action.displayName,
                                icon: action.icon
                              })
                            }
                            className={cn(
                              'flex cursor-move items-center gap-2.5 rounded-xl border-2 p-2.5 transition-all',
                              getActionColorClasses(action.icon)
                            )}
                            title={action.description || action.displayName}
                          >
                            <div className={cn('flex h-7 w-7 shrink-0 items-center justify-center rounded-md', getActionIconContainerClasses(action.icon))}>
                              <Icon className={cn('h-4 w-4', getActionIconColorClass())} />
                            </div>
                            <div className="min-w-0 flex-1">
                              <div className="truncate text-sm font-medium">{action.displayName}</div>
                              {action.group && (
                                <div className="truncate text-xs text-muted-foreground">
                                  {action.group}
                                </div>
                              )}
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

        {/* Core Nodes Section */}
        <AccordionItem value="core-nodes">
          <AccordionTrigger className="text-xs font-semibold uppercase tracking-wider text-muted-foreground hover:no-underline px-2">
            Core Nodes
          </AccordionTrigger>
          <AccordionContent className="px-2 pb-4">
            <div className="space-y-2">
              {coreNodes.map((node) => {
                const Icon = node.icon
                const isDisabled =
                  (node.type === 'start' && hasStartNode) ||
                  (node.type === 'end' && hasEndNode)

                return (
                  <div
                    key={node.type}
                    draggable={!isDisabled}
                    onDragStart={(e) => handleDragStart(e, node.type)}
                    className={cn(
                      'flex cursor-move items-center gap-3 rounded-xl border-2 p-3 transition-all',
                      getColorClasses(node.color),
                      isDisabled && 'cursor-not-allowed opacity-40'
                    )}
                    title={node.description}
                  >
                    <div className={cn('flex h-8 w-8 shrink-0 items-center justify-center rounded-lg', getIconContainerClasses(node.color))}>
                      <Icon className={cn('h-4 w-4', getIconColorClasses(node.color))} />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="text-sm font-medium">{node.label}</div>
                      <div className="truncate text-xs text-muted-foreground">
                        {node.description}
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          </AccordionContent>
        </AccordionItem>

        {/* Models Section */}
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
                      {[...providerModels].sort((a, b) => a.name.localeCompare(b.name)).map((model) => (
                        <div
                          key={model.id}
                          draggable
                          onDragStart={(e) =>
                            handleDragStart(e, 'model', {
                              provider: model.provider,
                              modelId: model.id,
                              modelName: model.name
                            })
                          }
                          className={cn(
                            'flex cursor-move items-center gap-2.5 rounded-xl border-2 p-2.5 transition-all',
                            getColorClasses('blue')
                          )}
                          title={`Add ${model.name} node`}
                        >
                          <div className={cn('flex h-7 w-7 shrink-0 items-center justify-center rounded-md', getIconContainerClasses('blue'))}>
                            {getProviderIcon(model.provider, true)}
                          </div>
                          <div className="min-w-0 flex-1">
                            <div className="truncate text-sm font-medium">{model.name}</div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </AccordionContent>
        </AccordionItem>

        {/* Utilities Section */}
        <AccordionItem value="utilities">
          <AccordionTrigger className="text-xs font-semibold uppercase tracking-wider text-muted-foreground hover:no-underline px-2">
            Utilities
          </AccordionTrigger>
          <AccordionContent className="px-2 pb-4">
            <div className="space-y-2">
              {[...utilityNodes].sort((a, b) => a.label.localeCompare(b.label)).map((node) => {
                const Icon = node.icon

                return (
                  <div
                    key={node.type}
                    draggable
                    onDragStart={(e) => handleDragStart(e, node.type)}
                    className={cn(
                      'flex cursor-move items-center gap-3 rounded-xl border-2 p-3 transition-all',
                      getColorClasses(node.color)
                    )}
                    title={node.description}
                  >
                    <div className={cn('flex h-8 w-8 shrink-0 items-center justify-center rounded-lg', getIconContainerClasses(node.color))}>
                      <Icon className={cn('h-4 w-4', getIconColorClasses(node.color))} />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="text-sm font-medium">{node.label}</div>
                      <div className="truncate text-xs text-muted-foreground">
                        {node.description}
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          </AccordionContent>
        </AccordionItem>
      </Accordion>
    </div>
  )
}
