import { useEffect, useState, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Download, Save, Loader2, History, Play } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet'
import { Canvas } from '@/components/editor/Canvas'
import { NodePalette } from '@/components/editor/NodePalette'
import { PropertiesPanel } from '@/components/editor/PropertiesPanel'
import { AgentMetadataDialog } from '@/components/editor/AgentMetadataDialog'
import { VersionHistorySheet } from '@/components/editor/VersionHistorySheet'
import { TestPanel } from '@/components/execution/TestPanel'
import { useEditorStore } from '@/store/editor'
import type { AgentVersion } from '@/lib/api'

export function AgentEditorPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const {
    agentId,
    agentName,
    agentDescription,
    isDraft,
    versionId,
    nodes,
    nodeConfigurations,
    loadAgent,
    reset,
    setAgentMetadata,
    save,
    exportToJson,
  } = useEditorStore()

  const [isMetadataDialogOpen, setIsMetadataDialogOpen] = useState(false)
  const [isVersionHistoryOpen, setIsVersionHistoryOpen] = useState(false)
  const [isTestPanelOpen, setIsTestPanelOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isPublishing, setIsPublishing] = useState(false)
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle')

  // Get input schema from start node
  const startNode = nodes.find(n => n.type === 'start')
  const inputSchema = startNode ? (nodeConfigurations[startNode.id] as any)?.inputSchema : undefined

  useEffect(() => {
    const loadAgentData = async () => {
      try {
        setIsLoading(true)

        if (id) {
          // Load existing agent
          const { agents } = await import('@/lib/api')
          const agent = await agents.get(id)
          const versions = await agents.listVersions(id)

          // Find draft version or use latest published
          const draftVersion = versions.find(v => v.isDraft)
          const currentVersion = draftVersion || versions.find(v => v.id === agent.currentVersionId)

          if (currentVersion) {
            // Merge top-level inputSchema into start node configuration
            const startNode = currentVersion.reactFlowData.nodes.find(n => n.type === 'start')
            const nodeConfigurations = {
              ...currentVersion.nodeConfigurations,
              ...(startNode ? {
                [startNode.id]: {
                  ...currentVersion.nodeConfigurations[startNode.id],
                  inputSchema: currentVersion.inputSchema
                }
              } : {})
            }

            // Load the version data into the store
            await loadAgent(
              agent.id,
              agent.name,
              agent.description || '',
              currentVersion.id,
              currentVersion.isDraft,
              currentVersion.reactFlowData,
              nodeConfigurations
            )
          } else {
            // No versions yet (shouldn't happen but handle it)
            loadAgent(agent.id, agent.name, agent.description || '')
          }
        } else {
          // Reset to default if no ID (new agent will be created when navigating from AgentsPage)
          reset()
        }
      } catch (error) {
        console.error('Failed to load agent:', error)
        // TODO: Show error toast
      } finally {
        setIsLoading(false)
      }
    }

    loadAgentData()
  }, [id, loadAgent, reset])

  const handleBack = () => {
    navigate('/agents')
  }

  const handleSave = useCallback(async (silent = false) => {
    if (!agentId) return

    try {
      if (!silent) {
        setIsSaving(true)
        setSaveStatus('saving')
      }

      await save()

      if (!silent) {
        setSaveStatus('saved')
        setTimeout(() => setSaveStatus('idle'), 2000)
      }
    } catch (error) {
      console.error('Failed to save:', error)
      if (!silent) {
        setSaveStatus('error')
        setTimeout(() => setSaveStatus('idle'), 3000)
      }
    } finally {
      if (!silent) {
        setIsSaving(false)
      }
    }
  }, [agentId, save])

  // Auto-save with debounce (5 seconds)
  // Note: This is a simple implementation. For production, consider using a more
  // sophisticated approach that tracks actual changes to nodes/edges/configs
  useEffect(() => {
    if (!agentId || isLoading) return

    const timer = setTimeout(() => {
      handleSave(true) // Auto-save silently
    }, 5000)

    return () => clearTimeout(timer)
  }, [agentId, isLoading, agentName, agentDescription, handleSave]) // Trigger on metadata changes

  const handleExport = useCallback(() => {
    const json = exportToJson()
    const blob = new Blob([json], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${agentName || 'agent'}_export.json`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  }, [exportToJson, agentName])

  const handleMetadataSave = useCallback(async (name: string, description: string) => {
    if (!agentId) return

    try {
      const { agents } = await import('@/lib/api')
      await agents.update(agentId, { name, description })
      setAgentMetadata(name, description)
    } catch (error) {
      console.error('Failed to update metadata:', error)
      // TODO: Show error toast
    }
  }, [agentId, setAgentMetadata])

  const handlePublish = useCallback(async () => {
    if (!agentId || !isDraft) return

    // Confirm before publishing
    if (!window.confirm('Are you sure you want to publish this version? The next save will create a new draft.')) {
      return
    }

    try {
      setIsPublishing(true)

      const { agents } = await import('@/lib/api')
      const publishedVersion = await agents.publish(agentId)

      // Merge top-level inputSchema into start node configuration
      const startNode = publishedVersion.reactFlowData.nodes.find(n => n.type === 'start')
      const nodeConfigurations = {
        ...publishedVersion.nodeConfigurations,
        ...(startNode ? {
          [startNode.id]: {
            ...publishedVersion.nodeConfigurations[startNode.id],
            inputSchema: publishedVersion.inputSchema
          }
        } : {})
      }

      // Update store to reflect published state
      // Stay on the current nodes/edges but mark as published
      // The next save will automatically create a new draft
      const store = useEditorStore.getState()
      store.loadAgent(
        agentId,
        agentName,
        agentDescription,
        publishedVersion.id,
        false, // isDraft = false (now published)
        publishedVersion.reactFlowData,
        nodeConfigurations
      )

      // TODO: Show success toast "Version published! The next save will create a new draft."
    } catch (error) {
      console.error('Failed to publish:', error)
      // TODO: Show error toast
    } finally {
      setIsPublishing(false)
    }
  }, [agentId, agentName, agentDescription, isDraft])

  const handleLoadVersion = useCallback(async (version: AgentVersion) => {
    // Merge top-level inputSchema into start node configuration
    const startNode = version.reactFlowData.nodes.find(n => n.type === 'start')
    const nodeConfigurations = {
      ...version.nodeConfigurations,
      ...(startNode ? {
        [startNode.id]: {
          ...version.nodeConfigurations[startNode.id],
          inputSchema: version.inputSchema
        }
      } : {})
    }

    const store = useEditorStore.getState()
    store.loadAgent(
      agentId!,
      agentName,
      agentDescription,
      version.id,
      version.isDraft,
      version.reactFlowData,
      nodeConfigurations
    )
  }, [agentId, agentName, agentDescription])

  const handleCreateDraftFromVersion = useCallback(async (version: AgentVersion) => {
    // Merge top-level inputSchema into start node configuration
    const startNode = version.reactFlowData.nodes.find(n => n.type === 'start')
    const nodeConfigurations = {
      ...version.nodeConfigurations,
      ...(startNode ? {
        [startNode.id]: {
          ...version.nodeConfigurations[startNode.id],
          inputSchema: version.inputSchema
        }
      } : {})
    }

    // Create a new draft by loading the version's data and marking as draft
    const store = useEditorStore.getState()
    store.loadAgent(
      agentId!,
      agentName,
      agentDescription,
      undefined, // No version ID yet (will be created on save)
      true, // Mark as draft
      version.reactFlowData,
      nodeConfigurations
    )
    // TODO: Show success toast "Draft created from version X. Save to persist."
  }, [agentId, agentName, agentDescription])

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="flex h-screen flex-col overflow-hidden">
      {/* Header */}
      <header className="flex items-center justify-between border-b border-border bg-card px-4 py-3">
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            size="icon"
            onClick={handleBack}
            className="h-8 w-8"
          >
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div className="flex items-center gap-3">
            <button
              onClick={() => setIsMetadataDialogOpen(true)}
              className="text-left hover:opacity-80 transition-opacity"
            >
              <h1 className="text-lg font-semibold">{agentName}</h1>
              {agentDescription && (
                <p className="text-sm text-muted-foreground">{agentDescription}</p>
              )}
            </button>
            {isDraft ? (
              <Badge variant="secondary">Draft</Badge>
            ) : versionId ? (
              <Badge variant="outline">Published</Badge>
            ) : null}
          </div>
        </div>
        <div className="flex items-center gap-2">
          {saveStatus === 'saving' && (
            <span className="text-sm text-muted-foreground flex items-center gap-2">
              <Loader2 className="h-3 w-3 animate-spin" />
              Saving...
            </span>
          )}
          {saveStatus === 'saved' && (
            <span className="text-sm text-green-600">Saved</span>
          )}
          {saveStatus === 'error' && (
            <span className="text-sm text-destructive">Save failed</span>
          )}
          <Button
            variant="outline"
            size="sm"
            onClick={() => setIsTestPanelOpen(true)}
            disabled={!agentId}
          >
            <Play className="h-4 w-4" />
            <span className="hidden sm:inline">Test</span>
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setIsVersionHistoryOpen(true)}
          >
            <History className="h-4 w-4" />
            <span className="hidden sm:inline">Versions</span>
          </Button>
          <Button variant="outline" size="sm" onClick={handleExport}>
            <Download className="h-4 w-4" />
            <span className="hidden sm:inline">Export</span>
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleSave(false)}
            disabled={isSaving || !agentId}
          >
            {isSaving ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Save className="h-4 w-4" />
            )}
            <span className="hidden sm:inline">Save Draft</span>
          </Button>
          <Button
            size="sm"
            onClick={handlePublish}
            disabled={!agentId || isPublishing}
          >
            {isPublishing ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : null}
            {isDraft ? 'Publish' : 'Publish Current'}
          </Button>
        </div>
      </header>

      {/* Main content area */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left: Node Palette */}
        <aside className="w-64 border-r border-border bg-card p-4">
          <h2 className="mb-4 text-sm font-semibold">Node Palette</h2>
          <NodePalette />
        </aside>

        {/* Center: Canvas */}
        <main className="flex-1 bg-muted/20">
          <Canvas />
        </main>
      </div>

      {/* Properties Panel (flyout drawer) */}
      <PropertiesPanel />

      {/* Metadata Dialog */}
      <AgentMetadataDialog
        open={isMetadataDialogOpen}
        onOpenChange={setIsMetadataDialogOpen}
        name={agentName}
        description={agentDescription}
        onSave={handleMetadataSave}
      />

      {/* Version History Sheet */}
      {agentId && (
        <VersionHistorySheet
          open={isVersionHistoryOpen}
          onOpenChange={setIsVersionHistoryOpen}
          agentId={agentId}
          currentVersionId={versionId}
          onLoadVersion={handleLoadVersion}
          onCreateDraftFromVersion={handleCreateDraftFromVersion}
        />
      )}

      {/* Test Panel Sheet */}
      <Sheet open={isTestPanelOpen} onOpenChange={setIsTestPanelOpen}>
        <SheetContent side="right" className="w-full sm:max-w-2xl">
          <SheetHeader>
            <SheetTitle>Test Agent</SheetTitle>
          </SheetHeader>
          <div className="mt-6 h-[calc(100vh-8rem)]">
            {agentId && <TestPanel agentId={agentId} inputSchema={inputSchema} />}
          </div>
        </SheetContent>
      </Sheet>
    </div>
  )
}
