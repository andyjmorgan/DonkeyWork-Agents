import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { ExternalLink } from "lucide-react";
import type { CitationBox } from "@/types/agent-chat";

function getDomain(url: string): string {
  try {
    return new URL(url).hostname.replace(/^www\./, "");
  } catch {
    return url;
  }
}

function getFaviconUrl(url: string): string {
  try {
    const { hostname } = new URL(url);
    return `https://www.google.com/s2/favicons?domain=${encodeURIComponent(hostname)}&sz=32`;
  } catch {
    return "";
  }
}

export function CitationChip({ citation }: { citation: CitationBox }) {
  const [open, setOpen] = useState(false);
  const domain = getDomain(citation.url);
  const favicon = getFaviconUrl(citation.url);

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full border border-border bg-muted/50 hover:bg-muted hover:border-cyan-500/30 transition-all text-xs text-muted-foreground hover:text-foreground cursor-pointer"
      >
        {favicon && (
          <img
            src={favicon}
            alt=""
            className="w-3.5 h-3.5 rounded-sm"
            onError={(e) => { (e.target as HTMLImageElement).style.display = "none"; }}
          />
        )}
        <span className="truncate max-w-[120px]">{domain}</span>
      </button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="sm:max-w-lg bg-card border-border overflow-hidden">
          <DialogHeader className="min-w-0">
            <DialogTitle className="text-sm font-medium text-foreground flex items-center gap-2 min-w-0">
              {favicon && <img src={favicon} alt="" className="w-4 h-4 rounded-sm shrink-0" />}
              <span className="truncate min-w-0">{citation.title || domain}</span>
            </DialogTitle>
            <DialogDescription className="sr-only">Citation details</DialogDescription>
          </DialogHeader>
          <div className="space-y-3 min-w-0 overflow-hidden">
            <a
              href={citation.url}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-start gap-1.5 text-xs text-cyan-400 hover:text-cyan-300 transition-colors min-w-0"
            >
              <ExternalLink className="w-3 h-3 shrink-0 mt-0.5" />
              <span className="break-all min-w-0">{citation.url}</span>
            </a>
            {citation.citedText && (
              <div className="rounded-lg border border-border bg-muted/30 px-3 py-2.5 overflow-hidden">
                <p className="text-sm text-muted-foreground leading-relaxed whitespace-pre-wrap break-words">
                  {citation.citedText}
                </p>
              </div>
            )}
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}

export function CitationChipRow({ citations }: { citations: CitationBox[] }) {
  if (citations.length === 0) return null;
  return (
    <div className="flex flex-wrap items-center gap-1.5 mt-0.5 mb-1.5">
      {citations.map((c, i) => (
        <CitationChip key={i} citation={c} />
      ))}
    </div>
  );
}
