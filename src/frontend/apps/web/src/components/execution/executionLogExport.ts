import type { NodeExecution } from '@donkeywork/api-client'

export interface ExecutionLogExport {
  executionId: string
  exportedAt: string
  nodes: NodeExecution[]
}

export function buildExecutionLogExport(executionId: string, nodeExecutions: NodeExecution[]): ExecutionLogExport {
  return {
    executionId,
    exportedAt: new Date().toISOString(),
    nodes: nodeExecutions,
  }
}

export function downloadExecutionLog(executionId: string, nodeExecutions: NodeExecution[]): void {
  const payload = buildExecutionLogExport(executionId, nodeExecutions)
  const json = JSON.stringify(payload, null, 2)
  const blob = new Blob([json], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `execution-log-${executionId}.json`
  anchor.click()
  URL.revokeObjectURL(url)
}
