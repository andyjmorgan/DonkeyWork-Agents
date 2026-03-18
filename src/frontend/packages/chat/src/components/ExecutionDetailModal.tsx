import { useState, useEffect } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  ScrollArea,
  Button,
} from "@donkeywork/ui";
import { agentExecutions } from "@donkeywork/api-client";
import type { AgentExecutionDetail } from "@donkeywork/api-client";
import { BoxList } from "./BoxRenderer";
import { internalToChat } from "./MessageRenderer";
import { useChatConfig } from "../context";
import {
  Check,
  X,
  Ban,
  Loader2,
  Timer,
  Cpu,
  ChevronDown,
  ChevronRight,
  MessageSquare,
  AlertTriangle,
} from "lucide-react";

function StatusBadge({ status }: { status: string }) {
  const lower = status.toLowerCase();
  const styles: Record<string, string> = {
    completed: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
    running: "bg-cyan-500/10 text-cyan-400 border-cyan-500/20",
    failed: "bg-red-500/10 text-red-400 border-red-500/20",
    cancelled: "bg-amber-500/10 text-amber-400 border-amber-500/20",
  };
  const icons: Record<string, React.ReactNode> = {
    completed: <Check className="w-3 h-3" />,
    failed: <X className="w-3 h-3" />,
    cancelled: <Ban className="w-3 h-3" />,
    running: <Loader2 className="w-3 h-3 animate-spin" />,
  };
  return (
    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium border ${styles[lower] ?? styles.running}`}>
      {icons[lower]}
      {status}
    </span>
  );
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString();
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
  if (count < 1000) return count.toLocaleString();
  return `${(count / 1000).toFixed(1)}k`;
}

function CollapsibleSection({
  title,
  defaultOpen = false,
  children,
}: {
  title: string;
  defaultOpen?: boolean;
  children: React.ReactNode;
}) {
  const [open, setOpen] = useState(defaultOpen);
  return (
    <div className="border border-border rounded-lg overflow-hidden">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center gap-2 px-3 py-2 text-xs font-medium text-foreground hover:bg-muted/30 transition-colors cursor-pointer"
      >
        {open ? <ChevronDown className="w-3 h-3" /> : <ChevronRight className="w-3 h-3" />}
        {title}
      </button>
      {open && (
        <div className="px-3 pb-3 border-t border-border">
          {children}
        </div>
      )}
    </div>
  );
}

export function ExecutionDetailModal({
  executionId,
  open,
  onOpenChange,
}: {
  executionId: string | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const { renderJson } = useChatConfig();
  const [detail, setDetail] = useState<AgentExecutionDetail | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [messagesVisible, setMessagesVisible] = useState(false);
  const [messageBoxes, setMessageBoxes] = useState<React.ReactNode | null>(null);
  const [messagesLoading, setMessagesLoading] = useState(false);

  useEffect(() => {
    if (!open || !executionId) return;

    setIsLoading(true);
    setError(null);
    setDetail(null);
    setMessagesVisible(false);
    setMessageBoxes(null);

    agentExecutions
      .get(executionId)
      .then(setDetail)
      .catch(() => setError("Failed to load execution details"))
      .finally(() => setIsLoading(false));
  }, [open, executionId]);

  const handleViewMessages = async () => {
    if (!executionId || messagesVisible) {
      setMessagesVisible((v) => !v);
      return;
    }
    setMessagesLoading(true);
    try {
      const response = await agentExecutions.getMessages(executionId);
      const chatMessages = internalToChat(response.messages);
      const boxes = chatMessages.flatMap((m) =>
        m.role === "assistant" ? m.boxes : []
      );
      setMessageBoxes(
        boxes.length > 0 ? (
          <BoxList boxes={boxes} isStreaming={false} />
        ) : (
          <p className="text-sm text-muted-foreground py-4 text-center">
            No messages recorded
          </p>
        )
      );
      setMessagesVisible(true);
    } catch {
      setMessageBoxes(
        <p className="text-sm text-red-400 py-4 text-center">
          Failed to load messages
        </p>
      );
      setMessagesVisible(true);
    } finally {
      setMessagesLoading(false);
    }
  };

  let contractJson: unknown = null;
  if (detail?.contractSnapshot) {
    try {
      contractJson = JSON.parse(detail.contractSnapshot);
    } catch {
      contractJson = null;
    }
  }

  const typeColor: Record<string, { bg: string; border: string; text: string }> = {
    conversation: { bg: "bg-cyan-500/10", border: "border-cyan-500/20", text: "text-cyan-400" },
    delegate: { bg: "bg-purple-500/10", border: "border-purple-500/20", text: "text-purple-400" },
    agent: { bg: "bg-blue-500/10", border: "border-blue-500/20", text: "text-blue-400" },
  };
  const color = detail
    ? typeColor[detail.agentType.toLowerCase()] ?? typeColor.agent
    : typeColor.agent;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-4xl max-h-[85vh] flex flex-col gap-0 p-0 overflow-hidden bg-card border-border">
        {isLoading ? (
          <div className="flex flex-col items-center justify-center py-16">
            <Loader2 className="w-6 h-6 text-cyan-400 animate-spin mb-2" />
            <p className="text-sm text-muted-foreground">Loading execution...</p>
          </div>
        ) : error ? (
          <div className="flex flex-col items-center justify-center py-16">
            <AlertTriangle className="w-6 h-6 text-red-400 mb-2" />
            <p className="text-sm text-red-400">{error}</p>
          </div>
        ) : detail ? (
          <>
            <DialogHeader className="px-6 pt-6 pb-3 border-b border-border">
              <DialogTitle className="flex items-center gap-3 flex-wrap">
                <span className={`rounded-lg ${color.bg} ${color.text} px-2.5 py-1 text-xs uppercase tracking-wider font-semibold border ${color.border}`}>
                  {detail.agentType}
                </span>
                <StatusBadge status={detail.status} />
                {detail.modelId && (
                  <span className="text-xs text-muted-foreground font-mono">
                    {detail.modelId}
                  </span>
                )}
              </DialogTitle>
              {detail.label && (
                <DialogDescription className="text-sm text-muted-foreground mt-1 truncate">
                  {detail.label}
                </DialogDescription>
              )}
              <p className="text-[10px] font-mono text-muted-foreground mt-0.5 select-all">
                {detail.grainKey}
              </p>
            </DialogHeader>

            <ScrollArea className="flex-1 min-h-0 overflow-y-auto">
              <div className="px-6 py-4 flex flex-col gap-4">
                {/* Stats row */}
                <div className="flex flex-wrap items-center gap-4 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <Timer className="w-3.5 h-3.5" />
                    Started {formatDate(detail.startedAt)}
                  </span>
                  {detail.durationMs != null && (
                    <span className="flex items-center gap-1">
                      <Timer className="w-3.5 h-3.5" />
                      {formatDuration(detail.durationMs)}
                    </span>
                  )}
                  {detail.inputTokensUsed != null && (
                    <span className="flex items-center gap-1">
                      <Cpu className="w-3.5 h-3.5" />
                      In: {formatTokens(detail.inputTokensUsed)}
                    </span>
                  )}
                  {detail.outputTokensUsed != null && (
                    <span className="flex items-center gap-1">
                      <Cpu className="w-3.5 h-3.5" />
                      Out: {formatTokens(detail.outputTokensUsed)}
                    </span>
                  )}
                </div>

                {/* Error message */}
                {detail.errorMessage && (
                  <div className="rounded-lg border border-red-500/20 bg-red-500/5 px-4 py-3">
                    <div className="flex items-center gap-2 mb-1">
                      <AlertTriangle className="w-3.5 h-3.5 text-red-400" />
                      <span className="text-xs font-medium text-red-400">Error</span>
                    </div>
                    <p className="text-sm text-red-300 whitespace-pre-wrap">{detail.errorMessage}</p>
                  </div>
                )}

                {/* Input */}
                {detail.input && (
                  <CollapsibleSection title="Input">
                    <pre className="text-xs text-muted-foreground whitespace-pre-wrap mt-2 font-mono">
                      {detail.input}
                    </pre>
                  </CollapsibleSection>
                )}

                {/* Output */}
                {detail.output && (
                  <CollapsibleSection title="Output" defaultOpen>
                    <pre className="text-xs text-muted-foreground whitespace-pre-wrap mt-2 font-mono">
                      {detail.output}
                    </pre>
                  </CollapsibleSection>
                )}

                {/* Contract snapshot */}
                {contractJson != null && (
                  <CollapsibleSection title="Contract Snapshot">
                    <div className="mt-2">
                      {renderJson(contractJson, { collapsed: 2, className: "text-xs" })}
                    </div>
                  </CollapsibleSection>
                )}

                {/* Messages */}
                <div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleViewMessages}
                    disabled={messagesLoading}
                    className="gap-1.5"
                  >
                    {messagesLoading ? (
                      <Loader2 className="w-3.5 h-3.5 animate-spin" />
                    ) : (
                      <MessageSquare className="w-3.5 h-3.5" />
                    )}
                    {messagesVisible ? "Hide Messages" : "View Messages"}
                  </Button>
                  {messagesVisible && messageBoxes && (
                    <div className="mt-3 rounded-lg border border-border p-4">
                      {messageBoxes}
                    </div>
                  )}
                </div>
              </div>
            </ScrollArea>
          </>
        ) : null}
      </DialogContent>
    </Dialog>
  );
}
