import { useEffect, useCallback } from 'react'
import { Button } from '@donkeywork/ui'
import { Save, Undo2, Loader2 } from 'lucide-react'
import { MarkdownEditor } from '@donkeywork/editor'
import Editor from '@monaco-editor/react'

interface SkillFileEditorProps {
  path: string
  content: string
  onChange: (content: string) => void
  onSave: () => void
  onDiscard: () => void
  saving: boolean
  isDirty: boolean
}

function getLanguageFromPath(path: string): string {
  const ext = path.split('.').pop()?.toLowerCase()
  switch (ext) {
    case 'js': return 'javascript'
    case 'ts': return 'typescript'
    case 'jsx': return 'javascript'
    case 'tsx': return 'typescript'
    case 'json': return 'json'
    case 'html': return 'html'
    case 'css': return 'css'
    case 'py': return 'python'
    case 'sh': case 'bash': return 'shell'
    case 'yaml': case 'yml': return 'yaml'
    case 'xml': return 'xml'
    case 'sql': return 'sql'
    case 'cs': return 'csharp'
    case 'go': return 'go'
    case 'rs': return 'rust'
    case 'rb': return 'ruby'
    case 'java': return 'java'
    case 'toml': return 'ini'
    default: return 'plaintext'
  }
}

function isMarkdownFile(path: string): boolean {
  const ext = path.split('.').pop()?.toLowerCase()
  return ext === 'md' || ext === 'markdown'
}

export function SkillFileEditor({ path, content, onChange, onSave, onDiscard, saving, isDirty }: SkillFileEditorProps) {
  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 's') {
      e.preventDefault()
      if (isDirty && !saving) {
        onSave()
      }
    }
  }, [isDirty, saving, onSave])

  useEffect(() => {
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [handleKeyDown])

  const fileName = path.split('/').pop() || path

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-2 px-4 py-2 border-b border-border shrink-0">
        <div className="flex-1 min-w-0">
          <span className="text-sm font-medium truncate block">{fileName}</span>
          <span className="text-xs text-muted-foreground truncate block">{path}</span>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <Button
            variant="ghost"
            size="sm"
            onClick={onDiscard}
            disabled={!isDirty}
            title="Discard changes"
          >
            <Undo2 className="h-4 w-4 mr-1" />
            Discard
          </Button>
          <Button
            size="sm"
            onClick={onSave}
            disabled={!isDirty || saving}
          >
            {saving ? (
              <Loader2 className="h-4 w-4 mr-1 animate-spin" />
            ) : (
              <Save className="h-4 w-4 mr-1" />
            )}
            Save
          </Button>
        </div>
      </div>
      <div className="flex-1 overflow-hidden">
        {isMarkdownFile(path) ? (
          <div className="h-full overflow-y-auto p-4">
            <MarkdownEditor
              content={content}
              onChange={onChange}
            />
          </div>
        ) : (
          <Editor
            height="100%"
            language={getLanguageFromPath(path)}
            value={content}
            onChange={(val: string | undefined) => onChange(val ?? '')}
            theme="vs-dark"
            options={{
              minimap: { enabled: false },
              scrollBeyondLastLine: false,
              fontSize: 13,
              wordWrap: 'on',
              lineNumbers: 'on',
              padding: { top: 12 },
            }}
          />
        )}
      </div>
    </div>
  )
}
