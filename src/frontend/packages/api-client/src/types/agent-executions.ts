export interface AgentExecutionSummary {
  id: string
  conversationId: string
  agentType: string
  label: string
  grainKey: string
  status: string
  modelId?: string
  startedAt: string
  completedAt?: string
  durationMs?: number
  inputTokensUsed?: number
  outputTokensUsed?: number
}

export interface AgentExecutionDetail extends AgentExecutionSummary {
  parentGrainKey?: string
  contractSnapshot: string
  input?: string
  output?: string
  errorMessage?: string
}
