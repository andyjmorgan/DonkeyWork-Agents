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
  const createProps = (data: ActionNodeData, selected = false): NodeProps<ActionNodeData> => ({
    id: 'test-node',
    type: 'action',
    data,
    selected,
    isConnectable: true,
    zIndex: 0,
    xPos: 0,
    yPos: 0,
    dragging: false,
    // @ts-ignore - minimal props for testing
    positionAbsoluteX: 0,
    positionAbsoluteY: 0
  })

  it('should render action node with display name', () => {
    const props = createProps({
      actionType: 'http_request',
      displayName: 'HTTP Request',
      icon: 'globe'
    })

    render(<ActionNode {...props} />, { wrapper: Wrapper })
    expect(screen.getByText('HTTP Request')).toBeInTheDocument()
    expect(screen.getByText('Action')).toBeInTheDocument()
  })

  it('should render with globe icon', () => {
    const props = createProps({
      actionType: 'http_request',
      displayName: 'HTTP Request',
      icon: 'globe'
    })

    const { container } = render(<ActionNode {...props} />, { wrapper: Wrapper })
    const iconContainer = container.querySelector('.bg-purple-500\\/10')
    expect(iconContainer).toBeInTheDocument()
  })

  it('should render with mail icon', () => {
    const props = createProps({
      actionType: 'send_email',
      displayName: 'Send Email',
      icon: 'mail'
    })

    const { container } = render(<ActionNode {...props} />, { wrapper: Wrapper })
    const iconContainer = container.querySelector('.bg-purple-500\\/10')
    expect(iconContainer).toBeInTheDocument()
  })

  it('should render with default icon when no icon specified', () => {
    const props = createProps({
      actionType: 'custom_action',
      displayName: 'Custom Action'
    })

    const { container } = render(<ActionNode {...props} />, { wrapper: Wrapper })
    const iconContainer = container.querySelector('.bg-purple-500\\/10')
    expect(iconContainer).toBeInTheDocument()
  })

  it('should apply selected styles when selected', () => {
    const props = createProps(
      {
        actionType: 'http_request',
        displayName: 'HTTP Request',
        icon: 'globe'
      },
      true
    )

    const { container } = render(<ActionNode {...props} />, { wrapper: Wrapper })
    const nodeElement = container.querySelector('.border-purple-500')
    expect(nodeElement).toBeInTheDocument()
  })

  it('should apply unselected styles when not selected', () => {
    const props = createProps(
      {
        actionType: 'http_request',
        displayName: 'HTTP Request',
        icon: 'globe'
      },
      false
    )

    const { container } = render(<ActionNode {...props} />, { wrapper: Wrapper })
    const nodeElement = container.querySelector('.border-purple-500\\/30')
    expect(nodeElement).toBeInTheDocument()
  })

  it('should include parameters in data', () => {
    const props = createProps({
      actionType: 'http_request',
      displayName: 'HTTP Request',
      icon: 'globe',
      parameters: {
        url: 'https://api.example.com',
        method: 'GET'
      }
    })

    render(<ActionNode {...props} />, { wrapper: Wrapper })
    expect(props.data.parameters).toEqual({
      url: 'https://api.example.com',
      method: 'GET'
    })
  })
})
