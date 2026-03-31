import { describe, it, expect } from 'vitest'
import { buildSuggestions, extractPathFromText, getOutputProperties, type Predecessor } from './scribanSuggestions'

const predecessors: Predecessor[] = [
  { nodeId: '1', nodeName: 'Get_Name', nodeType: 'MultimodalChatModel' },
  { nodeId: '2', nodeName: 'GPT_4o_Mini_TTS', nodeType: 'TextToSpeech' },
  { nodeId: '3', nodeName: 'Get_Description', nodeType: 'MultimodalChatModel' },
]

const inputProperties = ['input', 'topic']

describe('getOutputProperties', () => {
  it('returns properties for exact case match', () => {
    expect(getOutputProperties('TextToSpeech')).toEqual(
      ['ObjectKey', 'FileName', 'ContentType', 'SizeBytes', 'Transcript', 'Voice', 'Model']
    )
  })

  it('returns properties for case-insensitive match', () => {
    expect(getOutputProperties('texttospeech')).toEqual(
      ['ObjectKey', 'FileName', 'ContentType', 'SizeBytes', 'Transcript', 'Voice', 'Model']
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

  it('returns path with dot', () => {
    expect(extractPathFromText('{{Steps.Get_Name', 16)).toBe('Steps.Get_Name')
  })

  it('returns path with nested dot', () => {
    expect(extractPathFromText('{{Steps.Get_Name.ResponseText', 29)).toBe('Steps.Get_Name.ResponseText')
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
})

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

    it('returns no items for non-matching partial text', () => {
      const items = buildSuggestions('X', predecessors, inputProperties)
      expect(items).toHaveLength(0)
    })

    it('filters root items by partial text', () => {
      const items = buildSuggestions('In', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toContain('Input')
      expect(labels).not.toContain('Steps')
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

    it('filters predecessors by partial name', () => {
      const items = buildSuggestions('Steps.Get', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toEqual(['Get_Name', 'Get_Description'])
    })
  })

  describe('Steps - output properties', () => {
    it('lists output properties after "Steps.name."', () => {
      const items = buildSuggestions('Steps.GPT_4o_Mini_TTS.', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toEqual(
        ['ObjectKey', 'FileName', 'ContentType', 'SizeBytes', 'Transcript', 'Voice', 'Model']
      )
    })

    it('lists model output properties', () => {
      const items = buildSuggestions('Steps.Get_Name.', predecessors, inputProperties)
      const labels = items.map(i => i.label)
      expect(labels).toContain('ResponseText')
      expect(labels).toContain('TotalTokens')
    })

    it('filters output properties by partial name', () => {
      const items = buildSuggestions('Steps.GPT_4o_Mini_TTS.Obj', predecessors, inputProperties)
      expect(items).toHaveLength(1)
      expect(items[0].label).toBe('ObjectKey')
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
  })
})
