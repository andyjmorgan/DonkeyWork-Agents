/**
 * Node Type Metadata Dictionary
 *
 * Centralizes all node type information for consistent rendering across:
 * - Canvas nodes (icons, colors)
 * - Properties panel (titles, descriptions)
 * - Node palette (categories, display names)
 *
 * Icons and display metadata are frontend-only concerns - they are NOT
 * stored in nodeConfigurations or sent to the backend.
 */

import type { LucideIcon } from 'lucide-react'
import {
  Play,
  Square,
  Bot,
  Zap,
  FileText,
  Volume2,
  Database,
  // Provider icons
  Sparkles,
  Brain,
  Globe,
  Cloud,
} from 'lucide-react'
import type { ConfigFieldSchema, TabSchema } from '@donkeywork/api-client'

/** Node categories for palette organization */
export type NodeCategory = 'flow' | 'ai' | 'utility' | 'action'

/** Schema source - where to get the config schema from */
export type SchemaSource =
  | { type: 'local'; schema: LocalNodeSchema }
  | { type: 'backend-model'; endpoint: (modelId: string) => string }
  | { type: 'backend-action'; endpoint: (actionType: string) => string }

/** Local schema definition for simple node types */
export interface LocalNodeSchema {
  tabs: TabSchema[]
  fields: ConfigFieldSchema[]
}

/** Complete metadata for a node type */
export interface NodeTypeMetadata {
  /** Unique type identifier (matches ReactFlow node type) */
  type: string
  /** Human-readable display name */
  displayName: string
  /** Brief description of what this node does */
  description: string
  /** Lucide icon component */
  icon: LucideIcon
  /** Tailwind border color class (e.g., "border-green-500") */
  borderColor: string
  /** Tailwind background color class for icon container (e.g., "bg-green-500/10") */
  iconBgColor: string
  /** Tailwind text color class for icon (e.g., "text-green-500") */
  iconColor: string
  /** Handle color for ReactFlow connections */
  handleColor: string
  /** Category for palette organization */
  category: NodeCategory
  /** Whether this node can be deleted (false for start/end) */
  canDelete: boolean
  /** Schema source for properties panel */
  schemaSource: SchemaSource
  /** Whether to show editable name in properties panel */
  showEditableName: boolean
}

// ============================================================================
// Local Schemas for Simple Node Types
// ============================================================================

/**
 * Start node has no per-node configuration fields.
 * The inputSchema is at the AgentVersion level, not per-node.
 * The properties panel shows agent-level schema editor.
 */
const startNodeSchema: LocalNodeSchema = {
  tabs: [],
  fields: [],
  // Note: inputSchema is stored at agent version level, edited in Start properties
}

/**
 * End node has no per-node configuration fields.
 * The outputSchema is at the AgentVersion level, not per-node.
 * The properties panel shows agent-level schema editor.
 */
const endNodeSchema: LocalNodeSchema = {
  tabs: [],
  fields: [],
  // Note: outputSchema is stored at agent version level, edited in End properties
}

const messageFormatterNodeSchema: LocalNodeSchema = {
  tabs: [{ name: 'Template', order: 0 }],
  fields: [
    {
      name: 'template',
      label: 'Template',
      description: 'Use {{...}} for Scriban expressions. Type {{ for autocomplete.',
      controlType: 'Code',
      propertyType: 'string',
      order: 0,
      tab: 'Template',
      required: true,
      resolvable: true,
    },
  ],
}

// ============================================================================
// Node Type Registry
// ============================================================================

export const NODE_TYPES: Record<string, NodeTypeMetadata> = {
  start: {
    type: 'start',
    displayName: 'Start',
    description: 'Entry point - validates input against schema',
    icon: Play,
    borderColor: 'border-green-500',
    iconBgColor: 'bg-green-500/10',
    iconColor: 'text-green-500',
    handleColor: '!bg-green-500',
    category: 'flow',
    canDelete: false,
    schemaSource: { type: 'local', schema: startNodeSchema },
    showEditableName: false,
  },
  end: {
    type: 'end',
    displayName: 'End',
    description: 'Completion - returns output',
    icon: Square,
    borderColor: 'border-orange-500',
    iconBgColor: 'bg-orange-500/10',
    iconColor: 'text-orange-500',
    handleColor: '!bg-orange-500',
    category: 'flow',
    canDelete: false,
    schemaSource: { type: 'local', schema: endNodeSchema },
    showEditableName: false,
  },
  model: {
    type: 'model',
    displayName: 'Model',
    description: 'Calls an LLM with the provided configuration',
    icon: Bot,
    borderColor: 'border-blue-500',
    iconBgColor: 'bg-blue-500/10',
    iconColor: 'text-blue-500',
    handleColor: '!bg-blue-500',
    category: 'ai',
    canDelete: true,
    schemaSource: {
      type: 'backend-model',
      endpoint: (modelId: string) => `/api/v1/models/${modelId}/config-schema`,
    },
    showEditableName: true,
  },
  action: {
    type: 'action',
    displayName: 'Action',
    description: 'Executes a configured action',
    icon: Zap,
    borderColor: 'border-purple-500',
    iconBgColor: 'bg-purple-500/10',
    iconColor: 'text-purple-500',
    handleColor: '!bg-purple-500',
    category: 'action',
    canDelete: true,
    schemaSource: {
      type: 'backend-action',
      endpoint: (actionType: string) => `/api/v1/actions/${actionType}/schema`,
    },
    showEditableName: true,
  },
  messageFormatter: {
    type: 'messageFormatter',
    displayName: 'Message Formatter',
    description: 'Format messages using Scriban templates',
    icon: FileText,
    borderColor: 'border-cyan-500',
    iconBgColor: 'bg-cyan-500/10',
    iconColor: 'text-cyan-500',
    handleColor: '!bg-cyan-500',
    category: 'utility',
    canDelete: true,
    schemaSource: { type: 'local', schema: messageFormatterNodeSchema },
    showEditableName: true,
  },
  textToSpeech: {
    type: 'textToSpeech',
    displayName: 'Text to Speech',
    description: 'Generate speech audio from text using OpenAI TTS',
    icon: Volume2,
    borderColor: 'border-pink-500',
    iconBgColor: 'bg-pink-500/10',
    iconColor: 'text-pink-500',
    handleColor: '!bg-pink-500',
    category: 'action',
    canDelete: true,
    schemaSource: {
      type: 'backend-action',
      endpoint: () => '/api/v1/node-types',
    },
    showEditableName: true,
  },
  storeAudio: {
    type: 'storeAudio',
    displayName: 'Store Audio',
    description: 'Save generated audio with metadata as a recording',
    icon: Database,
    borderColor: 'border-emerald-500',
    iconBgColor: 'bg-emerald-500/10',
    iconColor: 'text-emerald-500',
    handleColor: '!bg-emerald-500',
    category: 'utility',
    canDelete: true,
    schemaSource: {
      type: 'backend-action',
      endpoint: () => '/api/v1/node-types',
    },
    showEditableName: true,
  },
}

// ============================================================================
// Provider Icons (for Model nodes)
// ============================================================================

export const PROVIDER_ICONS: Record<string, LucideIcon> = {
  OpenAi: Sparkles,
  Anthropic: Brain,
  Google: Globe,
  Azure: Cloud,
}

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Get metadata for a node type
 */
export function getNodeTypeMetadata(type: string): NodeTypeMetadata | undefined {
  return NODE_TYPES[type]
}

/**
 * Get all node types in a category
 */
export function getNodeTypesByCategory(category: NodeCategory): NodeTypeMetadata[] {
  return Object.values(NODE_TYPES).filter((meta) => meta.category === category)
}

/**
 * Get provider icon
 */
export function getProviderIcon(provider: string): LucideIcon {
  return PROVIDER_ICONS[provider] || Bot
}

/**
 * Check if a node type uses local schema (vs backend API)
 */
export function isLocalSchema(type: string): boolean {
  const meta = NODE_TYPES[type]
  return meta?.schemaSource.type === 'local'
}

/**
 * Get local schema for a node type
 */
export function getLocalSchema(type: string): LocalNodeSchema | undefined {
  const meta = NODE_TYPES[type]
  if (meta?.schemaSource.type === 'local') {
    return meta.schemaSource.schema
  }
  return undefined
}
