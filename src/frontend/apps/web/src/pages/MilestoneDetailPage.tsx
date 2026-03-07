import { useParams } from 'react-router-dom'
import { MilestoneDetailPage as SharedMilestoneDetailPage } from '@donkeywork/workspace'
import { useWorkspaceNav } from '@/hooks/useWorkspaceNav'

export function MilestoneDetailPage() {
  const { projectId, milestoneId } = useParams<{ projectId: string; milestoneId: string }>()
  const nav = useWorkspaceNav()
  return <SharedMilestoneDetailPage projectId={projectId!} milestoneId={milestoneId!} nav={nav} />
}
