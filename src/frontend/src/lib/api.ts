import { fetchWithAuth as baseFetchWithAuth } from '@/lib/fetchWithAuth'

const BASE_URL = ''

async function fetchWithAuth(url: string, options: RequestInit = {}, retryOnUnauthorized = true): Promise<Response> {
  return baseFetchWithAuth(
    `${BASE_URL}${url}`,
    {
      ...options,
      headers: {
        ...options.headers,
        'Content-Type': 'application/json',
      },
    },
    retryOnUnauthorized
  )
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
  max_input_tokens: number
  max_output_tokens: number
  input_cost_per_million_tokens: number
  output_cost_per_million_tokens: number
}

export interface GetModelsResponse {
  models: ModelDefinition[]
}

// Model config schema types
export type FieldControlType =
  | 'Slider'
  | 'NumberInput'
  | 'TextInput'
  | 'TextArea'
  | 'Select'
  | 'Toggle'
  | 'Credential'
  | 'Code'
  | 'Json'
  | 'KeyValueList'

export interface TabSchema {
  name: string
  order: number
  icon?: string
}

export interface ReliesUponSchema {
  fieldName: string
  value: unknown
  requiredWhenEnabled: boolean
}

export interface FieldDependency {
  field: string
  value: unknown
}

export interface ConfigFieldSchema {
  name: string
  label: string
  description?: string
  controlType: FieldControlType
  propertyType: string
  order: number
  group?: string
  tab?: string
  required: boolean
  resolvable: boolean
  min?: number
  max?: number
  step?: number
  default?: unknown
  options?: string[]
  dependsOn?: FieldDependency[]
  reliesUpon?: ReliesUponSchema
  credentialTypes?: string[]
}

export interface ModelConfigSchema {
  modelId: string
  modelName: string
  provider: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
  mode: string
  tabs: TabSchema[]
  fields: ConfigFieldSchema[]
}

// API functions
export const apiKeys = {
  list: (offset = 0, limit = 10) => api.get<PaginatedResponse<ApiKeyItem>>(`/api/v1/apikeys?offset=${offset}&limit=${limit}`),
  get: (id: string) => api.get<GetApiKeyResponse>(`/api/v1/apikeys/${id}`),
  create: (data: CreateApiKeyRequest) => api.post<CreateApiKeyResponse>('/api/v1/apikeys', data),
  delete: (id: string) => api.delete(`/api/v1/apikeys/${id}`),
}

export interface GetConfigSchemasResponse {
  schemas: Record<string, ModelConfigSchema>
}

export const models = {
  list: async () => {
    const response = await api.get<GetModelsResponse>('/api/v1/models')
    return response.models
  },
  get: (modelId: string) => api.get<ModelDefinition>(`/api/v1/models/${modelId}`),
  getConfigSchema: (modelId: string) => api.get<ModelConfigSchema>(`/api/v1/models/${modelId}/config-schema`),
  getAllConfigSchemas: async () => {
    const response = await api.get<GetConfigSchemasResponse>('/api/v1/models/config-schemas')
    return response.schemas
  },
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

// Orchestration types
export interface Orchestration {
  id: string
  name: string
  description: string
  currentVersionId?: string
  createdAt: string
}

// Interface configuration types (polymorphic, 1:1 - each orchestration has exactly one interface)
export type InterfaceType = 'DirectInterfaceConfig' | 'ChatInterfaceConfig' | 'McpInterfaceConfig' | 'A2aInterfaceConfig' | 'WebhookInterfaceConfig'

export interface InterfaceConfigBase {
  type: InterfaceType
  name?: string
  description?: string
}

export interface DirectInterfaceConfig extends InterfaceConfigBase {
  type: 'DirectInterfaceConfig'
}

export interface ChatInterfaceConfig extends InterfaceConfigBase {
  type: 'ChatInterfaceConfig'
}

export interface McpInterfaceConfig extends InterfaceConfigBase {
  type: 'McpInterfaceConfig'
  toolName?: string
}

export interface A2aInterfaceConfig extends InterfaceConfigBase {
  type: 'A2aInterfaceConfig'
  agentId?: string
  capabilities?: string[]
}

export interface WebhookInterfaceConfig extends InterfaceConfigBase {
  type: 'WebhookInterfaceConfig'
  allowedMethods?: string[]
  requireSignature?: boolean
}

export type InterfaceConfig = DirectInterfaceConfig | ChatInterfaceConfig | McpInterfaceConfig | A2aInterfaceConfig | WebhookInterfaceConfig

// ReactFlow types - using 'any' to maintain compatibility with ReactFlow library types
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type ReactFlowData = any

export interface OrchestrationVersion {
  id: string
  orchestrationId: string
  versionNumber: number
  isDraft: boolean
  inputSchema: JSONSchema
  outputSchema?: JSONSchema
  reactFlowData: ReactFlowData
  nodeConfigurations: Record<string, Record<string, unknown>>
  interface: InterfaceConfig
  createdAt: string
  publishedAt?: string
}

export interface JSONSchema {
  type: string
  properties?: Record<string, unknown>
  required?: string[]
  [key: string]: unknown
}

export interface CreateOrchestrationRequest {
  name: string
  description: string
}

export interface SaveVersionRequest {
  reactFlowData: ReactFlowData
  nodeConfigurations: Record<string, Record<string, unknown>>
  inputSchema: JSONSchema
  outputSchema?: JSONSchema | null
  credentialMappings: Array<{ nodeId: string; credentialId: string }>
  interface: InterfaceConfig
}

export interface CreateOrchestrationResponse {
  id: string
  name: string
  description: string | null
  versionId: string
  createdAt: string
}

// Chat-enabled orchestration type (for agent selector)
export interface ChatEnabledOrchestration {
  id: string
  name: string
  description?: string
}

export const orchestrations = {
  list: () => api.get<Orchestration[]>('/api/v1/orchestrations'),
  create: (data: CreateOrchestrationRequest) => api.post<CreateOrchestrationResponse>('/api/v1/orchestrations', data),
  get: (id: string) => api.get<Orchestration>(`/api/v1/orchestrations/${id}`),
  update: (id: string, data: CreateOrchestrationRequest) => fetchWithAuth(`/api/v1/orchestrations/${id}`, { method: 'PUT', body: JSON.stringify(data) }).then(r => r.json() as Promise<Orchestration>),
  delete: (id: string) => api.delete(`/api/v1/orchestrations/${id}`),

  // Chat-enabled orchestrations (for agent selector)
  listChatEnabled: () => api.get<ChatEnabledOrchestration[]>('/api/v1/orchestrations/chat-enabled'),

  // Versions
  listVersions: (orchestrationId: string) => api.get<OrchestrationVersion[]>(`/api/v1/orchestrations/${orchestrationId}/versions`),
  getVersion: (orchestrationId: string, versionId: string) => api.get<OrchestrationVersion>(`/api/v1/orchestrations/${orchestrationId}/versions/${versionId}`),
  saveVersion: (orchestrationId: string, data: SaveVersionRequest) => api.post<OrchestrationVersion>(`/api/v1/orchestrations/${orchestrationId}/versions`, data),
  publish: (orchestrationId: string) => api.post<OrchestrationVersion>(`/api/v1/orchestrations/${orchestrationId}/versions/publish`, {}),
}

// Execution types
export interface ExecuteOrchestrationRequest {
  input: unknown
  versionId?: string
}

export interface ExecuteOrchestrationResponse {
  executionId: string
  status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled'
  output?: unknown
  error?: string
}

export interface ExecutionEvent {
  type: string
  executionId: string
  timestamp: string
  [key: string]: unknown
}

export interface OrchestrationExecution {
  id: string
  orchestrationId: string
  versionId: string
  status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled'
  input: unknown
  output?: unknown
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
  // Execute orchestration (production)
  execute: async (orchestrationId: string, input: unknown, versionId?: string) =>
    api.post<ExecuteOrchestrationResponse>(`/api/v1/orchestrations/${orchestrationId}/execute`, { input, versionId }),

  // Test orchestration (playground)
  test: async (orchestrationId: string, input: unknown, versionId?: string) =>
    api.post<ExecuteOrchestrationResponse>(`/api/v1/orchestrations/${orchestrationId}/test`, { input, versionId }),

  // Get execution details
  get: (executionId: string) => api.get<OrchestrationExecution>(`/api/v1/orchestrations/executions/${executionId}`),

  // List executions
  list: (orchestrationId?: string, offset = 0, limit = 20) => {
    const params = new URLSearchParams({ offset: offset.toString(), limit: limit.toString() })
    if (orchestrationId) params.append('orchestrationId', orchestrationId)
    return api.get<{ executions: OrchestrationExecution[], totalCount: number }>(
      `/api/v1/orchestrations/executions?${params}`
    )
  },

  // Get execution logs
  getLogs: (executionId: string, offset = 0, limit = 100) => {
    const params = new URLSearchParams({ offset: offset.toString(), limit: limit.toString() })
    return api.get<{ logs: ExecutionLog[], totalCount: number }>(
      `/api/v1/orchestrations/executions/${executionId}/logs?${params}`
    )
  },

  // Get node executions (execution trace)
  getNodeExecutions: (executionId: string, offset = 0, limit = 100) => {
    const params = new URLSearchParams({ offset: offset.toString(), limit: limit.toString() })
    return api.get<{ nodeExecutions: NodeExecution[], totalCount: number }>(
      `/api/v1/orchestrations/executions/${executionId}/nodes?${params}`
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
  content?: string
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
  content?: string
  successCriteria?: string
  status?: ProjectStatus
  tags?: TagRequest[]
  fileReferences?: FileReferenceRequest[]
}

export interface UpdateProjectRequest {
  name: string
  content?: string
  successCriteria?: string
  status: ProjectStatus
  tags?: TagRequest[]
  fileReferences?: FileReferenceRequest[]
}

export interface MilestoneSummary {
  id: string
  projectId: string
  name: string
  content?: string
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
  content?: string
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
  content?: string
  successCriteria?: string
  status?: MilestoneStatus
  dueDate?: string
  sortOrder?: number
  tags?: TagRequest[]
  fileReferences?: FileReferenceRequest[]
}

export interface UpdateMilestoneRequest {
  name: string
  content?: string
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

// Node Types - Schema API
export type NodeControlType =
  | 'Text'
  | 'TextArea'
  | 'TextAreaList'
  | 'Number'
  | 'Slider'
  | 'Select'
  | 'Toggle'
  | 'Code'
  | 'Json'
  | 'KeyValueList'
  | 'Credential'

export interface NodeFieldSchema {
  name: string
  label: string
  description?: string
  propertyType: string
  controlType: NodeControlType
  order: number
  tab?: string
  group?: string
  required: boolean
  supportsVariables: boolean
  immutable?: boolean
  placeholder?: string
  default?: unknown
  min?: number
  max?: number
  step?: number
  options?: string[]
  supportedBy?: string[]
  reliesUpon?: {
    fieldName: string
    value: unknown
    requiredWhenEnabled: boolean
  }
}

export interface NodeTabSchema {
  name: string
  order: number
  icon?: string
}

export interface NodeConfigSchema {
  nodeType: string
  tabs: NodeTabSchema[]
  fields: NodeFieldSchema[]
}

export interface NodeTypeInfo {
  type: string
  displayName: string
  description: string
  category: string
  icon?: string
  color?: string
  hasInputHandle: boolean
  hasOutputHandle: boolean
  canDelete: boolean
  configSchema: NodeConfigSchema
}

export interface GetNodeTypesResponse {
  nodeTypes: NodeTypeInfo[]
}

// Node Types API
export const nodeTypes = {
  list: async () => {
    const response = await api.get<GetNodeTypesResponse>('/api/v1/orchestrations/node-types')
    return response.nodeTypes
  },
  getSchema: async (nodeType: string) => {
    const nodeTypesList = await nodeTypes.list()
    const found = nodeTypesList.find(nt => nt.type.toLowerCase() === nodeType.toLowerCase())
    return found?.configSchema
  },
}

// Multimodal Chat Schema API
export const multimodalChat = {
  getSchema: async (provider: string): Promise<NodeConfigSchema> => {
    const response = await api.get<{ schema: NodeConfigSchema }>(`/api/v1/multimodal-chat/schema?provider=${provider}`)
    return response.schema
  },
}

// Conversation Types
export type MessageRole = 'User' | 'Assistant' | 'System'

// Content part types (polymorphic, type discriminator)
export interface TextContentPart {
  type: 'text'
  text: string
}

export interface ImageContentPart {
  type: 'image'
  fileId: string
  mediaType: string
}

// Union type for all content parts
export type ContentPart = TextContentPart | ImageContentPart

// Image upload response
export interface UploadImageResponse {
  fileId: string
  fileName: string
  contentType: string
  sizeBytes: number
}

export interface ConversationMessage {
  id: string
  role: MessageRole
  content: ContentPart[]
  inputTokens?: number
  outputTokens?: number
  totalTokens?: number
  provider?: string
  model?: string
  createdAt: string
}

export interface ConversationSummary {
  id: string
  orchestrationId: string
  orchestrationName: string
  title: string
  messageCount: number
  createdAt: string
  updatedAt?: string
}

export interface ConversationDetails {
  id: string
  orchestrationId: string
  orchestrationName: string
  title: string
  messages: ConversationMessage[]
  createdAt: string
  updatedAt?: string
}

export interface CreateConversationRequest {
  orchestrationId: string
  title?: string
}

export interface UpdateConversationRequest {
  title: string
}

export interface SendMessageRequest {
  content: ContentPart[]
}

// SSE Event types for conversation streaming
export type ConversationStreamEventType =
  | 'ResponseStartEvent'
  | 'PartStartEvent'
  | 'PartDeltaEvent'
  | 'PartEndEvent'
  | 'TokenUsageEvent'
  | 'ResponseErrorEvent'
  | 'ResponseEndEvent'

export interface ConversationStreamEvent {
  type: ConversationStreamEventType
  [key: string]: unknown
}

export interface ResponseStartEvent extends ConversationStreamEvent {
  type: 'ResponseStartEvent'
  messageId: string
}

export interface PartStartEvent extends ConversationStreamEvent {
  type: 'PartStartEvent'
  partType: string
  partIndex: number
}

export interface PartDeltaEvent extends ConversationStreamEvent {
  type: 'PartDeltaEvent'
  partIndex: number
  content: string
}

export interface PartEndEvent extends ConversationStreamEvent {
  type: 'PartEndEvent'
  partIndex: number
}

export interface TokenUsageEvent extends ConversationStreamEvent {
  type: 'TokenUsageEvent'
  inputTokens: number
  outputTokens: number
  totalTokens: number
}

export interface ResponseErrorEvent extends ConversationStreamEvent {
  type: 'ResponseErrorEvent'
  error: string
}

export interface ResponseEndEvent extends ConversationStreamEvent {
  type: 'ResponseEndEvent'
  message: ConversationMessage
}

// Conversations API
export const conversations = {
  // List conversations (paginated, newest first)
  list: (offset = 0, limit = 20) =>
    api.get<PaginatedResponse<ConversationSummary>>(`/api/v1/conversations?offset=${offset}&limit=${limit}`),

  // Create a new conversation
  create: (data: CreateConversationRequest) =>
    api.post<ConversationDetails>('/api/v1/conversations', data),

  // Get conversation with all messages
  get: (id: string) =>
    api.get<ConversationDetails>(`/api/v1/conversations/${id}`),

  // Update conversation title
  updateTitle: (id: string, data: UpdateConversationRequest) =>
    fetchWithAuth(`/api/v1/conversations/${id}`, {
      method: 'PATCH',
      body: JSON.stringify(data)
    }).then(r => r.json() as Promise<ConversationDetails>),

  // Delete conversation
  delete: (id: string) =>
    api.delete(`/api/v1/conversations/${id}`),

  // Send message (returns created user message - streaming happens via SSE)
  sendMessage: (conversationId: string, content: ContentPart[]) =>
    api.post<ConversationMessage>(`/api/v1/conversations/${conversationId}/messages`, { content }),

  // Delete message
  deleteMessage: (conversationId: string, messageId: string) =>
    api.delete(`/api/v1/conversations/${conversationId}/messages/${messageId}`),

  // Upload image for conversation
  uploadImage: async (conversationId: string, file: File): Promise<UploadImageResponse> => {
    const formData = new FormData()
    formData.append('file', file)

    // Use baseFetchWithAuth directly - don't set Content-Type, let FormData set its own boundary
    const response = await baseFetchWithAuth(
      `${BASE_URL}/api/v1/conversations/${conversationId}/upload`,
      {
        method: 'POST',
        body: formData
      }
    )

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Upload failed' }))
      throw new Error(error.error || 'Upload failed')
    }

    return response.json()
  },
}

// Helper to get image download URL
export const getImageDownloadUrl = (fileId: string): string => {
  return `${BASE_URL}/api/v1/files/${fileId}/download`
}

// Export fetchWithAuth for hooks that need raw fetch access (e.g., SSE streaming)
export { fetchWithAuth }
