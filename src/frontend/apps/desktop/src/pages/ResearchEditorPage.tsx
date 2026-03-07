import { ResearchEditorPage as SharedResearchEditorPage } from '@donkeywork/workspace'
import { useDesktopWorkspaceNav } from '../hooks/useDesktopWorkspaceNav'
import type { Page, PageParams } from '../types'

interface Props {
  researchId?: string
  isNew?: boolean
  onNavigate: (page: Page, params?: PageParams) => void
}

export function ResearchEditorPage({ researchId, isNew, onNavigate }: Props) {
  const nav = useDesktopWorkspaceNav(onNavigate)
  return <SharedResearchEditorPage researchId={researchId} isNew={isNew} nav={nav} />
}
