import { useRef, useEffect, useState, useMemo, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useAgentConversation } from "@/hooks/useAgentConversation";
import type { ChatMessage, ContentBox } from "@/types/agent-chat";
import { BoxList } from "@/components/agent-chat/BoxRenderer";
import { PulseDots } from "@/components/agent-chat/PulseDots";
import { AgentCardGrid, type AgentEntry } from "@/components/agent-chat/AgentCardGrid";
import { AgentDetailModal } from "@/components/agent-chat/AgentDetailModal";
import { AgentSidePanel } from "@/components/agent-chat/AgentSidePanel";
import { extractAgentTree, countAll, countActive, type SidePanelAgent } from "@/components/agent-chat/agentTreeUtils";
import { BotMessageSquare, RefreshCw, MessageSquare, Send, Square, X, PanelRightOpen } from "lucide-react";

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
        </div>
      </div>
    );
  }

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
      <AgentCardGrid
        boxes={message.boxes}
        onAgentClick={onAgentClick}
        onAgentCancel={onAgentCancel}
      />
    </div>
  );
}

type ModalData = { agentType: string; agentKey: string; label?: string; boxes: ContentBox[]; isComplete: boolean };

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

export function AgentChatPanel() {
  const {
    messages,
    isProcessing,
    pendingCount,
    conversationId,
    sendMessage,
    cancel,
    resetConversation,
    isConnected,
  } = useAgentConversation();

  const swarmKey = conversationId ? `swarm:${conversationId}` : null;

  const [input, setInput] = useState("");
  const bottomRef = useRef<HTMLDivElement>(null);

  const [sidePanelOpen, setSidePanelOpen] = useState(false);

  const agentTree = useMemo(() => extractAgentTree(messages), [messages]);
  const agentTotal = useMemo(() => countAll(agentTree), [agentTree]);
  const agentActiveCount = useMemo(() => countActive(agentTree), [agentTree]);

  const [selectedAgent, setSelectedAgent] = useState<{
    messageId: string;
    toolUseId?: string;
    agentKey?: string;
    direct?: { agentType: string; agentKey: string; label?: string; boxes: ContentBox[]; isComplete: boolean };
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
        boxes: agent.boxes,
        isComplete: agent.isComplete,
      },
    });
  };

  let modalProps: {
    agentType: string;
    agentKey: string;
    label?: string;
    boxes: ContentBox[];
    isComplete: boolean;
  } | null = null;

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
              <BotMessageSquare className="w-4 h-4 text-white" />
            </div>
            <h1 className="text-lg font-semibold text-foreground">Agent Chat</h1>
            {isConnected && (
              <div className="flex items-center gap-1.5">
                <div className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
                <span className="text-[10px] text-muted-foreground">Connected</span>
              </div>
            )}
          </div>
          <div className="flex items-center gap-1">
            {agentTotal > 0 && !sidePanelOpen && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSidePanelOpen(true)}
                className="text-muted-foreground hover:text-foreground gap-1.5"
              >
                <PanelRightOpen className="w-4 h-4" />
                <span className="text-xs">{agentTotal}</span>
                {agentActiveCount > 0 && (
                  <span className="flex h-1.5 w-1.5 rounded-full bg-cyan-400 animate-pulse" />
                )}
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
              <div className="flex flex-col items-center justify-center py-24 px-6">
                <div className="flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-cyan-500/10 to-blue-600/10 border border-cyan-500/20 mb-6">
                  <MessageSquare className="w-8 h-8 text-cyan-400" strokeWidth={1.5} />
                </div>
                <p className="text-base font-medium text-foreground mb-1">Start an agent conversation</p>
                <p className="text-sm text-muted-foreground text-center max-w-sm">
                  Send messages anytime. Agent results are delivered automatically.
                  You can cancel active work or clear the queue.
                </p>
              </div>
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

      {/* Side panel */}
      <AgentSidePanel
        messages={messages}
        isOpen={sidePanelOpen}
        onToggle={() => setSidePanelOpen((v) => !v)}
        onAgentClick={handleSidePanelAgentClick}
      />

      {modalProps && (
        <AgentDetailModal
          open={!!selectedAgent}
          onOpenChange={(open) => { if (!open) setSelectedAgent(null); }}
          agentType={modalProps.agentType}
          agentKey={modalProps.agentKey}
          label={modalProps.label}
          boxes={modalProps.boxes}
          isComplete={modalProps.isComplete}
          isStreaming={isProcessing}
          onCancel={handleAgentCancel}
        />
      )}
    </div>
  );
}
