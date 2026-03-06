import { useState } from "react";
import { ScrollArea } from "@donkeywork/ui";
import type { McpServerStatus } from "@/types/agent-chat";
import { Check, X, PanelRightClose, ChevronDown, ChevronRight } from "lucide-react";

function ServerEntry({ server }: { server: McpServerStatus }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div
      className={`rounded-lg border px-2.5 py-2 ${
        server.success
          ? "border-emerald-500/25 bg-emerald-500/5"
          : "border-red-500/25 bg-red-500/5"
      }`}
    >
      <div className="flex items-center gap-2">
        <div className="shrink-0">
          {server.success ? (
            <Check className="w-3.5 h-3.5 text-emerald-400" strokeWidth={2.5} />
          ) : (
            <X className="w-3.5 h-3.5 text-red-400" strokeWidth={2.5} />
          )}
        </div>

        <div className="flex flex-col gap-0.5 min-w-0 flex-1">
          <span className="text-xs font-medium text-foreground truncate">
            {server.name}
          </span>
          <div className="flex items-center gap-2 text-[10px] text-muted-foreground">
            <span>{server.durationMs}ms</span>
            {server.success && (
              <>
                <span className="text-muted-foreground/40">·</span>
                <span>{server.toolCount} tool{server.toolCount !== 1 ? "s" : ""}</span>
              </>
            )}
          </div>
        </div>

        {server.error && (
          <button
            type="button"
            onClick={() => setExpanded((v) => !v)}
            className="shrink-0 p-0.5 rounded text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
          >
            {expanded ? (
              <ChevronDown className="w-3 h-3" />
            ) : (
              <ChevronRight className="w-3 h-3" />
            )}
          </button>
        )}
      </div>

      {server.error && expanded && (
        <div className="mt-1.5 px-5">
          <p className="text-[10px] text-red-400/80 break-words leading-relaxed">
            {server.error}
          </p>
        </div>
      )}
    </div>
  );
}

export function McpSidePanel({
  servers,
  isOpen,
  onToggle,
}: {
  servers: McpServerStatus[];
  isOpen: boolean;
  onToggle: () => void;
}) {
  if (!isOpen) return null;

  const connectedCount = servers.filter((s) => s.success).length;
  const failedCount = servers.filter((s) => !s.success).length;

  return (
    <div className="w-64 shrink-0 flex flex-col border-l border-border bg-card/50">
      <div className="flex items-center justify-between px-3 py-3">
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-foreground">MCP Servers</span>
          <span className="flex items-center gap-1 px-1.5 py-0.5 rounded-full bg-emerald-500/10 border border-emerald-500/20">
            <span className="text-[10px] text-emerald-400 font-medium">{connectedCount}</span>
          </span>
          {failedCount > 0 && (
            <span className="flex items-center gap-1 px-1.5 py-0.5 rounded-full bg-red-500/10 border border-red-500/20">
              <span className="text-[10px] text-red-400 font-medium">{failedCount}</span>
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
        <div className="flex flex-col gap-1.5 p-2">
          {servers.map((server) => (
            <ServerEntry key={server.name} server={server} />
          ))}
          {servers.length === 0 && (
            <p className="text-[10px] text-muted-foreground text-center py-4">
              No MCP servers connected
            </p>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
