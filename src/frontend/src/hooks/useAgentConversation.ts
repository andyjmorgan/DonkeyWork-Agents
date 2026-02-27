import { useState, useCallback, useRef, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "@/store/auth";
import { conversations } from "@/lib/api";
import { internalToChat } from "@/components/agent-chat/MessageRenderer";
import type {
  ContentBox,
  TextBox,
  ThinkingBox,
  AgentCompleteReason,
  ChatMessage,
  WebSearchResult,
} from "@/types/agent-chat";
import type { InternalMessage, GetStateResponse, TrackedAgent } from "@/types/internal-messages";

type AgentGroupEntry = { messageId: string; boxIndex: number };

export function useAgentConversation(initialConversationId?: string) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isProcessing, setIsProcessing] = useState(false);
  const [pendingCount, setPendingCount] = useState(0);
  const [conversationId, setConversationId] = useState<string | null>(initialConversationId ?? null);
  const [isConnected, setIsConnected] = useState(false);
  const [isReconnecting, setIsReconnecting] = useState(false);

  const navigate = useNavigate();
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

  function updateNestedGroup(
    box: ContentBox,
    targetKey: string,
    updater: (innerBoxes: ContentBox[]) => ContentBox[]
  ): ContentBox | null {
    if (box.type === "tool_use" && box.subAgent) {
      if (box.subAgent.agentKey === targetKey) {
        return { ...box, subAgent: { ...box.subAgent, boxes: updater(box.subAgent.boxes) } };
      }
      for (let i = 0; i < box.subAgent.boxes.length; i++) {
        const result = updateNestedGroup(box.subAgent.boxes[i], targetKey, updater);
        if (result) {
          const newInner = [...box.subAgent.boxes];
          newInner[i] = result;
          return { ...box, subAgent: { ...box.subAgent, boxes: newInner } };
        }
      }
    }
    if (box.type === "agent_group") {
      if (box.agentKey === targetKey) {
        return { ...box, boxes: updater(box.boxes) };
      }
      for (let i = 0; i < box.boxes.length; i++) {
        const result = updateNestedGroup(box.boxes[i], targetKey, updater);
        if (result) {
          const newInner = [...box.boxes];
          newInner[i] = result;
          return { ...box, boxes: newInner };
        }
      }
    }
    return null;
  }

  function appendOrCreate(
    fallbackMessageId: string,
    agentKey: string,
    boxType: string,
    newBox: ContentBox,
    appender?: (existing: ContentBox) => ContentBox
  ) {
    const entry = agentGroupIndexRef.current.get(agentKey);

    if (entry) {
      updateBoxes(entry.messageId, (boxes) => {
        const newBoxes = [...boxes];
        const host = newBoxes[entry.boxIndex];
        const updated = updateNestedGroup(host, agentKey, (inner) => {
          const last = inner[inner.length - 1];
          if (last?.type === boxType && appender) {
            return [...inner.slice(0, -1), appender(last)];
          }
          return [...inner, newBox];
        });
        if (updated) {
          newBoxes[entry.boxIndex] = updated;
          return newBoxes;
        }
        return newBoxes;
      });
    } else {
      updateBoxes(fallbackMessageId, (boxes) => {
        const last = boxes[boxes.length - 1];
        if (last?.type === boxType && appender) {
          return [...boxes.slice(0, -1), appender(last)];
        }
        return [...boxes, newBox];
      });
    }
  }

  function markAgentCompleteInMessage(messageId: string, boxes: ContentBox[], forAgent: string, reason: AgentCompleteReason = "completed"): ContentBox[] {
    const entry = agentGroupIndexRef.current.get(forAgent);
    if (entry && entry.messageId === messageId) {
      return boxes.map((b, i) => {
        if (i !== entry.boxIndex) return b;
        const updated = markNestedComplete(b, forAgent, reason);
        return updated ?? b;
      });
    }
    return boxes;
  }

  function markAgentCompleteGlobal(forAgent: string, reason: AgentCompleteReason = "completed") {
    const entry = agentGroupIndexRef.current.get(forAgent);
    if (entry) {
      updateBoxes(entry.messageId, (boxes) => markAgentCompleteInMessage(entry.messageId, boxes, forAgent, reason));
    }
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
        const parentEntry = agentGroupIndexRef.current.get(agentKey);
        const targetMessageId = parentEntry?.messageId ?? assistantId;

        updateBoxes(targetMessageId, (boxes) => {
          const newBoxes = [...boxes];

          if (parentEntry) {
            const host = newBoxes[parentEntry.boxIndex];
            const getInner = (): ContentBox[] | null => {
              if (host?.type === "tool_use" && host.subAgent) return [...host.subAgent.boxes];
              if (host?.type === "agent_group") return [...host.boxes];
              return null;
            };
            const innerBoxes = getInner();
            if (innerBoxes) {
              let attached = false;
              for (let i = innerBoxes.length - 1; i >= 0; i--) {
                const b = innerBoxes[i];
                if (b.type === "tool_use" && !b.subAgent) {
                  innerBoxes[i] = {
                    ...b,
                    subAgent: { type: "agent_group", agentKey: spawnedKey, agentType, label, boxes: [] },
                  };
                  attached = true;
                  break;
                }
              }
              if (!attached) {
                innerBoxes.push({
                  type: "agent_group" as const,
                  agentKey: spawnedKey,
                  agentType,
                  label,
                  boxes: [],
                });
              }
              agentGroupIndexRef.current.set(spawnedKey, { messageId: targetMessageId, boxIndex: parentEntry.boxIndex });
              if (host?.type === "tool_use" && host.subAgent) {
                newBoxes[parentEntry.boxIndex] = { ...host, subAgent: { ...host.subAgent, boxes: innerBoxes } };
              } else if (host?.type === "agent_group") {
                newBoxes[parentEntry.boxIndex] = { ...host, boxes: innerBoxes };
              }
              return newBoxes;
            }
          }

          for (let i = newBoxes.length - 1; i >= 0; i--) {
            const b = newBoxes[i];
            if (b.type === "tool_use" && !b.subAgent) {
              newBoxes[i] = {
                ...b,
                subAgent: { type: "agent_group", agentKey: spawnedKey, agentType, label, boxes: [] },
              };
              agentGroupIndexRef.current.set(spawnedKey, { messageId: targetMessageId, boxIndex: i });
              return newBoxes;
            }
          }
          const newIdx = newBoxes.length;
          agentGroupIndexRef.current.set(spawnedKey, { messageId: targetMessageId, boxIndex: newIdx });
          return [...newBoxes, {
            type: "agent_group" as const,
            agentKey: spawnedKey,
            agentType,
            label,
            boxes: [],
          }];
        });
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

      case "usage":
        appendOrCreate(assistantId, agentKey, "", {
          type: "usage",
          inputTokens: (data.inputTokens as number) ?? 0,
          outputTokens: (data.outputTokens as number) ?? 0,
          webSearchRequests: (data.webSearchRequests as number) ?? 0,
        });
        break;

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
    const protocol = window.location.protocol === "https:" ? "wss:" : "ws:";
    const wsUrl = `${protocol}//${window.location.host}/api/v1/conversations/${convId}/ws?token=${encodeURIComponent(token ?? "")}`;
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
      // Get state and agents from grain in parallel
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

      // Convert grain state to ChatMessage[] format, overlaying agent info
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

      // Stop buffering and replay
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
      // Create a conversation record and connect
      const result = await conversations.create({});
      activeConvId = result.id;
      setConversationId(activeConvId);

      // Update URL without triggering React Router remount
      window.history.replaceState(null, "", `/agent-chat/${activeConvId}`);

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
    agentGroupIndexRef.current = new Map();
    currentAssistantIdRef.current = null;
    nextIdRef.current = 1;
    pendingRpcRef.current.clear();
    eventBufferRef.current = [];
    isBufferingRef.current = false;
    navigate("/agent-chat", { replace: true });
  }, [navigate]);

  return {
    messages,
    isProcessing,
    pendingCount,
    conversationId,
    isConnected,
    isReconnecting,
    sendMessage,
    cancel,
    resetConversation,
  };
}
