import { useState, useEffect } from "react";
import { ScrollArea } from "@donkeywork/ui";
import { agentExecutions } from "@donkeywork/api-client";
import type { AgentExecutionSummary } from "@donkeywork/api-client";
import { PulseDots } from "./PulseDots";
import {
  PanelRightClose,
  Check,
  X,
  Loader2,
  Ban,
  Timer,
  Cpu,
} from "lucide-react";

const TYPE_COLORS: Record<string, { bg: string; border: string; text: string }> = {
  conversation: { bg: "bg-cyan-500/10", border: "border-cyan-500/25", text: "text-cyan-400" },
  delegate: { bg: "bg-purple-500/10", border: "border-purple-500/25", text: "text-purple-400" },
  agent: { bg: "bg-blue-500/10", border: "border-blue-500/25", text: "text-blue-400" },
};

function getTypeColor(agentType: string) {
  return TYPE_COLORS[agentType.toLowerCase()] ?? TYPE_COLORS.agent;
}

function StatusIcon({ status }: { status: string }) {
  switch (status.toLowerCase()) {
    case "completed":
      return <Check className="w-3 h-3 text-emerald-400" strokeWidth={2.5} />;
    case "failed":
      return <X className="w-3 h-3 text-red-400" strokeWidth={2.5} />;
    case "cancelled":
      return <Ban className="w-3 h-3 text-amber-400" strokeWidth={2.5} />;
    case "running":
      return <PulseDots color="bg-cyan-400" size="w-1 h-1" />;
    default:
      return <Loader2 className="w-3 h-3 text-muted-foreground animate-spin" />;
  }
}

function formatDuration(ms: number): string {
  if (ms < 1000) return `${ms}ms`;
  const seconds = ms / 1000;
  if (seconds < 60) return `${seconds.toFixed(1)}s`;
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = Math.round(seconds % 60);
  return `${minutes}m ${remainingSeconds}s`;
}

function formatTokens(count: number): string {
  if (count < 1000) return `${count}`;
  return `${(count / 1000).toFixed(1)}k`;
}

function ExecutionCard({
  execution,
  onClick,
}: {
  execution: AgentExecutionSummary;
  onClick: () => void;
}) {
  const color = getTypeColor(execution.agentType);

  return (
    <button
      type="button"
      onClick={onClick}
      className={`w-full flex items-center gap-2 rounded-lg border ${color.border} ${color.bg} px-2 py-1.5 text-left transition-all hover:brightness-125 cursor-pointer`}
    >
      <div className="shrink-0">
        <StatusIcon status={execution.status} />
      </div>

      <div className="flex flex-col gap-0.5 min-w-0 flex-1">
        <span className={`text-[10px] uppercase tracking-wider font-semibold ${color.text} truncate`}>
          {execution.agentType}
        </span>
        {execution.label && (
          <span className="text-[10px] text-muted-foreground truncate">
            {execution.label}
          </span>
        )}
        <div className="flex items-center gap-2 text-[10px] text-muted-foreground">
          {execution.durationMs != null && (
            <span className="flex items-center gap-0.5">
              <Timer className="w-2.5 h-2.5" />
              {formatDuration(execution.durationMs)}
            </span>
          )}
          {(execution.inputTokensUsed != null || execution.outputTokensUsed != null) && (
            <span className="flex items-center gap-0.5">
              <Cpu className="w-2.5 h-2.5" />
              {formatTokens((execution.inputTokensUsed ?? 0) + (execution.outputTokensUsed ?? 0))}
            </span>
          )}
          {execution.modelId && (
            <span className="truncate">{execution.modelId}</span>
          )}
        </div>
      </div>
    </button>
  );
}

export function ExecutionSidePanel({
  conversationId,
  isOpen,
  onToggle,
  onExecutionClick,
}: {
  conversationId: string;
  isOpen: boolean;
  onToggle: () => void;
  onExecutionClick: (executionId: string) => void;
}) {
  const [executions, setExecutions] = useState<AgentExecutionSummary[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) return;

    setIsLoading(true);
    setError(null);
    agentExecutions
      .listByConversation(conversationId)
      .then(r => setExecutions(r.items))
      .catch(() => setError("Failed to load executions"))
      .finally(() => setIsLoading(false));
  }, [isOpen, conversationId]);

  if (!isOpen) return null;

  return (
    <div className="w-64 shrink-0 flex flex-col border-l border-border bg-card/50">
      <div className="flex items-center justify-between px-3 py-3">
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-foreground">Executions</span>
          {executions.length > 0 && (
            <span className="text-[10px] text-muted-foreground">{executions.length}</span>
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
          {isLoading ? (
            <div className="flex flex-col items-center justify-center py-8">
              <Loader2 className="w-5 h-5 text-cyan-400 animate-spin mb-2" />
              <p className="text-[10px] text-muted-foreground">Loading...</p>
            </div>
          ) : error ? (
            <p className="text-[10px] text-red-400 text-center py-4">{error}</p>
          ) : executions.length === 0 ? (
            <p className="text-[10px] text-muted-foreground text-center py-4">
              No executions recorded yet
            </p>
          ) : (
            executions.map((exec) => (
              <ExecutionCard
                key={exec.id}
                execution={exec}
                onClick={() => onExecutionClick(exec.id)}
              />
            ))
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
