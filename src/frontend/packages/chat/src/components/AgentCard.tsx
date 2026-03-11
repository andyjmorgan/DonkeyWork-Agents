import { PulseDots } from "./PulseDots";
import type { ContentBox, AgentCompleteReason } from "@donkeywork/api-client";
import { Check, X, Ban, AlertTriangle } from "lucide-react";

const AGENT_COLORS: Record<string, { bg: string; border: string; text: string; dotBg: string; glow: string }> = {
  custom: { bg: "bg-blue-500/10", border: "border-blue-500/25", text: "text-blue-400", dotBg: "bg-blue-400", glow: "hover:shadow-blue-500/10" },
  websearch: { bg: "bg-cyan-500/10", border: "border-cyan-500/25", text: "text-cyan-400", dotBg: "bg-cyan-400", glow: "hover:shadow-cyan-500/10" },
};

function getAgentColor(agentType: string) {
  const key = agentType.toLowerCase().replace(/[_\s-]/g, "");
  return AGENT_COLORS[key] ?? { bg: "bg-cyan-500/10", border: "border-cyan-500/25", text: "text-cyan-400", dotBg: "bg-cyan-400", glow: "hover:shadow-cyan-500/10" };
}

function countChildAgents(boxes: ContentBox[]): { total: number; completed: number } {
  let total = 0;
  let completed = 0;
  for (const b of boxes) {
    if (b.type === "tool_use" && b.subAgent) {
      total++;
      if (b.subAgent.isComplete || b.isComplete) completed++;
    } else if (b.type === "agent_group") {
      total++;
      if (b.isComplete) completed++;
    }
  }
  return { total, completed };
}

export function AgentCard({
  agentType,
  label,
  isComplete,
  completeReason,
  boxes,
  onClick,
  onCancel,
}: {
  agentType: string;
  label?: string;
  isComplete: boolean;
  completeReason?: AgentCompleteReason;
  boxes: ContentBox[];
  onClick: () => void;
  onCancel?: () => void;
}) {
  const searchCount = boxes.filter((b) => b.type === "tool_use" && b.toolName === "web_search").length;
  const citationCount = boxes.filter((b) => b.type === "citation").length;
  const { total: childTotal, completed: childDone } = countChildAgents(boxes);
  const badges: string[] = [];
  if (childTotal > 0) badges.push(`${childDone}/${childTotal} agents`);
  if (searchCount > 0) badges.push(`${searchCount} search${searchCount > 1 ? "es" : ""}`);
  if (citationCount > 0) badges.push(`${citationCount} citation${citationCount > 1 ? "s" : ""}`);

  const color = getAgentColor(agentType);

  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex items-center justify-between gap-2 rounded-xl border ${color.border} ${color.bg} px-3 py-2.5 text-left transition-all hover:shadow-lg ${color.glow} cursor-pointer`}
    >
      <div className="flex flex-col gap-1 min-w-0">
        <span className={`rounded-md ${color.bg} ${color.text} px-2 py-0.5 text-[10px] uppercase tracking-wider font-semibold w-fit`}>
          {agentType}
        </span>
        {label && (
          <span className="text-xs text-muted-foreground truncate">
            {label}
          </span>
        )}
        {badges.length > 0 && (
          <span className="text-[10px] text-muted-foreground truncate">
            {badges.join(" \u00b7 ")}
          </span>
        )}
      </div>
      <div className="shrink-0 flex items-center gap-2">
        {childTotal > 0 && !isComplete && (
          <span className={`text-[10px] font-medium ${color.text}`}>
            {childDone}/{childTotal}
          </span>
        )}
        {isComplete ? (
          completeReason === "cancelled" ? (
            <Ban className="w-4 h-4 text-red-400" strokeWidth={2.5} />
          ) : completeReason === "failed" ? (
            <AlertTriangle className="w-4 h-4 text-amber-400" strokeWidth={2.5} />
          ) : (
            <Check className="w-4 h-4 text-emerald-400" strokeWidth={2.5} />
          )
        ) : (
          <>
            <PulseDots color={color.dotBg} />
            {onCancel && (
              <button
                type="button"
                onClick={(e) => { e.stopPropagation(); onCancel(); }}
                className="p-0.5 rounded-md bg-red-500/10 border border-red-500/20 text-red-400 hover:bg-red-500/25 transition-colors cursor-pointer"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            )}
          </>
        )}
      </div>
    </button>
  );
}
