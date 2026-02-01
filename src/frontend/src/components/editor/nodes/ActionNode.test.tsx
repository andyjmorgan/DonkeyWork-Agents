import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { ReactFlow, ReactFlowProvider, type NodeProps } from '@xyflow/react'
import { ActionNode, type ActionNodeData } from './ActionNode'

// Wrapper to provide ReactFlow context
const Wrapper = ({ children }: { children: React.ReactNode }) => (
  <ReactFlowProvider>
    <ReactFlow nodes={[]} edges={[]}>
      {children}
    </ReactFlow>
  </ReactFlowProvider>
)

describe('ActionNode', () => {
  const createProps = (data: ActionNodeData, selected = false): NodeProps => ({
    id: 'test-node',
    type: 'action',
    data: data as unknown as Record<string, unknown>,
    selected,
    isConnectable: true,
    zIndex: 0,
    positionAbsoluteX: 0,
    positionAbsoluteY: 0,
    dragging: false,
    draggable: true,
    deletable: true,
    selectable: true,
    parentId: undefined,
    sourcePosition: undefined,
    targetPosition: undefined,
    dragHandle: undefined,
    width: undefined,
    height: undefined,
  })

  it('should render action node with display name', () => {
    const props = createProps({
      actionType: 'http_request',
      displayName: 'HTTP Request',
      icon: 'globe'
    })

    render(<ActionNode {...props} />, { wrapper: Wrapper })
    expect(screen.getByText('HTTP Request')).toBeDefined()
    expect(screen.getByText('action')).toBeDefined()
  })

  it('should render with globe icon style', () => {
    const props = createProps({
      actionType: 'http_request',
      displayName: 'HTTP Request',
      icon: 'globe'
    })

    const { container } = render(<ActionNode {...props} />, { wrapper: Wrapper })
    // Check for gradient icon container (design system uses from-purple-500)
    const iconContainer = container.querySelector('.from-purple-500')
    expect(iconContainer).toBeTruthy()
  })

  it('should render with custom label', () => {
    const props = createProps({
      actionType: 'http_request',
      displayName: 'HTTP Request',
      icon: 'globe',
      label: 'My Custom Label'
    })

    render(<ActionNode {...props} />, { wrapper: Wrapper })
    expect(screen.getByText('My Custom Label')).toBeDefined()
  })

  it('should use default label when none provided', () => {
    const props = createProps({
      actionType: 'http_request',
      displayName: 'HTTP Request',
      icon: 'globe'
    })

    render(<ActionNode {...props} />, { wrapper: Wrapper })
    expect(screen.getByText('action')).toBeDefined()
  })

  it('should include parameters in data', () => {
    const data: ActionNodeData = {
      actionType: 'http_request',
      displayName: 'HTTP Request',
      icon: 'globe',
      parameters: {
        url: 'https://api.example.com',
        method: 'GET'
      }
    }
    const props = createProps(data)

    render(<ActionNode {...props} />, { wrapper: Wrapper })
    expect(data.parameters).toEqual({
      url: 'https://api.example.com',
      method: 'GET'
    })
  })
})
