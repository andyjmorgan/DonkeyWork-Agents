import { describe, it, expect } from "vitest";
import type { ChatMessage } from "@donkeywork/api-client";
import { slotAssistantOnTurnStart, inlineConsumedMessage } from "./turnSlotting";

function userMsg(id: string, content: string, turnId?: string, pending?: boolean): ChatMessage {
  return { id, role: "user", content, boxes: [], _turnId: turnId, _pending: pending };
}

function assistantMsg(id: string, turnId?: string): ChatMessage {
  return { id, role: "assistant", content: "", boxes: [], _turnId: turnId };
}

function newAssistant(id: string, turnId: string): ChatMessage {
  return { id, role: "assistant", content: "", boxes: [], _turnId: turnId, _source: "user" };
}

describe("slotAssistantOnTurnStart", () => {
  it("appends assistant at end when there is no turnId", () => {
    const prev = [userMsg("u1", "hi")];
    const result = slotAssistantOnTurnStart(prev, newAssistant("a1", ""), undefined, "user", "");
    expect(result.map((m) => m.id)).toEqual(["u1", "a1"]);
  });

  it("slots assistant directly after the user message matched by turnId", () => {
    const prev = [userMsg("u1", "hi", "turn-1", true)];
    const result = slotAssistantOnTurnStart(prev, newAssistant("a1", "turn-1"), "turn-1", "user", "hi");
    expect(result.map((m) => m.id)).toEqual(["u1", "a1"]);
    expect(result.find((m) => m.id === "u1")?._pending).toBe(false);
  });

  it("keeps the assistant next to its user message when later messages are still queued", () => {
    const prev = [
      userMsg("u1", "first", "turn-1", false),
      assistantMsg("a-prev", "turn-1"),
      userMsg("u2", "second", "turn-2", true),
      userMsg("u3", "third", "turn-3", true),
    ];
    const result = slotAssistantOnTurnStart(prev, newAssistant("a2", "turn-2"), "turn-2", "user", "second");
    expect(result.map((m) => m.id)).toEqual(["u1", "a-prev", "u2", "a2", "u3"]);
    expect(result.find((m) => m.id === "u3")?._pending).toBe(true);
  });

  // The RPC reply that stamps _turnId races with turn_start. These cover the
  // case where turn_start wins and the user message has no _turnId yet.
  describe("RPC-reply race (user message not yet stamped with _turnId)", () => {
    it("matches the unstamped user message by preview and slots in place", () => {
      const prev = [
        userMsg("u1", "first", "turn-1", false),
        assistantMsg("a-prev", "turn-1"),
        userMsg("u2", "hi there"),
      ];
      const result = slotAssistantOnTurnStart(prev, newAssistant("a2", "turn-2"), "turn-2", "user", "hi there");
      expect(result.map((m) => m.id)).toEqual(["u1", "a-prev", "u2", "a2"]);
      const u2 = result.find((m) => m.id === "u2");
      expect(u2?._turnId).toBe("turn-2");
      expect(u2?._pending).toBe(false);
    });

    it("does not strand the assistant below later queued messages", () => {
      const prev = [
        userMsg("u1", "first", "turn-1", false),
        assistantMsg("a-prev", "turn-1"),
        userMsg("u2", "second message"),
        userMsg("u3", "third message"),
      ];
      const result = slotAssistantOnTurnStart(prev, newAssistant("a2", "turn-2"), "turn-2", "user", "second message");
      expect(result.map((m) => m.id)).toEqual(["u1", "a-prev", "u2", "a2", "u3"]);
    });

    it("matches the oldest unstamped user message via FIFO ordering when previews collide", () => {
      const prev = [
        userMsg("u2", "ok"),
        userMsg("u3", "ok"),
      ];
      const result = slotAssistantOnTurnStart(prev, newAssistant("a2", "turn-2"), "turn-2", "user", "ok");
      expect(result.map((m) => m.id)).toEqual(["u2", "a2", "u3"]);
    });

    it("matches a long message by its truncated preview prefix", () => {
      const longContent = "this is a very long message that the server truncated for the preview field";
      const prev = [userMsg("u1", longContent)];
      const result = slotAssistantOnTurnStart(prev, newAssistant("a1", "turn-1"), "turn-1", "user", "this is a very long message");
      expect(result.map((m) => m.id)).toEqual(["u1", "a1"]);
      expect(result.find((m) => m.id === "u1")?._turnId).toBe("turn-1");
    });

    it("does not steal an already-stamped user message belonging to another turn", () => {
      const prev = [userMsg("u1", "hi there", "turn-1", true)];
      const result = slotAssistantOnTurnStart(prev, newAssistant("a2", "turn-2"), "turn-2", "user", "hi there");
      expect(result.map((m) => m.id)).toEqual(["u1", "a2"]);
      expect(result.find((m) => m.id === "u1")?._turnId).toBe("turn-1");
    });
  });

  it("appends at end when no user message matches at all", () => {
    const prev = [assistantMsg("a-prev", "turn-1")];
    const result = slotAssistantOnTurnStart(prev, newAssistant("a2", "turn-2"), "turn-2", "user", "orphan");
    expect(result.map((m) => m.id)).toEqual(["a-prev", "a2"]);
  });
});

describe("inlineConsumedMessage", () => {
  function assistantWithBoxes(id: string, turnId: string, boxes: ChatMessage["boxes"]): ChatMessage {
    return { id, role: "assistant", content: "", boxes, _turnId: turnId };
  }

  // Models a queued message drained into an active turn: it never gets a
  // turn_start, so it must fold into the turn's assistant boxes at the
  // consumption point (current end of the box stream) rather than stay a bubble.
  it("removes the bubble and appends a user_message box at the host turn's box stream", () => {
    const host = assistantWithBoxes("a-host", "turn-host", [
      { type: "text", text: "Round 1 sleeping" },
      { type: "tool_use", toolName: "bash", toolUseId: "t1" },
    ]);
    const prev = [
      userMsg("u-host", "lets repeat", "turn-host", false),
      host,
      userMsg("u1", "1", "turn-1", true),
    ];
    const result = inlineConsumedMessage(prev, "turn-1", "a-host");
    expect(result.map((m) => m.id)).toEqual(["u-host", "a-host"]);
    const boxes = result.find((m) => m.id === "a-host")!.boxes;
    expect(boxes[boxes.length - 1]).toEqual({ type: "user_message", text: "1" });
  });

  it("appends multiple consumed messages in order at the consumption point", () => {
    let msgs: ChatMessage[] = [
      assistantWithBoxes("a-host", "turn-host", [{ type: "tool_use", toolName: "bash", toolUseId: "t1" }]),
      userMsg("u1", "1", "turn-1", true),
      userMsg("u2", "2", "turn-2", true),
    ];
    msgs = inlineConsumedMessage(msgs, "turn-1", "a-host");
    msgs = inlineConsumedMessage(msgs, "turn-2", "a-host");
    expect(msgs.map((m) => m.id)).toEqual(["a-host"]);
    expect(msgs[0].boxes.filter((b) => b.type === "user_message")).toEqual([
      { type: "user_message", text: "1" },
      { type: "user_message", text: "2" },
    ]);
  });

  it("does not touch a message that is not yet queued (no match)", () => {
    const prev = [assistantWithBoxes("a-host", "turn-host", [])];
    const result = inlineConsumedMessage(prev, "turn-missing", "a-host");
    expect(result).toEqual(prev);
  });

  it("falls back to clearing _pending in place when the host assistant is unknown", () => {
    const prev = [userMsg("u1", "1", "turn-1", true)];
    const result = inlineConsumedMessage(prev, "turn-1", undefined);
    expect(result.map((m) => m.id)).toEqual(["u1"]);
    expect(result.find((m) => m.id === "u1")?._pending).toBe(false);
  });
});
