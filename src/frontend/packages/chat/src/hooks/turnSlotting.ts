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
 * Acknowledges a queued user message that was drained into an active turn rather
 * than starting its own. Such a message never receives a `turn_start`, so its
 * bubble is left `_pending` (showing a stuck cancel-X) and stranded at the bottom
 * of the transcript. Clears `_pending` and slots the bubble just before the host
 * turn's assistant message — the turn it was folded into — so it reads
 * chronologically rather than piling up below later responses.
 */
export function slotConsumedMessage(
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

  const ackedMsg: ChatMessage = { ...messages[idx], _pending: false };
  const rest = [...messages.slice(0, idx), ...messages.slice(idx + 1)];

  const hostIdx = hostAssistantId
    ? rest.findIndex((m) => m.id === hostAssistantId)
    : -1;
  const insertAt = hostIdx === -1 ? rest.length : hostIdx;

  return [...rest.slice(0, insertAt), ackedMsg, ...rest.slice(insertAt)];
}
