const FALLBACK_OUTPUT_PROPERTIES: Record<string, string[]> = {
  Start: [],
  End: [],
  Model: ['ResponseText', 'TotalTokens', 'InputTokens', 'OutputTokens'],
  MultimodalChatModel: ['ResponseText', 'TotalTokens', 'InputTokens', 'OutputTokens'],
  Action: ['Result', 'ActionType'],
  MessageFormatter: ['FormattedMessage'],
  HttpRequest: ['StatusCode', 'Body', 'Headers', 'IsSuccess'],
  Sleep: ['DurationSeconds'],
  TextToSpeech: ['AudioBase64', 'ContentType', 'FileExtension', 'SizeBytes', 'Transcript', 'Voice', 'Model'],
  StoreAudio: ['RecordingId', 'Name', 'Description', 'FilePath', 'Transcript'],
}

export interface SuggestionItem {
  label: string
  detail: string
  insertText: string
  hasChildren: boolean
}

export interface Predecessor {
  nodeId: string
  nodeName: string
  nodeType: string
}

export function getOutputProperties(
  nodeType: string,
  backendOutputProperties?: Record<string, string[]>
): string[] {
  if (backendOutputProperties) {
    const backendMatch = Object.keys(backendOutputProperties).find(
      k => k.toLowerCase() === nodeType.toLowerCase()
    )
    if (backendMatch) return backendOutputProperties[backendMatch]
  }

  const fallbackMatch = Object.keys(FALLBACK_OUTPUT_PROPERTIES).find(
    k => k.toLowerCase() === nodeType.toLowerCase()
  )
  return fallbackMatch ? FALLBACK_OUTPUT_PROPERTIES[fallbackMatch] : []
}

export function buildSuggestions(
  currentPath: string,
  predecessors: Predecessor[],
  inputProperties: string[],
  backendOutputProperties?: Record<string, string[]>
): SuggestionItem[] {
  const endsWithDot = currentPath.endsWith('.')
  const segments = currentPath.split('.').filter(p => p.length > 0)

  const filterText = endsWithDot ? '' : (segments[segments.length - 1] ?? '')
  const resolved = endsWithDot ? segments : segments.slice(0, -1)
  const resolvedLower = resolved.map(p => p.toLowerCase())

  let items: SuggestionItem[] = []

  if (resolvedLower.length === 0) {
    items.push({ label: 'Input', detail: 'Workflow input', insertText: 'Input', hasChildren: inputProperties.length > 0 })
    items.push({ label: 'ExecutionId', detail: 'Execution ID', insertText: 'ExecutionId', hasChildren: false })
    items.push({ label: 'UserId', detail: 'User ID', insertText: 'UserId', hasChildren: false })
    if (predecessors.length > 0) {
      items.push({ label: 'Steps', detail: 'Previous steps', insertText: 'Steps', hasChildren: true })
    }
  } else if (resolvedLower.length === 1 && resolvedLower[0] === 'input') {
    inputProperties.forEach(prop => {
      items.push({ label: prop, detail: 'Input property', insertText: prop, hasChildren: false })
    })
  } else if (resolvedLower.length === 1 && resolvedLower[0] === 'steps') {
    predecessors.forEach(pred => {
      const outputs = getOutputProperties(pred.nodeType, backendOutputProperties)
      items.push({ label: pred.nodeName, detail: pred.nodeType, insertText: pred.nodeName, hasChildren: outputs.length > 0 })
    })
  } else if (resolvedLower.length === 2 && resolvedLower[0] === 'steps') {
    const pred = predecessors.find(p => p.nodeName.toLowerCase() === resolvedLower[1])
    if (pred) {
      getOutputProperties(pred.nodeType, backendOutputProperties).forEach(prop => {
        items.push({ label: prop, detail: 'Output', insertText: prop, hasChildren: false })
      })
    }
  }

  if (filterText) {
    items = items.filter(i => i.label.toLowerCase().startsWith(filterText.toLowerCase()))
  }

  return items
}

export function extractPathFromText(text: string, cursorPos: number): string | null {
  const textUpToCursor = text.substring(0, cursorPos)
  const lastBrace = textUpToCursor.lastIndexOf('{{')

  if (lastBrace === -1 || textUpToCursor.substring(lastBrace + 2).includes('}}')) {
    return null
  }

  const afterBrace = textUpToCursor.substring(lastBrace + 2)
  const match = afterBrace.match(/^\s*([\w.]*)$/)
  return match ? match[1] : null
}
