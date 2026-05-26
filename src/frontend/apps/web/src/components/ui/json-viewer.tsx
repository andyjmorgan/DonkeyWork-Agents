import { useState } from 'react'
import { Check, Copy } from 'lucide-react'

interface JsonViewerProps {
  data: unknown
  collapsed?: number
  name?: string | false
  className?: string
}

export function JsonViewer({ data, collapsed = 2, name = false, className }: JsonViewerProps) {
  let parsedData = data
  if (typeof data === 'string') {
    try {
      parsedData = JSON.parse(data)
    } catch {
      parsedData = { value: data }
    }
  }

  if (parsedData === null || parsedData === undefined) {
    return (
      <div className={`text-sm text-muted-foreground ${className ?? ''}`}>
        No data
      </div>
    )
  }

  return (
    <div
      className={`rounded-md overflow-auto bg-muted p-3 text-xs font-mono leading-relaxed ${className ?? ''}`}
    >
      <JsonNode
        value={parsedData}
        name={typeof name === 'string' ? name : undefined}
        depth={0}
        openDepth={collapsed}
      />
    </div>
  )
}

interface JsonNodeProps {
  value: unknown
  name?: string
  depth: number
  openDepth: number
}

function JsonNode({ value, name, depth, openDepth }: JsonNodeProps) {
  const open = depth < openDepth

  if (value === null) {
    return <PrimitiveRow name={name}><span className="text-pink-400">null</span></PrimitiveRow>
  }

  if (typeof value === 'string') {
    return (
      <PrimitiveRow name={name}>
        <span className="text-emerald-400 break-all">"{value}"</span>
      </PrimitiveRow>
    )
  }

  if (typeof value === 'number') {
    return <PrimitiveRow name={name}><span className="text-amber-400">{value}</span></PrimitiveRow>
  }

  if (typeof value === 'boolean') {
    return <PrimitiveRow name={name}><span className="text-blue-400">{String(value)}</span></PrimitiveRow>
  }

  if (Array.isArray(value)) {
    return (
      <CollapsibleRow
        name={name}
        openBracket="["
        closeBracket="]"
        count={value.length}
        isEmpty={value.length === 0}
        defaultOpen={open}
        copyValue={value}
      >
        {value.map((item, i) => (
          <JsonNode key={i} value={item} name={String(i)} depth={depth + 1} openDepth={openDepth} />
        ))}
      </CollapsibleRow>
    )
  }

  // object
  const entries = Object.entries(value as Record<string, unknown>)
  return (
    <CollapsibleRow
      name={name}
      openBracket="{"
      closeBracket="}"
      count={entries.length}
      isEmpty={entries.length === 0}
      defaultOpen={open}
      copyValue={value}
    >
      {entries.map(([k, v]) => (
        <JsonNode key={k} value={v} name={k} depth={depth + 1} openDepth={openDepth} />
      ))}
    </CollapsibleRow>
  )
}

function PrimitiveRow({ name, children }: { name?: string; children: React.ReactNode }) {
  return (
    <div className="flex items-baseline gap-1 pl-4">
      {name !== undefined && (
        <span className="text-cyan-300 shrink-0">"{name}":</span>
      )}
      <span>{children}</span>
    </div>
  )
}

function CollapsibleRow({
  name,
  openBracket,
  closeBracket,
  count,
  isEmpty,
  defaultOpen,
  copyValue,
  children,
}: {
  name?: string
  openBracket: string
  closeBracket: string
  count: number
  isEmpty: boolean
  defaultOpen: boolean
  copyValue: unknown
  children: React.ReactNode
}) {
  const [copied, setCopied] = useState(false)

  const onCopy = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    try {
      navigator.clipboard.writeText(JSON.stringify(copyValue, null, 2))
      setCopied(true)
      window.setTimeout(() => setCopied(false), 1500)
    } catch {
      // ignore clipboard failures
    }
  }

  if (isEmpty) {
    return (
      <div className="flex items-baseline gap-1 pl-4">
        {name !== undefined && (
          <span className="text-cyan-300 shrink-0">"{name}":</span>
        )}
        <span className="text-muted-foreground">{openBracket}{closeBracket}</span>
      </div>
    )
  }

  return (
    <details open={defaultOpen} className="group">
      <summary
        className="cursor-pointer select-none list-none [&::-webkit-details-marker]:hidden flex items-center gap-1 hover:bg-muted-foreground/5 rounded"
      >
        <span className="inline-block w-3 text-muted-foreground text-[10px] transition-transform group-open:rotate-90">
          ▶
        </span>
        {name !== undefined && (
          <span className="text-cyan-300 shrink-0">"{name}":</span>
        )}
        <span className="text-muted-foreground">
          {openBracket} <span className="text-[10px]">{count}</span> {closeBracket}
        </span>
        <button
          type="button"
          onClick={onCopy}
          className="ml-1 text-muted-foreground/50 hover:text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity"
          aria-label="Copy"
        >
          {copied ? <Check className="w-3 h-3" /> : <Copy className="w-3 h-3" />}
        </button>
      </summary>
      <div className="border-l border-border/40 ml-1.5 pl-2">
        {children}
      </div>
      <div className="pl-4 text-muted-foreground">{closeBracket}</div>
    </details>
  )
}

