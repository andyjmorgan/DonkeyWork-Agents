import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import type { NodeExecution } from '@donkeywork/api-client'
import { buildExecutionLogExport, downloadExecutionLog } from './executionLogExport'

const makeNode = (overrides: Partial<NodeExecution> = {}): NodeExecution => ({
  id: 'node-exec-1',
  nodeId: 'node-1',
  nodeType: 'MultimodalChatModel',
  nodeName: 'My Model',
  status: 'Completed',
  startedAt: '2024-01-01T00:00:00.000Z',
  completedAt: '2024-01-01T00:00:01.000Z',
  durationMs: 1000,
  input: '{"prompt":"hello"}',
  output: '{"text":"world"}',
  ...overrides,
})

describe('buildExecutionLogExport', () => {
  it('includes the executionId', () => {
    const result = buildExecutionLogExport('exec-123', [makeNode()])
    expect(result.executionId).toBe('exec-123')
  })

  it('includes an ISO exportedAt timestamp', () => {
    const before = new Date().toISOString()
    const result = buildExecutionLogExport('exec-123', [makeNode()])
    const after = new Date().toISOString()
    expect(result.exportedAt >= before).toBe(true)
    expect(result.exportedAt <= after).toBe(true)
  })

  it('includes the full untruncated node list', () => {
    const nodes = [
      makeNode({ id: 'n1', input: 'x'.repeat(10_000) }),
      makeNode({ id: 'n2', output: 'y'.repeat(10_000) }),
    ]
    const result = buildExecutionLogExport('exec-123', nodes)
    expect(result.nodes).toHaveLength(2)
    expect(result.nodes[0].input).toHaveLength(10_000)
    expect(result.nodes[1].output).toHaveLength(10_000)
  })

  it('preserves all NodeExecution fields', () => {
    const node = makeNode({
      errorMessage: 'something went wrong',
      tokensUsed: 42,
      fullResponse: '{"raw":true}',
      actionType: 'http_request',
    })
    const result = buildExecutionLogExport('exec-123', [node])
    const exported = result.nodes[0]
    expect(exported.errorMessage).toBe('something went wrong')
    expect(exported.tokensUsed).toBe(42)
    expect(exported.fullResponse).toBe('{"raw":true}')
    expect(exported.actionType).toBe('http_request')
  })

  it('produces valid JSON when stringified', () => {
    const result = buildExecutionLogExport('exec-456', [makeNode()])
    expect(() => JSON.parse(JSON.stringify(result, null, 2))).not.toThrow()
  })
})

describe('downloadExecutionLog', () => {
  let createObjectURLSpy: ReturnType<typeof vi.fn>
  let revokeObjectURLSpy: ReturnType<typeof vi.fn>
  let clickSpy: ReturnType<typeof vi.fn>
  let createdAnchor: HTMLAnchorElement

  beforeEach(() => {
    createObjectURLSpy = vi.fn().mockReturnValue('blob:mock-url')
    revokeObjectURLSpy = vi.fn()
    clickSpy = vi.fn()

    global.URL.createObjectURL = createObjectURLSpy
    global.URL.revokeObjectURL = revokeObjectURLSpy

    createdAnchor = document.createElement('a')
    createdAnchor.click = clickSpy

    const createElementOrig = document.createElement.bind(document)
    vi.spyOn(document, 'createElement').mockImplementation((tag: string) => {
      if (tag === 'a') return createdAnchor
      return createElementOrig(tag)
    })
  })

  it('creates a Blob from the JSON payload', () => {
    const nodes = [makeNode()]
    downloadExecutionLog('exec-789', nodes)

    expect(createObjectURLSpy).toHaveBeenCalledOnce()
    const blob: Blob = createObjectURLSpy.mock.calls[0][0]
    expect(blob.type).toBe('application/json')
  })

  it('sets the download filename to execution-log-{id}.json', () => {
    downloadExecutionLog('exec-789', [makeNode()])
    expect(createdAnchor.download).toBe('execution-log-exec-789.json')
  })

  it('triggers a click on the anchor', () => {
    downloadExecutionLog('exec-789', [makeNode()])
    expect(clickSpy).toHaveBeenCalledOnce()
  })

  it('revokes the object URL after the click', () => {
    downloadExecutionLog('exec-789', [makeNode()])
    expect(revokeObjectURLSpy).toHaveBeenCalledWith('blob:mock-url')
  })

  it('embeds valid JSON in the Blob with the correct executionId and node count', () => {
    const OriginalBlob = global.Blob
    let capturedParts: BlobPart[] = []
    global.Blob = class extends OriginalBlob {
      constructor(parts?: BlobPart[], options?: BlobPropertyBag) {
        capturedParts = parts ?? []
        super(parts, options)
      }
    }

    downloadExecutionLog('exec-789', [makeNode({ id: 'n1' }), makeNode({ id: 'n2' })])

    global.Blob = OriginalBlob
    const text = capturedParts[0] as string
    const parsed = JSON.parse(text)
    expect(parsed.executionId).toBe('exec-789')
    expect(parsed.nodes).toHaveLength(2)
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })
})
