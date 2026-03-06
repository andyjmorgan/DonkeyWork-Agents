import type { ChatMessage, ContentBox, AgentCompleteReason } from "@donkeywork/api-client";

export type SidePanelAgent = {
  agentKey: string;
  agentType: string;
  label: string;
  isComplete: boolean;
  completeReason?: AgentCompleteReason;
  boxes: ContentBox[];
  messageId: string;
  toolUseId?: string;
  children: SidePanelAgent[];
};

/** Walk all messages and build an agent tree preserving parent->child nesting. */
export function extractAgentTree(messages: ChatMessage[]): SidePanelAgent[] {
  const roots: SidePanelAgent[] = [];
  const seen = new Set<string>();

  function walkBoxes(boxes: ContentBox[], messageId: string): SidePanelAgent[] {
    const nodes: SidePanelAgent[] = [];

    for (const box of boxes) {
      if (box.type === "tool_use" && box.subAgent) {
        const key = box.subAgent.agentKey;
        if (seen.has(key)) continue;
        seen.add(key);

        const children = walkBoxes(box.subAgent.boxes, messageId);
        nodes.push({
          agentKey: key,
          agentType: box.subAgent.agentType,
          label: box.displayName ?? box.toolName,
          isComplete: !!box.subAgent.isComplete || !!box.isComplete,
          completeReason: box.completeReason ?? box.subAgent.completeReason,
          boxes: box.subAgent.boxes,
          messageId,
          toolUseId: box.toolUseId,
          children,
        });
      } else if (box.type === "agent_group") {
        const key = box.agentKey;
        if (seen.has(key)) continue;
        seen.add(key);

        const children = walkBoxes(box.boxes, messageId);
        nodes.push({
          agentKey: key,
          agentType: box.agentType,
          label: box.label ?? box.agentType,
          isComplete: !!box.isComplete,
          completeReason: box.completeReason,
          boxes: box.boxes,
          messageId,
          children,
        });
      }
    }

    return nodes;
  }

  for (const msg of messages) {
    roots.push(...walkBoxes(msg.boxes, msg.id));
  }

  return roots;
}

/** Count all agents in a tree (including nested). */
export function countAll(nodes: SidePanelAgent[]): number {
  let n = 0;
  for (const node of nodes) {
    n += 1 + countAll(node.children);
  }
  return n;
}

/** Count active (incomplete) agents in a tree. */
export function countActive(nodes: SidePanelAgent[]): number {
  let n = 0;
  for (const node of nodes) {
    if (!node.isComplete) n++;
    n += countActive(node.children);
  }
  return n;
}
