import { describe, it, expect } from "vitest";
import type { ChatMessage } from "@donkeywork/api-client";

function userMsg(id: string, turnId?: string, pending?: boolean): ChatMessage {
  return { id, role: "user", content: `msg-${id}`, boxes: [], _turnId: turnId, _pending: pending };
}

function assistantMsg(id: string, turnId?: string): ChatMessage {
  return { id, role: "assistant", content: "", boxes: [], _turnId: turnId };
}

function applyTurnStart(messages: ChatMessage[], eventTurnId: string, newAssistantId: string): ChatMessage[] {
  const updated = messages.map((m) =>
    m._turnId === eventTurnId && m.role === "user" ? { ...m, _pending: false } : m
  );
  return [
    ...updated,
    { id: newAssistantId, role: "assistant" as const, content: "", boxes: [], _turnId: eventTurnId },
  ];
}

function applyTurnIdStamp(messages: ChatMessage[], userMsgId: string, turnId: string): ChatMessage[] {
  return messages.map((m) => m.id === userMsgId ? { ...m, _turnId: turnId, _pending: true } : m);
}

function applyCancelTurn(messages: ChatMessage[], turnId: string): ChatMessage[] {
  return messages.filter((m) => !(m.role === "user" && m._turnId === turnId));
}

describe("pending flag stamping", () => {
  it("sets _pending true when turnId arrives from server", () => {
    const msgs: ChatMessage[] = [userMsg("u1")];
    const result = applyTurnIdStamp(msgs, "u1", "turn-abc");
    expect(result[0]._pending).toBe(true);
    expect(result[0]._turnId).toBe("turn-abc");
  });

  it("clears _pending on turn_start for matching turnId", () => {
    const msgs: ChatMessage[] = [userMsg("u1", "turn-abc", true)];
    const result = applyTurnStart(msgs, "turn-abc", "asst-1");
    const userMessage = result.find((m) => m.id === "u1");
    expect(userMessage?._pending).toBe(false);
  });

  it("does not clear _pending on turn_start for different turnId", () => {
    const msgs: ChatMessage[] = [
      userMsg("u1", "turn-1", true),
      userMsg("u2", "turn-2", true),
    ];
    const result = applyTurnStart(msgs, "turn-1", "asst-1");
    const u2 = result.find((m) => m.id === "u2");
    expect(u2?._pending).toBe(true);
  });
});

describe("cancelTurn message removal", () => {
  it("removes only the user message with matching turnId", () => {
    const msgs: ChatMessage[] = [
      userMsg("u1", "turn-1", false),
      userMsg("u2", "turn-2", true),
      userMsg("u3", "turn-3", true),
    ];
    const result = applyCancelTurn(msgs, "turn-2");
    expect(result).toHaveLength(2);
    expect(result.find((m) => m.id === "u2")).toBeUndefined();
    expect(result.find((m) => m.id === "u1")).toBeDefined();
    expect(result.find((m) => m.id === "u3")).toBeDefined();
  });

  it("does not remove assistant messages with the same turnId", () => {
    const msgs: ChatMessage[] = [
      userMsg("u1", "turn-1", true),
      assistantMsg("a1", "turn-1"),
    ];
    const result = applyCancelTurn(msgs, "turn-1");
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe("a1");
  });

  it("leaves messages untouched when turnId does not match", () => {
    const msgs: ChatMessage[] = [
      userMsg("u1", "turn-1", true),
      userMsg("u2", "turn-2", true),
    ];
    const result = applyCancelTurn(msgs, "turn-999");
    expect(result).toHaveLength(2);
  });
});
