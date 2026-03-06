import { useMemo, useState } from "react";
import { PulseDots } from "@/components/agent-chat/PulseDots";
import { ScrollArea } from "@donkeywork/ui";
import type { ChatMessage } from "@/types/agent-chat";
import type { SidePanelAgent } from "@/components/agent-chat/agentTreeUtils";
import { extractAgentTree, countAll, countActive } from "@/components/agent-chat/agentTreeUtils";
import { Check, ChevronRight, PanelRightClose, Ban, AlertTriangle } from "lucide-react";

const AGENT_COLORS: Record<string, { bg: string; border: string; text: string; dotBg: string }> = {
  research: { bg: "bg-blue-500/10", border: "border-blue-500/25", text: "text-blue-400", dotBg: "bg-blue-400" },
  deepresearch: { bg: "bg-purple-500/10", border: "border-purple-500/25", text: "text-purple-400", dotBg: "bg-purple-400" },
  websearch: { bg: "bg-cyan-500/10", border: "border-cyan-500/25", text: "text-cyan-400", dotBg: "bg-cyan-400" },
};

function getColor(agentType: string) {
  const key = agentType.toLowerCase().replace(/[_\s-]/g, "");
  return AGENT_COLORS[key] ?? AGENT_COLORS.websearch;
}

function AgentTreeNode({
  agent,
  depth,
  collapsed,
  onToggleCollapse,
  onClick,
}: {
  agent: SidePanelAgent;
  depth: number;
  collapsed: Set<string>;
  onToggleCollapse: (key: string) => void;
  onClick: (agent: SidePanelAgent) => void;
}) {
  const color = getColor(agent.agentType);
  const searchCount = agent.boxes.filter((b) => b.type === "tool_use" && b.toolName === "web_search").length;
  const citationCount = agent.boxes.filter((b) => b.type === "citation").length;
  const hasChildren = agent.children.length > 0;
  const isCollapsed = collapsed.has(agent.agentKey);

  const stats: string[] = [];
  if (searchCount > 0) stats.push(`${searchCount} search${searchCount > 1 ? "es" : ""}`);
  if (citationCount > 0) stats.push(`${citationCount} citation${citationCount > 1 ? "s" : ""}`);

  return (
    <>
      <div
        className="flex items-center gap-0.5"
        style={{ paddingLeft: `${depth * 12}px` }}
      >
        {hasChildren ? (
          <button
            type="button"
            onClick={(e) => { e.stopPropagation(); onToggleCollapse(agent.agentKey); }}
            className="shrink-0 p-0.5 rounded text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
          >
            <ChevronRight
              className={`w-3 h-3 transition-transform ${isCollapsed ? "" : "rotate-90"}`}
            />
          </button>
        ) : (
          <div className="w-4 shrink-0" />
        )}

        <button
          type="button"
          onClick={() => onClick(agent)}
          className={`flex-1 min-w-0 flex items-center gap-2 rounded-lg border ${color.border} ${color.bg} px-2 py-1.5 text-left transition-all hover:brightness-125 cursor-pointer`}
        >
          <div className="shrink-0">
            {agent.isComplete ? (
              agent.completeReason === "cancelled" ? (
                <Ban className="w-3 h-3 text-red-400" strokeWidth={2.5} />
              ) : agent.completeReason === "failed" ? (
                <AlertTriangle className="w-3 h-3 text-amber-400" strokeWidth={2.5} />
              ) : (
                <Check className="w-3 h-3 text-emerald-400" strokeWidth={2.5} />
              )
            ) : (
              <PulseDots color={color.dotBg} size="w-1 h-1" />
            )}
          </div>

          <div className="flex flex-col gap-0.5 min-w-0 flex-1">
            <span className={`text-[10px] uppercase tracking-wider font-semibold ${color.text} truncate`}>
              {agent.agentType}
            </span>
            {agent.label && agent.label !== agent.agentType && (
              <span className="text-[10px] text-muted-foreground truncate">
                {agent.label}
              </span>
            )}
            {stats.length > 0 && (
              <span className="text-[10px] text-muted-foreground truncate">
                {stats.join(" \u00b7 ")}
              </span>
            )}
          </div>

          {hasChildren && (
            <span className="shrink-0 text-[10px] text-muted-foreground">
              {agent.children.filter((c) => c.isComplete).length}/{agent.children.length}
            </span>
          )}
        </button>
      </div>

      {hasChildren && !isCollapsed && agent.children.map((child) => (
        <AgentTreeNode
          key={child.agentKey}
          agent={child}
          depth={depth + 1}
          collapsed={collapsed}
          onToggleCollapse={onToggleCollapse}
          onClick={onClick}
        />
      ))}
    </>
  );
}

export function AgentSidePanel({
  messages,
  isOpen,
  onToggle,
  onAgentClick,
}: {
  messages: ChatMessage[];
  isOpen: boolean;
  onToggle: () => void;
  onAgentClick: (agent: SidePanelAgent) => void;
}) {
  const tree = useMemo(() => extractAgentTree(messages), [messages]);
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());

  const totalCount = useMemo(() => countAll(tree), [tree]);
  const activeCount = useMemo(() => countActive(tree), [tree]);
  const hasAgents = totalCount > 0;

  const toggleCollapse = (key: string) => {
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  };

  if (!isOpen) return null;

  return (
    <div className="w-64 shrink-0 flex flex-col border-l border-border bg-card/50">
      <div className="flex items-center justify-between px-3 py-3">
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-foreground">Agents</span>
          {activeCount > 0 && (
            <span className="flex items-center gap-1 px-1.5 py-0.5 rounded-full bg-cyan-500/10 border border-cyan-500/20">
              <span className="flex h-1.5 w-1.5 rounded-full bg-cyan-400 animate-pulse" />
              <span className="text-[10px] text-cyan-400 font-medium">{activeCount}</span>
            </span>
          )}
        </div>
        <button
          type="button"
          onClick={onToggle}
          className="p-1 rounded-md text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
        >
          <PanelRightClose className="w-3.5 h-3.5" />
        </button>
      </div>
      <div className="h-px bg-gradient-to-r from-transparent via-border to-transparent" />

      <ScrollArea className="flex-1">
        <div className="flex flex-col gap-1 p-2">
          {tree.map((agent) => (
            <AgentTreeNode
              key={agent.agentKey}
              agent={agent}
              depth={0}
              collapsed={collapsed}
              onToggleCollapse={toggleCollapse}
              onClick={onAgentClick}
            />
          ))}
          {!hasAgents && (
            <p className="text-[10px] text-muted-foreground text-center py-4">
              No agents spawned yet
            </p>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
