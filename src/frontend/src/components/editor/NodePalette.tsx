import { useState, useEffect } from 'react'
import { Play, Flag, Globe, Mail, Database, File, Zap, FileText, Clock } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useEditorStore } from '@/store/editor'
import { models, type ModelDefinition } from '@/lib/api'
import { OpenAIIcon } from '@/components/icons/OpenAIIcon'
import { AnthropicIcon } from '@/components/icons/AnthropicIcon'
import { GoogleIcon } from '@/components/icons/GoogleIcon'
import { useActions } from '@/hooks/useActions'
import type { ActionNodeSchema } from '@/types/actions'
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
    type: 'start',
    label: 'Start',
    icon: Play,
    description: 'Entry point - validates input',
    color: 'green'
  },
  {
    type: 'end',
    label: 'End',
    icon: Flag,
    description: 'Output and completion',
    color: 'red'
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
  const { actions, actionsByCategory, loading: actionsLoading } = useActions()

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
    if (color === 'green') return 'border-green-500/20 bg-green-500/5 hover:border-green-500/40 hover:bg-green-500/10'
    if (color === 'red') return 'border-red-500/20 bg-red-500/5 hover:border-red-500/40 hover:bg-red-500/10'
    if (color === 'blue') return 'border-blue-500/20 bg-blue-500/5 hover:border-blue-500/40 hover:bg-blue-500/10'
    if (color === 'amber') return 'border-amber-500/20 bg-amber-500/5 hover:border-amber-500/40 hover:bg-amber-500/10'
    return 'border-border bg-muted/50 hover:bg-muted'
  }

  const getIconColorClasses = (color: string) => {
    if (color === 'green') return 'text-green-500'
    if (color === 'red') return 'text-red-500'
    if (color === 'blue') return 'text-blue-500'
    if (color === 'amber') return 'text-amber-500'
    return 'text-muted-foreground'
  }

  const getProviderIcon = (provider: string) => {
    switch (provider) {
      case 'OpenAI':
        return <OpenAIIcon className="h-3.5 w-3.5 text-blue-500" />
      case 'Anthropic':
        return <AnthropicIcon className="h-3.5 w-3.5 text-blue-500" />
      case 'Google':
        return <GoogleIcon className="h-3.5 w-3.5 text-blue-500" />
      default:
        return <OpenAIIcon className="h-3.5 w-3.5 text-blue-500" />
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
        return 'border-purple-500/20 bg-purple-500/5 hover:border-purple-500/40 hover:bg-purple-500/10'
      case 'clock':
        return 'border-cyan-500/20 bg-cyan-500/5 hover:border-cyan-500/40 hover:bg-cyan-500/10'
      case 'mail':
        return 'border-pink-500/20 bg-pink-500/5 hover:border-pink-500/40 hover:bg-pink-500/10'
      case 'database':
        return 'border-emerald-500/20 bg-emerald-500/5 hover:border-emerald-500/40 hover:bg-emerald-500/10'
      case 'file':
        return 'border-orange-500/20 bg-orange-500/5 hover:border-orange-500/40 hover:bg-orange-500/10'
      default:
        return 'border-violet-500/20 bg-violet-500/5 hover:border-violet-500/40 hover:bg-violet-500/10'
    }
  }

  const getActionIconColorClass = (iconName?: string) => {
    switch (iconName) {
      case 'globe':
        return 'text-purple-500'
      case 'clock':
        return 'text-cyan-500'
      case 'mail':
        return 'text-pink-500'
      case 'database':
        return 'text-emerald-500'
      case 'file':
        return 'text-orange-500'
      default:
        return 'text-violet-500'
    }
  }

  return (
    <div className="h-full overflow-y-auto">
      <Accordion type="multiple" defaultValue={['core-nodes', 'utilities', 'actions']} className="space-y-2">
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
                      'flex cursor-move items-center gap-3 rounded-lg border-2 p-3 transition-all',
                      getColorClasses(node.color),
                      isDisabled && 'cursor-not-allowed opacity-40'
                    )}
                    title={node.description}
                  >
                    <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-background/50">
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

        {/* Utilities Section */}
        <AccordionItem value="utilities">
          <AccordionTrigger className="text-xs font-semibold uppercase tracking-wider text-muted-foreground hover:no-underline px-2">
            Utilities
          </AccordionTrigger>
          <AccordionContent className="px-2 pb-4">
            <div className="space-y-2">
              {utilityNodes.map((node) => {
                const Icon = node.icon

                return (
                  <div
                    key={node.type}
                    draggable
                    onDragStart={(e) => handleDragStart(e, node.type)}
                    className={cn(
                      'flex cursor-move items-center gap-3 rounded-lg border-2 p-3 transition-all',
                      getColorClasses(node.color)
                    )}
                    title={node.description}
                  >
                    <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-background/50">
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
                {Object.entries(modelsByProvider).map(([provider, providerModels]) => (
                  <div key={provider}>
                    <div className="mb-2 flex items-center gap-2">
                      <div className="flex h-4 w-4 items-center justify-center">
                        {getProviderIcon(provider)}
                      </div>
                      <h4 className="text-xs font-medium text-muted-foreground">{provider}</h4>
                    </div>
                    <div className="space-y-1.5">
                      {providerModels.map((model) => (
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
                            'flex cursor-move items-center gap-2.5 rounded-lg border-2 p-2.5 transition-all',
                            getColorClasses('blue')
                          )}
                          title={`Add ${model.name} node`}
                        >
                          <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-background/50">
                            {getProviderIcon(model.provider)}
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

        {/* Actions Section */}
        <AccordionItem value="actions">
          <AccordionTrigger className="text-xs font-semibold uppercase tracking-wider text-muted-foreground hover:no-underline px-2">
            Actions
          </AccordionTrigger>
          <AccordionContent className="px-2 pb-4">
            {actionsLoading ? (
              <div className="text-sm text-muted-foreground">Loading actions...</div>
            ) : actions.length === 0 ? (
              <div className="text-sm text-muted-foreground">No actions available</div>
            ) : (
              <div className="space-y-4">
                {Object.entries(actionsByCategory).map(([category, categoryActions]) => (
                  <div key={category}>
                    <h4 className="mb-2 text-xs font-medium text-muted-foreground">{category}</h4>
                    <div className="space-y-1.5">
                      {categoryActions.map((action) => {
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
                              'flex cursor-move items-center gap-2.5 rounded-lg border-2 p-2.5 transition-all',
                              getActionColorClasses(action.icon)
                            )}
                            title={action.description || action.displayName}
                          >
                            <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-background/50">
                              <Icon className={cn('h-4 w-4', getActionIconColorClass(action.icon))} />
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
      </Accordion>
    </div>
  )
}
