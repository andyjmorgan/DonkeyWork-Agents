import { useState, useEffect } from 'react'
import { Clock, CheckCircle2, FileText, Loader2 } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { agents, type AgentVersion } from '@/lib/api'

interface VersionHistorySheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  agentId: string
  currentVersionId: string | null
  onLoadVersion: (version: AgentVersion) => void
  onCreateDraftFromVersion: (version: AgentVersion) => void
}

export function VersionHistorySheet({
  open,
  onOpenChange,
  agentId,
  currentVersionId,
  onLoadVersion,
  onCreateDraftFromVersion,
}: VersionHistorySheetProps) {
  const [versions, setVersions] = useState<AgentVersion[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [loadingVersionId, setLoadingVersionId] = useState<string | null>(null)

  useEffect(() => {
    if (open && agentId) {
      loadVersions()
    }
  }, [open, agentId])

  const loadVersions = async () => {
    try {
      setIsLoading(true)
      const data = await agents.listVersions(agentId)
      setVersions(data)
    } catch (error) {
      console.error('Failed to load versions:', error)
      // TODO: Show error toast
    } finally {
      setIsLoading(false)
    }
  }

  const handleLoadVersion = async (version: AgentVersion) => {
    setLoadingVersionId(version.id)
    try {
      await onLoadVersion(version)
      onOpenChange(false)
    } catch (error) {
      console.error('Failed to load version:', error)
      // TODO: Show error toast
    } finally {
      setLoadingVersionId(null)
    }
  }

  const handleCreateDraft = async (version: AgentVersion) => {
    setLoadingVersionId(version.id)
    try {
      await onCreateDraftFromVersion(version)
      onOpenChange(false)
    } catch (error) {
      console.error('Failed to create draft:', error)
      // TODO: Show error toast
    } finally {
      setLoadingVersionId(null)
    }
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-lg">
        <SheetHeader>
          <SheetTitle>Version History</SheetTitle>
          <SheetDescription>
            View and manage all versions of this agent
          </SheetDescription>
        </SheetHeader>

        <div className="mt-6 space-y-3">
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : versions.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-8 text-center">
              <FileText className="h-8 w-8 text-muted-foreground" />
              <p className="mt-2 text-sm text-muted-foreground">
                No versions found
              </p>
            </div>
          ) : (
            versions.map((version) => {
              const isCurrentVersion = version.id === currentVersionId
              const isLoadingThis = loadingVersionId === version.id

              return (
                <div
                  key={version.id}
                  className={`rounded-lg border p-4 space-y-3 ${
                    isCurrentVersion
                      ? 'border-primary bg-primary/5'
                      : 'border-border'
                  }`}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div className="space-y-1">
                      <div className="flex items-center gap-2">
                        <h4 className="font-semibold">
                          Version {version.versionNumber}
                        </h4>
                        {version.isDraft ? (
                          <Badge variant="secondary" className="text-xs">
                            Draft
                          </Badge>
                        ) : (
                          <Badge variant="outline" className="text-xs">
                            Published
                          </Badge>
                        )}
                        {isCurrentVersion && (
                          <Badge className="text-xs">Current</Badge>
                        )}
                      </div>
                      <div className="space-y-1 text-xs text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          <span>
                            Created: {new Date(version.createdAt).toLocaleString()}
                          </span>
                        </div>
                        {version.publishedAt && (
                          <div className="flex items-center gap-1">
                            <CheckCircle2 className="h-3 w-3" />
                            <span>
                              Published:{' '}
                              {new Date(version.publishedAt).toLocaleString()}
                            </span>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    {!isCurrentVersion && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleLoadVersion(version)}
                        disabled={isLoadingThis}
                        className="flex-1"
                      >
                        {isLoadingThis ? (
                          <Loader2 className="h-3 w-3 animate-spin" />
                        ) : null}
                        View
                      </Button>
                    )}
                    {!version.isDraft && (
                      <Button
                        variant="default"
                        size="sm"
                        onClick={() => handleCreateDraft(version)}
                        disabled={isLoadingThis}
                        className="flex-1"
                      >
                        {isLoadingThis ? (
                          <Loader2 className="h-3 w-3 animate-spin" />
                        ) : null}
                        Create Draft from This
                      </Button>
                    )}
                  </div>
                </div>
              )
            })
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
