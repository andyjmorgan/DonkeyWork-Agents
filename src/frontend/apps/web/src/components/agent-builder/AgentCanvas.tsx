import { ReactFlowProvider } from '@xyflow/react'
import { AgentCanvasInner } from './AgentCanvasInner'

export function AgentCanvas() {
  return (
    <div className="h-full w-full">
      <ReactFlowProvider>
        <AgentCanvasInner />
      </ReactFlowProvider>
    </div>
  )
}
