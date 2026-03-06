import { useEditor, EditorContent, ReactNodeViewRenderer } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Underline from '@tiptap/extension-underline'
import TaskList from '@tiptap/extension-task-list'
import TaskItem from '@tiptap/extension-task-item'
import Placeholder from '@tiptap/extension-placeholder'
import Image from '@tiptap/extension-image'
import CodeBlockLowlight from '@tiptap/extension-code-block-lowlight'
import { Table } from '@tiptap/extension-table'
import { TableRow } from '@tiptap/extension-table-row'
import { TableHeader } from '@tiptap/extension-table-header'
import { TableCell } from '@tiptap/extension-table-cell'
import { common, createLowlight } from 'lowlight'
import { useEffect, useRef, useState, useCallback, useMemo } from 'react'
import { parseMarkdown, serializeMarkdown } from '../lib/markdown'
import { MarkdownToolbar } from './MarkdownToolbar'
import { RawMarkdownEditor } from './RawMarkdownEditor'
import { CodeBlockWithCopy } from './CodeBlockWithCopy'
import { TableWithControls } from './TableWithControls'
import { useKeyboardShortcuts } from '../hooks/useKeyboardShortcuts'
import '../styles/markdown-editor.css'

// Create lowlight instance with common languages
const lowlight = createLowlight(common)

// Custom Table extension with controls
const CustomTable = Table.extend({
  addNodeView() {
    return ReactNodeViewRenderer(TableWithControls, {
      contentDOMElementTag: 'table',
    })
  },
})

export type ViewMode = 'preview' | 'code' | 'split'

interface MarkdownEditorProps {
  content: string
  onChange: (content: string) => void
  placeholder?: string
  className?: string
  autoFocus?: boolean
  defaultViewMode?: ViewMode
}

export function MarkdownEditor({
  content,
  onChange,
  placeholder = 'Start writing...',
  className = '',
  autoFocus = false,
  defaultViewMode = 'preview',
}: MarkdownEditorProps) {
  const [viewMode, setViewMode] = useState<ViewMode>(defaultViewMode)
  const isUpdatingFromProps = useRef(false)
  const isUpdatingFromRaw = useRef(false)
  const lastContent = useRef(content)

  const extensions = useMemo(() => [
    StarterKit.configure({
      heading: {
        levels: [1, 2, 3],
      },
      codeBlock: false, // Use CodeBlockLowlight instead
    }),
    CodeBlockLowlight.extend({
      addNodeView() {
        return ReactNodeViewRenderer(CodeBlockWithCopy)
      },
    }).configure({
      lowlight,
      defaultLanguage: 'plaintext',
    }),
    Link.configure({
      openOnClick: false,
      HTMLAttributes: {
        class: 'text-primary underline cursor-pointer',
      },
    }),
    Underline,
    TaskList,
    TaskItem.configure({
      nested: true,
    }),
    Placeholder.configure({
      placeholder,
    }),
    Image.configure({
      inline: false,
      allowBase64: true,
    }),
    CustomTable.configure({
      resizable: true,
      HTMLAttributes: {
        class: 'editor-table',
      },
    }),
    TableRow,
    TableHeader,
    TableCell,
  ], [placeholder])

  const editor = useEditor({
    extensions,
    content: parseMarkdown(content),
    editorProps: {
      attributes: {
        class: 'prose prose-sm dark:prose-invert max-w-none focus:outline-none min-h-[200px] p-4',
      },
    },
    onUpdate: ({ editor }) => {
      if (isUpdatingFromProps.current || isUpdatingFromRaw.current) return

      const html = editor.getHTML()
      const markdown = serializeMarkdown(html)

      if (markdown !== lastContent.current) {
        lastContent.current = markdown
        onChange(markdown)
      }
    },
    immediatelyRender: false,
  })

  // Handle raw markdown changes
  const handleRawChange = useCallback((markdown: string) => {
    if (!editor) return
    if (markdown === lastContent.current) return

    isUpdatingFromRaw.current = true
    lastContent.current = markdown
    onChange(markdown)

    const html = parseMarkdown(markdown)
    editor.commands.setContent(html, { emitUpdate: false })

    isUpdatingFromRaw.current = false
  }, [editor, onChange])

  // Insert markdown at the end of the content (for code mode toolbar actions)
  const handleInsertMarkdown = useCallback((markdown: string) => {
    const newContent = content + markdown
    lastContent.current = newContent
    onChange(newContent)

    // Also update TipTap so it stays in sync
    if (editor) {
      isUpdatingFromRaw.current = true
      const html = parseMarkdown(newContent)
      editor.commands.setContent(html, { emitUpdate: false })
      isUpdatingFromRaw.current = false
    }
  }, [content, onChange, editor])

  // Update editor content when props change
  useEffect(() => {
    if (!editor) return
    if (content === lastContent.current) return

    isUpdatingFromProps.current = true
    lastContent.current = content

    const html = parseMarkdown(content)
    editor.commands.setContent(html, { emitUpdate: false })

    isUpdatingFromProps.current = false
  }, [content, editor])

  // Auto focus
  useEffect(() => {
    if (autoFocus && editor) {
      editor.commands.focus('end')
    }
  }, [autoFocus, editor])

  // Keyboard shortcuts
  useKeyboardShortcuts({ editor })

  if (!editor) {
    return null
  }

  return (
    <div className={`markdown-editor flex flex-col rounded-md border border-input bg-background ${className}`}>
      <MarkdownToolbar
        editor={editor}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        onInsertMarkdown={handleInsertMarkdown}
      />

      <div className={`markdown-editor-content flex-1 min-h-0 ${viewMode === 'split' ? 'grid grid-cols-2 divide-x divide-border' : ''}`}>
        {/* Raw markdown editor */}
        {(viewMode === 'code' || viewMode === 'split') && (
          <div className="overflow-auto">
            <RawMarkdownEditor
              value={content}
              onChange={handleRawChange}
              className={viewMode === 'split' ? 'border-r border-border' : ''}
            />
          </div>
        )}

        {/* WYSIWYG preview */}
        {(viewMode === 'preview' || viewMode === 'split') && (
          <div className="overflow-auto">
            <EditorContent editor={editor} />
          </div>
        )}
      </div>
    </div>
  )
}
