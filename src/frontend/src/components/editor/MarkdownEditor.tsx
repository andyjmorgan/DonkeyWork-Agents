import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Underline from '@tiptap/extension-underline'
import TaskList from '@tiptap/extension-task-list'
import TaskItem from '@tiptap/extension-task-item'
import Placeholder from '@tiptap/extension-placeholder'
import { useEffect, useRef } from 'react'
import { parseMarkdown, serializeMarkdown } from '@/lib/markdown'
import { MarkdownToolbar } from './MarkdownToolbar'
import './markdown-editor.css'

interface MarkdownEditorProps {
  content: string
  onChange: (content: string) => void
  placeholder?: string
  className?: string
  autoFocus?: boolean
}

export function MarkdownEditor({
  content,
  onChange,
  placeholder = 'Start writing...',
  className = '',
  autoFocus = false,
}: MarkdownEditorProps) {
  const isUpdatingFromProps = useRef(false)
  const lastContent = useRef(content)

  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: {
          levels: [1, 2, 3],
        },
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
    ],
    content: parseMarkdown(content),
    editorProps: {
      attributes: {
        class: 'prose prose-sm dark:prose-invert max-w-none focus:outline-none min-h-[200px] p-4',
      },
    },
    onUpdate: ({ editor }) => {
      if (isUpdatingFromProps.current) return

      const html = editor.getHTML()
      const markdown = serializeMarkdown(html)

      if (markdown !== lastContent.current) {
        lastContent.current = markdown
        onChange(markdown)
      }
    },
    immediatelyRender: false,
  })

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

  if (!editor) {
    return null
  }

  return (
    <div className={`markdown-editor rounded-md border border-input bg-background ${className}`}>
      <MarkdownToolbar editor={editor} />
      <EditorContent editor={editor} />
    </div>
  )
}
