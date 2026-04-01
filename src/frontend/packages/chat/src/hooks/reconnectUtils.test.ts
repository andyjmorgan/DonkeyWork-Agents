import { describe, it, expect } from "vitest";
import { getReconnectDelay, shouldAttemptReconnect, MAX_RETRIES } from "./reconnectUtils";

describe("getReconnectDelay", () => {
  it("returns 0 for attempt 0 (immediate)", () => {
    expect(getReconnectDelay(0)).toBe(0);
  });

  it("returns ~1s for attempt 1", () => {
    const delay = getReconnectDelay(1);
    expect(delay).toBeGreaterThanOrEqual(800);
    expect(delay).toBeLessThanOrEqual(1200);
  });

  it("returns ~2s for attempt 2", () => {
    const delay = getReconnectDelay(2);
    expect(delay).toBeGreaterThanOrEqual(1600);
    expect(delay).toBeLessThanOrEqual(2400);
  });

  it("returns ~4s for attempt 3", () => {
    const delay = getReconnectDelay(3);
    expect(delay).toBeGreaterThanOrEqual(3200);
    expect(delay).toBeLessThanOrEqual(4800);
  });

  it("caps at 30s for high attempts", () => {
    for (let i = 8; i < 20; i++) {
      const delay = getReconnectDelay(i);
      expect(delay).toBeLessThanOrEqual(36000); // 30000 * 1.2 jitter
      expect(delay).toBeGreaterThanOrEqual(24000); // 30000 * 0.8 jitter
    }
  });

  it("increases monotonically (ignoring jitter) through the first 6 attempts", () => {
    const samples = 50;
    for (let attempt = 1; attempt <= 5; attempt++) {
      let lowerAvg = 0;
      let upperAvg = 0;
      for (let i = 0; i < samples; i++) {
        lowerAvg += getReconnectDelay(attempt);
        upperAvg += getReconnectDelay(attempt + 1);
      }
      expect(upperAvg / samples).toBeGreaterThan(lowerAvg / samples);
    }
  });
});

describe("shouldAttemptReconnect", () => {
  const validParams = {
    intentionalClose: false,
    conversationId: "conv-123",
    isAuthenticated: true,
    attemptCount: 0,
    isMounted: true,
  };

  it("returns true when all conditions are met", () => {
    expect(shouldAttemptReconnect(validParams)).toBe(true);
  });

  it("returns false when close was intentional", () => {
    expect(shouldAttemptReconnect({ ...validParams, intentionalClose: true })).toBe(false);
  });

  it("returns false when no conversation exists", () => {
    expect(shouldAttemptReconnect({ ...validParams, conversationId: null })).toBe(false);
  });

  it("returns false when user is not authenticated", () => {
    expect(shouldAttemptReconnect({ ...validParams, isAuthenticated: false })).toBe(false);
  });

  it("returns false when max retries exceeded", () => {
    expect(shouldAttemptReconnect({ ...validParams, attemptCount: MAX_RETRIES })).toBe(false);
  });

  it("returns true at one below max retries", () => {
    expect(shouldAttemptReconnect({ ...validParams, attemptCount: MAX_RETRIES - 1 })).toBe(true);
  });

  it("returns false when component is unmounted", () => {
    expect(shouldAttemptReconnect({ ...validParams, isMounted: false })).toBe(false);
  });
});
