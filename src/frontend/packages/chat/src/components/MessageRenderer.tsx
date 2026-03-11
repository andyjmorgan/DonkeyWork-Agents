import { useMemo } from "react";
import type {
  InternalMessage,
  InternalContentMessage,
  InternalAssistantMessage,
  InternalToolResultMessage,
  InternalContentBlock,
  TrackedAgent,
} from "@donkeywork/api-client";
import type { ChatMessage, ContentBox, AgentGroupBox } from "@donkeywork/api-client";

const SPAWN_TOOL_NAMES = new Set([
  "spawn_agent",
  "spawn_delegate",
]);

/**
 * Converts InternalMessage[] (grain state format) into ChatMessage[] (UI rendering format).
 * When TrackedAgent[] is provided, reconstructs agent_group nesting on spawn tool_use boxes.
 */
export function internalToChat(
  messages: InternalMessage[],
  agents?: TrackedAgent[],
): ChatMessage[] {
  const result: ChatMessage[] = [];

  // Build agent lookup: agentKey -> TrackedAgent
  const agentMap = new Map<string, TrackedAgent>();
  if (agents) {
    for (const a of agents) {
      agentMap.set(a.agentKey, a);
    }
  }

  // Build map: toolUseId -> tool result content (for extracting agent_key from spawn results)
  const toolResultMap = new Map<string, { content: string; isError: boolean }>();
  for (const msg of messages) {
    if (msg.$type === "InternalToolResultMessage") {
      const trm = msg as InternalToolResultMessage;
      toolResultMap.set(trm.toolUseId, { content: trm.content, isError: trm.isError });
    }
  }

  for (let i = 0; i < messages.length; i++) {
    const msg = messages[i];

    switch (msg.$type) {
      case "InternalContentMessage": {
        const cm = msg as InternalContentMessage;
        if (cm.role === "User") {
          result.push({
            id: `msg-${i}`,
            role: "user",
            content: cm.content,
            boxes: [],
          });
        } else {
          result.push({
            id: `msg-${i}`,
            role: "assistant",
            content: cm.content,
            boxes: [{ type: "text", text: cm.content }],
          });
        }
        break;
      }

      case "InternalAssistantMessage": {
        const am = msg as InternalAssistantMessage;
        const boxes = convertContentBlocks(am.contentBlocks);

        // If there's textContent but no text block, add one
        if (am.textContent && !boxes.some((b) => b.type === "text")) {
          boxes.unshift({ type: "text", text: am.textContent });
        }

        // Merge tool results from subsequent InternalToolResultMessage entries
        for (let j = i + 1; j < messages.length; j++) {
          const next = messages[j];
          if (next.$type !== "InternalToolResultMessage") break;
          const trm = next as InternalToolResultMessage;
          const toolBox = boxes.find(
            (b) => b.type === "tool_use" && b.toolUseId === trm.toolUseId
          );
          if (toolBox && toolBox.type === "tool_use") {
            toolBox.result = trm.content;
            toolBox.success = !trm.isError;
            toolBox.isComplete = true;
          }
        }

        // Overlay agent info on spawn tool_use boxes
        overlayAgents(boxes, agentMap, toolResultMap);

        result.push({
          id: `msg-${i}`,
          role: "assistant",
          content: am.textContent ?? "",
          boxes,
        });
        break;
      }

      case "InternalToolResultMessage": {
        // Consumed by the preceding InternalAssistantMessage
        break;
      }
    }
  }

  return result;
}

/**
 * For each tool_use box that's a spawn tool, parse the tool result to extract
 * the agent_key, find the matching TrackedAgent, and attach an AgentGroupBox.
 */
function overlayAgents(
  boxes: ContentBox[],
  agentMap: Map<string, TrackedAgent>,
  toolResultMap: Map<string, { content: string; isError: boolean }>,
) {
  for (let i = 0; i < boxes.length; i++) {
    const box = boxes[i];
    if (box.type !== "tool_use" || !SPAWN_TOOL_NAMES.has(box.toolName)) continue;

    // Parse the tool result to get the agent_key
    const resultEntry = toolResultMap.get(box.toolUseId);
    if (!resultEntry) continue;

    let agentKey: string | undefined;
    let agentType: string | undefined;
    let label: string | undefined;
    try {
      const parsed = JSON.parse(resultEntry.content);
      agentKey = parsed.agent_key;
      agentType = parsed.agent_type;
      label = parsed.label;
    } catch {
      continue;
    }

    if (!agentKey) continue;

    const tracked = agentMap.get(agentKey);

    // If we have TrackedAgent data, use it for status. Otherwise assume completed
    // (the registry grain is in-memory only, so old conversations won't have entries).
    const isComplete = tracked
      ? tracked.status !== "Pending"
      : true;
    const completeReason = tracked
      ? tracked.status === "Completed" ? "completed"
        : tracked.status === "Failed" ? "failed"
        : tracked.status === "TimedOut" ? "failed"
        : undefined
      : "completed";

    // Build the agent group content boxes
    const agentBoxes: ContentBox[] = [];
    if (tracked?.result?.content) {
      agentBoxes.push({ type: "text", text: tracked.result.content });
    }

    // Find child agents (agents whose parentAgentKey matches this agent)
    // and create nested agent_group boxes for them
    for (const [childKey, childAgent] of agentMap) {
      if (childAgent.parentAgentKey === agentKey) {
        const childComplete = childAgent.status !== "Pending";
        const childBoxes: ContentBox[] = [];
        if (childAgent.result?.content) {
          childBoxes.push({ type: "text", text: childAgent.result.content });
        }
        agentBoxes.push({
          type: "agent_group",
          agentKey: childKey,
          agentType: "researcher",
          label: childAgent.label,
          boxes: childBoxes,
          isComplete: childComplete,
          completeReason: childComplete
            ? childAgent.status === "Completed" ? "completed" : "failed"
            : undefined,
        });
      }
    }

    const subAgent: AgentGroupBox = {
      type: "agent_group",
      agentKey,
      agentType: agentType ?? "researcher",
      label: label ?? tracked?.label,
      boxes: agentBoxes,
      isComplete,
      completeReason,
    };

    boxes[i] = { ...box, subAgent };
  }
}

function convertContentBlocks(blocks: InternalContentBlock[]): ContentBox[] {
  const result: ContentBox[] = [];

  for (const block of blocks) {
    switch (block.$type) {
      case "InternalTextBlock":
        result.push({ type: "text", text: block.text });
        break;

      case "InternalThinkingBlock":
        result.push({ type: "thinking", text: block.text });
        break;

      case "InternalToolUseBlock":
        result.push({
          type: "tool_use",
          toolName: block.name,
          toolUseId: block.id,
          arguments: typeof block.input === "string"
            ? block.input
            : JSON.stringify(block.input),
        });
        break;

      case "InternalServerToolUseBlock":
        result.push({
          type: "tool_use",
          toolName: block.name,
          toolUseId: block.id,
          arguments: typeof block.input === "string"
            ? block.input
            : JSON.stringify(block.input),
        });
        break;

      case "InternalCitationBlock":
        result.push({
          type: "citation",
          title: block.title,
          url: block.url,
          citedText: block.citedText,
        });
        break;

      case "InternalWebSearchResultBlock":
        break;

      case "InternalWebFetchToolResultBlock":
        break;
    }
  }

  return result;
}

/**
 * Hook to memoize conversion of InternalMessage[] to ChatMessage[].
 */
export function useInternalToChat(messages: InternalMessage[], agents?: TrackedAgent[]): ChatMessage[] {
  return useMemo(() => internalToChat(messages, agents), [messages, agents]);
}
