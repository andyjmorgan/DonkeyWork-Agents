import { useParams } from 'react-router-dom'
import { ResearchEditorPage as SharedResearchEditorPage } from '@donkeywork/workspace'
import { useWorkspaceNav } from '@/hooks/useWorkspaceNav'

export function ResearchEditorPage() {
  const { researchId } = useParams<{ researchId: string }>()
  const nav = useWorkspaceNav()
  const isNew = researchId === 'new'
  return <SharedResearchEditorPage researchId={isNew ? undefined : researchId} isNew={isNew} nav={nav} />
}
