import { renderHook, waitFor } from '@testing-library/react'
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useActions } from './useActions'
import type { ActionNodeSchema } from '@/types/actions'

describe('useActions', () => {
  const mockActions: ActionNodeSchema[] = [
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

  beforeEach(() => {
    global.fetch = vi.fn()
  })

  it('should load actions from schema file', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockActions
    })

    const { result } = renderHook(() => useActions())

    expect(result.current.loading).toBe(true)
    expect(result.current.actions).toEqual([])

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    // Should filter out disabled actions
    expect(result.current.actions).toHaveLength(2)
    expect(result.current.actions[0].actionType).toBe('http_request')
    expect(result.current.actions[1].actionType).toBe('send_email')
  })

  it('should group actions by category', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockActions
    })

    const { result } = renderHook(() => useActions())

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    const { actionsByCategory } = result.current

    expect(Object.keys(actionsByCategory)).toEqual(['Communication'])
    expect(actionsByCategory.Communication).toHaveLength(2)
    expect(actionsByCategory.Communication[0].actionType).toBe('http_request')
    expect(actionsByCategory.Communication[1].actionType).toBe('send_email')
  })

  it('should get action by type', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockActions
    })

    const { result } = renderHook(() => useActions())

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    const action = result.current.getAction('http_request')
    expect(action).toBeDefined()
    expect(action?.displayName).toBe('HTTP Request')

    const notFound = result.current.getAction('nonexistent')
    expect(notFound).toBeUndefined()
  })

  it('should handle fetch errors', async () => {
    (global.fetch as any).mockRejectedValueOnce(new Error('Network error'))

    const { result } = renderHook(() => useActions())

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    expect(result.current.error).toBeDefined()
    expect(result.current.error?.message).toBe('Network error')
    expect(result.current.actions).toEqual([])
  })

  it('should handle HTTP errors', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: false,
      statusText: 'Not Found'
    })

    const { result } = renderHook(() => useActions())

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    expect(result.current.error).toBeDefined()
    expect(result.current.actions).toEqual([])
  })

  it('should filter disabled actions', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockActions
    })

    const { result } = renderHook(() => useActions())

    await waitFor(() => {
      expect(result.current.loading).toBe(false)
    })

    const disabledAction = result.current.getAction('disabled_action')
    expect(disabledAction).toBeUndefined()
  })
})
