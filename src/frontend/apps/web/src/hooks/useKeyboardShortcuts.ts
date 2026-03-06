import { useEffect } from 'react'
import { Editor } from '@tiptap/react'

interface UseKeyboardShortcutsProps {
  editor: Editor | null
}

export function useKeyboardShortcuts({ editor }: UseKeyboardShortcutsProps) {
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      // Use Cmd on Mac, Ctrl on Windows/Linux
      const modifier = event.metaKey || event.ctrlKey

      if (!modifier) return

      // If no editor, return
      if (!editor) return

      // Cmd/Ctrl + K - Insert link
      if (event.key === 'k' || event.key === 'K') {
        event.preventDefault()
        const previousUrl = editor.getAttributes('link').href
        const url = window.prompt('Enter URL:', previousUrl)

        if (url === null) return
        if (url === '') {
          editor.chain().focus().extendMarkRange('link').unsetLink().run()
          return
        }
        editor.chain().focus().extendMarkRange('link').setLink({ href: url }).run()
        return
      }

      // Cmd/Ctrl + 1/2/3 - Headings
      if (event.key === '1') {
        event.preventDefault()
        editor.chain().focus().toggleHeading({ level: 1 }).run()
        return
      }
      if (event.key === '2') {
        event.preventDefault()
        editor.chain().focus().toggleHeading({ level: 2 }).run()
        return
      }
      if (event.key === '3') {
        event.preventDefault()
        editor.chain().focus().toggleHeading({ level: 3 }).run()
        return
      }

      // Cmd/Ctrl + Shift + L - Toggle bullet list
      if (event.shiftKey && (event.key === 'l' || event.key === 'L')) {
        event.preventDefault()
        editor.chain().focus().toggleBulletList().run()
        return
      }

      // Cmd/Ctrl + Shift + O - Toggle ordered list
      if (event.shiftKey && (event.key === 'o' || event.key === 'O')) {
        event.preventDefault()
        editor.chain().focus().toggleOrderedList().run()
        return
      }

      // Cmd/Ctrl + Shift + T - Toggle task list
      if (event.shiftKey && (event.key === 't' || event.key === 'T')) {
        event.preventDefault()
        editor.chain().focus().toggleTaskList().run()
        return
      }

      // Cmd/Ctrl + E - Toggle code block
      if (event.key === 'e' || event.key === 'E') {
        event.preventDefault()
        editor.chain().focus().toggleCodeBlock().run()
        return
      }

      // Cmd/Ctrl + Shift + Q - Toggle blockquote
      if (event.shiftKey && (event.key === 'q' || event.key === 'Q')) {
        event.preventDefault()
        editor.chain().focus().toggleBlockquote().run()
        return
      }
    }

    window.addEventListener('keydown', handleKeyDown)

    return () => {
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [editor])
}
