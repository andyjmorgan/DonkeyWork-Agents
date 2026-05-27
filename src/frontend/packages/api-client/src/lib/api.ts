import { fetchWithAuth as baseFetchWithAuth } from './fetchWithAuth'
import { getPlatformConfig } from '@donkeywork/platform'

async function fetchWithAuth(url: string, options: RequestInit = {}, retryOnUnauthorized = true): Promise<Response> {
  const { apiBaseUrl } = getPlatformConfig()
  return baseFetchWithAuth(
    `${apiBaseUrl}${url}`,
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

export interface JSONSchema {
  type: string
  properties?: Record<string, unknown>
  required?: string[]
  [key: string]: unknown
}

// OAuth types
export type OAuthProvider = 'Google' | 'Microsoft' | 'GitHub' | 'Custom'
export type OAuthTokenStatus = 'Active' | 'ExpiringSoon' | 'Expired'

export interface OAuthScopeMetadata {
  name: string
  description: string
  friendlyName?: string
  isRequired: boolean
  isDefault: boolean
}

export interface OAuthProviderMetadata {
  provider: OAuthProvider
  displayName: string
  authorizationUrl: string
  tokenUrl: string
  userInfoUrl: string
  defaultScopes: string[]
  setupUrl: string
  setupInstructions: string
  isBuiltIn: boolean
  availableScopes: OAuthScopeMetadata[]
}

export interface OAuthProviderConfig {
  id: string
  provider: OAuthProvider
  redirectUri: string
  createdAt: string
  hasToken: boolean
  customProviderName?: string
  scopes?: string[]
}

export interface OAuthProviderConfigDetail {
  id: string
  provider: OAuthProvider
  clientId: string
  clientSecret: string
  redirectUri: string
  createdAt: string
  authorizationUrl?: string
  tokenUrl?: string
  userInfoUrl?: string
  scopes?: string[]
  customProviderName?: string
}

export interface CreateOAuthProviderConfigRequest {
  provider: OAuthProvider
  clientId: string
  clientSecret: string
  redirectUri: string
  authorizationUrl?: string
  tokenUrl?: string
  userInfoUrl?: string
  scopes?: string[]
  customProviderName?: string
}

export interface UpdateOAuthProviderConfigRequest {
  clientId?: string
  clientSecret?: string
  redirectUri?: string
  authorizationUrl?: string
  tokenUrl?: string
  userInfoUrl?: string
  scopes?: string[]
  customProviderName?: string
}

export interface OAuthToken {
  id: string
  provider: OAuthProvider
  email: string
  externalUserId: string
  status: OAuthTokenStatus
  expiresAt?: string | null
  lastRefreshedAt?: string
  createdAt: string
  scopes: string[]
  canRefresh: boolean
}

export interface OAuthTokenDetail {
  id: string
  provider: OAuthProvider
  email: string
  externalUserId: string
  accessToken: string
  scopes: string[]
  status: OAuthTokenStatus
  expiresAt?: string | null
  lastRefreshedAt?: string
  createdAt: string
}

export interface GetOAuthAccessTokenResponse {
  id: string
  provider: OAuthProvider
  email: string
  accessToken: string
  status: OAuthTokenStatus
  expiresAt?: string | null
}

export interface AvailableCredential {
  credentialType: 'OAuth' | 'ApiKey'
  provider: string
  isConfigured: boolean
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
  getProviderMetadata: () => api.get<OAuthProviderMetadata[]>('/api/v1/oauth/configs/providers'),

  listConfigs: () => api.get<OAuthProviderConfig[]>('/api/v1/oauth/configs'),
  getConfig: (id: string) => api.get<OAuthProviderConfigDetail>(`/api/v1/oauth/configs/${id}`),
  createConfig: (data: CreateOAuthProviderConfigRequest) => api.post<OAuthProviderConfig>('/api/v1/oauth/configs', data),
  updateConfig: (id: string, data: UpdateOAuthProviderConfigRequest) => api.post<OAuthProviderConfigDetail>(`/api/v1/oauth/configs/${id}`, data),
  deleteConfig: (id: string) => api.delete(`/api/v1/oauth/configs/${id}`),

  getAuthorizationUrl: (provider: OAuthProvider, scopes?: string[]) => {
    const params = scopes?.length ? `?${scopes.map(s => `scopes=${encodeURIComponent(s)}`).join('&')}` : ''
    return api.get<GetAuthorizationUrlResponse>(`/api/v1/oauth/${provider}/authorize${params}`)
  },

  listTokens: () => api.get<OAuthToken[]>('/api/v1/oauth/tokens'),
  getToken: (id: string) => api.get<OAuthTokenDetail>(`/api/v1/oauth/tokens/${id}`),
  getAccessToken: (id: string) => api.get<GetOAuthAccessTokenResponse>(`/api/v1/oauth/tokens/${id}/access-token`),
  refreshToken: (id: string) => api.post<RefreshTokenResponse>(`/api/v1/oauth/tokens/${id}/refresh`, {}),
  disconnectToken: (id: string) => api.delete(`/api/v1/oauth/tokens/${id}`),
}

// Available Credentials API
export const availableCredentials = {
  list: () => api.get<AvailableCredential[]>('/api/v1/credentials/available'),
}

// Node Types - Schema API
// Conversation Types
export type MessageRole = 'User' | 'Assistant' | 'System'

// Content part types (polymorphic, type discriminator)
export interface TextContentPart {
  type: 'text'
  text: string
}

export interface ImageContentPart {
  type: 'image'
  objectKey: string
  mediaType: string
}

// Union type for all content parts
export type ContentPart = TextContentPart | ImageContentPart

// Image upload response
export interface UploadImageResponse {
  objectKey: string
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
  title: string
  messageCount: number
  createdAt: string
  updatedAt?: string
}

export interface ConversationDetails {
  id: string
  title: string
  messages: ConversationMessage[]
  createdAt: string
  updatedAt?: string
}

export interface CreateConversationRequest {
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

// Agent Executions API
import type { AgentExecutionSummary, AgentExecutionDetail } from '../types/agent-executions'
import type { InternalMessage } from '../types/internal-messages'

export interface GetAgentExecutionMessagesResponse {
  messages: InternalMessage[]
}

export const agentExecutions = {
  list: (offset = 0, limit = 20) =>
    api.get<PaginatedResponse<AgentExecutionSummary>>(`/api/v1/agent-executions?offset=${offset}&limit=${limit}`),
  listByConversation: (conversationId: string) =>
    api.get<PaginatedResponse<AgentExecutionSummary>>(`/api/v1/agent-executions?conversationId=${conversationId}`),
  get: (id: string) =>
    api.get<AgentExecutionDetail>(`/api/v1/agent-executions/${id}`),
  getMessages: (id: string) =>
    api.get<GetAgentExecutionMessagesResponse>(`/api/v1/agent-executions/${id}/messages`),
}

// MCP Traces API
export interface McpTraceSummary {
  id: string
  method: string
  httpStatusCode: number
  isSuccess: boolean
  durationMs?: number
  startedAt: string
  clientIpAddress?: string
  userAgent?: string
}

export interface McpTraceDetail extends McpTraceSummary {
  userId?: string
  jsonRpcId?: string
  requestBody: string
  responseBody?: string
  errorMessage?: string
  completedAt?: string
  createdAt: string
}

export const mcpTraces = {
  list: (offset = 0, limit = 20) =>
    api.get<PaginatedResponse<McpTraceSummary>>(`/api/v1/mcp-traces?offset=${offset}&limit=${limit}`),
  get: (id: string) =>
    api.get<McpTraceDetail>(`/api/v1/mcp-traces/${id}`),
}

// Conversations API
export const conversations = {
  list: (offset = 0, limit = 20) =>
    api.get<PaginatedResponse<ConversationSummary>>(`/api/v1/conversations?offset=${offset}&limit=${limit}`),

  listNavi: (offset = 0, limit = 10) =>
    api.get<PaginatedResponse<ConversationSummary>>(`/api/v1/conversations?agentOnly=true&offset=${offset}&limit=${limit}`),

  create: (data: CreateConversationRequest) =>
    api.post<ConversationDetails>('/api/v1/conversations', data),

  get: (id: string) =>
    api.get<ConversationDetails>(`/api/v1/conversations/${id}`),

  updateTitle: (id: string, data: UpdateConversationRequest) =>
    fetchWithAuth(`/api/v1/conversations/${id}`, {
      method: 'PATCH',
      body: JSON.stringify(data)
    }).then(r => r.json() as Promise<ConversationDetails>),

  delete: (id: string) =>
    api.delete(`/api/v1/conversations/${id}`),

  bulkDelete: (ids: string[]) =>
    api.post<{ deletedCount: number }>('/api/v1/conversations/bulk-delete', { ids }),

  sendMessage: (conversationId: string, content: ContentPart[]) =>
    api.post<ConversationMessage>(`/api/v1/conversations/${conversationId}/messages`, { content }),

  deleteMessage: (conversationId: string, messageId: string) =>
    api.delete(`/api/v1/conversations/${conversationId}/messages/${messageId}`),

  uploadImage: async (conversationId: string, file: File): Promise<UploadImageResponse> => {
    const formData = new FormData()
    formData.append('file', file)

    // Use baseFetchWithAuth directly - don't set Content-Type, let FormData set its own boundary
    const response = await baseFetchWithAuth(
      `${getPlatformConfig().apiBaseUrl}/api/v1/conversations/${conversationId}/upload`,
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
export const getImageDownloadUrl = (objectKey: string): string => {
  return `${getPlatformConfig().apiBaseUrl}/api/v1/files/download/${objectKey}`
}

// Storage File Types
export interface FileItem {
  fileName: string
  sizeBytes: number
  lastModified: string
}

export interface FileListingResponse {
  files: FileItem[]
  folders: string[]
}

export interface StorageUploadResult {
  objectKey: string
  fileName: string
  contentType: string
  sizeBytes: number
}

// Files API
export const files = {
  list: (prefix?: string) => {
    const params = prefix ? `?prefix=${encodeURIComponent(prefix)}` : ''
    return api.get<FileListingResponse>(`/api/v1/files${params}`)
  },

  upload: async (file: File): Promise<StorageUploadResult> => {
    const formData = new FormData()
    formData.append('file', file)

    const response = await baseFetchWithAuth(
      `${getPlatformConfig().apiBaseUrl}/api/v1/files`,
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

  delete: (fileName: string) =>
    api.delete(`/api/v1/files/${encodeURIComponent(fileName)}`),

  deleteFolder: (prefix: string) =>
    api.delete(`/api/v1/files/folder/${prefix}`),

  download: async (fileName: string): Promise<{ blob: Blob; fileName: string; contentType: string }> => {
    const response = await baseFetchWithAuth(`${getPlatformConfig().apiBaseUrl}/api/v1/files/download/${fileName}`)
    if (!response.ok) {
      throw new Error(`Download failed: ${response.status}`)
    }

    const contentDisposition = response.headers.get('content-disposition')
    const contentType = response.headers.get('content-type') || 'application/octet-stream'

    let downloadFileName = fileName
    if (contentDisposition) {
      const match = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
      if (match) {
        downloadFileName = match[1].replace(/['"]/g, '')
      }
    }

    const blob = await response.blob()
    return { blob, fileName: downloadFileName, contentType }
  },

  getUrl: async (key: string): Promise<string> => {
    const data = await api.get<{ url: string; expiresAt: string }>(`/api/v1/files/url/${key}`)
    return data.url
  },

  fetchText: async (key: string): Promise<string> => {
    const response = await baseFetchWithAuth(`${getPlatformConfig().apiBaseUrl}/api/v1/files/download/${key}`)
    if (!response.ok) {
      throw new Error(`Download failed: ${response.status}`)
    }
    return response.text()
  },
}

// Skill Types
export interface SkillItem {
  name: string
  createdAt: string
}

export interface SkillUploadResult {
  name: string
}

export interface SkillFileNode {
  name: string
  isDirectory: boolean
  children?: SkillFileNode[]
}

export interface SkillFileContent {
  path: string
  name: string
  content: string
  contentType: string
  size: number
  lastModified: string
}

export interface WriteFileRequest {
  content: string
}

export interface WriteFileResponse {
  path: string
  name: string
  size: number
  lastModified: string
}

export interface RenameRequest {
  newName: string
}

export interface RenameResponse {
  oldPath: string
  newPath: string
  newName: string
}

export interface DuplicateFileResponse {
  path: string
  name: string
}

export interface CreateFolderResponse {
  path: string
  name: string
}

// Skills API
export const skills = {
  list: () =>
    api.get<SkillItem[]>('/api/v1/skills'),

  upload: async (file: File): Promise<SkillUploadResult> => {
    const formData = new FormData()
    formData.append('file', file)

    const response = await baseFetchWithAuth(
      `${getPlatformConfig().apiBaseUrl}/api/v1/skills`,
      {
        method: 'POST',
        body: formData
      }
    )

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Upload failed' }))
      throw new Error(error.error || error.detail || 'Upload failed')
    }

    return response.json()
  },

  delete: (name: string) =>
    api.delete(`/api/v1/skills/${encodeURIComponent(name)}`),

  getContents: (name: string) =>
    api.get<SkillFileNode[]>(`/api/v1/skills/${encodeURIComponent(name)}/contents`),

  readFile: (name: string, path: string) =>
    api.get<SkillFileContent>(`/api/v1/skills/${encodeURIComponent(name)}/files/${path}`),

  writeFile: (name: string, path: string, request: WriteFileRequest) =>
    api.put<WriteFileResponse>(`/api/v1/skills/${encodeURIComponent(name)}/files/${path}`, request),

  deleteFile: (name: string, path: string) =>
    api.delete(`/api/v1/skills/${encodeURIComponent(name)}/files/${path}`),

  rename: (name: string, path: string, request: RenameRequest) =>
    api.post<RenameResponse>(`/api/v1/skills/${encodeURIComponent(name)}/rename/${path}`, request),

  duplicateFile: (name: string, path: string) =>
    api.post<DuplicateFileResponse>(`/api/v1/skills/${encodeURIComponent(name)}/duplicate/${path}`, {}),

  createFolder: (name: string, path: string) =>
    api.post<CreateFolderResponse>(`/api/v1/skills/${encodeURIComponent(name)}/folders/${path}`, {}),

  deleteFolder: (name: string, path: string) =>
    api.delete(`/api/v1/skills/${encodeURIComponent(name)}/folders/${path}`),

  download: async (name: string): Promise<void> => {
    const response = await baseFetchWithAuth(
      `${getPlatformConfig().apiBaseUrl}/api/v1/skills/${encodeURIComponent(name)}/download`
    )
    if (!response.ok) {
      throw new Error(`Download failed: ${response.status}`)
    }
    const blob = await response.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${name}.zip`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  },
}

/**
 * Fetch an image with auth headers and return a blob URL.
 * This is needed because img src doesn't include Authorization headers.
 */
export const fetchImageAsBlob = async (objectKey: string): Promise<string> => {
  const response = await baseFetchWithAuth(`${getPlatformConfig().apiBaseUrl}/api/v1/files/download/${objectKey}`)
  if (!response.ok) {
    throw new Error(`Failed to fetch image: ${response.status}`)
  }
  const blob = await response.blob()
  return URL.createObjectURL(blob)
}

// Export fetchWithAuth for hooks that need raw fetch access (e.g., SSE streaming)
export { fetchWithAuth }

// Sandbox Credential Mapping Types
export type CredentialFieldType = 'ApiKey' | 'AccessToken' | 'RefreshToken' | 'Username' | 'Password' | 'ClientId' | 'ClientSecret' | 'WebhookSecret' | 'Custom'

export interface CredentialFieldsResponse {
  fields: CredentialFieldType[]
}
export type HeaderValueFormat = 'Raw' | 'BasicAuth'

export interface SandboxProviderStatus {
  provider: string
  displayName: string
  hasOAuthToken: boolean
  isEnabled: boolean
  domains: string[]
}

export interface SandboxCredentialMapping {
  id: string
  baseDomain: string
  headerName: string
  managedByProvider?: string
  headerValuePrefix?: string
  headerValueFormat: HeaderValueFormat
  basicAuthUsername?: string
  credentialId: string
  credentialType: string
  credentialFieldType: CredentialFieldType
  createdAt: string
}

export interface CreateSandboxCredentialMappingRequest {
  baseDomain: string
  headerName: string
  headerValuePrefix?: string
  headerValueFormat?: HeaderValueFormat
  basicAuthUsername?: string
  credentialId: string
  credentialType: string
  credentialFieldType: CredentialFieldType
}

export interface UpdateSandboxCredentialMappingRequest {
  headerName?: string
  headerValuePrefix?: string
  headerValueFormat?: HeaderValueFormat
  basicAuthUsername?: string
  credentialId?: string
  credentialType?: string
  credentialFieldType?: CredentialFieldType
}

export const sandboxCredentialMappings = {
  list: () => api.get<SandboxCredentialMapping[]>('/api/v1/sandbox-credential-mappings'),
  get: (id: string) => api.get<SandboxCredentialMapping>(`/api/v1/sandbox-credential-mappings/${id}`),
  create: (data: CreateSandboxCredentialMappingRequest) =>
    api.post<SandboxCredentialMapping>('/api/v1/sandbox-credential-mappings', data),
  update: (id: string, data: UpdateSandboxCredentialMappingRequest) =>
    api.put<SandboxCredentialMapping>(`/api/v1/sandbox-credential-mappings/${id}`, data),
  delete: (id: string) => api.delete(`/api/v1/sandbox-credential-mappings/${id}`),
  domains: () => api.get<string[]>('/api/v1/sandbox-credential-mappings/domains'),
  listProviders: () => api.get<SandboxProviderStatus[]>('/api/v1/sandbox-credential-mappings/providers'),
  enableProvider: (provider: string) =>
    api.post<SandboxCredentialMapping[]>(`/api/v1/sandbox-credential-mappings/providers/${provider}`, {}),
  disableProvider: (provider: string) =>
    api.delete(`/api/v1/sandbox-credential-mappings/providers/${provider}`),
  getCredentialFields: (credentialId: string, credentialType: string) =>
    api.get<CredentialFieldsResponse>(`/api/v1/sandbox-credential-mappings/credential-fields?credentialId=${credentialId}&credentialType=${credentialType}`),
}

// Sandbox Custom Variable Types
export interface SandboxCustomVariable {
  id: string
  key: string
  value: string
  isSecret: boolean
  createdAt: string
  updatedAt?: string
}

export interface CreateSandboxCustomVariableRequest {
  key: string
  value: string
  isSecret: boolean
}

export interface UpdateSandboxCustomVariableRequest {
  value?: string
  isSecret?: boolean
}

export const sandboxCustomVariables = {
  list: () => api.get<SandboxCustomVariable[]>('/api/v1/sandbox-custom-variables'),
  get: (id: string) => api.get<SandboxCustomVariable>(`/api/v1/sandbox-custom-variables/${id}`),
  create: (data: CreateSandboxCustomVariableRequest) =>
    api.post<SandboxCustomVariable>('/api/v1/sandbox-custom-variables', data),
  update: (id: string, data: UpdateSandboxCustomVariableRequest) =>
    api.put<SandboxCustomVariable>(`/api/v1/sandbox-custom-variables/${id}`, data),
  delete: (id: string) => api.delete(`/api/v1/sandbox-custom-variables/${id}`),
}

// MCP Server Configuration Types
export type McpTransportType = 'Stdio' | 'Http'
export type McpHttpTransportMode = 'AutoDetect' | 'Sse' | 'StreamableHttp'
export type McpHttpAuthType = 'None' | 'OAuth' | 'Header'

export interface McpEnvironmentVariableV1 {
  name: string
  isCredentialReference: boolean
  value?: string
  credentialId?: string
  credentialFieldType?: string
}

export interface McpStdioConfigurationV1 {
  command: string
  arguments?: string[]
  environmentVariables?: McpEnvironmentVariableV1[]
  preExecScripts?: string[]
  workingDirectory?: string
}

export interface McpHttpHeaderConfigurationV1 {
  headerName: string
  headerValue?: string
  isCredentialReference: boolean
  credentialId?: string
  credentialFieldType?: string
}

export interface McpHttpOAuthConfigurationV1 {
  clientId: string
  clientSecret?: string
  redirectUri: string
  scopes?: string[]
  authorizationEndpoint: string
  tokenEndpoint: string
}

export interface McpHttpConfigurationV1 {
  endpoint: string
  transportMode: McpHttpTransportMode
  authType: McpHttpAuthType
  oauthConfiguration?: McpHttpOAuthConfigurationV1
  oauthTokenId?: string
  headerConfigurations?: McpHttpHeaderConfigurationV1[]
}

export interface McpServerSummary {
  id: string
  name: string
  description?: string
  transportType: McpTransportType
  isEnabled: boolean
  connectToNavi?: boolean
  createdAt: string
  updatedAt?: string
}

export interface McpServerDetails {
  id: string
  name: string
  description?: string
  transportType: McpTransportType
  isEnabled: boolean
  connectToNavi?: boolean
  stdioConfiguration?: McpStdioConfigurationV1
  httpConfiguration?: McpHttpConfigurationV1
  createdAt: string
  updatedAt?: string
}

export interface CreateMcpEnvironmentVariableRequest {
  name: string
  value?: string
  credentialId?: string
  credentialFieldType?: string
}

export interface CreateMcpStdioConfigurationRequest {
  command: string
  arguments?: string[]
  environmentVariables?: CreateMcpEnvironmentVariableRequest[]
  preExecScripts?: string[]
  workingDirectory?: string
}

export interface CreateMcpHttpHeaderConfigurationRequest {
  headerName: string
  headerValue?: string
  credentialId?: string
  credentialFieldType?: string
}

export interface CreateMcpHttpOAuthConfigurationRequest {
  clientId: string
  clientSecret?: string
  redirectUri: string
  scopes?: string[]
  authorizationEndpoint: string
  tokenEndpoint: string
}

export interface CreateMcpHttpConfigurationRequest {
  endpoint: string
  transportMode: McpHttpTransportMode
  authType: McpHttpAuthType
  oauthConfiguration?: CreateMcpHttpOAuthConfigurationRequest
  oauthTokenId?: string
  headerConfigurations?: CreateMcpHttpHeaderConfigurationRequest[]
}

export interface CreateMcpServerRequest {
  name: string
  description?: string
  transportType: McpTransportType
  isEnabled?: boolean
  connectToNavi?: boolean
  stdioConfiguration?: CreateMcpStdioConfigurationRequest
  httpConfiguration?: CreateMcpHttpConfigurationRequest
}

export interface UpdateMcpServerRequest {
  name: string
  description?: string
  transportType: McpTransportType
  isEnabled?: boolean
  connectToNavi?: boolean
  stdioConfiguration?: CreateMcpStdioConfigurationRequest
  httpConfiguration?: CreateMcpHttpConfigurationRequest
}

export interface McpToolInfo {
  name: string
  description?: string
  inputSchema?: object
}

export interface McpServerTestResult {
  success: boolean
  serverName?: string
  serverVersion?: string
  elapsedMs: number
  tools: McpToolInfo[]
  error?: string
}

// MCP Servers API
export const mcpServers = {
  list: () => api.get<McpServerSummary[]>('/api/v1/mcp-servers'),
  get: (id: string) => api.get<McpServerDetails>(`/api/v1/mcp-servers/${id}`),
  create: (data: CreateMcpServerRequest) => api.post<McpServerDetails>('/api/v1/mcp-servers', data),
  update: (id: string, data: UpdateMcpServerRequest) => api.put<McpServerDetails>(`/api/v1/mcp-servers/${id}`, data),
  delete: (id: string) => api.delete(`/api/v1/mcp-servers/${id}`),
  test: (id: string) => api.post<McpServerTestResult>(`/api/v1/mcp-servers/${id}/test`, {}),
}

// A2A Server Types
export interface A2aServerSummary {
  id: string
  name: string
  description?: string
  address: string
  isEnabled: boolean
  connectToNavi: boolean
  createdAt: string
  updatedAt: string
}

export interface A2aHeaderConfiguration {
  id: string
  headerName: string
  headerValue?: string
  isCredentialReference: boolean
  credentialId?: string
  credentialFieldType?: string
}

export interface A2aServerDetails extends A2aServerSummary {
  headerConfigurations: A2aHeaderConfiguration[]
}

export interface CreateA2aHeaderConfigurationRequest {
  headerName: string
  headerValue?: string
  credentialId?: string
  credentialFieldType?: string
}

export interface CreateA2aServerRequest {
  name: string
  description?: string
  address: string
  isEnabled?: boolean
  connectToNavi?: boolean
  headerConfigurations?: CreateA2aHeaderConfigurationRequest[]
}

export interface UpdateA2aServerRequest extends CreateA2aServerRequest {}

export interface A2aAgentCardSkill {
  id: string
  name: string
  description: string
  friendlyName?: string
  tags?: string[]
}

export interface A2aAgentCardCapabilities {
  streaming: boolean
  pushNotifications: boolean
}

export interface A2aSecurityScheme {
  type?: string
  in?: string
  name?: string
  scheme?: string
  description?: string
}

export interface A2aAgentCard {
  name: string
  description: string
  friendlyName?: string
  url: string
  version: string
  skills: A2aAgentCardSkill[]
  capabilities?: A2aAgentCardCapabilities
  defaultInputModes?: string[]
  defaultOutputModes?: string[]
  securitySchemes?: Record<string, A2aSecurityScheme>
  security?: Record<string, string[]>[]
}

export interface A2aServerTestResult {
  success: boolean
  elapsedMs: number
  error?: string
  agentCard?: A2aAgentCard
}

// A2A Servers API
export const a2aServers = {
  list: () => api.get<A2aServerSummary[]>('/api/v1/a2a-servers'),
  get: (id: string) => api.get<A2aServerDetails>(`/api/v1/a2a-servers/${id}`),
  create: (data: CreateA2aServerRequest) => api.post<A2aServerDetails>('/api/v1/a2a-servers', data),
  update: (id: string, data: UpdateA2aServerRequest) => api.put<A2aServerDetails>(`/api/v1/a2a-servers/${id}`, data),
  delete: (id: string) => api.delete(`/api/v1/a2a-servers/${id}`),
  test: (id: string) => api.post<A2aServerTestResult>(`/api/v1/a2a-servers/${id}/test`, {}),
  discover: (address: string) => api.post<A2aServerTestResult>(`/api/v1/a2a-servers/discover`, { address }),
}

// Tool Groups API
export const toolGroups = {
  list: () => api.get<ToolGroupDefinition[]>('/api/v1/tool-groups'),
}

// Agent Definition Types
export type AgentLifecycle = 'Task' | 'Linger'
export type AgentType = 'Standard' | 'System'

export type ReasoningEffort = 'None' | 'Low' | 'Medium' | 'High'

export interface McpServerReference {
  id: string
  name: string
  description?: string
  deferToolLoading?: boolean
}

export interface SubAgentReference {
  id: string
  name: string
  description?: string
}

export interface A2aServerReference {
  id: string
  name: string
  description?: string
}

export interface ToolOverride {
  source: string
  toolName: string
  enabled?: boolean
  deferred?: boolean
}

export interface ToolConfig {
  deferToolLoading?: boolean
  toolOverrides?: ToolOverride[]
}

export interface ToolGroupDefinition {
  id: string
  name: string
  tools: ToolDefinition[]
}

export interface ToolDefinition {
  name: string
  displayName?: string
  description?: string
}

export interface ContextManagementConfigV1 {
  compactionEnabled?: boolean
  compactionTriggerTokens?: number
  clearToolResultsEnabled?: boolean
  clearToolResultsTriggerTokens?: number
  clearToolResultsKeep?: number
  clearThinkingEnabled?: boolean
  clearThinkingKeepTurns?: number
}

export interface AgentContractV1 {
  systemPrompt?: string[]
  toolGroups?: string[]
  maxTokens?: number
  reasoningEffort?: ReasoningEffort
  stream?: boolean
  webSearch?: { enabled: boolean; maxUses: number }
  webFetch?: { enabled: boolean; maxUses: number }
  persistMessages?: boolean
  lifecycle?: AgentLifecycle
  lingerSeconds?: number
  agentType?: AgentType
  keyPrefix?: string
  timeoutSeconds?: number
  mcpServers?: McpServerReference[]
  subAgents?: SubAgentReference[]
  a2aServers?: A2aServerReference[]
  enableSandbox?: boolean
  modelId?: string
  prompts?: string[]
  toolConfiguration?: ToolConfig
  allowDelegation?: boolean
  displayName?: string
  contextManagement?: ContextManagementConfigV1
}

export interface AgentDefinitionSummary {
  id: string
  name: string
  description?: string
  isSystem: boolean
  connectToNavi?: boolean
  createdAt: string
}

export interface AgentDefinitionDetails {
  id: string
  name: string
  description?: string
  icon?: string
  isSystem: boolean
  connectToNavi?: boolean
  contract: AgentContractV1
  reactFlowData?: { nodes: unknown[]; edges: unknown[]; viewport: { x: number; y: number; zoom: number } }
  nodeConfigurations?: Record<string, unknown>
  createdAt: string
  updatedAt?: string
}

export interface CreateAgentDefinitionRequest {
  name: string
  description?: string
  icon?: string
}

export interface UpdateAgentDefinitionRequest {
  name?: string
  description?: string
  icon?: string
  connectToNavi?: boolean
  contract?: AgentContractV1
  reactFlowData?: { nodes: unknown[]; edges: unknown[]; viewport: { x: number; y: number; zoom: number } }
  nodeConfigurations?: Record<string, unknown>
}

// Agent Definitions API
export const agentDefinitions = {
  list: () => api.get<AgentDefinitionSummary[]>('/api/v1/agent-definitions'),
  get: (id: string) => api.get<AgentDefinitionDetails>(`/api/v1/agent-definitions/${id}`),
  create: (data: CreateAgentDefinitionRequest) => api.post<AgentDefinitionDetails>('/api/v1/agent-definitions', data),
  update: (id: string, data: UpdateAgentDefinitionRequest) => api.put<AgentDefinitionDetails>(`/api/v1/agent-definitions/${id}`, data),
  delete: (id: string) => api.delete(`/api/v1/agent-definitions/${id}`),
}

// Prompt Types
export type PromptType = 'System' | 'User'

export interface PromptSummary {
  id: string
  name: string
  description?: string
  promptType: PromptType
  createdAt: string
}

export interface PromptDetails extends PromptSummary {
  content: string
  updatedAt?: string
}

export interface CreatePromptRequest {
  name: string
  description?: string
  content: string
  promptType: PromptType
}

export interface UpdatePromptRequest {
  name?: string
  description?: string
  content?: string
  promptType?: PromptType
}

// Prompts API
export const prompts = {
  list: () => api.get<PromptSummary[]>('/api/v1/prompts'),
  get: (id: string) => api.get<PromptDetails>(`/api/v1/prompts/${id}`),
  create: (data: CreatePromptRequest) => api.post<PromptDetails>('/api/v1/prompts', data),
  update: (id: string, data: UpdatePromptRequest) => api.put<PromptDetails>(`/api/v1/prompts/${id}`, data),
  delete: (id: string) => api.delete(`/api/v1/prompts/${id}`),
}

// Scheduling Types
export type ScheduleMode = 'OneOff' | 'Recurring'
export type ScheduleJobType = 'AgentInvocation' | 'Reminder' | 'Maintenance' | 'Cleanup' | 'Archival' | 'ReportGeneration' | 'WorkflowExecution'
export type ScheduleTargetType = 'Navi' | 'CustomAgent'
export type ScheduleExecutionStatus = 'Running' | 'Succeeded' | 'Failed' | 'Cancelled'
export type ScheduleTriggerSource = 'Cron' | 'OneOff' | 'Manual' | 'Webhook'

export interface ScheduledJobSummary {
  id: string
  name: string
  jobType: ScheduleJobType
  scheduleMode: ScheduleMode
  cronExpression?: string
  runAtUtc?: string
  timeZoneId: string
  isEnabled: boolean
  targetType: ScheduleTargetType
  targetName?: string
  nextFireTimeUtc?: string
  prevFireTimeUtc?: string
  createdAt: string
}

export interface ScheduledJobPayload {
  userPrompt: string
  inputContext?: string
  version: number
}

export interface ScheduledJobDetail {
  id: string
  name: string
  description?: string
  jobType: ScheduleJobType
  scheduleMode: ScheduleMode
  cronExpression?: string
  runAtUtc?: string
  timeZoneId: string
  isEnabled: boolean
  targetType: ScheduleTargetType
  targetAgentDefinitionId?: string
  targetName?: string
  quartzJobKey?: string
  quartzTriggerKey?: string
  payload?: ScheduledJobPayload
  nextFireTimeUtc?: string
  prevFireTimeUtc?: string
  createdAt: string
  updatedAt?: string
}

export interface ScheduledJobExecution {
  id: string
  scheduledJobId: string
  quartzFireInstanceId?: string
  triggerSource: ScheduleTriggerSource
  startedAtUtc: string
  completedAtUtc?: string
  status: ScheduleExecutionStatus
  errorDetails?: string
  outputSummary?: string
  executingNodeId?: string
  correlationId?: string
}

export interface CreateScheduleRequest {
  name: string
  description?: string
  jobType: ScheduleJobType
  scheduleMode: ScheduleMode
  cronExpression?: string
  runAtUtc?: string
  timeZoneId?: string
  targetType: ScheduleTargetType
  targetAgentDefinitionId?: string
  userPrompt: string
  inputContext?: string
}

export interface CreateScheduleResponse {
  id: string
  name: string
  nextFireTimeUtc?: string
  createdAt: string
}

export interface UpdateScheduleRequest {
  name?: string
  description?: string
  jobType?: ScheduleJobType
  cronExpression?: string
  runAtUtc?: string
  timeZoneId?: string
  targetType?: ScheduleTargetType
  targetAgentDefinitionId?: string
  userPrompt?: string
  inputContext?: string
}

export interface UpdateScheduleResponse {
  id: string
  name: string
  nextFireTimeUtc?: string
  updatedAt?: string
}

// Scheduling API
export const schedules = {
  list: (params?: { jobType?: ScheduleJobType; targetType?: ScheduleTargetType; scheduleMode?: ScheduleMode; isEnabled?: boolean; offset?: number; limit?: number }) => {
    const searchParams = new URLSearchParams()
    if (params?.jobType) searchParams.set('jobType', params.jobType)
    if (params?.targetType) searchParams.set('targetType', params.targetType)
    if (params?.scheduleMode) searchParams.set('scheduleMode', params.scheduleMode)
    if (params?.isEnabled !== undefined) searchParams.set('isEnabled', String(params.isEnabled))
    if (params?.offset !== undefined) searchParams.set('offset', String(params.offset))
    if (params?.limit !== undefined) searchParams.set('limit', String(params.limit))
    const qs = searchParams.toString()
    return api.get<PaginatedResponse<ScheduledJobSummary>>(`/api/v1/schedules${qs ? `?${qs}` : ''}`)
  },

  get: (id: string) =>
    api.get<ScheduledJobDetail>(`/api/v1/schedules/${id}`),

  create: (data: CreateScheduleRequest) =>
    api.post<CreateScheduleResponse>('/api/v1/schedules', data),

  update: (id: string, data: UpdateScheduleRequest) =>
    api.put<UpdateScheduleResponse>(`/api/v1/schedules/${id}`, data),

  delete: (id: string) =>
    api.delete(`/api/v1/schedules/${id}`),

  enable: (id: string) =>
    api.post(`/api/v1/schedules/${id}/enable`, {}),

  disable: (id: string) =>
    api.post(`/api/v1/schedules/${id}/disable`, {}),

  trigger: (id: string) =>
    api.post(`/api/v1/schedules/${id}/trigger`, {}),

  listExecutions: (id: string, offset = 0, limit = 20) =>
    api.get<PaginatedResponse<ScheduledJobExecution>>(`/api/v1/schedules/${id}/executions?offset=${offset}&limit=${limit}`),

  getExecution: (executionId: string) =>
    api.get<ScheduledJobExecution>(`/api/v1/schedules/executions/${executionId}`),
}
