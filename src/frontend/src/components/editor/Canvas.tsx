import { ReactFlowProvider } from '@xyflow/react'
import { CanvasInner } from './CanvasInner'

export function Canvas() {
  return (
    <div className="h-full w-full">
      <ReactFlowProvider>
        <CanvasInner />
      </ReactFlowProvider>
    </div>
  )
}
