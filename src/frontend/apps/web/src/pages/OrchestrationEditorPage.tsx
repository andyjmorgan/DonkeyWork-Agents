import { useEffect, useState, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Download, Save, Loader2, History, Play, Settings2 } from 'lucide-react'
import {
  Button,
  Badge,
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@donkeywork/ui'
import { Canvas } from '@/components/editor/Canvas'
import { NodePalette } from '@/components/editor/NodePalette'
import { PropertiesPanel } from '@/components/editor/PropertiesPanel'
import { OrchestrationMetadataDialog } from '@/components/editor/OrchestrationMetadataDialog'
import { VersionHistorySheet } from '@/components/editor/VersionHistorySheet'
import { ExportJsonDialog } from '@/components/editor/ExportJsonDialog'
import { InterfacesPanel } from '@/components/editor/InterfacesPanel'
import { TestPanel } from '@/components/execution/TestPanel'
import { useEditorStore } from '@/store/editor'
import type { OrchestrationVersion, JSONSchema } from '@donkeywork/api-client'
import { toast } from 'sonner'

// Type for nodes in ReactFlowData from API response
interface ApiNode {
  id: string
  type?: string
  data?: { nodeType?: string }
}

export function OrchestrationEditorPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const orchestrationId = useEditorStore((state) => state.orchestrationId)
  const orchestrationName = useEditorStore((state) => state.orchestrationName)
  const orchestrationDescription = useEditorStore((state) => state.orchestrationDescription)
  const isDraft = useEditorStore((state) => state.isDraft)
  const versionId = useEditorStore((state) => state.versionId)
  const nodes = useEditorStore((state) => state.nodes)
  const nodeConfigurations = useEditorStore((state) => state.nodeConfigurations)
  const loadOrchestration = useEditorStore((state) => state.loadOrchestration)
  const reset = useEditorStore((state) => state.reset)
  const setOrchestrationMetadata = useEditorStore((state) => state.setOrchestrationMetadata)
  const save = useEditorStore((state) => state.save)
  const exportToJson = useEditorStore((state) => state.exportToJson)

  const [isMetadataDialogOpen, setIsMetadataDialogOpen] = useState(false)
  const [isVersionHistoryOpen, setIsVersionHistoryOpen] = useState(false)
  const [isTestPanelOpen, setIsTestPanelOpen] = useState(false)
  const [isExportDialogOpen, setIsExportDialogOpen] = useState(false)
  const [isInterfacesPanelOpen, setIsInterfacesPanelOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isPublishing, setIsPublishing] = useState(false)
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle')

  const startNode = nodes.find(n => n.data?.nodeType === 'Start')
  const inputSchema = startNode ? (nodeConfigurations[startNode.id] as { inputSchema?: JSONSchema } | undefined)?.inputSchema : undefined

  useEffect(() => {
    const loadOrchestrationData = async () => {
      try {
        setIsLoading(true)

        if (id) {
          const { orchestrations } = await import('@donkeywork/api-client')
          const orchestration = await orchestrations.get(id)
          const versions = await orchestrations.listVersions(id)

          const draftVersion = versions.find(v => v.isDraft)
          const currentVersion = draftVersion || versions.find(v => v.id === orchestration.currentVersionId)

          if (currentVersion) {
            // Merge top-level inputSchema into start node configuration
            const startNode = currentVersion.reactFlowData.nodes.find((n: ApiNode) => n.type === 'start' || n.data?.nodeType === 'Start')
            const nodeConfigurations = {
              ...currentVersion.nodeConfigurations,
              ...(startNode ? {
                [startNode.id]: {
                  ...currentVersion.nodeConfigurations[startNode.id],
                  inputSchema: currentVersion.inputSchema
                }
              } : {})
            }

            await loadOrchestration(
              orchestration.id,
              orchestration.name,
              orchestration.description || '',
              currentVersion.id,
              currentVersion.isDraft,
              currentVersion.reactFlowData,
              nodeConfigurations,
              currentVersion.interfaces
            )
          } else {
            // No versions yet (shouldn't happen but handle it)
            loadOrchestration(orchestration.id, orchestration.name, orchestration.description || '')
          }
        } else {
          // Reset to default if no ID (new orchestration will be created when navigating from OrchestrationsPage)
          reset()
        }
      } catch (error) {
        console.error('Failed to load orchestration:', error)
        // TODO: Show error toast
      } finally {
        setIsLoading(false)
      }
    }

    loadOrchestrationData()
  }, [id, loadOrchestration, reset])

  const handleBack = () => {
    navigate('/orchestrations')
  }

  const handleSave = useCallback(async (silent = false) => {
    if (!orchestrationId) return

    try {
      if (!silent) {
        setIsSaving(true)
        setSaveStatus('saving')
      }

      await save()

      if (!silent) {
        setSaveStatus('saved')
        toast.success('Draft saved successfully')
        setTimeout(() => setSaveStatus('idle'), 2000)
      }
    } catch (error) {
      console.error('Failed to save:', error)
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred'
      toast.error('Failed to save draft', {
        description: errorMessage,
      })
      if (!silent) {
        setSaveStatus('error')
        setTimeout(() => setSaveStatus('idle'), 3000)
      }
    } finally {
      if (!silent) {
        setIsSaving(false)
      }
    }
  }, [orchestrationId, save])

  // Auto-save with debounce (5 seconds)
  // Note: This is a simple implementation. For production, consider using a more
  // sophisticated approach that tracks actual changes to nodes/edges/configs
  useEffect(() => {
    if (!orchestrationId || isLoading) return

    const timer = setTimeout(() => {
      handleSave(true) // Auto-save silently
    }, 5000)

    return () => clearTimeout(timer)
  }, [orchestrationId, isLoading, orchestrationName, orchestrationDescription, handleSave]) // Trigger on metadata changes

  const handleExport = useCallback(() => {
    setIsExportDialogOpen(true)
  }, [])

  const handleMetadataSave = useCallback(async (name: string, description: string) => {
    if (!orchestrationId) return

    try {
      const { orchestrations } = await import('@donkeywork/api-client')
      await orchestrations.update(orchestrationId, { name, description })
      setOrchestrationMetadata(name, description)
    } catch (error) {
      console.error('Failed to update metadata:', error)
      // TODO: Show error toast
    }
  }, [orchestrationId, setOrchestrationMetadata])

  const handlePublish = useCallback(async () => {
    if (!orchestrationId) return

    // Confirm before publishing
    if (!window.confirm('Are you sure you want to publish this version? The next save will create a new draft.')) {
      return
    }

    try {
      setIsPublishing(true)

      // First save current state as draft to ensure latest changes are captured
      await save()

      const { orchestrations } = await import('@donkeywork/api-client')
      const publishedVersion = await orchestrations.publish(orchestrationId)

      // Merge top-level inputSchema into start node configuration
      const startNode = publishedVersion.reactFlowData.nodes.find((n: ApiNode) => n.type === 'start' || n.data?.nodeType === 'Start')
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
      store.loadOrchestration(
        orchestrationId,
        orchestrationName,
        orchestrationDescription,
        publishedVersion.id,
        false, // isDraft = false (now published)
        publishedVersion.reactFlowData,
        nodeConfigurations,
        publishedVersion.interfaces
      )

      // TODO: Show success toast "Version published! The next save will create a new draft."
    } catch (error) {
      console.error('Failed to publish:', error)
      // TODO: Show error toast
    } finally {
      setIsPublishing(false)
    }
  }, [orchestrationId, orchestrationName, orchestrationDescription, save])

  const handleLoadVersion = useCallback(async (version: OrchestrationVersion) => {
    // Merge top-level inputSchema into start node configuration
    const startNode = version.reactFlowData.nodes.find((n: ApiNode) => n.type === 'start' || n.data?.nodeType === 'Start')
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
    store.loadOrchestration(
      orchestrationId!,
      orchestrationName,
      orchestrationDescription,
      version.id,
      version.isDraft,
      version.reactFlowData,
      nodeConfigurations,
      version.interfaces
    )
  }, [orchestrationId, orchestrationName, orchestrationDescription])

  const handleCreateDraftFromVersion = useCallback(async (version: OrchestrationVersion) => {
    // Merge top-level inputSchema into start node configuration
    const startNode = version.reactFlowData.nodes.find((n: ApiNode) => n.type === 'start' || n.data?.nodeType === 'Start')
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
    store.loadOrchestration(
      orchestrationId!,
      orchestrationName,
      orchestrationDescription,
      undefined, // No version ID yet (will be created on save)
      true, // Mark as draft
      version.reactFlowData,
      nodeConfigurations,
      version.interfaces
    )
    // TODO: Show success toast "Draft created from version X. Save to persist."
  }, [orchestrationId, orchestrationName, orchestrationDescription])

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
              <h1 className="text-lg font-semibold">{orchestrationName}</h1>
              {orchestrationDescription && (
                <p className="text-sm text-muted-foreground">{orchestrationDescription}</p>
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
            disabled={!orchestrationId}
          >
            <Play className="h-4 w-4" />
            <span className="hidden sm:inline">Test</span>
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setIsInterfacesPanelOpen(true)}
          >
            <Settings2 className="h-4 w-4" />
            <span className="hidden sm:inline">Interface</span>
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
            disabled={isSaving || !orchestrationId}
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
            disabled={!orchestrationId || isPublishing}
          >
            {isPublishing ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : null}
            Publish
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
      <OrchestrationMetadataDialog
        open={isMetadataDialogOpen}
        onOpenChange={setIsMetadataDialogOpen}
        name={orchestrationName}
        description={orchestrationDescription}
        onSave={handleMetadataSave}
      />

      {/* Version History Sheet */}
      {orchestrationId && (
        <VersionHistorySheet
          open={isVersionHistoryOpen}
          onOpenChange={setIsVersionHistoryOpen}
          orchestrationId={orchestrationId}
          currentVersionId={versionId}
          onLoadVersion={handleLoadVersion}
          onCreateDraftFromVersion={handleCreateDraftFromVersion}
        />
      )}

      {/* Test Panel Sheet */}
      <Sheet open={isTestPanelOpen} onOpenChange={setIsTestPanelOpen}>
        <SheetContent side="right" className="w-full sm:max-w-2xl">
          <SheetHeader>
            <SheetTitle>Test Orchestration</SheetTitle>
          </SheetHeader>
          <div className="mt-6 h-[calc(100vh-8rem)]">
            {orchestrationId && <TestPanel orchestrationId={orchestrationId} inputSchema={inputSchema} />}
          </div>
        </SheetContent>
      </Sheet>

      {/* Export JSON Dialog */}
      <ExportJsonDialog
        open={isExportDialogOpen}
        onOpenChange={setIsExportDialogOpen}
        json={exportToJson()}
        filename={`${orchestrationName || 'orchestration'}_export.json`}
      />

      {/* Interfaces Panel */}
      <InterfacesPanel
        open={isInterfacesPanelOpen}
        onOpenChange={setIsInterfacesPanelOpen}
      />
    </div>
  )
}
