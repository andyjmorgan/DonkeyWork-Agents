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

const SPAWN_TOOL_NAMES = new Set(["spawn_agent", "spawn_delegate"]);

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

export function useAgentConversation(initialConversationId?: string, options?: UseAgentConversationOptions) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isProcessing, setIsProcessing] = useState(false);
  const [pendingCount, setPendingCount] = useState(0);
  const [conversationId, setConversationId] = useState<string | null>(initialConversationId ?? null);
  const [isConnected, setIsConnected] = useState(false);
  const [isReconnecting, setIsReconnecting] = useState(false);
  const [mcpServerStatuses, setMcpServerStatuses] = useState<McpServerStatus[]>([]);
  const [sandboxStatus, setSandboxStatus] = useState<SandboxStatus | null>(null);
  const [socketEvents, setSocketEvents] = useState<SocketEvent[]>([]);
  const socketEventIdRef = useRef(0);

  const wsRef = useRef<WebSocket | null>(null);
  const nextIdRef = useRef(1);
  const agentGroupIndexRef = useRef<Map<string, AgentGroupEntry>>(new Map());
  const currentAssistantIdRef = useRef<string | null>(null);
  const handleEventRef = useRef<(data: Record<string, unknown>) => void>(() => {});

  // Buffer for events received during reconnection
  const eventBufferRef = useRef<Record<string, unknown>[]>([]);
  const isBufferingRef = useRef(false);

  // Track pending RPC requests for getState
  const pendingRpcRef = useRef<Map<number, { resolve: (v: unknown) => void; reject: (e: Error) => void }>>(new Map());

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
      // No index entry — scan all messages for a nested agent_group matching this agentKey.
      // Uses a single setMessages call so the scan result is available for the fallback decision.
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

        // Fallback: append to the target message's root boxes
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

  // --- Event handling ---

  function handleEvent(data: Record<string, unknown>) {
    const eventType = (data.eventType as string) ?? "";
    const agentKey = (data.agentKey as string) ?? "";
    let assistantId = currentAssistantIdRef.current;

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

    if (eventType === "turn_start") {
      const newId = crypto.randomUUID();
      currentAssistantIdRef.current = newId;
      assistantId = newId;
      const source = (data.source as string) ?? "user";
      const preview = (data.messagePreview as string) ?? "";

      setMessages((prev) => [
        ...prev,
        {
          id: newId,
          role: "assistant",
          content: "",
          boxes: [],
          _source: source,
          _preview: preview,
        },
      ]);
      setIsProcessing(true);
      return;
    }

    if (eventType === "turn_end") {
      setIsProcessing(false);
      return;
    }

    if (eventType === "queue_status") {
      setPendingCount((data.pendingCount as number) ?? 0);
      setIsProcessing((data.isProcessing as boolean) ?? false);
      return;
    }

    if (eventType === "cancelled") {
      setIsProcessing(false);
      if (assistantId) {
        setMessages((prev) => prev.filter((m) => m.id !== assistantId));
        currentAssistantIdRef.current = null;
      }
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

        // Set a provisional ref entry synchronously so that child events
        // arriving before React processes the setMessages callback can still
        // find this agent via agentGroupIndexRef.
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

      case "agent_complete": {
        const completeReason = (data.reason as AgentCompleteReason) ?? "completed";
        markAgentCompleteGlobal(agentKey, completeReason);
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
        markAgentCompleteGlobal(subKey);
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
      // Timeout after 10s
      setTimeout(() => {
        if (pendingRpcRef.current.has(id)) {
          pendingRpcRef.current.delete(id);
          reject(new Error(`RPC timeout: ${method}`));
        }
      }, 10000);
    });
  }, []);

  // --- WebSocket connection ---

  const connect = useCallback(async (convId: string) => {
    const token = useAuthStore.getState().accessToken;
    const { wsBaseUrl } = getPlatformConfig();
    const wsUrl = `${wsBaseUrl}/api/v1/conversations/${convId}/ws?token=${encodeURIComponent(token ?? "")}`;
    const ws = new WebSocket(wsUrl);
    wsRef.current = ws;

    ws.onopen = () => {
      setIsConnected(true);
    };

    ws.onmessage = (evt) => {
      let frame: Record<string, unknown>;
      try {
        frame = JSON.parse(evt.data);
      } catch {
        return;
      }

      // Handle RPC responses (has id and result/error)
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

      // Handle server notifications (events)
      if (frame.method === "event" && Array.isArray(frame.params)) {
        const eventData = (frame.params as unknown[])[0] as Record<string, unknown>;
        if (eventData) {
          if (isBufferingRef.current) {
            eventBufferRef.current.push(eventData);
          } else {
            handleEventRef.current(eventData);
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
    };

    ws.onerror = () => {
      setIsConnected(false);
    };
  }, []);

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

  // --- Reconnection ---

  const reconnect = useCallback(async (convId: string) => {
    setIsReconnecting(true);
    isBufferingRef.current = true;
    eventBufferRef.current = [];

    // Connect WebSocket (events start flowing but are buffered)
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

      console.debug("[reconnect] getState returned", grainMessages.length, "messages, listAgents returned", agents.length, "agents");
      if (grainMessages.length > 0) {
        console.debug("[reconnect] first message $type:", grainMessages[0]?.$type);
      }

      if (grainMessages.length > 0) {
        const chatMessages = internalToChat(grainMessages, agents);
        setMessages(chatMessages);

        // Populate agentGroupIndexRef so live events can target agent groups
        rebuildAgentGroupIndex(chatMessages);

        // Set currentAssistantIdRef to last assistant message so live events append correctly
        const lastAssistant = chatMessages.filter((m) => m.role === "assistant").pop();
        if (lastAssistant) {
          currentAssistantIdRef.current = lastAssistant.id;
        }
      }

      // If there are still-running agents, mark processing
      if (agents.some((a) => a.status === "Pending")) {
        setIsProcessing(true);
      }

      isBufferingRef.current = false;
      const buffered = eventBufferRef.current;
      eventBufferRef.current = [];
      for (const evt of buffered) {
        handleEventRef.current(evt);
      }
    } catch {
      // If getState fails, just stop buffering and let live events flow
      isBufferingRef.current = false;
      const buffered = eventBufferRef.current;
      eventBufferRef.current = [];
      for (const evt of buffered) {
        handleEventRef.current(evt);
      }
    } finally {
      setIsReconnecting(false);
    }
  }, [connect, sendRpc]);

  // --- Auto-connect on mount when conversationId is in URL ---

  useEffect(() => {
    if (initialConversationId) {
      // Reconnect to existing conversation
      setConversationId(initialConversationId);
      reconnect(initialConversationId);
    } else {
      // Fresh chat — reset everything
      wsRef.current?.close();
      wsRef.current = null;
      setMessages([]);
      setConversationId(null);
      setIsProcessing(false);
      setPendingCount(0);
      setIsConnected(false);
      setIsReconnecting(false);
      agentGroupIndexRef.current = new Map();
      currentAssistantIdRef.current = null;
      nextIdRef.current = 1;
      pendingRpcRef.current.clear();
      eventBufferRef.current = [];
      isBufferingRef.current = false;
    }

    return () => {
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

      await connect(activeConvId);
      await new Promise<void>((resolve) => {
        const check = () => {
          if (wsRef.current?.readyState === WebSocket.OPEN) resolve();
          else setTimeout(check, 50);
        };
        check();
      });
    }

    const userMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: "user",
      content: text,
      boxes: [],
    };
    setMessages((prev) => [...prev, userMsg]);

    const id = nextIdRef.current++;
    wsRef.current?.send(JSON.stringify({
      jsonrpc: "2.0",
      id,
      method: "message",
      params: { text },
    }));
  }, [conversationId, connect]);

  const cancel = useCallback((key: string, scope?: string) => {
    wsRef.current?.send(JSON.stringify({
      jsonrpc: "2.0",
      method: "cancel",
      params: { key, ...(scope ? { scope } : {}) },
    }));
  }, []);

  const resetConversation = useCallback(() => {
    wsRef.current?.close();
    wsRef.current = null;
    setMessages([]);
    setConversationId(null);
    setIsProcessing(false);
    setPendingCount(0);
    setIsConnected(false);
    setIsReconnecting(false);
    setMcpServerStatuses([]);
    setSandboxStatus(null);
    agentGroupIndexRef.current = new Map();
    currentAssistantIdRef.current = null;
    nextIdRef.current = 1;
    pendingRpcRef.current.clear();
    eventBufferRef.current = [];
    isBufferingRef.current = false;
    options?.onReset?.();
  }, [options]);

  return {
    messages,
    isProcessing,
    pendingCount,
    conversationId,
    isConnected,
    isReconnecting,
    mcpServerStatuses,
    sandboxStatus,
    socketEvents,
    sendMessage,
    sendRpc,
    cancel,
    resetConversation,
    clearSocketEvents: () => setSocketEvents([]),
  };
}
