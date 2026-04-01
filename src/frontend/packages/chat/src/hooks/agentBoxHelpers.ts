import type { ContentBox, AgentGroupBox, ChatMessage } from "@donkeywork/api-client";

export type AgentGroupEntry = { messageId: string; boxIndex: number };

export const SPAWN_TOOL_NAMES = new Set(["spawn_agent", "spawn_delegate"]);

export function updateNestedGroup(
  box: ContentBox,
  targetKey: string,
  updater: (innerBoxes: ContentBox[]) => ContentBox[]
): ContentBox | null {
  if (box.type === "tool_use" && box.subAgent) {
    if (box.subAgent.agentKey === targetKey) {
      return { ...box, subAgent: { ...box.subAgent, boxes: updater(box.subAgent.boxes) } };
    }
    for (let i = 0; i < box.subAgent.boxes.length; i++) {
      const result = updateNestedGroup(box.subAgent.boxes[i], targetKey, updater);
      if (result) {
        const newInner = [...box.subAgent.boxes];
        newInner[i] = result;
        return { ...box, subAgent: { ...box.subAgent, boxes: newInner } };
      }
    }
  }
  if (box.type === "agent_group") {
    if (box.agentKey === targetKey) {
      return { ...box, boxes: updater(box.boxes) };
    }
    for (let i = 0; i < box.boxes.length; i++) {
      const result = updateNestedGroup(box.boxes[i], targetKey, updater);
      if (result) {
        const newInner = [...box.boxes];
        newInner[i] = result;
        return { ...box, boxes: newInner };
      }
    }
  }
  return null;
}

export function tryUpdateNested(
  boxes: ContentBox[],
  agentKey: string,
  hintIndex: number,
  updater: (innerBoxes: ContentBox[]) => ContentBox[]
): { boxes: ContentBox[]; boxIndex: number } | null {
  const newBoxes = [...boxes];

  if (hintIndex >= 0) {
    const host = newBoxes[hintIndex];
    if (host) {
      const updated = updateNestedGroup(host, agentKey, updater);
      if (updated) {
        newBoxes[hintIndex] = updated;
        return { boxes: newBoxes, boxIndex: hintIndex };
      }
    }
  }

  for (let i = 0; i < newBoxes.length; i++) {
    if (i === hintIndex) continue;
    const updated = updateNestedGroup(newBoxes[i], agentKey, updater);
    if (updated) {
      newBoxes[i] = updated;
      return { boxes: newBoxes, boxIndex: i };
    }
  }

  return null;
}

export function makeAgentGroup(
  spawnedKey: string,
  agentType: string,
  label?: string,
  icon?: string,
  displayName?: string,
): AgentGroupBox {
  return {
    type: "agent_group",
    agentKey: spawnedKey,
    agentType,
    label,
    icon,
    displayName,
    boxes: [],
  };
}

export function attachToInner(
  innerBoxes: ContentBox[],
  newGroup: AgentGroupBox
): { boxes: ContentBox[]; attached: boolean } {
  const copy = [...innerBoxes];
  for (let i = copy.length - 1; i >= 0; i--) {
    const b = copy[i];
    if (b.type === "tool_use" && !b.subAgent) {
      copy[i] = { ...b, subAgent: newGroup };
      return { boxes: copy, attached: true };
    }
  }
  copy.push(newGroup);
  return { boxes: copy, attached: false };
}

/**
 * Attach a child agent inside its parent's agent group. Searches the indexed
 * message first (fast path), then scans all messages (slow path). Returns
 * updated messages and the resolved entry, or null if parent not found.
 */
export function attachChildAgent(
  messages: ChatMessage[],
  parentEntry: AgentGroupEntry,
  parentAgentKey: string,
  childGroup: AgentGroupBox,
  fallbackMessageId: string,
): { messages: ChatMessage[]; entry: AgentGroupEntry } {
  const attachInner = (inner: ContentBox[]) => attachToInner(inner, childGroup).boxes;

  // Fast path: try the indexed message/position
  const targetMsg = messages.find((m) => m.id === parentEntry.messageId);
  if (targetMsg) {
    const result = tryUpdateNested(targetMsg.boxes, parentAgentKey, parentEntry.boxIndex, attachInner);
    if (result) {
      return {
        messages: messages.map((m) => m.id === parentEntry.messageId ? { ...m, boxes: result.boxes } : m),
        entry: { messageId: parentEntry.messageId, boxIndex: result.boxIndex },
      };
    }
  }

  // Slow path: scan ALL messages for the parent agent group
  for (const m of messages) {
    for (let i = 0; i < m.boxes.length; i++) {
      const updated = updateNestedGroup(m.boxes[i], parentAgentKey, attachInner);
      if (updated) {
        const newBoxes = [...m.boxes];
        newBoxes[i] = updated;
        return {
          messages: messages.map((msg) => msg.id === m.id ? { ...msg, boxes: newBoxes } : msg),
          entry: { messageId: m.id, boxIndex: i },
        };
      }
    }
  }

  // Not found — append standalone to fallback message
  const fallbackId = parentEntry.messageId ?? fallbackMessageId;
  let newIdx = 0;
  const updated = messages.map((m) => {
    if (m.id !== fallbackId) return m;
    newIdx = m.boxes.length;
    return { ...m, boxes: [...m.boxes, childGroup] };
  });
  return { messages: updated, entry: { messageId: fallbackId, boxIndex: newIdx } };
}

/**
 * Attach a root-level agent to the last unattached spawn tool_use,
 * or append as a standalone agent_group.
 */
export function attachRootAgent(
  messages: ChatMessage[],
  targetMessageId: string,
  childGroup: AgentGroupBox,
): { messages: ChatMessage[]; entry: AgentGroupEntry } {
  let entry: AgentGroupEntry = { messageId: targetMessageId, boxIndex: -1 };
  const updated = messages.map((m) => {
    if (m.id !== targetMessageId) return m;
    const boxes = [...m.boxes];
    for (let i = boxes.length - 1; i >= 0; i--) {
      const b = boxes[i];
      if (b.type === "tool_use" && !b.subAgent && SPAWN_TOOL_NAMES.has(b.toolName)) {
        boxes[i] = { ...b, subAgent: childGroup };
        entry = { messageId: targetMessageId, boxIndex: i };
        return { ...m, boxes };
      }
    }
    entry = { messageId: targetMessageId, boxIndex: boxes.length };
    return { ...m, boxes: [...boxes, childGroup] };
  });
  return { messages: updated, entry };
}
