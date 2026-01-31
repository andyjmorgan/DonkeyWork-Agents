import { renderHook } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { useActions } from './useActions'

// Mock the actions schema JSON
vi.mock('@/schemas/actions.json', () => ({
  default: [
    {
      actionType: 'http_request',
      displayName: 'HTTP Request',
      category: 'Communication',
      group: 'HTTP',
      icon: 'globe',
      description: 'Make HTTP requests',
      maxInputs: -1,
      maxOutputs: -1,
      enabled: true,
      parameters: []
    },
    {
      actionType: 'send_email',
      displayName: 'Send Email',
      category: 'Communication',
      group: 'Email',
      icon: 'mail',
      description: 'Send emails',
      maxInputs: -1,
      maxOutputs: -1,
      enabled: true,
      parameters: []
    },
    {
      actionType: 'disabled_action',
      displayName: 'Disabled',
      category: 'Test',
      icon: 'test',
      description: 'Disabled action',
      maxInputs: -1,
      maxOutputs: -1,
      enabled: false,
      parameters: []
    }
  ]
}))

describe('useActions', () => {
  it('should load actions from schema file and filter disabled', () => {
    const { result } = renderHook(() => useActions())

    // Should filter out disabled actions
    expect(result.current.actions).toHaveLength(2)
    expect(result.current.actions[0].actionType).toBe('http_request')
    expect(result.current.actions[1].actionType).toBe('send_email')
  })

  it('should group actions by category', () => {
    const { result } = renderHook(() => useActions())

    const { actionsByCategory } = result.current

    expect(Object.keys(actionsByCategory)).toEqual(['Communication'])
    expect(actionsByCategory.Communication).toHaveLength(2)
    expect(actionsByCategory.Communication[0].actionType).toBe('http_request')
    expect(actionsByCategory.Communication[1].actionType).toBe('send_email')
  })

  it('should get action by type', () => {
    const { result } = renderHook(() => useActions())

    const action = result.current.getAction('http_request')
    expect(action).toBeDefined()
    expect(action?.displayName).toBe('HTTP Request')

    const notFound = result.current.getAction('nonexistent')
    expect(notFound).toBeUndefined()
  })

  it('should filter disabled actions', () => {
    const { result } = renderHook(() => useActions())

    const disabledAction = result.current.getAction('disabled_action')
    expect(disabledAction).toBeUndefined()
  })
})
