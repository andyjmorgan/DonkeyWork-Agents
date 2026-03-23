import { useRef, useEffect, useState, useMemo, useCallback, type FormEvent } from "react";
import { Button, ScrollArea } from '@donkeywork/ui'
import { useAgentConversation } from "../hooks/useAgentConversation";
import { internalToChat } from "./MessageRenderer";
import type { ChatMessage, ContentBox } from "@donkeywork/api-client";
import type { InternalMessage, GetStateResponse } from "@donkeywork/api-client";
import { BoxList } from "./BoxRenderer";
import { PulseDots } from "./PulseDots";
import { AgentCardGrid, type AgentEntry } from "./AgentCardGrid";
import { AgentDetailModal } from "./AgentDetailModal";
import { AgentSidePanel } from "./AgentSidePanel";
import { McpSidePanel } from "./McpSidePanel";
import { ExecutionSidePanel } from "./ExecutionSidePanel";
import { SocketEventPanel } from "./SocketEventPanel";
import { ExecutionDetailModal } from "./ExecutionDetailModal";
import { extractAgentTree, countAll, countActive, type SidePanelAgent } from "./agentTreeUtils";
import { Bubbles, RefreshCw, Send, Square, X, PanelRightOpen, Plug, Loader2, Copy, Check, Container, History, Radio } from "lucide-react";

function extractTextFromBoxes(boxes: ContentBox[]): string {
  return boxes
    .filter((box) => box.type === "text")
    .map((box) => (box as { text: string }).text)
    .join("\n\n");
}

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <button
      onClick={handleCopy}
      className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/50 transition-colors cursor-pointer"
      aria-label="Copy message"
      type="button"
    >
      {copied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
    </button>
  );
}

function MessageBubble({
  message,
  isStreaming,
  isCurrentTurn,
  onAgentClick,
  onAgentCancel,
}: {
  message: ChatMessage;
  isStreaming: boolean;
  isCurrentTurn: boolean;
  onAgentClick: (entry: AgentEntry) => void;
  onAgentCancel?: (agentKey: string) => void;
}) {
  if (message.role === "progress") {
    return (
      <div className="flex justify-start px-6 py-1.5">
        <div className="flex items-center gap-2.5 px-3 py-1.5 rounded-lg bg-cyan-500/5 border border-cyan-500/10">
          <PulseDots color="bg-cyan-400" />
          <span className="text-xs text-cyan-300/80 font-medium">{message.content}</span>
        </div>
      </div>
    );
  }

  const isUser = message.role === "user";
  const source = message._source;

  if (isUser) {
    return (
      <div className="flex justify-end px-6 py-1.5">
        <div className="max-w-[75%]">
          <div className="rounded-2xl rounded-br-md px-4 py-2.5 text-sm bg-gradient-to-r from-cyan-500 to-blue-600 text-white shadow-lg shadow-cyan-500/10">
            <span className="whitespace-pre-wrap">{message.content}</span>
          </div>
          <div className="flex justify-end mt-1">
            <CopyButton text={message.content} />
          </div>
        </div>
      </div>
    );
  }

  const textContent = extractTextFromBoxes(message.boxes);

  return (
    <div className="px-6 py-1.5">
      {source === "agent_result" && (
        <div className="flex items-center gap-1.5 mb-1">
          <div className="w-1.5 h-1.5 rounded-full bg-purple-400" />
          <span className="text-[10px] text-purple-400/70 font-medium">Agent result</span>
        </div>
      )}
      <div className="text-sm text-foreground">
        <BoxList boxes={message.boxes} isStreaming={isStreaming} />
        {isCurrentTurn && (
          <span className="ml-1.5 mt-1 block"><PulseDots color="bg-cyan-400" size="w-1 h-1" /></span>
        )}
      </div>
      {textContent && !isCurrentTurn && (
        <div className="flex mt-1">
          <CopyButton text={textContent} />
        </div>
      )}
      <AgentCardGrid
        boxes={message.boxes}
        onAgentClick={onAgentClick}
        onAgentCancel={onAgentCancel}
      />
    </div>
  );
}

type ModalData = { agentType: string; agentKey: string; label?: string; icon?: string; displayName?: string; boxes: ContentBox[]; isComplete: boolean };

function findAgentInBoxes(
  boxes: ContentBox[],
  toolUseId?: string,
  agentKey?: string,
): ModalData | null {
  for (const box of boxes) {
    if (box.type === "tool_use") {
      if (toolUseId && box.toolUseId === toolUseId && box.subAgent) {
        return {
          agentType: box.subAgent.agentType,
          agentKey: box.subAgent.agentKey,
          label: box.subAgent.label,
          icon: box.subAgent.icon,
          displayName: box.subAgent.displayName,
          boxes: box.subAgent.boxes,
          isComplete: !!box.isComplete,
        };
      }
      if (box.subAgent) {
        if (agentKey && box.subAgent.agentKey === agentKey) {
          return {
            agentType: box.subAgent.agentType,
            agentKey: box.subAgent.agentKey,
            label: box.subAgent.label,
            icon: box.subAgent.icon,
            displayName: box.subAgent.displayName,
            boxes: box.subAgent.boxes,
            isComplete: !!box.subAgent.isComplete || !!box.isComplete,
          };
        }
        const nested = findAgentInBoxes(box.subAgent.boxes, toolUseId, agentKey);
        if (nested) return nested;
      }
    } else if (box.type === "agent_group") {
      if (agentKey && box.agentKey === agentKey) {
        return {
          agentType: box.agentType,
          agentKey: box.agentKey,
          label: box.label,
          icon: box.icon,
          displayName: box.displayName,
          boxes: box.boxes,
          isComplete: !!box.isComplete,
        };
      }
      const nested = findAgentInBoxes(box.boxes, toolUseId, agentKey);
      if (nested) return nested;
    }
  }
  return null;
}

interface AgentChatPanelProps {
  conversationId?: string
  onConversationCreated?: (conversationId: string) => void
  onReset?: () => void
}

export function AgentChatPanel({ conversationId: initialConversationId, onConversationCreated, onReset }: AgentChatPanelProps) {
  const {
    messages,
    isProcessing,
    pendingCount,
    conversationId,
    sendMessage,
    sendRpc,
    cancel,
    resetConversation,
    isConnected,
    isReconnecting,
    mcpServerStatuses,
    sandboxStatus,
    socketEvents,
    clearSocketEvents,
  } = useAgentConversation(initialConversationId, { onConversationCreated, onReset });

  const swarmKey = conversationId ? `swarm:${conversationId}` : null;

  const [input, setInput] = useState("");
  const bottomRef = useRef<HTMLDivElement>(null);

  const [sidePanelOpen, setSidePanelOpen] = useState(false);
  const [mcpPanelOpen, setMcpPanelOpen] = useState(false);
  const [executionPanelOpen, setExecutionPanelOpen] = useState(false);
  const [socketPanelOpen, setSocketPanelOpen] = useState(false);
  const [selectedExecutionId, setSelectedExecutionId] = useState<string | null>(null);

  const mcpConnectedCount = mcpServerStatuses.filter((s) => s.success).length;
  const mcpHasFailures = mcpServerStatuses.some((s) => !s.success);

  const agentTree = useMemo(() => extractAgentTree(messages), [messages]);
  const agentTotal = useMemo(() => countAll(agentTree), [agentTree]);
  const agentActiveCount = useMemo(() => countActive(agentTree), [agentTree]);

  const fetchAgentMessages = useCallback(async (agentKey: string): Promise<ContentBox[]> => {
    const result = await sendRpc("getAgentMessages", { agentKey }) as GetStateResponse;
    const msgs: InternalMessage[] = result?.messages ?? [];
    const chatMessages = internalToChat(msgs);
    return chatMessages.flatMap((m) => m.role === "assistant" ? m.boxes : []);
  }, [sendRpc]);

  const [selectedAgent, setSelectedAgent] = useState<{
    messageId: string;
    toolUseId?: string;
    agentKey?: string;
    direct?: ModalData;
  } | null>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const text = input.trim();
    if (!text) return;
    setInput("");
    sendMessage(text);
  };

  const handleAgentClick = (messageId: string) => (entry: AgentEntry) => {
    if (entry.kind === "tool_use") {
      setSelectedAgent({ messageId, toolUseId: entry.box.toolUseId });
    } else {
      setSelectedAgent({ messageId, agentKey: entry.box.agentKey });
    }
  };

  const handleAgentCancel = (agentKey: string) => cancel(agentKey);

  const handleSidePanelAgentClick = (agent: SidePanelAgent) => {
    setSelectedAgent({
      messageId: agent.messageId,
      toolUseId: agent.toolUseId,
      agentKey: agent.agentKey,
      direct: {
        agentType: agent.agentType,
        agentKey: agent.agentKey,
        label: agent.label,
        icon: agent.icon,
        displayName: agent.displayName,
        boxes: agent.boxes,
        isComplete: agent.isComplete,
      },
    });
  };

  let modalProps: ModalData | null = null;

  if (selectedAgent) {
    if (selectedAgent.direct) {
      modalProps = selectedAgent.direct;
    } else {
      const msg = messages.find((m) => m.id === selectedAgent.messageId);
      if (msg) {
        const found = findAgentInBoxes(
          msg.boxes,
          selectedAgent.toolUseId,
          selectedAgent.agentKey,
        );
        if (found) modalProps = found;
      }
    }
  }

  return (
    <div className="flex h-full">
    {/* Chat column */}
    <div className="flex flex-col flex-1 min-w-0">
      {/* Header */}
      <div className="flex-none">
        <div className="flex items-center justify-between px-6 py-4">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-gradient-to-br from-cyan-500 to-blue-600">
              <Bubbles className="w-4 h-4 text-white" />
            </div>
            <h1 className="text-lg font-semibold text-foreground">Navi</h1>
            {isConnected && (
              <div className="flex items-center gap-1.5">
                <div className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
                <span className="text-[10px] text-muted-foreground">Connected</span>
              </div>
            )}
          </div>
          <div className="flex items-center gap-1">
            {sandboxStatus && (
              <div
                className="flex items-center gap-1.5 px-2 py-1 text-muted-foreground"
                title={sandboxStatus.podName ?? sandboxStatus.message ?? sandboxStatus.status}
              >
                <Container className="w-4 h-4" />
                {sandboxStatus.status === "provisioning" && (
                  <Loader2 className="w-3 h-3 animate-spin text-cyan-400" />
                )}
                {sandboxStatus.status === "ready" && (
                  <span className="flex h-1.5 w-1.5 rounded-full bg-emerald-400" />
                )}
                {sandboxStatus.status === "failed" && (
                  <span className="flex h-1.5 w-1.5 rounded-full bg-red-400" />
                )}
              </div>
            )}
            {mcpServerStatuses.length > 0 && !mcpPanelOpen && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => { setMcpPanelOpen(true); setSidePanelOpen(false); setExecutionPanelOpen(false); }}
                className="text-muted-foreground hover:text-foreground gap-1.5"
              >
                <Plug className="w-4 h-4" />
                <span className="text-xs">{mcpConnectedCount}</span>
                {mcpHasFailures && (
                  <span className="flex h-1.5 w-1.5 rounded-full bg-amber-400" />
                )}
              </Button>
            )}
            {agentTotal > 0 && !sidePanelOpen && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => { setSidePanelOpen(true); setMcpPanelOpen(false); setExecutionPanelOpen(false); }}
                className="text-muted-foreground hover:text-foreground gap-1.5"
              >
                <PanelRightOpen className="w-4 h-4" />
                <span className="text-xs">{agentTotal}</span>
                {agentActiveCount > 0 && (
                  <span className="flex h-1.5 w-1.5 rounded-full bg-cyan-400 animate-pulse" />
                )}
              </Button>
            )}
            {conversationId && !executionPanelOpen && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => { setExecutionPanelOpen(true); setSidePanelOpen(false); setMcpPanelOpen(false); setSocketPanelOpen(false); }}
                className="text-muted-foreground hover:text-foreground gap-1.5"
              >
                <History className="w-4 h-4" />
              </Button>
            )}
            {!socketPanelOpen && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => { setSocketPanelOpen(true); setSidePanelOpen(false); setMcpPanelOpen(false); setExecutionPanelOpen(false); }}
                className="text-muted-foreground hover:text-foreground gap-1.5"
              >
                <Radio className="w-4 h-4" />
              </Button>
            )}
            <Button
              variant="ghost"
              size="sm"
              onClick={resetConversation}
              className="text-muted-foreground hover:text-foreground"
            >
              <RefreshCw className="w-4 h-4 mr-1.5" />
              New Chat
            </Button>
          </div>
        </div>
        <div className="h-px bg-gradient-to-r from-transparent via-cyan-500/20 to-transparent" />
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-hidden">
        <ScrollArea className="h-full">
          <div className="flex flex-col gap-1 py-6">
            {messages.length === 0 && (
              isReconnecting ? (
                <div className="flex flex-col items-center justify-center py-24 px-6">
                  <Loader2 className="w-8 h-8 text-cyan-400 animate-spin mb-4" />
                  <p className="text-sm text-muted-foreground">Restoring conversation...</p>
                </div>
              ) : (
                <div className="flex flex-col items-center justify-center py-24 px-6">
                  <div className="flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-cyan-500/10 to-blue-600/10 border border-cyan-500/20 mb-6">
                    <Bubbles className="w-8 h-8 text-cyan-400" strokeWidth={1.5} />
                  </div>
                  <p className="text-base font-medium text-foreground mb-1">Hey, I'm Navi</p>
                  <p className="text-sm text-muted-foreground text-center max-w-sm">
                    Send me a message and I'll get to work. Results are delivered automatically.
                    You can cancel active work or clear the queue.
                  </p>
                </div>
              )
            )}
            {messages.map((msg, i) => (
              <MessageBubble
                key={msg.id}
                message={msg}
                isStreaming={isProcessing && msg.role === "assistant"}
                isCurrentTurn={isProcessing && msg.role === "assistant" && i === messages.length - 1}
                onAgentClick={handleAgentClick(msg.id)}
                onAgentCancel={handleAgentCancel}
              />
            ))}
            <div ref={bottomRef} />
          </div>
        </ScrollArea>
      </div>

      {/* Controls + Input */}
      <div className="flex-none">
        <div className="h-px bg-gradient-to-r from-transparent via-border to-transparent" />

        {(isProcessing || pendingCount > 0) && (
          <div className="flex items-center gap-2 px-6 pt-3 pb-1">
            {isProcessing && swarmKey && (
              <button
                onClick={() => cancel(swarmKey, "active")}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium bg-red-500/10 text-red-400 border border-red-500/20 hover:bg-red-500/20 transition-colors cursor-pointer"
              >
                <Square className="w-3 h-3" />
                Stop
              </button>
            )}
            {pendingCount > 0 && swarmKey && (
              <button
                onClick={() => cancel(swarmKey, "pending")}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium bg-amber-500/10 text-amber-400 border border-amber-500/20 hover:bg-amber-500/20 transition-colors cursor-pointer"
              >
                <X className="w-3 h-3" />
                Clear queue
                <span className="ml-0.5 px-1.5 py-0.5 rounded-full bg-amber-500/20 text-[10px]">
                  {pendingCount}
                </span>
              </button>
            )}
          </div>
        )}

        <div className="px-6 py-4">
          <form onSubmit={handleSubmit} className="flex gap-3">
            <div className="flex-1 relative">
              <input
                placeholder={isProcessing ? "Send another message..." : "Send a message..."}
                value={input}
                onChange={(e) => setInput(e.target.value)}
                className="w-full h-11 rounded-xl border border-input bg-secondary px-4 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:border-cyan-500/50 focus:ring-2 focus:ring-cyan-500/20 transition-all"
              />
            </div>
            <button
              type="submit"
              disabled={!input.trim()}
              className="h-11 px-5 rounded-xl text-sm font-medium text-white bg-gradient-to-r from-cyan-500 to-blue-600 hover:shadow-lg hover:shadow-cyan-500/25 disabled:opacity-40 disabled:hover:shadow-none transition-all cursor-pointer disabled:cursor-not-allowed"
            >
              <Send className="w-4 h-4" />
            </button>
          </form>
        </div>
      </div>

    </div>

      {/* Side panels (mutually exclusive) */}
      <AgentSidePanel
        messages={messages}
        isOpen={sidePanelOpen}
        onToggle={() => setSidePanelOpen((v) => !v)}
        onAgentClick={handleSidePanelAgentClick}
      />
      <McpSidePanel
        servers={mcpServerStatuses}
        isOpen={mcpPanelOpen}
        onToggle={() => setMcpPanelOpen((v) => !v)}
      />
      {conversationId && (
        <ExecutionSidePanel
          conversationId={conversationId}
          isOpen={executionPanelOpen}
          onToggle={() => setExecutionPanelOpen((v) => !v)}
          onExecutionClick={(id) => setSelectedExecutionId(id)}
        />
      )}
      <SocketEventPanel
        events={socketEvents}
        isOpen={socketPanelOpen}
        onToggle={() => setSocketPanelOpen((v) => !v)}
        onClear={clearSocketEvents}
      />

      <ExecutionDetailModal
        executionId={selectedExecutionId}
        open={!!selectedExecutionId}
        onOpenChange={(open) => { if (!open) setSelectedExecutionId(null); }}
      />

      {modalProps && (
        <AgentDetailModal
          open={!!selectedAgent}
          onOpenChange={(open) => { if (!open) setSelectedAgent(null); }}
          agentType={modalProps.agentType}
          agentKey={modalProps.agentKey}
          label={modalProps.label}
          icon={modalProps.icon}
          displayName={modalProps.displayName}
          boxes={modalProps.boxes}
          isComplete={modalProps.isComplete}
          isStreaming={isProcessing}
          onCancel={handleAgentCancel}
          onFetchMessages={fetchAgentMessages}
        />
      )}
    </div>
  );
}
