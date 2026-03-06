import React, { useState, useEffect, useRef } from 'react'
import { Trash2, Upload, Loader2, Zap, Plus, X, ChevronRight, Folder, FolderOpen, FileText } from 'lucide-react'
import {
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@donkeywork/ui'
import { skills, type SkillItem, type SkillFileNode } from '@donkeywork/api-client'
import { cn } from '@/lib/utils'

function FileTreeNode({ node, depth }: { node: SkillFileNode; depth: number }) {
  const [open, setOpen] = useState(false)

  if (!node.isDirectory) {
    return (
      <div style={{ paddingLeft: `${depth * 16 + 8}px` }} className="flex items-center gap-2 py-1 text-sm text-muted-foreground">
        <FileText className="h-3.5 w-3.5 shrink-0" />
        <span className="truncate">{node.name}</span>
      </div>
    )
  }

  return (
    <Collapsible open={open} onOpenChange={setOpen}>
      <CollapsibleTrigger style={{ paddingLeft: `${depth * 16 + 8}px` }} className="flex w-full items-center gap-2 py-1 text-sm hover:bg-muted/50 rounded cursor-pointer">
        <ChevronRight className={cn("h-3.5 w-3.5 shrink-0 transition-transform", open && "rotate-90")} />
        {open ? <FolderOpen className="h-3.5 w-3.5 text-amber-500 shrink-0" /> : <Folder className="h-3.5 w-3.5 text-amber-500 shrink-0" />}
        <span className="truncate">{node.name}</span>
      </CollapsibleTrigger>
      <CollapsibleContent>
        {node.children?.map((child) => (
          <FileTreeNode key={child.name} node={child} depth={depth + 1} />
        ))}
      </CollapsibleContent>
    </Collapsible>
  )
}

export function SkillsPage() {
  const [allSkills, setAllSkills] = useState<SkillItem[]>([])
  const [loading, setLoading] = useState(true)
  const [deletingName, setDeletingName] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [expandedSkills, setExpandedSkills] = useState<Record<string, SkillFileNode[] | 'loading' | undefined>>({})
  const fileInputRef = useRef<HTMLInputElement>(null)

  const loadSkills = async () => {
    try {
      setLoading(true)
      const data = await skills.list()
      setAllSkills(data)
    } catch (err) {
      console.error('Failed to load skills:', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadSkills()
  }, [])

  const handleDelete = async (name: string) => {
    if (!confirm('Are you sure you want to delete this skill? This action cannot be undone.')) {
      return
    }

    try {
      setDeletingName(name)
      await skills.delete(name)
      setAllSkills(prev => prev.filter(s => s.name !== name))
    } catch (err) {
      console.error('Failed to delete skill:', err)
      alert('Failed to delete skill')
    } finally {
      setDeletingName(null)
    }
  }

  const handleUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = event.target.files
    if (!selectedFiles || selectedFiles.length === 0) return

    try {
      setUploading(true)
      setError(null)
      await skills.upload(selectedFiles[0])
      await loadSkills()
    } catch (err) {
      console.error('Failed to upload skill:', err)
      setError(err instanceof Error ? err.message : 'Failed to upload skill')
    } finally {
      setUploading(false)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    }
  }

  const toggleSkillContents = async (name: string) => {
    if (expandedSkills[name] !== undefined) {
      setExpandedSkills(prev => {
        const next = { ...prev }
        delete next[name]
        return next
      })
      return
    }
    setExpandedSkills(prev => ({ ...prev, [name]: 'loading' }))
    try {
      const contents = await skills.getContents(name)
      setExpandedSkills(prev => ({ ...prev, [name]: contents }))
    } catch {
      setExpandedSkills(prev => {
        const next = { ...prev }
        delete next[name]
        return next
      })
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Skills</h1>
          <p className="text-muted-foreground">
            Manage sandbox skills available to your agents
          </p>
        </div>
      </div>

      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">All Skills</h2>
          <div>
            <input
              ref={fileInputRef}
              type="file"
              className="hidden"
              accept=".zip"
              onChange={handleUpload}
            />
            <Button onClick={() => fileInputRef.current?.click()} disabled={uploading}>
              {uploading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Uploading...
                </>
              ) : (
                <>
                  <Upload className="h-4 w-4 mr-2" />
                  Upload Skill
                </>
              )}
            </Button>
          </div>
        </div>

        {error && (
          <div className="flex items-center justify-between rounded-lg border border-red-500/50 bg-red-500/10 px-4 py-3 text-sm text-red-500">
            <span>{error}</span>
            <Button variant="ghost" size="sm" onClick={() => setError(null)}>
              <X className="h-4 w-4" />
            </Button>
          </div>
        )}

        {loading ? (
          <div className="flex items-center justify-center rounded-lg border border-border p-12">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground mr-2" />
            <p className="text-sm text-muted-foreground">Loading skills...</p>
          </div>
        ) : allSkills.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
            <div className="rounded-full bg-muted p-4">
              <Zap className="h-8 w-8 text-muted-foreground" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">No skills yet</h3>
            <p className="mt-2 text-sm text-muted-foreground max-w-sm">
              Upload a skill zip to get started. Each skill is a folder containing a SKILL.md file.
            </p>
            <Button className="mt-4" onClick={() => fileInputRef.current?.click()} disabled={uploading}>
              <Plus className="h-4 w-4 mr-2" />
              Upload Skill
            </Button>
          </div>
        ) : (
          <>
            {/* Mobile view - card layout */}
            <div className="space-y-3 md:hidden">
              {allSkills.map((skill) => (
                <div key={skill.name} className="rounded-lg border border-border bg-card p-4 space-y-2">
                  <div className="flex items-start justify-between gap-2">
                    <button
                      onClick={() => toggleSkillContents(skill.name)}
                      className="mt-0.5 shrink-0 cursor-pointer"
                    >
                      <ChevronRight className={cn("h-4 w-4 text-muted-foreground transition-transform", expandedSkills[skill.name] !== undefined && "rotate-90")} />
                    </button>
                    <div className="space-y-1 min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <Zap className="h-4 w-4 text-violet-500 shrink-0" />
                        <span className="text-sm font-medium truncate">{skill.name}</span>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Created: </span>
                        <span>{formatDate(skill.createdAt)}</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-1 shrink-0">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDelete(skill.name)}
                        disabled={deletingName === skill.name}
                      >
                        {deletingName === skill.name ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Trash2 className="h-4 w-4 text-red-500" />
                        )}
                      </Button>
                    </div>
                  </div>
                  {expandedSkills[skill.name] !== undefined && (
                    <div className="border-t border-border pt-2 mt-2">
                      {expandedSkills[skill.name] === 'loading' ? (
                        <div className="flex items-center gap-2 py-2 pl-2">
                          <Loader2 className="h-3.5 w-3.5 animate-spin text-muted-foreground" />
                          <span className="text-sm text-muted-foreground">Loading contents...</span>
                        </div>
                      ) : (
                        (expandedSkills[skill.name] as SkillFileNode[]).map((node) => (
                          <FileTreeNode key={node.name} node={node} depth={0} />
                        ))
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>

            {/* Desktop view - table layout */}
            <div className="hidden md:block rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Created</TableHead>
                    <TableHead className="w-[80px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {allSkills.map((skill) => (
                    <React.Fragment key={skill.name}>
                      <TableRow className="cursor-pointer" onClick={() => toggleSkillContents(skill.name)}>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <ChevronRight className={cn("h-4 w-4 text-muted-foreground transition-transform shrink-0", expandedSkills[skill.name] !== undefined && "rotate-90")} />
                            <Zap className="h-4 w-4 text-violet-500 shrink-0" />
                            <span className="font-medium">{skill.name}</span>
                          </div>
                        </TableCell>
                        <TableCell>{formatDate(skill.createdAt)}</TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={(e) => { e.stopPropagation(); handleDelete(skill.name); }}
                              disabled={deletingName === skill.name}
                              title="Delete"
                            >
                              {deletingName === skill.name ? (
                                <Loader2 className="h-4 w-4 animate-spin" />
                              ) : (
                                <Trash2 className="h-4 w-4 text-red-500" />
                              )}
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                      {expandedSkills[skill.name] !== undefined && (
                        <TableRow>
                          <TableCell colSpan={3} className="p-0">
                            <div className="bg-muted/30 px-4 py-2 border-t border-border">
                              {expandedSkills[skill.name] === 'loading' ? (
                                <div className="flex items-center gap-2 py-2">
                                  <Loader2 className="h-3.5 w-3.5 animate-spin text-muted-foreground" />
                                  <span className="text-sm text-muted-foreground">Loading contents...</span>
                                </div>
                              ) : (
                                (expandedSkills[skill.name] as SkillFileNode[]).map((node) => (
                                  <FileTreeNode key={node.name} node={node} depth={0} />
                                ))
                              )}
                            </div>
                          </TableCell>
                        </TableRow>
                      )}
                    </React.Fragment>
                  ))}
                </TableBody>
              </Table>
            </div>
          </>
        )}
      </section>
    </div>
  )
}
