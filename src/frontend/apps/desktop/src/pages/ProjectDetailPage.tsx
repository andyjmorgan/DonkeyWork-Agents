import { ProjectDetailPage as SharedProjectDetailPage } from '@donkeywork/workspace'
import { useDesktopWorkspaceNav } from '../hooks/useDesktopWorkspaceNav'
import type { Page, PageParams } from '../types'

interface Props {
  projectId: string
  onNavigate: (page: Page, params?: PageParams) => void
}

export function ProjectDetailPage({ projectId, onNavigate }: Props) {
  const nav = useDesktopWorkspaceNav(onNavigate)
  return <SharedProjectDetailPage projectId={projectId} nav={nav} />
}
