import { AgentCard } from "@/components/agent-chat/AgentCard";
import type { ContentBox, ToolUseBox, AgentGroupBox, AgentCompleteReason } from "@donkeywork/api-client";

export type AgentEntry = {
  kind: "tool_use";
  box: ToolUseBox;
} | {
  kind: "agent_group";
  box: AgentGroupBox;
};

export function AgentCardGrid({
  boxes,
  onAgentClick,
  onAgentCancel,
}: {
  boxes: ContentBox[];
  onAgentClick: (entry: AgentEntry) => void;
  onAgentCancel?: (agentKey: string) => void;
}) {
  const agents: AgentEntry[] = [];
  for (const box of boxes) {
    if (box.type === "tool_use" && box.subAgent) {
      agents.push({ kind: "tool_use", box });
    } else if (box.type === "agent_group") {
      agents.push({ kind: "agent_group", box });
    }
  }

  if (agents.length === 0) return null;

  const isEntryComplete = (a: AgentEntry) =>
    a.kind === "tool_use" ? !!a.box.isComplete : !!a.box.isComplete;
  const active = agents.filter((a) => !isEntryComplete(a));
  const completed = agents.filter((a) => isEntryComplete(a));
  const sorted = [...active, ...completed];

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-2 mt-3">
      {sorted.map((entry, i) => {
        const agentType = entry.kind === "tool_use"
          ? entry.box.subAgent!.agentType
          : entry.box.agentType;
        const isComplete = isEntryComplete(entry);
        const innerBoxes = entry.kind === "tool_use"
          ? entry.box.subAgent!.boxes
          : entry.box.boxes;

        const agentKey = entry.kind === "tool_use"
          ? entry.box.subAgent!.agentKey
          : entry.box.agentKey;

        const completeReason: AgentCompleteReason | undefined = entry.kind === "tool_use"
          ? (entry.box.completeReason ?? entry.box.subAgent!.completeReason)
          : entry.box.completeReason;

        const label = entry.kind === "tool_use"
          ? entry.box.subAgent!.label
          : entry.box.label;

        return (
          <AgentCard
            key={entry.kind === "tool_use" ? entry.box.toolUseId : `ag-${i}`}
            agentType={agentType}
            label={label}
            isComplete={isComplete}
            completeReason={completeReason}
            boxes={innerBoxes}
            onClick={() => onAgentClick(entry)}
            onCancel={onAgentCancel && !isComplete ? () => onAgentCancel(agentKey) : undefined}
          />
        );
      })}
    </div>
  );
}
