import React, { useRef, useCallback, useMemo, useState, useLayoutEffect } from 'react'
import { useEditorStore, type NodeConfig } from '@/store/editor'
import { buildSuggestions, extractPathFromText, getOutputProperties, type SuggestionItem, type Predecessor } from './scribanSuggestions'

interface ScribanEditorProps {
  nodeId?: string
  value: string
  onChange: (value: string) => void
  height?: string
  placeholder?: string
  className?: string
  predecessors?: Predecessor[]
}

export function ScribanEditor({
  nodeId,
  value,
  onChange,
  height = '200px',
  placeholder,
  className,
  predecessors: predecessorsProp,
}: ScribanEditorProps) {
  const getReachablePredecessors = useEditorStore((state) => state.getReachablePredecessors)
  const nodes = useEditorStore((state) => state.nodes)
  const nodeConfigurations = useEditorStore((state) => state.nodeConfigurations)

  const textareaRef = useRef<HTMLTextAreaElement>(null)
  const pendingCursorRef = useRef<number | null>(null)
  const [showSuggestions, setShowSuggestions] = useState(false)
  const [suggestions, setSuggestions] = useState<SuggestionItem[]>([])
  const [selectedIndex, setSelectedIndex] = useState(0)

  const predecessors = useMemo(() => {
    const all = predecessorsProp ?? (nodeId ? getReachablePredecessors(nodeId) : [])
    const filtered = all.filter(p => p.nodeType.toLowerCase() !== 'start')
    console.log('[ScribanEditor] predecessors:', filtered.map(p => ({ name: p.nodeName, type: p.nodeType, outputs: getOutputProperties(p.nodeType) })))
    return filtered
  }, [predecessorsProp, nodeId, getReachablePredecessors])

  const inputProperties = useMemo(() => {
    const startNode = nodes.find(n => n.data?.nodeType === 'Start')
    if (!startNode) return []
    const startConfig = nodeConfigurations[startNode.id] as NodeConfig | undefined
    const inputSchema = startConfig?.inputSchema as { properties?: Record<string, unknown> } | undefined
    if (!inputSchema?.properties) return []
    return Object.keys(inputSchema.properties)
  }, [nodes, nodeConfigurations])

  const getSuggestions = useCallback((currentPath: string): SuggestionItem[] => {
    return buildSuggestions(currentPath, predecessors, inputProperties)
  }, [predecessors, inputProperties])

  const checkForSuggestions = useCallback(() => {
    const textarea = textareaRef.current
    if (!textarea) return

    if (pendingCursorRef.current !== null) {
      const pos = pendingCursorRef.current
      textarea.selectionStart = pos
      textarea.selectionEnd = pos
      pendingCursorRef.current = null
    }

    const path = extractPathFromText(value, textarea.selectionStart)
    if (path === null) {
      setShowSuggestions(false)
      return
    }

    const items = getSuggestions(path)
    if (items.length > 0) {
      setSuggestions(items)
      setSelectedIndex(0)
      setShowSuggestions(true)
    } else {
      setShowSuggestions(false)
    }
  }, [value, getSuggestions])

  const insertSuggestion = useCallback((item: SuggestionItem) => {
    const textarea = textareaRef.current
    if (!textarea) return

    const pos = textarea.selectionStart
    const text = value.substring(0, pos)
    const after = value.substring(pos)
    const lastBrace = text.lastIndexOf('{{')
    const afterBrace = text.substring(lastBrace + 2)
    const match = afterBrace.match(/^\s*([\w.]*)$/)

    if (match) {
      const path = match[1]
      const lastDot = path.lastIndexOf('.') + 1
      const replaceFrom = lastBrace + 2 + afterBrace.indexOf(path) + lastDot
      const textToInsert = item.hasChildren ? item.insertText + '.' : item.insertText
      const newValue = value.substring(0, replaceFrom) + textToInsert + after

      pendingCursorRef.current = replaceFrom + textToInsert.length
      onChange(newValue)
    }
    setShowSuggestions(false)
  }, [value, onChange])

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (!showSuggestions) return

    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setSelectedIndex(i => Math.min(i + 1, suggestions.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setSelectedIndex(i => Math.max(i - 1, 0))
    } else if (e.key === 'Enter' || e.key === 'Tab') {
      e.preventDefault()
      if (suggestions[selectedIndex]) insertSuggestion(suggestions[selectedIndex])
    } else if (e.key === 'Escape') {
      setShowSuggestions(false)
    }
  }, [showSuggestions, suggestions, selectedIndex, insertSuggestion])

  useLayoutEffect(() => {
    checkForSuggestions()
  }, [value, checkForSuggestions])

  const renderHighlightedText = useMemo(() => {
    if (!value) return null

    const parts: React.ReactNode[] = []
    let lastIndex = 0
    const regex = /\{\{[\s\S]*?\}\}/g
    let match

    while ((match = regex.exec(value)) !== null) {
      if (match.index > lastIndex) {
        parts.push(
          <span key={`text-${lastIndex}`} className="text-foreground">
            {value.slice(lastIndex, match.index)}
          </span>
        )
      }
      parts.push(
        <span key={`expr-${match.index}`} className="text-cyan-400">
          {match[0]}
        </span>
      )
      lastIndex = match.index + match[0].length
    }

    if (lastIndex < value.length) {
      parts.push(
        <span key={`text-${lastIndex}`} className="text-foreground">
          {value.slice(lastIndex)}
        </span>
      )
    }

    return parts
  }, [value])

  return (
    <div className={`relative rounded-lg border border-border bg-background ${className ?? ''}`}>
      {/* Highlighted text layer */}
      <div
        className="absolute inset-0 p-3 font-mono text-sm whitespace-pre-wrap break-words pointer-events-none select-none overflow-hidden leading-[1.5]"
        aria-hidden="true"
      >
        {renderHighlightedText}
      </div>
      {/* Textarea with transparent text */}
      <textarea
        ref={textareaRef}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        className="relative w-full bg-transparent p-3 font-mono text-sm resize-none focus:outline-none leading-[1.5]"
        style={{
          height,
          minHeight: height,
          color: 'transparent',
          caretColor: 'hsl(var(--foreground))',
          WebkitTextFillColor: 'transparent',
        }}
        spellCheck={false}
      />
      {showSuggestions && suggestions.length > 0 && (
        <div className="absolute z-50 left-3 top-10 bg-popover border border-border rounded-md shadow-lg min-w-48 max-w-72 overflow-hidden">
          {suggestions.map((item, i) => (
            <div
              key={item.label}
              className={`px-3 py-1.5 cursor-pointer text-sm flex justify-between gap-4 ${i === selectedIndex ? 'bg-muted' : 'hover:bg-muted/50'}`}
              onClick={() => insertSuggestion(item)}
              onMouseEnter={() => setSelectedIndex(i)}
            >
              <span className="font-medium">{item.label}</span>
              <span className="text-xs text-muted-foreground">{item.detail}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
