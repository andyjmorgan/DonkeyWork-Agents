import { useState, useRef, useEffect } from "react";
import { ScrollArea } from "@donkeywork/ui";
import { PanelRightClose, Trash2, ChevronDown, ChevronRight, Maximize2, Minimize2, Download } from "lucide-react";
import type { SocketEvent } from "../hooks/useAgentConversation";

const EVENT_COLORS: Record<string, string> = {
  turn_start: "text-cyan-400",
  turn_end: "text-cyan-400",
  message: "text-foreground",
  thinking: "text-purple-400",
  tool_use: "text-amber-400",
  tool_result: "text-amber-400",
  tool_complete: "text-amber-400",
  agent_spawn: "text-blue-400",
  agent_complete: "text-blue-400",
  agent_result_data: "text-blue-400",
  usage: "text-muted-foreground",
  web_search: "text-cyan-400",
  web_search_complete: "text-cyan-400",
  mcp_server_status: "text-emerald-400",
  sandbox_status: "text-emerald-400",
  error: "text-red-400",
  complete: "text-emerald-400",
  progress: "text-muted-foreground",
  queue_status: "text-muted-foreground",
  cancelled: "text-red-400",
};

function formatTime(ts: number): string {
  const d = new Date(ts);
  return d.toLocaleTimeString("en-US", { hour12: false, hour: "2-digit", minute: "2-digit", second: "2-digit", fractionalSecondDigits: 3 });
}

function shortKey(key: string): string {
  if (!key) return "";
  const parts = key.split(":");
  const last = parts[parts.length - 1];
  return last.length > 8 ? last.slice(0, 8) : last;
}

function EventRow({ event }: { event: SocketEvent }) {
  const [expanded, setExpanded] = useState(false);
  const color = EVENT_COLORS[event.eventType] ?? "text-muted-foreground";

  let summary = "";
  if (event.eventType === "message" || event.eventType === "thinking") {
    const text = (event.data.text as string) ?? "";
    summary = text.length > 60 ? text.slice(0, 60) + "..." : text;
  } else if (event.eventType === "tool_use") {
    summary = (event.data.toolName as string) ?? "";
  } else if (event.eventType === "tool_result") {
    summary = (event.data.toolName as string) ?? "";
  } else if (event.eventType === "agent_spawn") {
    summary = (event.data.displayName as string) ?? (event.data.agentType as string) ?? "";
  } else if (event.eventType === "agent_complete") {
    summary = (event.data.reason as string) ?? "";
  } else if (event.eventType === "usage") {
    summary = `in=${event.data.inputTokens} out=${event.data.outputTokens}`;
  } else if (event.eventType === "error") {
    summary = (event.data.error as string) ?? "";
  }

  return (
    <div className="border-b border-border/50 last:border-b-0">
      <button
        type="button"
        onClick={() => setExpanded(!expanded)}
        className="w-full flex items-center gap-1.5 px-2 py-1 text-left hover:bg-muted/30 transition-colors cursor-pointer"
      >
        <span className="shrink-0 text-muted-foreground">
          {expanded ? <ChevronDown className="w-2.5 h-2.5" /> : <ChevronRight className="w-2.5 h-2.5" />}
        </span>
        <span className="shrink-0 text-[9px] font-mono text-muted-foreground w-[72px]">
          {formatTime(event.timestamp)}
        </span>
        <span className={`shrink-0 text-[10px] font-semibold w-[100px] truncate ${color}`}>
          {event.eventType}
        </span>
        <span className="text-[9px] font-mono text-muted-foreground w-[56px] truncate shrink-0">
          {shortKey(event.agentKey)}
        </span>
        <span className="text-[10px] text-muted-foreground truncate flex-1 min-w-0">
          {summary}
        </span>
      </button>
      {expanded && (
        <div className="px-2 pb-2 ml-4">
          {event.debug && (
            <div className="text-[10px] text-amber-400/80 font-mono mb-1 px-2 py-1 rounded bg-amber-500/5 border border-amber-500/10">
              {event.debug}
            </div>
          )}
          <pre className="text-[10px] text-muted-foreground font-mono whitespace-pre-wrap break-all bg-muted/20 rounded px-2 py-1 max-h-48 overflow-y-auto">
            {JSON.stringify(event.data, null, 2)}
          </pre>
        </div>
      )}
    </div>
  );
}

export function SocketEventPanel({
  events,
  isOpen,
  onToggle,
  onClear,
}: {
  events: SocketEvent[];
  isOpen: boolean;
  onToggle: () => void;
  onClear: () => void;
}) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [autoScroll, setAutoScroll] = useState(true);
  const [wide, setWide] = useState(false);

  useEffect(() => {
    if (autoScroll && scrollRef.current) {
      const el = scrollRef.current.querySelector("[data-radix-scroll-area-viewport]");
      if (el) el.scrollTop = el.scrollHeight;
    }
  }, [events.length, autoScroll]);

  if (!isOpen) return null;

  return (
    <div className={`${wide ? "w-[600px]" : "w-80"} shrink-0 flex flex-col border-l border-border bg-card/50 transition-all`}>
      <div className="flex items-center justify-between px-3 py-3">
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-foreground">Socket Events</span>
          <span className="text-[10px] text-muted-foreground">{events.length}</span>
        </div>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => setAutoScroll(!autoScroll)}
            className={`px-1.5 py-0.5 rounded text-[9px] font-medium transition-colors cursor-pointer ${
              autoScroll ? "bg-cyan-500/10 text-cyan-400 border border-cyan-500/20" : "text-muted-foreground hover:text-foreground"
            }`}
          >
            auto
          </button>
          <button
            type="button"
            onClick={() => setWide(!wide)}
            className="p-1 rounded-md text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
          >
            {wide ? <Minimize2 className="w-3.5 h-3.5" /> : <Maximize2 className="w-3.5 h-3.5" />}
          </button>
          <button
            type="button"
            onClick={() => {
              const blob = new Blob([JSON.stringify(events, null, 2)], { type: "application/json" });
              const url = URL.createObjectURL(blob);
              const a = document.createElement("a");
              a.href = url;
              a.download = `socket-events-${new Date().toISOString().slice(0, 19).replace(/:/g, "-")}.json`;
              a.click();
              URL.revokeObjectURL(url);
            }}
            className="p-1 rounded-md text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
            title="Export events as JSON"
          >
            <Download className="w-3.5 h-3.5" />
          </button>
          <button
            type="button"
            onClick={onClear}
            className="p-1 rounded-md text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
          >
            <Trash2 className="w-3.5 h-3.5" />
          </button>
          <button
            type="button"
            onClick={onToggle}
            className="p-1 rounded-md text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
          >
            <PanelRightClose className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
      <div className="h-px bg-gradient-to-r from-transparent via-border to-transparent" />

      <ScrollArea ref={scrollRef} className="flex-1">
        <div className="flex flex-col">
          {events.map((evt) => (
            <EventRow key={evt.id} event={evt} />
          ))}
          {events.length === 0 && (
            <p className="text-[10px] text-muted-foreground text-center py-8">
              No events yet
            </p>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
