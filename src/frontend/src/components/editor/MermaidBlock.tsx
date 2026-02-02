import { NodeViewWrapper, NodeViewContent } from '@tiptap/react'
import type { NodeViewProps } from '@tiptap/react'
import { useEffect, useRef, useState } from 'react'
import { GitBranch, Code, Eye, AlertCircle } from 'lucide-react'
import mermaid from 'mermaid'

// Initialize mermaid with theme detection
mermaid.initialize({
  startOnLoad: false,
  theme: 'dark',
  securityLevel: 'loose',
  fontFamily: 'Inter, ui-sans-serif, system-ui, sans-serif',
})

type ViewMode = 'preview' | 'source'

export function MermaidBlock({ node }: NodeViewProps) {
  const [viewMode, setViewMode] = useState<ViewMode>('preview')
  const [svg, setSvg] = useState<string>('')
  const [error, setError] = useState<string | null>(null)
  const diagramRef = useRef<HTMLDivElement>(null)
  const idRef = useRef(`mermaid-${Math.random().toString(36).slice(2, 11)}`)

  const code = node.textContent || ''

  useEffect(() => {
    // Update mermaid theme based on document
    const isDark = document.documentElement.classList.contains('dark')
    mermaid.initialize({
      startOnLoad: false,
      theme: isDark ? 'dark' : 'default',
      securityLevel: 'loose',
      fontFamily: 'Inter, ui-sans-serif, system-ui, sans-serif',
    })
  }, [])

  useEffect(() => {
    if (!code.trim() || viewMode === 'source') {
      return
    }

    const renderDiagram = async () => {
      try {
        setError(null)
        // Check if the diagram is valid first
        const isValid = await mermaid.parse(code)
        if (isValid) {
          const { svg: renderedSvg } = await mermaid.render(idRef.current, code)
          setSvg(renderedSvg)
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to render diagram'
        setError(errorMessage)
        setSvg('')
      }
    }

    renderDiagram()
  }, [code, viewMode])

  return (
    <NodeViewWrapper className="mermaid-block-wrapper" data-type="mermaidBlock">
      <div className="mermaid-block-container">
        <div className="mermaid-block-controls">
          <span className="mermaid-block-label">
            <GitBranch className="w-3.5 h-3.5" />
            Mermaid Diagram
          </span>
          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => setViewMode('preview')}
              className={`code-block-copy-button ${viewMode === 'preview' ? 'copied' : ''}`}
              title="Preview diagram"
            >
              <Eye className="w-4 h-4" />
            </button>
            <button
              type="button"
              onClick={() => setViewMode('source')}
              className={`code-block-copy-button ${viewMode === 'source' ? 'copied' : ''}`}
              title="Edit source"
            >
              <Code className="w-4 h-4" />
            </button>
          </div>
        </div>

        {viewMode === 'preview' ? (
          <>
            {error ? (
              <div className="mermaid-error flex items-start gap-2">
                <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" />
                <span>{error}</span>
              </div>
            ) : svg ? (
              <div
                ref={diagramRef}
                className="mermaid-diagram"
                dangerouslySetInnerHTML={{ __html: svg }}
              />
            ) : (
              <div className="mermaid-diagram text-muted-foreground text-sm">
                Loading diagram...
              </div>
            )}
          </>
        ) : (
          <pre className="mermaid-source">
            <code>
              <NodeViewContent />
            </code>
          </pre>
        )}
      </div>
    </NodeViewWrapper>
  )
}
