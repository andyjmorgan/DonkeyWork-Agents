import { useAuthStore } from '@/store/auth'

const BASE_URL = ''

async function fetchWithAuth(url: string, options: RequestInit = {}, retryOnUnauthorized = true): Promise<Response> {
  const { logout, refreshTokens, shouldRefreshToken } = useAuthStore.getState()

  // Proactively refresh token if it's about to expire
  if (shouldRefreshToken() && retryOnUnauthorized) {
    const refreshed = await refreshTokens()
    if (!refreshed) {
      logout()
      window.location.href = '/login'
      throw new Error('Session expired')
    }
  }

  // Get potentially updated token after refresh
  const currentToken = useAuthStore.getState().accessToken

  const response = await fetch(`${BASE_URL}${url}`, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${currentToken}`,
      'Content-Type': 'application/json',
    },
  })

  if (response.status === 401 && retryOnUnauthorized) {
    // Try to refresh the token
    const refreshed = await refreshTokens()

    if (refreshed) {
      // Retry the request with the new token (don't retry again on 401)
      return fetchWithAuth(url, options, false)
    }

    // Refresh failed - logout and redirect
    logout()
    window.location.href = '/login'
    throw new Error('Session expired')
  }

  return response
}

export const api = {
  get: <T>(url: string) => fetchWithAuth(url).then(r => r.json() as Promise<T>),
  post: <T>(url: string, body: unknown) => fetchWithAuth(url, { method: 'POST', body: JSON.stringify(body) }).then(r => r.json() as Promise<T>),
  put: <T>(url: string, body: unknown) => fetchWithAuth(url, { method: 'PUT', body: JSON.stringify(body) }).then(r => r.json() as Promise<T>),
  delete: (url: string) => fetchWithAuth(url, { method: 'DELETE' }),
}

// Types
export interface PaginatedResponse<T> {
  items: T[]
  offset: number
  limit: number
  totalCount: number
}

export interface ApiKeyItem {
  id: string
  name: string
  description?: string
  maskedKey: string
  createdAt: string
}

export interface CreateApiKeyRequest {
  name: string
  description?: string
}

export interface CreateApiKeyResponse {
  id: string
  name: string
  description?: string
  key: string
  createdAt: string
}

export interface GetApiKeyResponse {
  id: string
  name: string
  description?: string
  key: string
  createdAt: string
}

// Model types
export interface ModelDefinition {
  id: string
  name: string
  provider: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
  mode: string
  maxInputTokens: number
  maxOutputTokens: number
  inputCostPerMillionTokens: number
  outputCostPerMillionTokens: number
}

export interface GetModelsResponse {
  models: ModelDefinition[]
}

// API functions
export const apiKeys = {
  list: (offset = 0, limit = 10) => api.get<PaginatedResponse<ApiKeyItem>>(`/api/v1/apikeys?offset=${offset}&limit=${limit}`),
  get: (id: string) => api.get<GetApiKeyResponse>(`/api/v1/apikeys/${id}`),
  create: (data: CreateApiKeyRequest) => api.post<CreateApiKeyResponse>('/api/v1/apikeys', data),
  delete: (id: string) => api.delete(`/api/v1/apikeys/${id}`),
}

export const models = {
  list: async () => {
    const response = await api.get<GetModelsResponse>('/api/v1/models')
    return response.models
  },
  get: (modelId: string) => api.get<ModelDefinition>(`/api/v1/models/${modelId}`),
}

// Credential types
export interface CredentialSummary {
  id: string
  name: string
  provider: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
  createdAt: string
}

export interface CredentialDetail {
  id: string
  provider: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
  name: string
  apiKey: string
  createdAt: string
  lastUsedAt?: string
}

export interface PaginatedCredentialsResponse {
  items: CredentialSummary[]
  offset: number
  limit: number
  totalCount: number
}

export interface CreateCredentialRequest {
  provider: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
  name: string
  apiKey: string
}

export interface CreateCredentialResponse {
  id: string
  provider: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
  name: string
  apiKey: string
  createdAt: string
  lastUsedAt?: string
}

export const credentials = {
  list: async () => {
    const response = await api.get<PaginatedCredentialsResponse>('/api/v1/credentials?offset=0&limit=100')
    return response.items
  },
  get: (id: string) => api.get<CredentialDetail>(`/api/v1/credentials/${id}`),
  create: (data: CreateCredentialRequest) => api.post<CreateCredentialResponse>('/api/v1/credentials', data),
  delete: (id: string) => api.delete(`/api/v1/credentials/${id}`),
}

// Agent types
export interface Agent {
  id: string
  name: string
  description: string
  currentVersionId?: string
  createdAt: string
}

export interface AgentVersion {
  id: string
  agentId: string
  versionNumber: number
  isDraft: boolean
  inputSchema: JSONSchema
  outputSchema?: JSONSchema
  reactFlowData: { nodes: any[], edges: any[], viewport: any }
  nodeConfigurations: Record<string, any>
  createdAt: string
  publishedAt?: string
}

export interface JSONSchema {
  type: string
  properties?: Record<string, unknown>
  required?: string[]
  [key: string]: unknown
}

export interface CreateAgentRequest {
  name: string
  description: string
}

export interface SaveVersionRequest {
  reactFlowData: { nodes: any[], edges: any[], viewport: any }
  nodeConfigurations: Record<string, any>
  inputSchema: JSONSchema
  outputSchema?: JSONSchema | null
  credentialMappings: Array<{ nodeId: string; credentialId: string }>
}

export interface CreateAgentResponse {
  id: string
  name: string
  description: string | null
  versionId: string
  createdAt: string
}

export const agents = {
  list: () => api.get<Agent[]>('/api/v1/agents'),
  create: (data: CreateAgentRequest) => api.post<CreateAgentResponse>('/api/v1/agents', data),
  get: (id: string) => api.get<Agent>(`/api/v1/agents/${id}`),
  update: (id: string, data: CreateAgentRequest) => fetchWithAuth(`/api/v1/agents/${id}`, { method: 'PUT', body: JSON.stringify(data) }).then(r => r.json() as Promise<Agent>),
  delete: (id: string) => api.delete(`/api/v1/agents/${id}`),

  // Versions
  listVersions: (agentId: string) => api.get<AgentVersion[]>(`/api/v1/agents/${agentId}/versions`),
  getVersion: (agentId: string, versionId: string) => api.get<AgentVersion>(`/api/v1/agents/${agentId}/versions/${versionId}`),
  saveVersion: (agentId: string, data: SaveVersionRequest) => api.post<AgentVersion>(`/api/v1/agents/${agentId}/versions`, data),
  publish: (agentId: string) => api.post<AgentVersion>(`/api/v1/agents/${agentId}/versions/publish`, {}),
}

// Execution types
export interface ExecuteAgentRequest {
  input: any
  versionId?: string
}

export interface ExecuteAgentResponse {
  executionId: string
  status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled'
  output?: any
  error?: string
}

export interface ExecutionEvent {
  type: string
  executionId: string
  timestamp: string
  [key: string]: any
}

export interface AgentExecution {
  id: string
  agentId: string
  versionId: string
  status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled'
  input: any
  output?: any
  errorMessage?: string
  startedAt: string
  completedAt?: string
  totalTokensUsed?: number
}

export interface ExecutionLog {
  id: string
  executionId: string
  logLevel: string
  message: string
  details?: string
  nodeId?: string
  source: string
  createdAt: string
}

export interface NodeExecution {
  id: string
  nodeId: string
  nodeType: string
  nodeName: string
  actionType?: string
  status: string
  input?: string
  output?: string
  errorMessage?: string
  startedAt: string
  completedAt?: string
  durationMs?: number
  tokensUsed?: number
  fullResponse?: string
}

// Execution API functions
export const executions = {
  // Execute agent (production)
  execute: async (agentId: string, input: any, versionId?: string) =>
    api.post<ExecuteAgentResponse>(`/api/v1/agents/${agentId}/execute`, { input, versionId }),

  // Test agent (playground)
  test: async (agentId: string, input: any, versionId?: string) =>
    api.post<ExecuteAgentResponse>(`/api/v1/agents/${agentId}/test`, { input, versionId }),

  // Get execution details
  get: (executionId: string) => api.get<AgentExecution>(`/api/v1/agents/executions/${executionId}`),

  // List executions
  list: (agentId?: string, offset = 0, limit = 20) => {
    const params = new URLSearchParams({ offset: offset.toString(), limit: limit.toString() })
    if (agentId) params.append('agentId', agentId)
    return api.get<{ executions: AgentExecution[], totalCount: number }>(
      `/api/v1/agents/executions?${params}`
    )
  },

  // Get execution logs
  getLogs: (executionId: string, offset = 0, limit = 100) => {
    const params = new URLSearchParams({ offset: offset.toString(), limit: limit.toString() })
    return api.get<{ logs: ExecutionLog[], totalCount: number }>(
      `/api/v1/agents/executions/${executionId}/logs?${params}`
    )
  },

  // Get node executions (execution trace)
  getNodeExecutions: (executionId: string, offset = 0, limit = 100) => {
    const params = new URLSearchParams({ offset: offset.toString(), limit: limit.toString() })
    return api.get<{ nodeExecutions: NodeExecution[], totalCount: number }>(
      `/api/v1/agents/executions/${executionId}/nodes?${params}`
    )
  },
}

// OAuth types
export type OAuthProvider = 'Google' | 'Microsoft' | 'GitHub'
export type OAuthTokenStatus = 'Active' | 'ExpiringSoon' | 'Expired'

export interface OAuthProviderConfig {
  id: string
  provider: OAuthProvider
  redirectUri: string
  createdAt: string
  hasToken: boolean
}

export interface OAuthProviderConfigDetail {
  id: string
  provider: OAuthProvider
  clientId: string
  clientSecret: string
  redirectUri: string
  createdAt: string
}

export interface CreateOAuthProviderConfigRequest {
  provider: OAuthProvider
  clientId: string
  clientSecret: string
  redirectUri: string
}

export interface UpdateOAuthProviderConfigRequest {
  clientId?: string
  clientSecret?: string
  redirectUri?: string
}

export interface OAuthToken {
  id: string
  provider: OAuthProvider
  email: string
  externalUserId: string
  status: OAuthTokenStatus
  expiresAt: string
  lastRefreshedAt?: string
  createdAt: string
}

export interface OAuthTokenDetail {
  id: string
  provider: OAuthProvider
  email: string
  externalUserId: string
  accessToken: string
  scopes: string[]
  status: OAuthTokenStatus
  expiresAt: string
  lastRefreshedAt?: string
  createdAt: string
}

export interface GetAuthorizationUrlResponse {
  authorizationUrl: string
  state: string
}

export interface RefreshTokenResponse {
  success: boolean
  expiresAt?: string
  error?: string
}

// OAuth API functions
export const oauth = {
  // Provider configs
  listConfigs: () => api.get<OAuthProviderConfig[]>('/api/v1/oauth/configs'),
  getConfig: (id: string) => api.get<OAuthProviderConfigDetail>(`/api/v1/oauth/configs/${id}`),
  createConfig: (data: CreateOAuthProviderConfigRequest) => api.post<OAuthProviderConfig>('/api/v1/oauth/configs', data),
  updateConfig: (id: string, data: UpdateOAuthProviderConfigRequest) => api.post<OAuthProviderConfigDetail>(`/api/v1/oauth/configs/${id}`, data),
  deleteConfig: (id: string) => api.delete(`/api/v1/oauth/configs/${id}`),

  // OAuth flow
  getAuthorizationUrl: (provider: OAuthProvider) => api.get<GetAuthorizationUrlResponse>(`/api/v1/oauth/${provider}/authorize`),

  // Tokens
  listTokens: () => api.get<OAuthToken[]>('/api/v1/oauth/tokens'),
  getToken: (id: string) => api.get<OAuthTokenDetail>(`/api/v1/oauth/tokens/${id}`),
  refreshToken: (id: string) => api.post<RefreshTokenResponse>(`/api/v1/oauth/tokens/${id}/refresh`, {}),
  disconnectToken: (id: string) => api.delete(`/api/v1/oauth/tokens/${id}`),
}

// Project Management Types
export type ProjectStatus = 'NotStarted' | 'InProgress' | 'OnHold' | 'Completed' | 'Cancelled'
export type MilestoneStatus = 'NotStarted' | 'InProgress' | 'OnHold' | 'Completed' | 'Cancelled'
export type TodoStatus = 'Pending' | 'InProgress' | 'Completed' | 'Cancelled'
export type TodoPriority = 'Low' | 'Medium' | 'High' | 'Critical'

export interface Tag {
  id: string
  name: string
  color?: string
}

export interface TagRequest {
  name: string
  color?: string
}

export interface FileReference {
  id: string
  filePath: string
  displayName?: string
  description?: string
  sortOrder: number
}

export interface FileReferenceRequest {
  filePath: string
  displayName?: string
  description?: string
  sortOrder: number
}

export interface ProjectSummary {
  id: string
  name: string
  description?: string
  status: ProjectStatus
  tags: Tag[]
  milestoneCount: number
  todoCount: number
  completedTodoCount: number
  createdAt: string
  updatedAt?: string
}

export interface ProjectDetails {
  id: string
  name: string
  description?: string
  successCriteria?: string
  status: ProjectStatus
  tags: Tag[]
  fileReferences: FileReference[]
  milestones: MilestoneSummary[]
  todos: Todo[]
  notes: Note[]
  createdAt: string
  updatedAt?: string
}

export interface CreateProjectRequest {
  name: string
  description?: string
  successCriteria?: string
  status?: ProjectStatus
  tags?: TagRequest[]
  fileReferences?: FileReferenceRequest[]
}

export interface UpdateProjectRequest {
  name: string
  description?: string
  successCriteria?: string
  status: ProjectStatus
  tags?: TagRequest[]
  fileReferences?: FileReferenceRequest[]
}

export interface MilestoneSummary {
  id: string
  projectId: string
  name: string
  description?: string
  status: MilestoneStatus
  dueDate?: string
  sortOrder: number
  tags: Tag[]
  todoCount: number
  completedTodoCount: number
  createdAt: string
  updatedAt?: string
}

export interface MilestoneDetails {
  id: string
  projectId: string
  name: string
  description?: string
  successCriteria?: string
  status: MilestoneStatus
  dueDate?: string
  sortOrder: number
  tags: Tag[]
  fileReferences: FileReference[]
  todos: Todo[]
  notes: Note[]
  createdAt: string
  updatedAt?: string
}

export interface CreateMilestoneRequest {
  name: string
  description?: string
  successCriteria?: string
  status?: MilestoneStatus
  dueDate?: string
  sortOrder?: number
  tags?: TagRequest[]
  fileReferences?: FileReferenceRequest[]
}

export interface UpdateMilestoneRequest {
  name: string
  description?: string
  successCriteria?: string
  status: MilestoneStatus
  dueDate?: string
  sortOrder: number
  tags?: TagRequest[]
  fileReferences?: FileReferenceRequest[]
}

export interface Todo {
  id: string
  title: string
  description?: string
  status: TodoStatus
  priority: TodoPriority
  completionNotes?: string
  dueDate?: string
  completedAt?: string
  sortOrder: number
  projectId?: string
  milestoneId?: string
  tags: Tag[]
  createdAt: string
  updatedAt?: string
}

export interface CreateTodoRequest {
  title: string
  description?: string
  status?: TodoStatus
  priority?: TodoPriority
  dueDate?: string
  sortOrder?: number
  projectId?: string
  milestoneId?: string
  tags?: TagRequest[]
}

export interface UpdateTodoRequest {
  title: string
  description?: string
  status: TodoStatus
  priority: TodoPriority
  completionNotes?: string
  dueDate?: string
  sortOrder: number
  projectId?: string
  milestoneId?: string
  tags?: TagRequest[]
}

export interface Note {
  id: string
  title: string
  content?: string
  sortOrder: number
  projectId?: string
  milestoneId?: string
  tags: Tag[]
  createdAt: string
  updatedAt?: string
}

export interface CreateNoteRequest {
  title: string
  content?: string
  sortOrder?: number
  projectId?: string
  milestoneId?: string
  tags?: TagRequest[]
}

export interface UpdateNoteRequest {
  title: string
  content?: string
  sortOrder: number
  projectId?: string
  milestoneId?: string
  tags?: TagRequest[]
}

// Projects API
export const projects = {
  list: () => api.get<ProjectSummary[]>('/api/v1/projects'),
  get: (id: string) => api.get<ProjectDetails>(`/api/v1/projects/${id}`),
  create: (data: CreateProjectRequest) => api.post<ProjectDetails>('/api/v1/projects', data),
  update: (id: string, data: UpdateProjectRequest) => api.put<ProjectDetails>(`/api/v1/projects/${id}`, data),
  delete: (id: string) => api.delete(`/api/v1/projects/${id}`),
}

// Milestones API
export const milestones = {
  list: (projectId: string) => api.get<MilestoneSummary[]>(`/api/v1/projects/${projectId}/milestones`),
  get: (projectId: string, id: string) => api.get<MilestoneDetails>(`/api/v1/projects/${projectId}/milestones/${id}`),
  create: (projectId: string, data: CreateMilestoneRequest) => api.post<MilestoneDetails>(`/api/v1/projects/${projectId}/milestones`, data),
  update: (projectId: string, id: string, data: UpdateMilestoneRequest) => api.put<MilestoneDetails>(`/api/v1/projects/${projectId}/milestones/${id}`, data),
  delete: (projectId: string, id: string) => api.delete(`/api/v1/projects/${projectId}/milestones/${id}`),
}

// Todos API
export const todos = {
  list: () => api.get<Todo[]>('/api/v1/todos'),
  listStandalone: () => api.get<Todo[]>('/api/v1/todos/standalone'),
  get: (id: string) => api.get<Todo>(`/api/v1/todos/${id}`),
  create: (data: CreateTodoRequest) => api.post<Todo>('/api/v1/todos', data),
  update: (id: string, data: UpdateTodoRequest) => api.put<Todo>(`/api/v1/todos/${id}`, data),
  delete: (id: string) => api.delete(`/api/v1/todos/${id}`),
}

// Notes API
export const notes = {
  list: () => api.get<Note[]>('/api/v1/notes'),
  listStandalone: () => api.get<Note[]>('/api/v1/notes/standalone'),
  get: (id: string) => api.get<Note>(`/api/v1/notes/${id}`),
  create: (data: CreateNoteRequest) => api.post<Note>('/api/v1/notes', data),
  update: (id: string, data: UpdateNoteRequest) => api.put<Note>(`/api/v1/notes/${id}`, data),
  delete: (id: string) => api.delete(`/api/v1/notes/${id}`),
}
