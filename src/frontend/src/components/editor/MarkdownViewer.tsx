import { useMemo } from 'react'
import { parseMarkdown } from '@/lib/markdown'
import './markdown-editor.css'

interface MarkdownViewerProps {
  content: string
  className?: string
}

/**
 * Read-only markdown viewer that renders markdown content as formatted HTML.
 * Uses the same parsing and styling as MarkdownEditor for consistency.
 */
export function MarkdownViewer({ content, className = '' }: MarkdownViewerProps) {
  const html = useMemo(() => parseMarkdown(content || ''), [content])

  if (!content) {
    return (
      <p className="text-sm text-muted-foreground italic">No content</p>
    )
  }

  return (
    <div className="markdown-editor">
      <div
        className={`prose prose-sm dark:prose-invert max-w-none ${className}`}
        dangerouslySetInnerHTML={{ __html: html }}
      />
    </div>
  )
}
