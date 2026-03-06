// TypeScript types mirroring C# InternalMessage hierarchy.
// The C# types use [JsonPolymorphic] with $type discriminator.

export type InternalMessageRole = "System" | "User" | "Assistant";

// --- Content blocks (polymorphic via $type) ---

export interface InternalTextBlock {
  $type: "InternalTextBlock";
  text: string;
}

export interface InternalThinkingBlock {
  $type: "InternalThinkingBlock";
  text: string;
  signature?: string | null;
}

export interface InternalToolUseBlock {
  $type: "InternalToolUseBlock";
  id: string;
  name: string;
  input: unknown;
}

export interface InternalServerToolUseBlock {
  $type: "InternalServerToolUseBlock";
  id: string;
  name: string;
  input: unknown;
}

export interface InternalWebSearchResultBlock {
  $type: "InternalWebSearchResultBlock";
  toolUseId: string;
  rawJson: string;
}

export interface InternalWebFetchToolResultBlock {
  $type: "InternalWebFetchToolResultBlock";
  toolUseId: string;
  rawJson: string;
}

export interface InternalCitationBlock {
  $type: "InternalCitationBlock";
  title: string;
  url: string;
  citedText: string;
}

export type InternalContentBlock =
  | InternalTextBlock
  | InternalThinkingBlock
  | InternalToolUseBlock
  | InternalServerToolUseBlock
  | InternalWebSearchResultBlock
  | InternalWebFetchToolResultBlock
  | InternalCitationBlock;

// --- Tool use record ---

export interface ToolUseRecord {
  id: string;
  name: string;
  input: unknown;
}

// --- Messages (polymorphic via $type) ---

export interface InternalContentMessage {
  $type: "InternalContentMessage";
  role: InternalMessageRole;
  content: string;
}

export interface InternalAssistantMessage {
  $type: "InternalAssistantMessage";
  role: "Assistant";
  textContent?: string | null;
  toolUses: ToolUseRecord[];
  contentBlocks: InternalContentBlock[];
}

export interface InternalToolResultMessage {
  $type: "InternalToolResultMessage";
  role: "Assistant";
  toolUseId: string;
  content: string;
  isError: boolean;
}

export type InternalMessage =
  | InternalContentMessage
  | InternalAssistantMessage
  | InternalToolResultMessage;

// --- TrackedAgent from listAgents RPC ---

export type AgentStatusType = "Pending" | "Completed" | "Failed" | "TimedOut";

export interface TrackedAgentResult {
  content: string;
  isError: boolean;
}

export interface TrackedAgent {
  agentKey: string;
  label: string;
  parentAgentKey: string;
  status: AgentStatusType;
  result: TrackedAgentResult | null;
  spawnedAt: string;
}

// --- getState RPC response ---

export interface GetStateResponse {
  messages: InternalMessage[];
}
