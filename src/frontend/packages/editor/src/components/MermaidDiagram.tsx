import { useEffect, useRef, useState } from 'react'
import mermaid from 'mermaid'

interface MermaidDiagramProps {
  code: string
  className?: string
}

mermaid.initialize({
  startOnLoad: false,
  theme: 'neutral',
  securityLevel: 'loose',
})

export function MermaidDiagram({ code, className = '' }: MermaidDiagramProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const [svg, setSvg] = useState<string>('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const renderDiagram = async () => {
      if (!code.trim()) {
        setSvg('')
        setError(null)
        return
      }

      try {
        const id = `mermaid-${Math.random().toString(36).substr(2, 9)}`
        const { svg } = await mermaid.render(id, code)
        setSvg(svg)
        setError(null)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to render diagram')
        setSvg('')
      }
    }

    renderDiagram()
  }, [code])

  if (error) {
    return (
      <div className={`p-4 rounded-md bg-destructive/10 text-destructive text-sm ${className}`}>
        <p className="font-medium">Mermaid Error</p>
        <p className="text-xs mt-1 font-mono">{error}</p>
      </div>
    )
  }

  if (!svg) {
    return (
      <div className={`p-4 rounded-md bg-muted text-muted-foreground text-sm ${className}`}>
        Enter mermaid code to see diagram...
      </div>
    )
  }

  return (
    <div
      ref={containerRef}
      className={`flex justify-center p-4 bg-background rounded-md overflow-auto ${className}`}
      dangerouslySetInnerHTML={{ __html: svg }}
    />
  )
}
