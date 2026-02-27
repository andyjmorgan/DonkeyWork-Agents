import { useRef, useCallback } from "react";

type StreamEvent = Record<string, unknown>;

/**
 * Buffers stream events during reconnection until state is restored,
 * then replays them and switches to live mode.
 */
export function useMessageAccumulator(
  onEvent: (event: StreamEvent) => void
) {
  const bufferRef = useRef<StreamEvent[]>([]);
  const isBufferingRef = useRef(false);

  const startBuffering = useCallback(() => {
    bufferRef.current = [];
    isBufferingRef.current = true;
  }, []);

  const stopBufferingAndReplay = useCallback(() => {
    isBufferingRef.current = false;
    const events = bufferRef.current;
    bufferRef.current = [];
    for (const evt of events) {
      onEvent(evt);
    }
  }, [onEvent]);

  const handleEvent = useCallback(
    (event: StreamEvent) => {
      if (isBufferingRef.current) {
        bufferRef.current.push(event);
      } else {
        onEvent(event);
      }
    },
    [onEvent]
  );

  return {
    handleEvent,
    startBuffering,
    stopBufferingAndReplay,
    isBuffering: isBufferingRef,
  };
}
