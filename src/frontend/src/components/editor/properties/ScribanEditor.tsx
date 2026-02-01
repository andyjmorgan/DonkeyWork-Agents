import { useRef, useCallback, useMemo, useEffect } from 'react'
import Editor, { type Monaco } from '@monaco-editor/react'
import { useEditorStore, type StartNodeConfig } from '@/store/editor'
import { useThemeStore } from '@/store/theme'
import type { editor, languages, Position } from 'monaco-editor'

interface ScribanEditorProps {
  /** Node ID - required for internal predecessor lookup if predecessors not provided */
  nodeId?: string
  value: string
  onChange: (value: string) => void
  height?: string
  placeholder?: string
  className?: string
  /** Optional predecessors - if provided, overrides internal lookup */
  predecessors?: Array<{ nodeId: string; nodeName: string; nodeType: string }>
}

// Scriban language definition for Monaco
const SCRIBAN_LANGUAGE_ID = 'scriban'

// Output properties for each node type
const NODE_OUTPUT_PROPERTIES: Record<string, string[]> = {
  start: [],
  model: ['ResponseText', 'TotalTokens', 'InputTokens', 'OutputTokens'],
  action: ['Result', 'ActionType'],
  messageFormatter: ['FormattedMessage'],
  end: []
}

function registerScribanLanguage(monaco: Monaco) {
  // Check if already registered
  if (monaco.languages.getLanguages().some((lang: languages.ILanguageExtensionPoint) => lang.id === SCRIBAN_LANGUAGE_ID)) {
    return
  }

  // Register the language
  monaco.languages.register({ id: SCRIBAN_LANGUAGE_ID })

  // Define tokens
  monaco.languages.setMonarchTokensProvider(SCRIBAN_LANGUAGE_ID, {
    tokenizer: {
      root: [
        // Scriban expressions {{ ... }}
        [/\{\{/, { token: 'delimiter.bracket', next: '@scribanExpression' }],
        // Plain text
        [/[^{]+/, 'string'],
        [/\{/, 'string']
      ],
      scribanExpression: [
        [/\}\}/, { token: 'delimiter.bracket', next: '@root' }],
        // Keywords
        [/\b(if|else|elseif|end|for|in|while|break|continue|func|ret|capture|readonly|import|with|wrap|include|raw)\b/, 'keyword'],
        // Built-in variables
        [/\b(input|steps|executionId|userId|Variables|execution_id|user_id)\b/, 'variable.predefined'],
        // Operators
        [/[=!<>]=?|&&|\|\||[+\-*\/%]/, 'operator'],
        // Numbers
        [/\d+(\.\d+)?/, 'number'],
        // Strings
        [/"[^"]*"/, 'string'],
        [/'[^']*'/, 'string'],
        // Identifiers (variable/property names)
        [/[a-zA-Z_]\w*/, 'identifier'],
        // Dots for property access
        [/\./, 'delimiter'],
        // Pipes
        [/\|/, 'delimiter'],
        // Whitespace
        [/\s+/, 'white']
      ]
    }
  })

  // Define language configuration for brackets and auto-closing
  monaco.languages.setLanguageConfiguration(SCRIBAN_LANGUAGE_ID, {
    brackets: [
      ['{{', '}}'],
      ['(', ')'],
      ['[', ']']
    ],
    autoClosingPairs: [
      { open: '{{', close: '}}' },
      { open: '"', close: '"' },
      { open: "'", close: "'" },
      { open: '(', close: ')' },
      { open: '[', close: ']' }
    ],
    surroundingPairs: [
      { open: '{{', close: '}}' },
      { open: '"', close: '"' },
      { open: "'", close: "'" }
    ]
  })
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
  const { theme } = useThemeStore()

  const monacoRef = useRef<Monaco | null>(null)
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null)
  const completionDisposableRef = useRef<{ dispose: () => void } | null>(null)

  // Get reachable predecessors for autocomplete, excluding start node
  // Use prop if provided, otherwise get from store via nodeId
  const allPredecessors = predecessorsProp ?? (nodeId ? getReachablePredecessors(nodeId) : [])
  const predecessors = useMemo(() =>
    allPredecessors.filter(p => p.nodeType !== 'start'),
    [allPredecessors]
  )

  // Get input schema properties from the start node
  const inputProperties = useMemo(() => {
    const startNode = nodes.find(n => n.type === 'start')
    if (!startNode) return []

    const startConfig = nodeConfigurations[startNode.id] as StartNodeConfig | undefined
    if (!startConfig?.inputSchema?.properties) return []

    return Object.keys(startConfig.inputSchema.properties)
  }, [nodes, nodeConfigurations])

  // Build completion items based on the current path context
  const buildCompletionItems = useCallback((monaco: Monaco, currentPath: string): { items: any[], hasCompletions: boolean } => {
    const items: any[] = []
    const pathParts = currentPath.split('.').filter(p => p.length > 0)
    const pathLower = pathParts.map(p => p.toLowerCase())

    // Root level - no path yet
    if (pathParts.length === 0) {
      items.push({
        label: 'Input',
        kind: monaco.languages.CompletionItemKind.Variable,
        insertText: 'Input',
        detail: 'Input provided to the workflow',
        documentation: 'Access the input object passed to this workflow execution'
      })
      items.push({
        label: 'ExecutionId',
        kind: monaco.languages.CompletionItemKind.Variable,
        insertText: 'ExecutionId',
        detail: 'Current execution ID',
        documentation: 'The unique identifier for this execution'
      })
      items.push({
        label: 'UserId',
        kind: monaco.languages.CompletionItemKind.Variable,
        insertText: 'UserId',
        detail: 'Current user ID',
        documentation: 'The ID of the user running this execution'
      })

      // Add Steps if there are predecessors
      if (predecessors.length > 0) {
        items.push({
          label: 'Steps',
          kind: monaco.languages.CompletionItemKind.Module,
          insertText: 'Steps',
          detail: 'Access outputs from previous steps',
          documentation: 'Contains the outputs from all previous workflow steps'
        })
      }
    }
    // After "Input" - show input schema properties
    else if (pathLower.length === 1 && pathLower[0] === 'input') {
      for (const prop of inputProperties) {
        items.push({
          label: prop,
          kind: monaco.languages.CompletionItemKind.Property,
          insertText: prop,
          detail: `Input property: ${prop}`,
          documentation: `Access the "${prop}" property from the workflow input`
        })
      }
    }
    // After "Steps" - show predecessor node names
    else if (pathLower.length === 1 && pathLower[0] === 'steps') {
      for (const pred of predecessors) {
        items.push({
          label: pred.nodeName,
          kind: monaco.languages.CompletionItemKind.Class,
          insertText: pred.nodeName,
          detail: `${pred.nodeType} node`,
          documentation: `Access outputs from the "${pred.nodeName}" step`
        })
      }
    }
    // After "Steps.<nodeName>" - show output properties for that node type
    else if (pathLower.length === 2 && pathLower[0] === 'steps') {
      const nodeName = pathParts[1]
      const pred = predecessors.find(p => p.nodeName.toLowerCase() === nodeName.toLowerCase())
      if (pred) {
        const outputProps = NODE_OUTPUT_PROPERTIES[pred.nodeType] || []
        for (const prop of outputProps) {
          items.push({
            label: prop,
            kind: monaco.languages.CompletionItemKind.Property,
            insertText: prop,
            detail: `Output property`,
            documentation: `The ${prop} output from this step`
          })
        }
      }
    }

    return { items, hasCompletions: items.length > 0 }
  }, [predecessors, inputProperties])

  // Setup completion provider
  const setupCompletionProvider = useCallback((monaco: Monaco) => {
    // Dispose previous provider if exists
    if (completionDisposableRef.current) {
      completionDisposableRef.current.dispose()
    }

    completionDisposableRef.current = monaco.languages.registerCompletionItemProvider(SCRIBAN_LANGUAGE_ID, {
      triggerCharacters: ['.', '{'],
      provideCompletionItems: (model: editor.ITextModel, position: Position) => {
        const textUntilPosition = model.getValueInRange({
          startLineNumber: position.lineNumber,
          startColumn: 1,
          endLineNumber: position.lineNumber,
          endColumn: position.column
        })

        // Find the last {{ before cursor
        const lastOpenBrace = textUntilPosition.lastIndexOf('{{')
        if (lastOpenBrace === -1) {
          return { suggestions: [] }
        }

        // Check if we're inside an unclosed {{ }}
        const afterBrace = textUntilPosition.substring(lastOpenBrace + 2)
        if (afterBrace.includes('}}')) {
          return { suggestions: [] }
        }

        // Get the current path (e.g., "Steps.MyNode" or "Input")
        const pathMatch = afterBrace.match(/^\s*([\w.]*?)\.?$/)
        const currentPath = pathMatch ? pathMatch[1] : ''

        const { items } = buildCompletionItems(monaco, currentPath)

        // Calculate the range for replacement
        const wordInfo = model.getWordUntilPosition(position)
        const range = {
          startLineNumber: position.lineNumber,
          endLineNumber: position.lineNumber,
          startColumn: wordInfo.startColumn,
          endColumn: wordInfo.endColumn
        }

        return {
          suggestions: items.map(item => ({
            ...item,
            range
          }))
        }
      }
    })
  }, [buildCompletionItems])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (completionDisposableRef.current) {
        completionDisposableRef.current.dispose()
      }
    }
  }, [])

  const handleEditorDidMount = useCallback((editor: editor.IStandaloneCodeEditor, monaco: Monaco) => {
    monacoRef.current = monaco
    editorRef.current = editor

    registerScribanLanguage(monaco)
    setupCompletionProvider(monaco)

    // Disable find widget (Ctrl+F / Cmd+F)
    editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyF, () => {
      // Do nothing - prevents find dialog
    })
  }, [setupCompletionProvider])

  // Re-setup completion provider when predecessors change
  useEffect(() => {
    if (monacoRef.current) {
      setupCompletionProvider(monacoRef.current)
    }
  }, [predecessors, inputProperties, setupCompletionProvider])

  return (
    <div className={`rounded-lg border border-border overflow-hidden ${className ?? ''}`}>
      <Editor
        height={height}
        language={SCRIBAN_LANGUAGE_ID}
        theme={theme === 'dark' ? 'vs-dark' : 'light'}
        value={value}
        onChange={(v) => onChange(v ?? '')}
        onMount={handleEditorDidMount}
        options={{
          minimap: { enabled: false },
          fontSize: 13,
          lineNumbers: 'off',
          glyphMargin: false,
          folding: false,
          lineDecorationsWidth: 0,
          lineNumbersMinChars: 0,
          scrollBeyondLastLine: false,
          wordWrap: 'on',
          wrappingIndent: 'indent',
          formatOnPaste: true,
          formatOnType: true,
          suggestOnTriggerCharacters: true,
          quickSuggestions: true,
          tabSize: 2,
          placeholder: placeholder,
          find: {
            addExtraSpaceOnTop: false,
            autoFindInSelection: 'never',
            seedSearchStringFromSelection: 'never',
          },
        }}
      />
    </div>
  )
}
