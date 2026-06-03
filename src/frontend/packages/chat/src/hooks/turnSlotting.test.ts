import { describe, it, expect } from "vitest";
import type { ChatMessage } from "@donkeywork/api-client";
import { slotAssistantOnTurnStart } from "./turnSlotting";

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
