// --- Content box types for agent chat ---

export type TextBox = { type: "text"; text: string };
export type ThinkingBox = { type: "thinking"; text: string };
export type WebSearchResult = { title: string; url: string };
export type CitationBox = { type: "citation"; title: string; url: string; citedText: string };
export type AgentCompleteReason = "completed" | "cancelled" | "failed";
export type ToolUseBox = {
  type: "tool_use";
  toolName: string;
  displayName?: string;
  toolUseId: string;
  arguments?: string;
  subAgent?: AgentGroupBox;
  isComplete?: boolean;
  completeReason?: AgentCompleteReason;
  result?: string;
  success?: boolean;
  durationMs?: number;
  webSearchResults?: WebSearchResult[];
};
export type UsageBox = { type: "usage"; inputTokens: number; outputTokens: number; webSearchRequests: number; contextWindowLimit: number; maxOutputTokens: number };
export type AgentGroupBox = {
  type: "agent_group";
  agentKey: string;
  agentType: string;
  label?: string;
  icon?: string;
  displayName?: string;
  boxes: ContentBox[];
  isComplete?: boolean;
  completeReason?: AgentCompleteReason;
};

export type ContentBox =
  | TextBox
  | ThinkingBox
  | CitationBox
  | ToolUseBox
  | UsageBox
  | AgentGroupBox;

export interface McpServerStatus {
  name: string
  success: boolean
  durationMs: number
  toolCount: number
  error?: string
}

export interface SandboxStatus {
  status: "provisioning" | "ready" | "failed"
  message?: string
  podName?: string
}

export type ChatMessage = {
  id: string;
  role: "user" | "assistant" | "progress";
  content: string;
  boxes: ContentBox[];
  _source?: string;
  _preview?: string;
};
