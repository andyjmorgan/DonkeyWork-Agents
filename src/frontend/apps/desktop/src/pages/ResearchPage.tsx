import { ResearchPage as SharedResearchPage } from '@donkeywork/workspace'
import { useDesktopWorkspaceNav } from '../hooks/useDesktopWorkspaceNav'
import type { Page, PageParams } from '../types'

interface Props {
  onNavigate: (page: Page, params?: PageParams) => void
}

export function ResearchPage({ onNavigate }: Props) {
  const nav = useDesktopWorkspaceNav(onNavigate)
  return <SharedResearchPage nav={nav} />
}
