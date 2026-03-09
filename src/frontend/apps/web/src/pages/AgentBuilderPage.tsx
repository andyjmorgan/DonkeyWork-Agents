import { useEffect, useState, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Download, Save, Loader2, Settings } from 'lucide-react'
import { Button, Badge } from '@donkeywork/ui'
import { AgentCanvas } from '@/components/agent-builder/AgentCanvas'
import { AgentNodePalette } from '@/components/agent-builder/AgentNodePalette'
import { AgentPropertiesPanel } from '@/components/agent-builder/AgentPropertiesPanel'
import { AgentMetadataDialog } from '@/components/agent-builder/AgentMetadataDialog'
import { useAgentBuilderStore } from '@/store/agentBuilder'
import { agentDefinitions } from '@donkeywork/api-client'
import { toast } from 'sonner'

export function AgentBuilderPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const agentId = useAgentBuilderStore((s) => s.agentId)
  const agentName = useAgentBuilderStore((s) => s.agentName)
  const agentDescription = useAgentBuilderStore((s) => s.agentDescription)
  const isReadOnly = useAgentBuilderStore((s) => s.isReadOnly)
  const isSystem = useAgentBuilderStore((s) => s.isSystem)
  const loadAgent = useAgentBuilderStore((s) => s.loadAgent)
  const setAgentMetadata = useAgentBuilderStore((s) => s.setAgentMetadata)
  const showAgentSettings = useAgentBuilderStore((s) => s.showAgentSettings)
  const reset = useAgentBuilderStore((s) => s.reset)
  const save = useAgentBuilderStore((s) => s.save)
  const exportToJson = useAgentBuilderStore((s) => s.exportToJson)

  const [isMetadataDialogOpen, setIsMetadataDialogOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const loadData = async () => {
      try {
        setIsLoading(true)
        if (id) {
          const details = await agentDefinitions.get(id)
          loadAgent(details)
        } else {
          reset()
        }
      } catch (error) {
        console.error('Failed to load agent:', error)
      } finally {
        setIsLoading(false)
      }
    }
    loadData()
  }, [id, loadAgent, reset])

  const handleBack = () => navigate('/agent-definitions')

  const handleSave = useCallback(async () => {
    if (!agentId) return
    try {
      setIsSaving(true)
      await save()
      toast.success('Agent saved successfully')
    } catch (error) {
      console.error('Failed to save:', error)
      const msg = error instanceof Error ? error.message : 'Unknown error'
      toast.error('Failed to save agent', { description: msg })
    } finally {
      setIsSaving(false)
    }
  }, [agentId, save])

  const handleMetadataSave = useCallback(
    async (name: string, description: string) => {
      setAgentMetadata(name, description)
      if (agentId) {
        try {
          await agentDefinitions.update(agentId, { name, description })
        } catch (error) {
          console.error('Failed to update metadata:', error)
        }
      }
    },
    [agentId, setAgentMetadata]
  )

  const handleExport = useCallback(() => {
    const json = exportToJson()
    const blob = new Blob([json], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${agentName || 'agent'}_export.json`
    a.click()
    URL.revokeObjectURL(url)
  }, [exportToJson, agentName])

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
          <Button variant="ghost" size="icon" onClick={handleBack} className="h-8 w-8">
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div className="flex items-center gap-3">
            <button
              onClick={() => !isReadOnly && setIsMetadataDialogOpen(true)}
              className="text-left hover:opacity-80 transition-opacity"
              disabled={isReadOnly}
            >
              <h1 className="text-lg font-semibold">{agentName}</h1>
              {agentDescription && (
                <p className="text-sm text-muted-foreground">{agentDescription}</p>
              )}
            </button>
            {isSystem && <Badge variant="secondary">System</Badge>}
            {isReadOnly && <Badge variant="outline">Read Only</Badge>}
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={handleExport}>
            <Download className="h-4 w-4" />
            <span className="hidden sm:inline">Export</span>
          </Button>
          <Button variant="outline" size="sm" onClick={showAgentSettings}>
            <Settings className="h-4 w-4" />
            <span className="hidden sm:inline">Settings</span>
          </Button>
          {!isReadOnly && (
            <Button
              size="sm"
              onClick={handleSave}
              disabled={isSaving || !agentId}
            >
              {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
              <span className="hidden sm:inline">Save</span>
            </Button>
          )}
        </div>
      </header>

      {/* Main content */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left: Node Palette */}
        {!isReadOnly && (
          <aside className="w-64 border-r border-border bg-card p-4">
            <h2 className="mb-4 text-sm font-semibold">Node Palette</h2>
            <AgentNodePalette />
          </aside>
        )}

        {/* Center: Canvas */}
        <main className="flex-1 bg-muted/20">
          <AgentCanvas />
        </main>
      </div>

      {/* Properties Panel */}
      <AgentPropertiesPanel />

      {/* Metadata Dialog */}
      <AgentMetadataDialog
        open={isMetadataDialogOpen}
        onOpenChange={setIsMetadataDialogOpen}
        name={agentName}
        description={agentDescription}
        onSave={handleMetadataSave}
      />
    </div>
  )
}
