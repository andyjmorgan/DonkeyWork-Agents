import CodeMirror from '@uiw/react-codemirror'
import { markdown } from '@codemirror/lang-markdown'
import { EditorView } from '@codemirror/view'
import { useThemeStore } from '@donkeywork/stores'

interface RawMarkdownEditorProps {
  value: string
  onChange: (value: string) => void
  className?: string
}

// Custom theme for dark mode
const darkTheme = EditorView.theme({
  '&': {
    backgroundColor: 'hsl(var(--background))',
    color: 'hsl(var(--foreground))',
  },
  '.cm-content': {
    caretColor: 'hsl(var(--foreground))',
    fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
    fontSize: '14px',
    lineHeight: '1.6',
    padding: '16px',
  },
  '.cm-cursor': {
    borderLeftColor: 'hsl(var(--foreground))',
  },
  '.cm-activeLine': {
    backgroundColor: 'hsl(var(--muted) / 0.3)',
  },
  '.cm-selectionBackground': {
    backgroundColor: 'hsl(var(--primary) / 0.2) !important',
  },
  '.cm-gutters': {
    backgroundColor: 'hsl(var(--muted))',
    color: 'hsl(var(--muted-foreground))',
    border: 'none',
  },
  '.cm-lineNumbers .cm-gutterElement': {
    padding: '0 8px',
  },
}, { dark: true })

// Custom theme for light mode
const lightTheme = EditorView.theme({
  '&': {
    backgroundColor: 'hsl(var(--background))',
    color: 'hsl(var(--foreground))',
  },
  '.cm-content': {
    caretColor: 'hsl(var(--foreground))',
    fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
    fontSize: '14px',
    lineHeight: '1.6',
    padding: '16px',
  },
  '.cm-cursor': {
    borderLeftColor: 'hsl(var(--foreground))',
  },
  '.cm-activeLine': {
    backgroundColor: 'hsl(var(--muted) / 0.3)',
  },
  '.cm-selectionBackground': {
    backgroundColor: 'hsl(var(--primary) / 0.2) !important',
  },
  '.cm-gutters': {
    backgroundColor: 'hsl(var(--muted))',
    color: 'hsl(var(--muted-foreground))',
    border: 'none',
  },
  '.cm-lineNumbers .cm-gutterElement': {
    padding: '0 8px',
  },
}, { dark: false })

export function RawMarkdownEditor({ value, onChange, className = '' }: RawMarkdownEditorProps) {
  const { theme } = useThemeStore()
  const isDark = theme === 'dark'

  return (
    <div className={`raw-markdown-editor h-full ${className}`}>
      <CodeMirror
        value={value}
        onChange={onChange}
        extensions={[
          markdown(),
          EditorView.lineWrapping,
        ]}
        theme={isDark ? darkTheme : lightTheme}
        basicSetup={{
          lineNumbers: true,
          foldGutter: true,
          highlightActiveLine: true,
          highlightSelectionMatches: true,
          bracketMatching: true,
          autocompletion: true,
          searchKeymap: true,
        }}
        className="min-h-[200px] h-full"
      />
    </div>
  )
}
