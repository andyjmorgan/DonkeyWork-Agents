import type { ChatMessage } from "@donkeywork/api-client";

/**
 * Places the new assistant message for a starting turn directly after the user
 * message that turn is responding to, clearing that user message's pending flag.
 *
 * The user message's `_turnId` is stamped asynchronously by the `message` RPC
 * reply, which races with the `turn_start` event. When turn_start wins the race
 * the user message has no `_turnId` yet, so a turnId match alone leaves the
 * assistant stranded at the bottom of the transcript (below later still-queued
 * messages) and the user message showing a stale cancel-X. The preview fallback
 * matches the oldest unstamped user message — the queue is FIFO, so that is the
 * message this turn belongs to — and stamps its `_turnId` so the later RPC reply
 * and any cancel correlate correctly.
 */
export function slotAssistantOnTurnStart(
  messages: ChatMessage[],
  newAssistant: ChatMessage,
  eventTurnId: string | undefined,
  source: string,
  preview: string
): ChatMessage[] {
  if (!eventTurnId) {
    return [...messages, newAssistant];
  }

  let ackIdx = messages.findIndex(
    (m) => m._turnId === eventTurnId && m.role === "user"
  );

  if (ackIdx === -1 && source === "user") {
    ackIdx = messages.findIndex(
      (m) =>
        m.role === "user" &&
        !m._turnId &&
        (!preview ||
          m.content.startsWith(preview) ||
          preview.startsWith(m.content))
    );
  }

  if (ackIdx === -1) {
    return [...messages, newAssistant];
  }

  const ackedMsg: ChatMessage = {
    ...messages[ackIdx],
    _pending: false,
    _turnId: eventTurnId,
  };

  return [
    ...messages.slice(0, ackIdx),
    ackedMsg,
    newAssistant,
    ...messages.slice(ackIdx + 1),
  ];
}

/**
 * Folds a queued user message that was drained into an active turn into that
 * turn's assistant message as an inline `user_message` box, removing the separate
 * top-level bubble. Such a message never starts its own turn (so it has no
 * turn_start to clear its pending cancel-X) and the whole turn renders as a single
 * assistant message whose rounds are boxes — so the only way to interleave the
 * message "between responses" is to append it as a box at the consumption point,
 * which is the current end of the assistant's box stream.
 *
 * Falls back to clearing `_pending` in place when the host assistant message isn't
 * found yet (e.g. an early race), so the bubble at least loses its stuck X.
 */
export function inlineConsumedMessage(
  messages: ChatMessage[],
  consumedTurnId: string,
  hostAssistantId: string | undefined
): ChatMessage[] {
  const idx = messages.findIndex(
    (m) => m.role === "user" && m._turnId === consumedTurnId
  );
  if (idx === -1) {
    return messages;
  }

  const hostIdx = hostAssistantId
    ? messages.findIndex((m) => m.id === hostAssistantId)
    : -1;
  if (hostIdx === -1) {
    return messages.map((m, i) => (i === idx ? { ...m, _pending: false } : m));
  }

  const text = messages[idx].content;
  const withoutBubble = messages.filter((_, i) => i !== idx);
  return withoutBubble.map((m) =>
    m.id === hostAssistantId
      ? { ...m, boxes: [...m.boxes, { type: "user_message" as const, text }] }
      : m
  );
}
