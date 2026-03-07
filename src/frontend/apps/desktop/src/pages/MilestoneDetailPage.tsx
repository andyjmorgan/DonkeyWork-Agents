import { MilestoneDetailPage as SharedMilestoneDetailPage } from '@donkeywork/workspace'
import { useDesktopWorkspaceNav } from '../hooks/useDesktopWorkspaceNav'
import type { Page, PageParams } from '../types'

interface Props {
  projectId: string
  milestoneId: string
  onNavigate: (page: Page, params?: PageParams) => void
}

export function MilestoneDetailPage({ projectId, milestoneId, onNavigate }: Props) {
  const nav = useDesktopWorkspaceNav(onNavigate)
  return <SharedMilestoneDetailPage projectId={projectId} milestoneId={milestoneId} nav={nav} />
}
