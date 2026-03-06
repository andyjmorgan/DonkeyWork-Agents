import React from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { ContentBox, CitationBox } from "@donkeywork/api-client";
import { CitationChipRow } from "./CitationChip";
import { PulseDots } from "./PulseDots";
import { useChatConfig } from "../context";
import { BrainCircuit, Wrench, Globe, Check, CircleX, Clock, ExternalLink } from "lucide-react";

const BLOCK_TAGS_RE = /<(?:system_warning|function_results|result|output)\b[^>]*>[\s\S]*?<\/(?:system_warning|function_results|result|output)>/g;
const ORPHAN_TAGS_RE = /<\/?(?:system_warning|function_results|result|output)\b[^>]*>/g;
const AGENT_RESULTS_RE = /\$AGENT_RESULTS\b/g;
const PARTIAL_TAG_RE = /<(?:system_warning|function_results|result|output)[^>]*$/;
function sanitizeText(text: string): string {
  return text
    .replace(BLOCK_TAGS_RE, "")
    .replace(ORPHAN_TAGS_RE, "")
    .replace(AGENT_RESULTS_RE, "")
    .replace(PARTIAL_TAG_RE, "");
}

export function BoxRenderer({ box, isStreaming = false }: { box: ContentBox; isStreaming?: boolean }) {
  const { renderJson } = useChatConfig();
  switch (box.type) {
    case "text": {
      const clean = sanitizeText(box.text);
      if (!clean.trim()) return null;
      return (
        <div className="prose prose-sm dark:prose-invert max-w-none prose-p:leading-relaxed prose-pre:bg-background prose-pre:border prose-pre:border-border prose-code:text-cyan-400 prose-a:text-cyan-400 prose-a:no-underline hover:prose-a:underline [&>*:last-child]:mb-0">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{clean}</ReactMarkdown>
        </div>
      );
    }

    case "thinking":
      return (
        <details open className="my-2 rounded-lg border border-purple-500/15 bg-purple-500/5 overflow-hidden group">
          <summary className="cursor-pointer select-none px-3 py-2 text-xs font-medium text-purple-400 flex items-center gap-2 hover:bg-purple-500/10 transition-colors list-none [&::-webkit-details-marker]:hidden">
            <svg className="w-3 h-3 shrink-0 transition-transform group-open:rotate-90" viewBox="0 0 24 24" fill="currentColor"><path d="M8 5l8 7-8 7z" /></svg>
            <BrainCircuit className="w-3.5 h-3.5 shrink-0" />
            Thinking
          </summary>
          <div className="px-3 py-2.5 border-t border-purple-500/10">
            <p className="text-xs text-muted-foreground italic whitespace-pre-wrap leading-relaxed">{box.text}</p>
          </div>
        </details>
      );

    case "tool_use": {
      if (box.subAgent) return null;

      const isWebSearch = box.toolName === "web_search";
      const isRunning = isStreaming && !box.isComplete;
      const hasResult = box.result !== undefined;
      const hasWebResults = isWebSearch && box.webSearchResults && box.webSearchResults.length > 0;
      const hasArgs = !!box.arguments;
      const isExpandable = hasArgs || hasResult || hasWebResults;

      const formatDuration = (ms: number) => {
        if (ms < 1000) return `${ms}ms`;
        return `${(ms / 1000).toFixed(1)}s`;
      };

      const tryParseJson = (raw: string): unknown | null => {
        try {
          const parsed = JSON.parse(raw);
          return typeof parsed === "object" && parsed !== null ? parsed : null;
        } catch { return null; }
      };

      const accentColor = isWebSearch ? "cyan" : "purple";
      const IconComponent = isWebSearch ? Globe : Wrench;
      const dotColor = isWebSearch ? "bg-cyan-400" : "bg-purple-400";

      const summaryColor = isWebSearch
        ? (box.isComplete ? "text-emerald-400" : "text-cyan-400")
        : (!hasResult ? "text-purple-400" : box.success ? "text-emerald-400" : "text-red-400");

      const borderColor = isWebSearch
        ? (isRunning ? "bg-cyan-500/5 border-cyan-500/20" : "bg-cyan-500/5 border-cyan-500/15")
        : isRunning
          ? "border-purple-500/20 bg-purple-500/5"
          : !hasResult
            ? "border-border bg-muted/30"
            : box.success
              ? "border-emerald-500/15 bg-emerald-500/5"
              : "border-red-500/15 bg-red-500/5";

      if (!isExpandable) {
        return (
          <div className={`flex items-center gap-2 my-1.5 px-2.5 py-1.5 rounded-lg border w-fit transition-colors ${borderColor}`}>
            <IconComponent className={`w-3.5 h-3.5 text-${accentColor}-400 shrink-0 ${isWebSearch && isRunning ? "animate-[orbit_3s_linear_infinite]" : ""}`} />
            <span className={`${isWebSearch ? "text-xs font-medium" : "font-mono text-xs"} text-foreground`}>{box.displayName ?? box.toolName}</span>
            {isRunning && <PulseDots color={dotColor} />}
            {box.isComplete && !hasResult && (
              <Check className="w-3.5 h-3.5 text-emerald-400 shrink-0" strokeWidth={2.5} />
            )}
          </div>
        );
      }

      return (
        <details className={`my-1.5 rounded-lg border overflow-hidden group ${borderColor}`}>
          <summary className={`cursor-pointer select-none px-2.5 py-1.5 text-xs flex items-center gap-2 hover:bg-muted/50 transition-colors list-none [&::-webkit-details-marker]:hidden ${summaryColor}`}>
            <svg className="w-3 h-3 shrink-0 transition-transform group-open:rotate-90" viewBox="0 0 24 24" fill="currentColor"><path d="M8 5l8 7-8 7z" /></svg>
            {(hasResult || box.isComplete) ? (
              box.success !== false ? <Check className="w-3.5 h-3.5 shrink-0" strokeWidth={2.5} /> : <CircleX className="w-3.5 h-3.5 shrink-0" />
            ) : (
              <IconComponent className={`w-3.5 h-3.5 shrink-0 ${isWebSearch && isRunning ? "animate-[orbit_3s_linear_infinite]" : ""}`} />
            )}
            <span className={`${isWebSearch ? "font-medium truncate" : "font-mono"} text-foreground`}>{box.displayName ?? box.toolName}</span>
            {isRunning && <PulseDots color={dotColor} />}
            {hasResult && box.durationMs !== undefined && (
              <span className="text-muted-foreground flex items-center gap-1">
                <Clock className="w-3 h-3" />
                {formatDuration(box.durationMs)}
              </span>
            )}
            {hasWebResults && (
              <span className="text-muted-foreground ml-auto shrink-0">{box.webSearchResults!.length} results</span>
            )}
          </summary>
          <div className={`border-t ${isWebSearch ? "border-cyan-500/10" : "border-border"} divide-y ${isWebSearch ? "divide-cyan-500/10" : "divide-border"}`}>
            {hasArgs && !isWebSearch && (() => {
              const parsed = tryParseJson(box.arguments!);
              return (
                <div className="px-3 py-2">
                  <span className="text-[10px] uppercase tracking-wider text-muted-foreground font-semibold">Request</span>
                  {parsed ? (
                    renderJson(parsed, { collapsed: 1, className: "mt-1 max-h-36 overflow-y-auto" })
                  ) : (
                    <pre className="mt-1 text-xs text-muted-foreground whitespace-pre-wrap break-words font-mono leading-relaxed max-h-36 overflow-y-auto">{box.arguments}</pre>
                  )}
                </div>
              );
            })()}
            {hasResult && (() => {
              const parsed = tryParseJson(box.result!);
              return (
                <div className="px-3 py-2">
                  <span className="text-[10px] uppercase tracking-wider text-muted-foreground font-semibold">Response</span>
                  {parsed ? (
                    renderJson(parsed, { collapsed: 1, className: "mt-1 max-h-48 overflow-y-auto" })
                  ) : (
                    <pre className="mt-1 text-xs text-muted-foreground whitespace-pre-wrap break-words font-mono leading-relaxed max-h-48 overflow-y-auto">{box.result}</pre>
                  )}
                </div>
              );
            })()}
            {hasWebResults && (
              <div className="px-3 py-2 space-y-1.5">
                {box.webSearchResults!.map((r, i) => (
                  <a
                    key={i}
                    href={r.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex items-center gap-2 text-xs text-cyan-400 hover:text-cyan-300 transition-colors group/link"
                  >
                    <ExternalLink className="w-3 h-3 shrink-0 opacity-50 group-hover/link:opacity-100" />
                    <span className="truncate">{r.title || r.url}</span>
                  </a>
                ))}
              </div>
            )}
          </div>
        </details>
      );
    }

    case "citation":
      return null;

    case "usage":
      return null;

    case "agent_group":
      return null;

    default:
      return null;
  }
}

/**
 * Renders an ordered list of content boxes, grouping consecutive citations
 * into chip rows appended after the preceding content box.
 */
export function BoxList({
  boxes,
  isStreaming = false,
  renderOverride,
}: {
  boxes: ContentBox[];
  isStreaming?: boolean;
  renderOverride?: (box: ContentBox, index: number) => React.ReactNode | undefined;
}) {
  const groups: Array<{ box: ContentBox; index: number; citations: CitationBox[] }> = [];

  for (let i = 0; i < boxes.length; i++) {
    const box = boxes[i];
    if (box.type === "citation") {
      if (groups.length > 0) {
        groups[groups.length - 1].citations.push(box);
      } else {
        groups.push({ box, index: i, citations: [] });
      }
    } else {
      groups.push({ box, index: i, citations: [] });
    }
  }

  return (
    <>
      {groups.map((group) => {
        const override = renderOverride?.(group.box, group.index);
        return (
          <React.Fragment key={group.index}>
            {override !== undefined
              ? override
              : group.box.type !== "citation" && (
                  <BoxRenderer box={group.box} isStreaming={isStreaming} />
                )}
            <CitationChipRow citations={group.citations} />
          </React.Fragment>
        );
      })}
    </>
  );
}
