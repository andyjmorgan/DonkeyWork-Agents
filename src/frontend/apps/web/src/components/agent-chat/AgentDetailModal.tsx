import { useState, useEffect, useRef } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  ScrollArea,
} from '@donkeywork/ui'
import { BoxList } from "@/components/agent-chat/BoxRenderer";
import { PulseDots, ActivityIndicator } from "@/components/agent-chat/PulseDots";
import { AgentCard } from "@/components/agent-chat/AgentCard";
import type { ContentBox } from "@donkeywork/api-client";
import { Check, Square, Loader2 } from "lucide-react";

type NestedAgent = {
  agentType: string;
  agentKey: string;
  label?: string;
  boxes: ContentBox[];
  isComplete: boolean;
};

function extractNestedAgent(box: ContentBox): NestedAgent | null {
  if (box.type === "tool_use" && box.subAgent) {
    return {
      agentType: box.subAgent.agentType,
      agentKey: box.subAgent.agentKey,
      label: box.subAgent.label,
      boxes: box.subAgent.boxes,
      isComplete: !!box.subAgent.isComplete || !!box.isComplete,
    };
  }
  if (box.type === "agent_group") {
    return {
      agentType: box.agentType,
      agentKey: box.agentKey,
      label: box.label,
      boxes: box.boxes,
      isComplete: !!box.isComplete,
    };
  }
  return null;
}

export function AgentDetailModal({
  open,
  onOpenChange,
  agentType,
  agentKey,
  label,
  boxes,
  isComplete,
  isStreaming,
  onCancel,
  onFetchMessages,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  agentType: string;
  agentKey: string;
  label?: string;
  boxes: ContentBox[];
  isComplete: boolean;
  isStreaming: boolean;
  onCancel?: (agentKey: string) => void;
  onFetchMessages?: (agentKey: string) => Promise<ContentBox[]>;
}) {
  const [selectedChild, setSelectedChild] = useState<NestedAgent | null>(null);
  const [fetchedBoxes, setFetchedBoxes] = useState<ContentBox[]>([]);
  const [isFetching, setIsFetching] = useState(false);
  const fetchedKeyRef = useRef<string | null>(null);
  const isActive = isStreaming && !isComplete;

  useEffect(() => {
    if (open) {
      setSelectedChild(null);
    }
  }, [open, agentKey]);

  // Fetch messages when opening a completed agent with empty boxes
  useEffect(() => {
    if (!open || !isComplete || !onFetchMessages) return;
    if (boxes.length > 0) return;
    if (fetchedKeyRef.current === agentKey) return;

    fetchedKeyRef.current = agentKey;
    setIsFetching(true);
    onFetchMessages(agentKey)
      .then((result) => setFetchedBoxes(result))
      .catch(() => setFetchedBoxes([]))
      .finally(() => setIsFetching(false));
  }, [open, isComplete, boxes.length, agentKey, onFetchMessages]);

  // Reset fetched state when modal closes or agent changes
  useEffect(() => {
    if (!open) {
      setFetchedBoxes([]);
      fetchedKeyRef.current = null;
    }
  }, [open]);

  const displayBoxes = boxes.length > 0 ? boxes : fetchedBoxes;

  const refreshedChild = selectedChild
    ? (() => {
        for (const box of displayBoxes) {
          const nested = extractNestedAgent(box);
          if (nested && nested.agentKey === selectedChild.agentKey) return nested;
        }
        return selectedChild;
      })()
    : null;

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-3xl max-h-[80vh] flex flex-col gap-0 p-0 overflow-hidden bg-card border-border">
          <DialogHeader className="px-6 pt-6 pb-3 border-b border-border">
            <DialogTitle className="flex items-center gap-3">
              <span className="rounded-lg bg-cyan-500/10 text-cyan-400 px-2.5 py-1 text-xs uppercase tracking-wider font-semibold border border-cyan-500/20">
                {agentType}
              </span>
              {isActive && <ActivityIndicator />}
              {isComplete && (
                <Check className="w-4 h-4 text-emerald-400" strokeWidth={2.5} />
              )}
              {isActive && onCancel && (
                <button
                  type="button"
                  onClick={() => onCancel(agentKey)}
                  className="flex items-center gap-1 px-2 py-1 rounded-lg text-[10px] font-medium bg-red-500/10 text-red-400 border border-red-500/20 hover:bg-red-500/20 transition-all cursor-pointer ml-auto"
                >
                  <Square className="w-2.5 h-2.5" />
                  Cancel
                </button>
              )}
            </DialogTitle>
            {label && (
              <DialogDescription className="text-sm text-muted-foreground mt-1">
                {label}
              </DialogDescription>
            )}
            <p className="text-[10px] font-mono text-muted-foreground mt-0.5 select-all">{agentKey}</p>
          </DialogHeader>

          <ScrollArea className="flex-1 min-h-0 overflow-y-auto">
            <div className="px-6 py-4 flex flex-col gap-1">
              {isFetching ? (
                <div className="flex flex-col items-center justify-center py-12">
                  <Loader2 className="w-6 h-6 text-cyan-400 animate-spin mb-2" />
                  <p className="text-sm text-muted-foreground">Loading transcript...</p>
                </div>
              ) : (
                <>
                  <BoxList
                    boxes={displayBoxes}
                    isStreaming={isActive}
                    renderOverride={(box) => {
                      const nested = extractNestedAgent(box);
                      if (nested) {
                        return (
                          <div className="my-1">
                            <AgentCard
                              agentType={nested.agentType}
                              isComplete={nested.isComplete}
                              boxes={nested.boxes}
                              onClick={() => setSelectedChild(nested)}
                            />
                          </div>
                        );
                      }
                      return undefined;
                    }}
                  />
                  {isActive && (
                    <span className="ml-1.5"><PulseDots color="bg-cyan-400" size="w-1 h-1" /></span>
                  )}
                  {displayBoxes.length === 0 && (
                    <div className="flex flex-col items-center justify-center py-12">
                      <p className="text-sm text-muted-foreground">
                        {isActive ? "Waiting for activity..." : "No activity recorded."}
                      </p>
                    </div>
                  )}
                </>
              )}
            </div>
          </ScrollArea>
        </DialogContent>
      </Dialog>

      {refreshedChild && (
        <AgentDetailModal
          open={!!selectedChild}
          onOpenChange={(o) => { if (!o) setSelectedChild(null); }}
          agentType={refreshedChild.agentType}
          agentKey={refreshedChild.agentKey}
          label={refreshedChild.label}
          boxes={refreshedChild.boxes}
          isComplete={refreshedChild.isComplete}
          isStreaming={isStreaming}
          onCancel={onCancel}
          onFetchMessages={onFetchMessages}
        />
      )}
    </>
  );
}
