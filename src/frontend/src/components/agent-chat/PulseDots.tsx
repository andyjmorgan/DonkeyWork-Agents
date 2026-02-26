/** Three bouncing dots — compact inline spinner for pills and cards. */
export function PulseDots({ color = "bg-cyan-400", size = "w-1 h-1" }: { color?: string; size?: string }) {
  return (
    <span className="inline-flex items-center gap-[3px] dot-bounce" aria-label="Loading">
      <span className={`rounded-full ${color} ${size}`} />
      <span className={`rounded-full ${color} ${size}`} />
      <span className={`rounded-full ${color} ${size}`} />
    </span>
  );
}

/** Orbital ring + dot — used as the primary "agent working" indicator in modals/headers. */
export function ActivityIndicator() {
  return (
    <span className="relative inline-flex items-center justify-center w-5 h-5">
      <span
        className="absolute inset-0 rounded-full border-2 border-cyan-500/20 border-t-cyan-400"
        style={{ animation: "orbit 1.2s linear infinite" }}
      />
      <span className="w-1.5 h-1.5 rounded-full bg-cyan-400 animate-pulse" />
    </span>
  );
}
