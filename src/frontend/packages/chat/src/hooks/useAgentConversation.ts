import { useState, useCallback, useRef, useEffect } from "react";
import { useAuthStore } from "@donkeywork/stores";
import { getPlatformConfig } from "@donkeywork/platform";
import { conversations } from "@donkeywork/api-client";
import { internalToChat } from "../components/MessageRenderer";
import type {
  ContentBox,
  TextBox,
  ThinkingBox,
  AgentCompleteReason,
  AgentGroupBox,
  ChatMessage,
  WebSearchResult,
  McpServerStatus,
  SandboxStatus,
} from "@donkeywork/api-client";
import type { InternalMessage, GetStateResponse, TrackedAgent } from "@donkeywork/api-client";
import {
  updateNestedGroup as _updateNestedGroup,
  tryUpdateNested as _tryUpdateNested,
  attachChildAgent,
  attachRootAgent,
  makeAgentGroup,
  type AgentGroupEntry,
} from "./agentBoxHelpers";

export type SocketEvent = {
  id: number;
  timestamp: number;
  eventType: string;
  agentKey: string;
  data: Record<string, unknown>;
  debug?: string;
};

export interface UseAgentConversationOptions {
  onConversationCreated?: (conversationId: string) => void
  onReset?: () => void
}

const MAX_RECONNECT_ATTEMPTS = 10;
const RECONNECT_BACKOFF_BASE_MS = 500;
const RECONNECT_BACKOFF_CAP_MS = 10000;

interface TurnCursor {
  lastSequence: number;
  complete: boolean;
}

export function useAgentConversation(initialConversationId?: string, options?: UseAgentConversationOptions) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isProcessing, setIsProcessing] = useState(false);
  const [pendingCount, setPendingCount] = useState(0);
  const [conversationId, setConversationId] = useState<string | null>(initialConversationId ?? null);
  const [isConnected, setIsConnected] = useState(false);
  const [isReconnecting, setIsReconnecting] = useState(false);
  const [activeTurnId, setActiveTurnId] = useState<string | null>(null);
  const [mcpServerStatuses, setMcpServerStatuses] = useState<McpServerStatus[]>([]);
  const [sandboxStatus, setSandboxStatus] = useState<SandboxStatus | null>(null);
  const [socketEvents, setSocketEvents] = useState<SocketEvent[]>([]);
  const socketEventIdRef = useRef(0);

  const wsRef = useRef<WebSocket | null>(null);
  const nextIdRef = useRef(1);
  const agentGroupIndexRef = useRef<Map<string, AgentGroupEntry>>(new Map());
  const currentAssistantIdRef = useRef<string | null>(null);
  const turnIdToMessageIdRef = useRef<Map<string, string>>(new Map());
  const handleEventRef = useRef<(data: Record<string, unknown>) => void>(() => {});

  // Buffer for events received during reconnection
  const eventBufferRef = useRef<Record<string, unknown>[]>([]);
  const isBufferingRef = useRef(false);

  // Track pending RPC requests for getState
  const pendingRpcRef = useRef<Map<number, { resolve: (v: unknown) => void; reject: (e: Error) => void }>>(new Map());

  // Per-turn sequence cursors: turnId -> { lastSequence, complete }
  // Tracks the highest JetStream sequence seen for each turn so reconnects can replay gaps.
  const turnCursorsRef = useRef<Map<string, TurnCursor>>(new Map());

  // Deduplication set: sequences already dispatched to handleEvent
  const seenSequencesRef = useRef<Set<number>>(new Set());

  // Reconnect state
  const reconnectAttemptsRef = useRef(0);
  const reconnectTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isManualCloseRef = useRef(false);

  // --- Box update helpers ---

  function updateBoxes(messageId: string, updater: (boxes: ContentBox[]) => ContentBox[]) {
    setMessages((prev) =>
      prev.map((m) =>
        m.id === messageId ? { ...m, boxes: updater(m.boxes) } : m
      )
    );
  }

  function tryUpdateNested(
    boxes: ContentBox[],
    messageId: string,
    agentKey: string,
    hintIndex: number,
    updater: (innerBoxes: ContentBox[]) => ContentBox[]
  ): { boxes: ContentBox[]; boxIndex: number } | null {
    const result = _tryUpdateNested(boxes, agentKey, hintIndex, updater);
    if (result && result.boxIndex !== hintIndex) {
      agentGroupIndexRef.current.set(agentKey, { messageId, boxIndex: result.boxIndex });
    }
    return result;
  }

  function appendOrCreate(
    fallbackMessageId: string,
    agentKey: string,
    boxType: string,
    newBox: ContentBox,
    appender?: (existing: ContentBox) => ContentBox
  ) {
    const entry = agentGroupIndexRef.current.get(agentKey);
    const innerUpdater = (inner: ContentBox[]) => {
      const last = inner[inner.length - 1];
      if (last?.type === boxType && appender) {
        return [...inner.slice(0, -1), appender(last)];
      }
      return [...inner, newBox];
    };

    if (entry) {
      updateBoxes(entry.messageId, (boxes) => {
        const result = tryUpdateNested(boxes, entry.messageId, agentKey, entry.boxIndex, innerUpdater);
        return result?.boxes ?? boxes;
      });
    } else {
      setMessages((prev) => {
        for (const m of prev) {
          for (let i = 0; i < m.boxes.length; i++) {
            const updated = _updateNestedGroup(m.boxes[i], agentKey, innerUpdater);
            if (updated) {
              const newBoxes = [...m.boxes];
              newBoxes[i] = updated;
              agentGroupIndexRef.current.set(agentKey, { messageId: m.id, boxIndex: i });
              return prev.map((msg) => msg.id === m.id ? { ...msg, boxes: newBoxes } : msg);
            }
          }
        }

        // No existing card found anywhere. If the event is for a child agent
        // (delegate/agent prefix), materialize a placeholder agent_group on
        // the assistant message rather than dumping the box into the main feed.
        // This rescues the case where agent_spawn/spawn-tool-result was missed
        // — e.g. due to a JetStream reconnect where the cursor advanced past
        // the spawn event — so live message/thinking deltas still collapse
        // into a card. If agent_spawn arrives later, the existing tryUpdateNested
        // path will find this placeholder by agentKey and merge into it.
        const isChildAgentKey = agentKey.startsWith("delegate:") || agentKey.startsWith("agent:");
        if (isChildAgentKey) {
          const placeholder: AgentGroupBox = {
            type: "agent_group",
            agentKey,
            agentType: agentKey.startsWith("delegate:") ? "delegate" : "agent",
            boxes: [newBox],
          };
          const { messages: next, entry: placeholderEntry } = attachRootAgent(prev, fallbackMessageId, placeholder);
          agentGroupIndexRef.current.set(agentKey, placeholderEntry);
          return next;
        }

        return prev.map((m) => {
          if (m.id !== fallbackMessageId) return m;
          const boxes = m.boxes;
          const last = boxes[boxes.length - 1];
          if (last?.type === boxType && appender) {
            return { ...m, boxes: [...boxes.slice(0, -1), appender(last)] };
          }
          return { ...m, boxes: [...boxes, newBox] };
        });
      });
    }
  }

  function markCompleteInBoxes(boxes: ContentBox[], forAgent: string, reason: AgentCompleteReason): ContentBox[] {
    let changed = false;
    const result = boxes.map((b) => {
      const updated = markNestedComplete(b, forAgent, reason);
      if (updated) {
        changed = true;
        return updated;
      }
      return b;
    });
    return changed ? result : boxes;
  }

  function markAgentCompleteGlobal(forAgent: string, reason: AgentCompleteReason = "completed") {
    setMessages((prev) =>
      prev.map((m) => {
        const newBoxes = markCompleteInBoxes(m.boxes, forAgent, reason);
        return newBoxes !== m.boxes ? { ...m, boxes: newBoxes } : m;
      })
    );
  }

  function markNestedComplete(box: ContentBox, forAgent: string, reason: AgentCompleteReason = "completed"): ContentBox | null {
    if (box.type === "tool_use" && box.subAgent) {
      if (box.subAgent.agentKey === forAgent) {
        console.log("[swarm-debug] markNestedComplete matched:", forAgent, "reason:", reason);
        return {
          ...box,
          isComplete: true,
          completeReason: reason,
          subAgent: { ...box.subAgent, isComplete: true, completeReason: reason, boxes: markAllSearchesDone(box.subAgent.boxes) },
        };
      }
      for (let i = 0; i < box.subAgent.boxes.length; i++) {
        const result = markNestedComplete(box.subAgent.boxes[i], forAgent, reason);
        if (result) {
          const newInner = [...box.subAgent.boxes];
          newInner[i] = result;
          return { ...box, subAgent: { ...box.subAgent, boxes: newInner } };
        }
      }
    }
    if (box.type === "agent_group") {
      if (box.agentKey === forAgent) {
        return { ...box, isComplete: true, completeReason: reason, boxes: markAllSearchesDone(box.boxes) };
      }
      for (let i = 0; i < box.boxes.length; i++) {
        const result = markNestedComplete(box.boxes[i], forAgent, reason);
        if (result) {
          const newInner = [...box.boxes];
          newInner[i] = result;
          return { ...box, boxes: newInner };
        }
      }
    }
    return null;
  }

  function markAllSearchesDone(boxes: ContentBox[]): ContentBox[] {
    return boxes.map((b) =>
      b.type === "tool_use" && b.toolName === "web_search" && !b.isComplete ? { ...b, isComplete: true, success: true } : b
    );
  }

  function clearIdleState(forAgent: string) {
    const markResumed = (boxes: ContentBox[]): ContentBox[] =>
      boxes.map((b) => {
        if (b.type === "tool_use" && b.subAgent?.agentKey === forAgent && b.subAgent.completeReason === "idle") {
          return { ...b, isComplete: false, completeReason: undefined, subAgent: { ...b.subAgent, isComplete: false, completeReason: undefined } };
        }
        if (b.type === "tool_use" && b.subAgent) {
          return { ...b, subAgent: { ...b.subAgent, boxes: markResumed(b.subAgent.boxes) } };
        }
        if (b.type === "agent_group" && b.agentKey === forAgent && b.completeReason === "idle") {
          return { ...b, isComplete: false, completeReason: undefined };
        }
        if (b.type === "agent_group") {
          return { ...b, boxes: markResumed(b.boxes) };
        }
        return b;
      });

    setMessages((prev) =>
      prev.map((m) => {
        const newBoxes = markResumed(m.boxes);
        return newBoxes !== m.boxes ? { ...m, boxes: newBoxes } : m;
      })
    );
  }

  // --- Event handling ---

  function handleEvent(data: Record<string, unknown>) {
    const eventType = (data.eventType as string) ?? "";
    const agentKey = (data.agentKey as string) ?? "";
    const eventTurnId = (data.turnId as string) ?? undefined;

    const resolvedAssistantId = eventTurnId
      ? (turnIdToMessageIdRef.current.get(eventTurnId) ?? currentAssistantIdRef.current)
      : currentAssistantIdRef.current;
    let assistantId = resolvedAssistantId;

    // Capture socket event for debug panel
    const socketEventId = ++socketEventIdRef.current;
    let debugInfo: string | undefined;
    if (eventType === "agent_spawn") {
      const parentEntry = agentGroupIndexRef.current.get(agentKey);
      debugInfo = parentEntry
        ? `parent found at boxIndex=${parentEntry.boxIndex}`
        : `no parent entry for ${agentKey.slice(-12)}`;
    }
    setSocketEvents((prev) => {
      const evt: SocketEvent = { id: socketEventId, timestamp: Date.now(), eventType, agentKey, data, debug: debugInfo };
      const next = [...prev, evt];
      return next.length > 500 ? next.slice(-500) : next;
    });

    if (eventType !== "agent_idle" && eventType !== "agent_complete" && agentGroupIndexRef.current.has(agentKey)) {
      console.log("[swarm-debug] clearIdleState triggered by:", eventType, "for:", agentKey);
      clearIdleState(agentKey);
    }

    if (eventType === "turn_start") {
      const newId = crypto.randomUUID();
      currentAssistantIdRef.current = newId;
      assistantId = newId;
      if (eventTurnId) {
        turnIdToMessageIdRef.current.set(eventTurnId, newId);
        setActiveTurnId(eventTurnId);
        if (!turnCursorsRef.current.has(eventTurnId)) {
          turnCursorsRef.current.set(eventTurnId, { lastSequence: 0, complete: false });
        }
      }
      const source = (data.source as string) ?? "user";
      const preview = (data.messagePreview as string) ?? "";

      setMessages((prev) => {
        const updated = eventTurnId
          ? prev.map((m) =>
              m._turnId === eventTurnId && m.role === "user" ? { ...m, _pending: false } : m
            )
          : prev;
        return [
          ...updated,
          {
            id: newId,
            role: "assistant" as const,
            content: "",
            boxes: [],
            _source: source,
            _preview: preview,
            _turnId: eventTurnId,
          },
        ];
      });
      setIsProcessing(true);
      return;
    }

    if (eventType === "turn_end") {
      setIsProcessing(false);
      setActiveTurnId(null);
      if (eventTurnId) {
        const cursor = turnCursorsRef.current.get(eventTurnId);
        if (cursor) {
          turnCursorsRef.current.set(eventTurnId, { ...cursor, complete: true });
        }
      }
      return;
    }

    if (eventType === "cancelled") {
      if (eventTurnId) {
        const cursor = turnCursorsRef.current.get(eventTurnId);
        if (cursor) {
          turnCursorsRef.current.set(eventTurnId, { ...cursor, complete: true });
        }
      }
      setIsProcessing(false);
      setActiveTurnId(null);
      if (assistantId) {
        setMessages((prev) => prev.filter((m) => m.id !== assistantId));
        currentAssistantIdRef.current = null;
      }
      return;
    }

    if (eventType === "queue_status") {
      setPendingCount((data.pendingCount as number) ?? 0);
      setIsProcessing((data.isProcessing as boolean) ?? false);
      return;
    }

    if (eventType === "mcp_server_status") {
      const serverName = (data.serverName as string) ?? "";
      setMcpServerStatuses((prev) => {
        const existing = prev.filter((s) => s.name !== serverName);
        return [...existing, {
          name: serverName,
          success: (data.success as boolean) ?? false,
          durationMs: (data.durationMs as number) ?? 0,
          toolCount: (data.toolCount as number) ?? 0,
          error: (data.error as string) ?? undefined,
        }];
      });
      return;
    }

    if (eventType === "sandbox_status") {
      setSandboxStatus({
        status: (data.status as SandboxStatus["status"]) ?? "provisioning",
        message: (data.message as string) ?? undefined,
        podName: (data.podName as string) ?? undefined,
      });
      return;
    }

    if (eventType === "agent_idle") {
      console.log("[swarm-debug] agent_idle received for:", agentKey);
      markAgentCompleteGlobal(agentKey, "idle");
      return;
    }

    if (eventType === "agent_complete") {
      const completeReason = (data.reason as AgentCompleteReason) ?? "completed";
      markAgentCompleteGlobal(agentKey, completeReason);
      return;
    }

    if (!assistantId) return;

    switch (eventType) {
      case "message": {
        const t = (data.text as string) ?? "";
        appendOrCreate(assistantId, agentKey, "text",
          { type: "text", text: t },
          (prev) => ({ ...prev, text: (prev as TextBox).text + t })
        );
        break;
      }

      case "thinking": {
        const t = (data.text as string) ?? "";
        appendOrCreate(assistantId, agentKey, "thinking",
          { type: "thinking", text: t },
          (prev) => ({ ...prev, text: (prev as ThinkingBox).text + t })
        );
        break;
      }

      case "tool_use":
        appendOrCreate(assistantId, agentKey, "", {
          type: "tool_use",
          toolName: (data.toolName as string) ?? "",
          displayName: (data.displayName as string) ?? undefined,
          toolUseId: (data.toolUseId as string) ?? "",
          arguments: (data.arguments as string) ?? undefined,
        });
        break;

      case "tool_result": {
        const resultToolId = (data.toolUseId as string) ?? "";
        const mergeResult = (boxes: ContentBox[]): ContentBox[] =>
          boxes.map((b) => {
            if (b.type === "tool_use" && b.toolUseId === resultToolId) {
              return {
                ...b,
                result: (data.result as string) ?? "",
                success: (data.success as boolean) ?? true,
                durationMs: (data.durationMs as number) ?? 0,
              };
            }
            if (b.type === "tool_use" && b.subAgent) {
              return { ...b, subAgent: { ...b.subAgent, boxes: mergeResult(b.subAgent.boxes) } };
            }
            if (b.type === "agent_group") {
              return { ...b, boxes: mergeResult(b.boxes) };
            }
            return b;
          });
        const toolEntry = agentGroupIndexRef.current.get(agentKey);
        updateBoxes(toolEntry?.messageId ?? assistantId, mergeResult);
        break;
      }

      case "tool_complete": {
        const toolUseId = (data.toolUseId as string) ?? "";
        const markToolComplete = (boxes: ContentBox[]): ContentBox[] =>
          boxes.map((b) => {
            if (b.type === "tool_use" && b.toolUseId === toolUseId) {
              if (b.subAgent) return b;
              return { ...b, isComplete: true };
            }
            if (b.type === "tool_use" && b.subAgent) {
              return { ...b, subAgent: { ...b.subAgent, boxes: markToolComplete(b.subAgent.boxes) } };
            }
            if (b.type === "agent_group") {
              return { ...b, boxes: markToolComplete(b.boxes) };
            }
            return b;
          });
        const tcEntry = agentGroupIndexRef.current.get(agentKey);
        updateBoxes(tcEntry?.messageId ?? assistantId, markToolComplete);
        break;
      }

      case "agent_spawn": {
        const spawnedKey = (data.spawnedAgentKey as string) ?? "";
        const agentType = (data.agentType as string) ?? "";
        const label = (data.label as string) || undefined;
        const icon = (data.icon as string) || undefined;
        const displayName = (data.displayName as string) || undefined;
        const parentEntry = agentGroupIndexRef.current.get(agentKey);
        const targetMessageId = parentEntry?.messageId ?? assistantId;
        const childGroup = makeAgentGroup(spawnedKey, agentType, label, icon, displayName);

        agentGroupIndexRef.current.set(spawnedKey, { messageId: targetMessageId, boxIndex: -1 });

        if (parentEntry) {
          setMessages((prev) => {
            const { messages: next, entry } = attachChildAgent(prev, parentEntry, agentKey, childGroup, assistantId);
            agentGroupIndexRef.current.set(spawnedKey, entry);
            if (entry.messageId !== parentEntry.messageId || entry.boxIndex !== parentEntry.boxIndex) {
              agentGroupIndexRef.current.set(agentKey, entry);
            }
            return next;
          });
        } else {
          setMessages((prev) => {
            const { messages: next, entry } = attachRootAgent(prev, targetMessageId, childGroup);
            agentGroupIndexRef.current.set(spawnedKey, entry);
            return next;
          });
        }
        break;
      }

      case "web_search": {
        const wsQuery = (data.query as string) ?? undefined;
        appendOrCreate(assistantId, agentKey, "", {
          type: "tool_use",
          toolName: "web_search",
          displayName: wsQuery ? `Searching: ${wsQuery}` : "Web Search",
          toolUseId: (data.toolUseId as string) ?? "",
          arguments: wsQuery ? JSON.stringify({ query: wsQuery }) : undefined,
        });
        break;
      }

      case "web_search_complete": {
        const wsToolId = (data.toolUseId as string) ?? "";
        const wsResults = (data.results as WebSearchResult[]) ?? [];
        const markSearchComplete = (boxes: ContentBox[]): ContentBox[] =>
          boxes.map((b) => {
            if (b.type === "tool_use" && b.toolUseId === wsToolId) {
              return { ...b, isComplete: true, success: true, webSearchResults: wsResults };
            }
            if (b.type === "tool_use" && b.subAgent) {
              return { ...b, subAgent: { ...b.subAgent, boxes: markSearchComplete(b.subAgent.boxes) } };
            }
            if (b.type === "agent_group") {
              return { ...b, boxes: markSearchComplete(b.boxes) };
            }
            return b;
          });
        const wsEntry = agentGroupIndexRef.current.get(agentKey);
        updateBoxes(wsEntry?.messageId ?? assistantId, markSearchComplete);
        break;
      }

      case "agent_message": {
        break;
      }

      case "agent_result_data": {
        const subKey = (data.subAgentKey as string) ?? "";
        const resultText = (data.text as string) ?? "";
        const citations = (data.citations as Array<{ title: string; url: string; citedText: string }>) ?? [];
        const resultBoxes: ContentBox[] = [];
        if (resultText) resultBoxes.push({ type: "text", text: resultText });
        for (const c of citations) {
          resultBoxes.push({
            type: "citation",
            title: c.title ?? "",
            url: c.url ?? "",
            citedText: c.citedText ?? "",
          });
        }
        if (resultBoxes.length > 0) {
          const targetKey = agentGroupIndexRef.current.has(subKey) ? subKey : agentKey;
          for (const box of resultBoxes) {
            appendOrCreate(assistantId, targetKey, "", box);
          }
        }
        break;
      }

      case "citation":
        appendOrCreate(assistantId, agentKey, "", {
          type: "citation",
          title: (data.title as string) ?? "",
          url: (data.url as string) ?? "",
          citedText: (data.citedText as string) ?? "",
        });
        break;

      case "usage": {
        const inTok = (data.inputTokens as number) ?? 0;
        const outTok = (data.outputTokens as number) ?? 0;
        const wsReq = (data.webSearchRequests as number) ?? 0;
        const ctxLimit = (data.contextWindowLimit as number) ?? 0;
        const maxOut = (data.maxOutputTokens as number) ?? 0;
        const usageBox: import("@donkeywork/api-client").UsageBox = {
          type: "usage", inputTokens: inTok, outputTokens: outTok,
          webSearchRequests: wsReq, contextWindowLimit: ctxLimit, maxOutputTokens: maxOut,
        };
        const mergeUsage = (boxes: ContentBox[]): ContentBox[] => {
          const idx = boxes.findIndex((b) => b.type === "usage");
          if (idx >= 0) {
            const prev = boxes[idx] as import("@donkeywork/api-client").UsageBox;
            const merged = {
              ...prev,
              inputTokens: inTok > 0 ? inTok : prev.inputTokens,
              outputTokens: prev.outputTokens + outTok,
              webSearchRequests: prev.webSearchRequests + wsReq,
              contextWindowLimit: ctxLimit || prev.contextWindowLimit,
              maxOutputTokens: maxOut || prev.maxOutputTokens,
            };
            return [...boxes.slice(0, idx), merged, ...boxes.slice(idx + 1)];
          }
          return [...boxes, usageBox];
        };
        const entry = agentGroupIndexRef.current.get(agentKey);
        if (entry) {
          updateBoxes(entry.messageId, (boxes) => {
            const result = tryUpdateNested(boxes, entry.messageId, agentKey, entry.boxIndex, mergeUsage);
            return result?.boxes ?? boxes;
          });
        } else {
          updateBoxes(assistantId, mergeUsage);
        }
        break;
      }

      case "complete": {
        const completeText = (data.text as string) ?? "";
        if (!completeText) break;

        if (agentGroupIndexRef.current.has(agentKey)) {
          appendOrCreate(assistantId, agentKey, "text",
            { type: "text", text: completeText },
            (prev) => ({ ...prev, text: (prev as TextBox).text + completeText })
          );
        } else {
          setMessages((prev) =>
            prev.map((m) => {
              if (m.id !== assistantId) return m;
              const hasText = m.boxes.some((b) => b.type === "text" && b.text.length > 0);
              if (hasText) return m;
              return { ...m, boxes: [...m.boxes, { type: "text" as const, text: completeText }] };
            })
          );
        }
        break;
      }

      case "error": {
        const errorText = `Error: ${(data.error as string) ?? "Unknown error"}`;
        if (agentGroupIndexRef.current.has(agentKey)) {
          appendOrCreate(assistantId, agentKey, "text",
            { type: "text", text: errorText });
        } else {
          appendOrCreate(assistantId, "", "text",
            { type: "text", text: errorText });
        }
        break;
      }

      case "retry": {
        const attempt = data.attempt as number;
        const maxRetries = data.maxRetries as number;
        const reason = (data.reason as string) ?? "Transient error";
        const retryText = `Retrying (${attempt}/${maxRetries}): ${reason}`;
        if (agentGroupIndexRef.current.has(agentKey)) {
          appendOrCreate(assistantId, agentKey, "text",
            { type: "text", text: retryText });
        } else {
          appendOrCreate(assistantId, "", "text",
            { type: "text", text: retryText });
        }
        break;
      }

      case "progress":
        break;
    }
  }

  useEffect(() => {
    handleEventRef.current = handleEvent;
  });

  // --- RPC helper ---

  const sendRpc = useCallback((method: string, params?: Record<string, unknown>): Promise<unknown> => {
    return new Promise((resolve, reject) => {
      const id = nextIdRef.current++;
      pendingRpcRef.current.set(id, { resolve, reject });
      wsRef.current?.send(JSON.stringify({
        jsonrpc: "2.0",
        id,
        method,
        params: params ?? {},
      }));
      setTimeout(() => {
        if (pendingRpcRef.current.has(id)) {
          pendingRpcRef.current.delete(id);
          reject(new Error(`RPC timeout: ${method}`));
        }
      }, 10000);
    });
  }, []);

  // --- Cursor tracking: dispatch event and update sequence ---

  const dispatchWithSequence = useCallback((eventData: Record<string, unknown>, sequence: number) => {
    const turnId = eventData.turnId as string | undefined;

    if (sequence > 0) {
      if (seenSequencesRef.current.has(sequence)) return;
      seenSequencesRef.current.add(sequence);

      if (turnId) {
        const existing = turnCursorsRef.current.get(turnId);
        if (!existing || sequence > existing.lastSequence) {
          turnCursorsRef.current.set(turnId, {
            lastSequence: sequence,
            complete: existing?.complete ?? false,
          });
        }
      }
    }

    handleEventRef.current(eventData);
  }, []);

  // --- Replay missed events after reconnect ---

  const replayMissedEvents = useCallback(async () => {
    const incompleteTurns = Array.from(turnCursorsRef.current.entries())
      .filter(([, cursor]) => !cursor.complete && cursor.lastSequence > 0);

    for (const [turnId, cursor] of incompleteTurns) {
      try {
        const result = await sendRpc("eventsSince", {
          turnId,
          afterSequence: cursor.lastSequence,
        }) as { events?: { payload: Record<string, unknown>; sequence: number }[]; lastSequence?: number; complete?: boolean } | null;

        if (!result) continue;

        const replayedEvents = result.events ?? [];
        const lastSeq = result.lastSequence ?? cursor.lastSequence;
        const isComplete = result.complete ?? false;

        for (const { payload, sequence } of replayedEvents) {
          dispatchWithSequence(payload, sequence);
        }

        turnCursorsRef.current.set(turnId, {
          lastSequence: lastSeq,
          complete: isComplete,
        });
      } catch {
        // If eventsSince fails, skip this turn — live events will continue
      }
    }
  }, [sendRpc, dispatchWithSequence]);

  // --- WebSocket connection ---

  const connect = useCallback(async (convId: string) => {
    const token = useAuthStore.getState().accessToken;
    const { wsBaseUrl } = getPlatformConfig();
    const wsUrl = `${wsBaseUrl}/api/v1/conversations/${convId}/ws?token=${encodeURIComponent(token ?? "")}`;
    const ws = new WebSocket(wsUrl);
    wsRef.current = ws;

    ws.onopen = () => {
      setIsConnected(true);
      reconnectAttemptsRef.current = 0;
    };

    ws.onmessage = (evt) => {
      let frame: Record<string, unknown>;
      try {
        frame = JSON.parse(evt.data);
      } catch {
        return;
      }

      if (frame.id !== undefined && frame.id !== null) {
        const id = frame.id as number;
        const pending = pendingRpcRef.current.get(id);
        if (pending) {
          pendingRpcRef.current.delete(id);
          if (frame.error) {
            const err = frame.error as Record<string, unknown>;
            pending.reject(new Error((err.message as string) ?? "RPC error"));
          } else {
            pending.resolve(frame.result);
          }
          return;
        }
      }

      if (frame.method === "event" && Array.isArray(frame.params)) {
        const params = frame.params as unknown[];
        const eventData = params[0] as Record<string, unknown>;
        const sequence = typeof params[1] === "number" ? params[1] : 0;

        if (eventData) {
          if (isBufferingRef.current) {
            eventBufferRef.current.push({ ...eventData, _sequence: sequence });
          } else {
            dispatchWithSequence(eventData, sequence);
          }
        }
      } else if (frame.error && !frame.id) {
        const err = frame.error as Record<string, unknown>;
        const assistantId = currentAssistantIdRef.current;
        if (assistantId) {
          handleEventRef.current({
            eventType: "error",
            error: (err.message as string) ?? "Unknown error",
          });
        }
      }
    };

    ws.onclose = () => {
      setIsConnected(false);
      if (!isManualCloseRef.current && reconnectAttemptsRef.current < MAX_RECONNECT_ATTEMPTS) {
        const delay = Math.min(
          RECONNECT_BACKOFF_BASE_MS * Math.pow(2, reconnectAttemptsRef.current),
          RECONNECT_BACKOFF_CAP_MS
        );
        reconnectAttemptsRef.current++;
        reconnectTimerRef.current = setTimeout(() => {
          doReconnect(convId);
        }, delay);
      }
    };

    ws.onerror = () => {
      setIsConnected(false);
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dispatchWithSequence]);

  // --- Rebuild agent group index from restored messages ---

  function rebuildAgentGroupIndex(chatMessages: ChatMessage[]) {
    agentGroupIndexRef.current = new Map();
    for (const msg of chatMessages) {
      for (let boxIdx = 0; boxIdx < msg.boxes.length; boxIdx++) {
        const box = msg.boxes[boxIdx];
        indexAgentBoxes(box, msg.id, boxIdx);
      }
    }
  }

  function indexAgentBoxes(box: ContentBox, messageId: string, boxIndex: number) {
    if (box.type === "tool_use" && box.subAgent) {
      agentGroupIndexRef.current.set(box.subAgent.agentKey, { messageId, boxIndex });
      for (const inner of box.subAgent.boxes) {
        indexAgentBoxes(inner, messageId, boxIndex);
      }
    }
    if (box.type === "agent_group") {
      agentGroupIndexRef.current.set(box.agentKey, { messageId, boxIndex });
      for (const inner of box.boxes) {
        indexAgentBoxes(inner, messageId, boxIndex);
      }
    }
  }

  // --- Reconnection (internal, used by ws.onclose and the public reconnect) ---

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const doReconnect = useCallback(async (convId: string) => {
    setIsReconnecting(true);
    isBufferingRef.current = true;
    eventBufferRef.current = [];

    await connect(convId);
    await new Promise<void>((resolve) => {
      const check = () => {
        if (wsRef.current?.readyState === WebSocket.OPEN) resolve();
        else setTimeout(check, 50);
      };
      check();
    });

    try {
      const [stateResult, agentsResult] = await Promise.all([
        sendRpc("getState") as Promise<GetStateResponse>,
        sendRpc("listAgents").catch(() => [] as TrackedAgent[]) as Promise<TrackedAgent[]>,
      ]);
      const grainMessages: InternalMessage[] = stateResult?.messages ?? [];
      const agents: TrackedAgent[] = Array.isArray(agentsResult) ? agentsResult : [];

      if (grainMessages.length > 0) {
        const chatMessages = internalToChat(grainMessages, agents);
        setMessages(chatMessages);
        rebuildAgentGroupIndex(chatMessages);

        const lastAssistant = chatMessages.filter((m) => m.role === "assistant").pop();
        if (lastAssistant) {
          currentAssistantIdRef.current = lastAssistant.id;
        }
      }

      if (agents.some((a) => a.status === "Pending")) {
        setIsProcessing(true);
      }

      // Replay missed events for turns that were in-progress when we disconnected
      await replayMissedEvents();

      isBufferingRef.current = false;
      const buffered = eventBufferRef.current;
      eventBufferRef.current = [];
      for (const evt of buffered) {
        const { _sequence, ...eventData } = evt as Record<string, unknown>;
        dispatchWithSequence(eventData as Record<string, unknown>, typeof _sequence === "number" ? _sequence : 0);
      }
    } catch {
      isBufferingRef.current = false;
      const buffered = eventBufferRef.current;
      eventBufferRef.current = [];
      for (const evt of buffered) {
        const { _sequence, ...eventData } = evt as Record<string, unknown>;
        dispatchWithSequence(eventData as Record<string, unknown>, typeof _sequence === "number" ? _sequence : 0);
      }
    } finally {
      setIsReconnecting(false);
    }
  }, [connect, sendRpc, replayMissedEvents, dispatchWithSequence]); // eslint-disable-line react-hooks/exhaustive-deps

  // --- Public reconnect (manual, used on initial conversation load) ---

  const reconnect = useCallback(async (convId: string) => {
    isManualCloseRef.current = false;
    reconnectAttemptsRef.current = 0;
    await doReconnect(convId);
  }, [doReconnect]);

  // --- Auto-connect on mount when conversationId is in URL ---

  useEffect(() => {
    if (initialConversationId) {
      setConversationId(initialConversationId);
      reconnect(initialConversationId);
    } else {
      isManualCloseRef.current = true;
      if (reconnectTimerRef.current) {
        clearTimeout(reconnectTimerRef.current);
        reconnectTimerRef.current = null;
      }
      wsRef.current?.close();
      wsRef.current = null;
      setMessages([]);
      setConversationId(null);
      setIsProcessing(false);
      setActiveTurnId(null);
      setPendingCount(0);
      setIsConnected(false);
      setIsReconnecting(false);
      agentGroupIndexRef.current = new Map();
      currentAssistantIdRef.current = null;
      turnIdToMessageIdRef.current = new Map();
      nextIdRef.current = 1;
      pendingRpcRef.current.clear();
      eventBufferRef.current = [];
      isBufferingRef.current = false;
      turnCursorsRef.current = new Map();
      seenSequencesRef.current = new Set();
      reconnectAttemptsRef.current = 0;
    }

    return () => {
      isManualCloseRef.current = true;
      if (reconnectTimerRef.current) {
        clearTimeout(reconnectTimerRef.current);
        reconnectTimerRef.current = null;
      }
      wsRef.current?.close();
      wsRef.current = null;
    };
  }, [initialConversationId]); // eslint-disable-line react-hooks/exhaustive-deps

  // --- Actions ---

  const sendMessage = useCallback(async (text: string) => {
    let activeConvId = conversationId;

    if (!activeConvId) {
      const result = await conversations.create({});
      activeConvId = result.id;
      setConversationId(activeConvId);

      options?.onConversationCreated?.(activeConvId);

      isManualCloseRef.current = false;
      reconnectAttemptsRef.current = 0;
      await connect(activeConvId);
      await new Promise<void>((resolve) => {
        const check = () => {
          if (wsRef.current?.readyState === WebSocket.OPEN) resolve();
          else setTimeout(check, 50);
        };
        check();
      });
    }

    const userMsgId = crypto.randomUUID();
    const userMsg: ChatMessage = {
      id: userMsgId,
      role: "user",
      content: text,
      boxes: [],
    };
    setMessages((prev) => [...prev, userMsg]);

    const rpcId = nextIdRef.current++;
    const rpcPromise = new Promise<unknown>((resolve, reject) => {
      pendingRpcRef.current.set(rpcId, { resolve, reject });
      setTimeout(() => {
        if (pendingRpcRef.current.has(rpcId)) {
          pendingRpcRef.current.delete(rpcId);
          reject(new Error("RPC timeout: message"));
        }
      }, 10000);
    });

    wsRef.current?.send(JSON.stringify({
      jsonrpc: "2.0",
      id: rpcId,
      method: "message",
      params: { text },
    }));

    rpcPromise.then((result) => {
      const res = result as Record<string, unknown>;
      const turnId = res?.turnId as string | undefined;
      if (turnId) {
        setMessages((prev) =>
          prev.map((m) => m.id === userMsgId ? { ...m, _turnId: turnId, _pending: true } : m)
        );
      }
    }).catch(() => {});
  }, [conversationId, connect, options]);

  const cancel = useCallback((key: string, scope?: string) => {
    wsRef.current?.send(JSON.stringify({
      jsonrpc: "2.0",
      method: "cancel",
      params: { key, ...(scope ? { scope } : {}) },
    }));
  }, []);

  const cancelTurn = useCallback(async (turnId: string) => {
    const result = await sendRpc("cancelTurn", { turnId }) as Record<string, unknown> | null;
    const outcome = (result as Record<string, unknown>)?.result as string | undefined;
    if (outcome === "pending" || outcome === "active" || outcome === "notFound") {
      setMessages((prev) => prev.filter((m) => !(m.role === "user" && m._turnId === turnId)));
    }
  }, [sendRpc]);

  const resetConversation = useCallback(() => {
    isManualCloseRef.current = true;
    if (reconnectTimerRef.current) {
      clearTimeout(reconnectTimerRef.current);
      reconnectTimerRef.current = null;
    }
    wsRef.current?.close();
    wsRef.current = null;
    setMessages([]);
    setConversationId(null);
    setIsProcessing(false);
    setActiveTurnId(null);
    setPendingCount(0);
    setIsConnected(false);
    setIsReconnecting(false);
    setMcpServerStatuses([]);
    setSandboxStatus(null);
    agentGroupIndexRef.current = new Map();
    currentAssistantIdRef.current = null;
    turnIdToMessageIdRef.current = new Map();
    nextIdRef.current = 1;
    pendingRpcRef.current.clear();
    eventBufferRef.current = [];
    isBufferingRef.current = false;
    turnCursorsRef.current = new Map();
    seenSequencesRef.current = new Set();
    reconnectAttemptsRef.current = 0;
    options?.onReset?.();
  }, [options]);

  return {
    messages,
    isProcessing,
    pendingCount,
    activeTurnId,
    conversationId,
    isConnected,
    isReconnecting,
    mcpServerStatuses,
    sandboxStatus,
    socketEvents,
    sendMessage,
    sendRpc,
    cancel,
    cancelTurn,
    resetConversation,
    clearSocketEvents: () => setSocketEvents([]),
  };
}
