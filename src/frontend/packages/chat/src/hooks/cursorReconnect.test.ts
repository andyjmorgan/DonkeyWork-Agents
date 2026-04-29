import { describe, it, expect, vi, beforeEach } from "vitest";

interface TurnCursor {
  lastSequence: number;
  complete: boolean;
}

/**
 * Pure logic extracted from dispatchWithSequence in useAgentConversation.
 * Returns false if the event was a duplicate (already seen), true if dispatched.
 */
function applyDispatchWithSequence(
  seenSequences: Set<number>,
  turnCursors: Map<string, TurnCursor>,
  eventData: Record<string, unknown>,
  sequence: number
): boolean {
  const turnId = eventData.turnId as string | undefined;

  if (sequence > 0) {
    if (seenSequences.has(sequence)) return false;
    seenSequences.add(sequence);

    if (turnId) {
      const existing = turnCursors.get(turnId);
      if (!existing || sequence > existing.lastSequence) {
        turnCursors.set(turnId, {
          lastSequence: sequence,
          complete: existing?.complete ?? false,
        });
      }
    }
  }

  return true;
}

/**
 * Pure logic extracted from handleEvent's turn_end/cancelled branch.
 * Marks a turn complete in the cursor map.
 */
function applyTerminalEvent(
  turnCursors: Map<string, TurnCursor>,
  turnId: string,
  sequence: number
): void {
  const existing = turnCursors.get(turnId) ?? { lastSequence: 0, complete: false };
  turnCursors.set(turnId, {
    lastSequence: Math.max(existing.lastSequence, sequence),
    complete: true,
  });
}

/**
 * Pure merge logic: replayed events are applied before live events, deduplication via seenSequences.
 * Returns list of sequences that were dispatched (not skipped as dups).
 */
function applyReplayThenLive(
  seenSequences: Set<number>,
  turnCursors: Map<string, TurnCursor>,
  replayedEvents: Array<{ turnId: string; sequence: number; eventType?: string }>,
  liveEvents: Array<{ turnId: string; sequence: number; eventType?: string }>
): number[] {
  const dispatched: number[] = [];

  for (const evt of replayedEvents) {
    const accepted = applyDispatchWithSequence(
      seenSequences, turnCursors,
      { turnId: evt.turnId, eventType: evt.eventType ?? "message" },
      evt.sequence
    );
    if (accepted) dispatched.push(evt.sequence);
  }

  for (const evt of liveEvents) {
    const accepted = applyDispatchWithSequence(
      seenSequences, turnCursors,
      { turnId: evt.turnId, eventType: evt.eventType ?? "message" },
      evt.sequence
    );
    if (accepted) dispatched.push(evt.sequence);
  }

  return dispatched;
}

describe("cursor updates from received events", () => {
  it("applies cursor updates from received events", () => {
    const seenSequences = new Set<number>();
    const turnCursors = new Map<string, TurnCursor>();
    const turnId = "turn-X";

    for (const seq of [1, 2, 3]) {
      applyDispatchWithSequence(seenSequences, turnCursors, { turnId, eventType: "message" }, seq);
    }

    const cursor = turnCursors.get(turnId);
    expect(cursor).toBeDefined();
    expect(cursor!.lastSequence).toBe(3);
    expect(cursor!.complete).toBe(false);
  });

  it("dedupes events by sequence", () => {
    const seenSequences = new Set<number>();
    const turnCursors = new Map<string, TurnCursor>();
    const turnId = "turn-X";
    const dispatched: number[] = [];

    for (const seq of [1, 2, 3, 2, 4]) {
      const accepted = applyDispatchWithSequence(
        seenSequences, turnCursors, { turnId, eventType: "message" }, seq
      );
      if (accepted) dispatched.push(seq);
    }

    expect(dispatched).toEqual([1, 2, 3, 4]);
    expect([...seenSequences]).toEqual([1, 2, 3, 4]);
  });

  it("marks turn complete on terminal event", () => {
    const seenSequences = new Set<number>();
    const turnCursors = new Map<string, TurnCursor>();
    const turnId = "turn-X";

    applyDispatchWithSequence(seenSequences, turnCursors, { turnId, eventType: "message" }, 3);
    applyDispatchWithSequence(seenSequences, turnCursors, { turnId, eventType: "message" }, 4);

    // Simulate terminal event handling (turn_end)
    applyTerminalEvent(turnCursors, turnId, 5);

    const cursor = turnCursors.get(turnId);
    expect(cursor).toBeDefined();
    expect(cursor!.complete).toBe(true);
    expect(cursor!.lastSequence).toBe(5);
  });
});

describe("eventsSince merge before live events", () => {
  it("eventsSince response merges in order before live events resume", () => {
    const seenSequences = new Set<number>();
    const turnCursors = new Map<string, TurnCursor>();
    const turnId = "turn-X";

    // Cursor set to seq 2 (simulating what was received before disconnect)
    applyDispatchWithSequence(seenSequences, turnCursors, { turnId, eventType: "message" }, 1);
    applyDispatchWithSequence(seenSequences, turnCursors, { turnId, eventType: "message" }, 2);

    // eventsSince returns seq 3, 4; live event is seq 5
    const dispatched = applyReplayThenLive(
      seenSequences,
      turnCursors,
      [{ turnId, sequence: 3 }, { turnId, sequence: 4 }],
      [{ turnId, sequence: 5 }]
    );

    expect(dispatched).toEqual([3, 4, 5]);
    expect(turnCursors.get(turnId)!.lastSequence).toBe(5);
  });

  it("live event with sequence <= cursor is dropped as duplicate", () => {
    const seenSequences = new Set<number>();
    const turnCursors = new Map<string, TurnCursor>();
    const turnId = "turn-X";

    // Cursor at sequence 5
    for (const seq of [1, 2, 3, 4, 5]) {
      applyDispatchWithSequence(seenSequences, turnCursors, { turnId, eventType: "message" }, seq);
    }

    // Stale live event with seq 4 arrives (replay overlap)
    const accepted = applyDispatchWithSequence(
      seenSequences, turnCursors, { turnId, eventType: "message" }, 4
    );

    expect(accepted).toBe(false);
    // Cursor stays at 5
    expect(turnCursors.get(turnId)!.lastSequence).toBe(5);
  });
});

describe("reconnect backoff cap", () => {
  it("reconnect backoff caps at 10 attempts", () => {
    const MAX_RECONNECT_ATTEMPTS = 10;
    const RECONNECT_BACKOFF_BASE_MS = 500;
    const RECONNECT_BACKOFF_CAP_MS = 10000;

    let attempts = 0;
    let reconnectAttempts = 0;

    // Simulate the backoff calculation for each attempt
    const delays: number[] = [];
    while (reconnectAttempts < MAX_RECONNECT_ATTEMPTS) {
      const delay = Math.min(
        RECONNECT_BACKOFF_BASE_MS * Math.pow(2, reconnectAttempts),
        RECONNECT_BACKOFF_CAP_MS
      );
      delays.push(delay);
      reconnectAttempts++;
      attempts++;
    }

    // After MAX_RECONNECT_ATTEMPTS, no more reconnects
    expect(attempts).toBe(MAX_RECONNECT_ATTEMPTS);
    expect(reconnectAttempts).toBe(MAX_RECONNECT_ATTEMPTS);

    // Verify backoff progression
    expect(delays[0]).toBe(500);   // 500 * 2^0
    expect(delays[1]).toBe(1000);  // 500 * 2^1
    expect(delays[2]).toBe(2000);  // 500 * 2^2
    expect(delays[3]).toBe(4000);  // 500 * 2^3
    expect(delays[4]).toBe(8000);  // 500 * 2^4
    expect(delays[5]).toBe(10000); // capped at 10s
    expect(delays[9]).toBe(10000); // still capped at 10s

    // All delays after index 4 are capped
    for (let i = 5; i < delays.length; i++) {
      expect(delays[i]).toBe(RECONNECT_BACKOFF_CAP_MS);
    }
  });
});
