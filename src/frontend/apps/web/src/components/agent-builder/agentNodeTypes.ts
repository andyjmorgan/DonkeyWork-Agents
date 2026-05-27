export interface AgentNodeTypeInfo {
  type: string
  displayName: string
  icon: string
  color: string
  canDelete: boolean
  /** Which handle on the Model node this connects to */
  targetHandle: 'prompts' | 'tools' | 'agents'
  maxInstances?: number
}

export const agentNodeTypes: Record<string, AgentNodeTypeInfo> = {
  agentModel: {
    type: 'agentModel',
    displayName: 'Model',
    icon: 'brain',
    color: 'blue',
    canDelete: false,
    targetHandle: 'prompts',
    maxInstances: 1,
  },
  agentPrompt: {
    type: 'agentPrompt',
    displayName: 'Prompt',
    icon: 'file-text',
    color: 'emerald',
    canDelete: true,
    targetHandle: 'prompts',
  },
  agentMcpServer: {
    type: 'agentMcpServer',
    displayName: 'MCP Server',
    icon: 'server',
    color: 'purple',
    canDelete: true,
    targetHandle: 'tools',
  },
  agentToolGroup: {
    type: 'agentToolGroup',
    displayName: 'Tool Group',
    icon: 'zap',
    color: 'amber',
    canDelete: true,
    targetHandle: 'tools',
  },
  agentSandbox: {
    type: 'agentSandbox',
    displayName: 'Sandbox',
    icon: 'container',
    color: 'cyan',
    canDelete: true,
    targetHandle: 'tools',
    maxInstances: 1,
  },
  agentSubAgent: {
    type: 'agentSubAgent',
    displayName: 'Sub-Agent',
    icon: 'bot',
    color: 'rose',
    canDelete: true,
    targetHandle: 'agents',
  },
  agentA2aServer: {
    type: 'agentA2aServer',
    displayName: 'A2A Server',
    icon: 'bot',
    color: 'orange',
    canDelete: true,
    targetHandle: 'agents',
  },
}
