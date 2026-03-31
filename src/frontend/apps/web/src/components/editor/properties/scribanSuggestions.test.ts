import { describe, it, expect } from 'vitest'
import { buildSuggestions, extractPathFromText, getOutputProperties, type Predecessor } from './scribanSuggestions'

const predecessors: Predecessor[] = [
  { nodeId: '1', nodeName: 'Get_Name', nodeType: 'MultimodalChatModel' },
  { nodeId: '2', nodeName: 'GPT_4o_Mini_TTS', nodeType: 'TextToSpeech' },
  { nodeId: '3', nodeName: 'Get_Description', nodeType: 'MultimodalChatModel' },
]

const inputProperties = ['input', 'topic']

// --- getOutputProperties ---

describe('getOutputProperties', () => {
  describe('fallback (no backend data)', () => {
    it('returns properties for exact case match', () => {
      expect(getOutputProperties('TextToSpeech')).toEqual(
        ['AudioBase64', 'ContentType', 'FileExtension', 'SizeBytes', 'Transcript', 'Voice', 'Model']
      )
    })

    it('returns properties for case-insensitive match', () => {
      expect(getOutputProperties('texttospeech')).toEqual(
        ['AudioBase64', 'ContentType', 'FileExtension', 'SizeBytes', 'Transcript', 'Voice', 'Model']
      )
    })

    it('returns model output properties for MultimodalChatModel', () => {
      expect(getOutputProperties('MultimodalChatModel')).toEqual(
        ['ResponseText', 'TotalTokens', 'InputTokens', 'OutputTokens']
      )
    })

    it('returns empty array for unknown node type', () => {
      expect(getOutputProperties('UnknownNode')).toEqual([])
    })

    it('returns empty array for Start node', () => {
      expect(getOutputProperties('Start')).toEqual([])
    })
  })

  describe('with backend data', () => {
    const backend: Record<string, string[]> = {
      TextToSpeech: ['AudioBase64', 'ContentType', 'NewProp'],
      CustomNode: ['Foo', 'Bar'],
    }

    it('prefers backend data over fallback', () => {
      expect(getOutputProperties('TextToSpeech', backend)).toEqual(['AudioBase64', 'ContentType', 'NewProp'])
    })

    it('is case-insensitive for backend keys', () => {
      expect(getOutputProperties('texttospeech', backend)).toEqual(['AudioBase64', 'ContentType', 'NewProp'])
    })

    it('returns backend data for types not in fallback', () => {
      expect(getOutputProperties('CustomNode', backend)).toEqual(['Foo', 'Bar'])
    })

    it('falls back when type not in backend data', () => {
      expect(getOutputProperties('MultimodalChatModel', backend)).toEqual(
        ['ResponseText', 'TotalTokens', 'InputTokens', 'OutputTokens']
      )
    })

    it('falls back for unknown type not in either', () => {
      expect(getOutputProperties('NothingHere', backend)).toEqual([])
    })
  })
})

// --- extractPathFromText ---

describe('extractPathFromText', () => {
  it('returns null when no {{ present', () => {
    expect(extractPathFromText('hello world', 11)).toBeNull()
  })

  it('returns empty string right after {{', () => {
    expect(extractPathFromText('{{', 2)).toBe('')
  })

  it('returns path after {{', () => {
    expect(extractPathFromText('{{Steps', 7)).toBe('Steps')
  })

  it('returns trailing dot for {{Steps.', () => {
    expect(extractPathFromText('{{Steps.', 8)).toBe('Steps.')
  })

  it('returns path with dot and partial', () => {
    expect(extractPathFromText('{{Steps.Get_Name', 16)).toBe('Steps.Get_Name')
  })

  it('returns path with nested dot', () => {
    expect(extractPathFromText('{{Steps.Get_Name.ResponseText', 29)).toBe('Steps.Get_Name.ResponseText')
  })

  it('returns trailing dot at depth 2', () => {
    expect(extractPathFromText('{{Steps.Get_Name.', 17)).toBe('Steps.Get_Name.')
  })

  it('returns null when braces are closed', () => {
    expect(extractPathFromText('{{Steps}}', 9)).toBeNull()
  })

  it('returns null when cursor is after closed braces', () => {
    expect(extractPathFromText('{{Steps}} more text', 19)).toBeNull()
  })

  it('handles text before the braces', () => {
    expect(extractPathFromText('Hello {{Steps', 13)).toBe('Steps')
  })

  it('returns path from second {{ when first is closed', () => {
    expect(extractPathFromText('{{Input.x}} and {{Steps', 23)).toBe('Steps')
  })

  it('handles whitespace after {{', () => {
    expect(extractPathFromText('{{ Steps', 8)).toBe('Steps')
  })

  it('returns null for non-word characters after {{', () => {
    expect(extractPathFromText('{{ @invalid', 11)).toBeNull()
  })

  it('uses cursor position, not end of string', () => {
    expect(extractPathFromText('{{Steps.Get_Name.ResponseText}} extra', 17)).toBe('Steps.Get_Name.')
  })

  it('returns empty when cursor is right after {{ with trailing text', () => {
    expect(extractPathFromText('{{Steps}}', 2)).toBe('')
  })
})

// --- buildSuggestions ---

describe('buildSuggestions', () => {
  describe('root level', () => {
    it('shows root items for empty path', () => {
      const items = buildSuggestions('', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toContain('Input')
      expect(labels).toContain('Steps')
      expect(labels).toContain('ExecutionId')
      expect(labels).toContain('UserId')
    })

    it('shows 4 root items with predecessors', () => {
      const items = buildSuggestions('', predecessors, inputProperties)
      expect(items).toHaveLength(4)
    })

    it('shows 3 root items without predecessors', () => {
      const items = buildSuggestions('', [], inputProperties)
      expect(items).toHaveLength(3)
    })

    it('returns no items for non-matching partial text', () => {
      const items = buildSuggestions('X', predecessors, inputProperties)
      expect(items).toHaveLength(0)
    })

    it('filters root items by partial text "In"', () => {
      const items = buildSuggestions('In', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toContain('Input')
      expect(labels).not.toContain('Steps')
    })

    it('filters root items by partial text "St"', () => {
      const items = buildSuggestions('St', predecessors, inputProperties)
      expect(items).toHaveLength(1)
      expect(items[0].label).toBe('Steps')
    })

    it('filters root items case-insensitively', () => {
      const items = buildSuggestions('ex', predecessors, inputProperties)
      expect(items).toHaveLength(1)
      expect(items[0].label).toBe('ExecutionId')
    })

    it('does not show Steps when no predecessors', () => {
      const items = buildSuggestions('', [], inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).not.toContain('Steps')
    })

    it('marks Input as having children when inputProperties exist', () => {
      const items = buildSuggestions('', predecessors, inputProperties)
      const input = items.find(i => i.label === 'Input')
      expect(input?.hasChildren).toBe(true)
    })

    it('marks Input as no children when inputProperties empty', () => {
      const items = buildSuggestions('', predecessors, [])
      const input = items.find(i => i.label === 'Input')
      expect(input?.hasChildren).toBe(false)
    })

    it('marks Steps as having children', () => {
      const items = buildSuggestions('', predecessors, inputProperties)
      const steps = items.find(i => i.label === 'Steps')
      expect(steps?.hasChildren).toBe(true)
    })

    it('marks ExecutionId as having no children', () => {
      const items = buildSuggestions('', predecessors, inputProperties)
      const execId = items.find(i => i.label === 'ExecutionId')
      expect(execId?.hasChildren).toBe(false)
    })
  })

  describe('Input properties', () => {
    it('typing "Input" filters root to show Input item', () => {
      const items = buildSuggestions('Input', predecessors, inputProperties)
      expect(items).toHaveLength(1)
      expect(items[0].label).toBe('Input')
    })

    it('lists input properties after "Input."', () => {
      const items = buildSuggestions('Input.', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toEqual(['input', 'topic'])
    })

    it('filters input properties by partial text', () => {
      const items = buildSuggestions('Input.to', predecessors, inputProperties)
      expect(items).toHaveLength(1)
      expect(items[0].label).toBe('topic')
    })

    it('returns empty when no input properties match filter', () => {
      const items = buildSuggestions('Input.xyz', predecessors, inputProperties)
      expect(items).toHaveLength(0)
    })

    it('marks input properties as no children', () => {
      const items = buildSuggestions('Input.', predecessors, inputProperties)
      expect(items.every(i => !i.hasChildren)).toBe(true)
    })
  })

  describe('Steps - predecessor listing', () => {
    it('typing "Steps" filters root to show Steps item', () => {
      const items = buildSuggestions('Steps', predecessors, inputProperties)
      expect(items).toHaveLength(1)
      expect(items[0].label).toBe('Steps')
    })

    it('lists all predecessors after "Steps."', () => {
      const items = buildSuggestions('Steps.', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toEqual(['Get_Name', 'GPT_4o_Mini_TTS', 'Get_Description'])
    })

    it('marks predecessors with outputs as having children', () => {
      const items = buildSuggestions('Steps.', predecessors, inputProperties)
      const tts = items.find(i => i.label === 'GPT_4o_Mini_TTS')
      expect(tts?.hasChildren).toBe(true)
    })

    it('shows predecessor nodeType as detail', () => {
      const items = buildSuggestions('Steps.', predecessors, inputProperties)
      const tts = items.find(i => i.label === 'GPT_4o_Mini_TTS')
      expect(tts?.detail).toBe('TextToSpeech')
    })

    it('filters predecessors by partial name', () => {
      const items = buildSuggestions('Steps.Get', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toEqual(['Get_Name', 'Get_Description'])
    })

    it('filters predecessors case-insensitively', () => {
      const items = buildSuggestions('Steps.gpt', predecessors, inputProperties)
      expect(items).toHaveLength(1)
      expect(items[0].label).toBe('GPT_4o_Mini_TTS')
    })

    it('returns empty for no matching predecessors', () => {
      const items = buildSuggestions('Steps.ZZZ', predecessors, inputProperties)
      expect(items).toHaveLength(0)
    })
  })

  describe('Steps - output properties', () => {
    it('lists output properties after "Steps.name."', () => {
      const items = buildSuggestions('Steps.GPT_4o_Mini_TTS.', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toEqual(
        ['AudioBase64', 'ContentType', 'FileExtension', 'SizeBytes', 'Transcript', 'Voice', 'Model']
      )
    })

    it('lists model output properties', () => {
      const items = buildSuggestions('Steps.Get_Name.', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toContain('ResponseText')
      expect(labels).toContain('TotalTokens')
    })

    it('filters output properties by partial name', () => {
      const items = buildSuggestions('Steps.GPT_4o_Mini_TTS.Aud', predecessors, inputProperties)
      expect(items).toHaveLength(1)
      expect(items[0].label).toBe('AudioBase64')
    })

    it('is case-insensitive for predecessor name', () => {
      const items = buildSuggestions('Steps.get_name.', predecessors, inputProperties)
      expect(items.length).toBeGreaterThan(0)
      expect(items[0].label).toBe('ResponseText')
    })

    it('returns empty for unknown predecessor', () => {
      const items = buildSuggestions('Steps.NonExistent.', predecessors, inputProperties)
      expect(items).toHaveLength(0)
    })

    it('marks output properties as no children', () => {
      const items = buildSuggestions('Steps.Get_Name.', predecessors, inputProperties)
      expect(items.every(i => !i.hasChildren)).toBe(true)
    })
  })

  describe('with backend output properties', () => {
    const backend: Record<string, string[]> = {
      TextToSpeech: ['AudioBase64', 'ContentType', 'BackendOnlyProp'],
      CustomNode: ['Alpha', 'Beta'],
    }

    const customPredecessors: Predecessor[] = [
      ...predecessors,
      { nodeId: '4', nodeName: 'My_Custom', nodeType: 'CustomNode' },
    ]

    it('uses backend properties over fallback for known types', () => {
      const items = buildSuggestions('Steps.GPT_4o_Mini_TTS.', predecessors, inputProperties, backend)
      const labels = items.map(i => i.label)
      expect(labels).toContain('BackendOnlyProp')
      expect(labels).not.toContain('SizeBytes')
    })

    it('uses backend properties for types not in fallback', () => {
      const items = buildSuggestions('Steps.My_Custom.', customPredecessors, inputProperties, backend)
      const labels = items.map(i => i.label)
      expect(labels).toEqual(['Alpha', 'Beta'])
    })

    it('falls back for types not in backend data', () => {
      const items = buildSuggestions('Steps.Get_Name.', predecessors, inputProperties, backend)
      const labels = items.map(i => i.label)
      expect(labels).toContain('ResponseText')
    })

    it('marks predecessor as having children based on backend data', () => {
      const items = buildSuggestions('Steps.', customPredecessors, inputProperties, backend)
      const custom = items.find(i => i.label === 'My_Custom')
      expect(custom?.hasChildren).toBe(true)
    })

    it('marks predecessor as no children when backend says no properties', () => {
      const emptyBackend: Record<string, string[]> = { TextToSpeech: [] }
      const items = buildSuggestions('Steps.', predecessors, inputProperties, emptyBackend)
      const tts = items.find(i => i.label === 'GPT_4o_Mini_TTS')
      expect(tts?.hasChildren).toBe(false)
    })
  })

  describe('edge cases', () => {
    it('handles empty predecessors and empty input', () => {
      const items = buildSuggestions('', [], [])
      expect(items).toHaveLength(3) // Input (no children), ExecutionId, UserId
      const input = items.find(i => i.label === 'Input')
      expect(input?.hasChildren).toBe(false)
    })

    it('handles deeply nested path gracefully', () => {
      const items = buildSuggestions('Steps.Get_Name.ResponseText.Something.', predecessors, inputProperties)
      expect(items).toHaveLength(0)
    })

    it('handles path with only dots as root level', () => {
      const items = buildSuggestions('...', predecessors, inputProperties)
      expect(items.length).toBeGreaterThan(0) // degrades to root level
    })

    it('handles single dot', () => {
      const items = buildSuggestions('.', predecessors, inputProperties)
      // resolved is empty (dot at start), so root level, no filter
      expect(items.length).toBeGreaterThan(0)
    })
  })
})
