import { useParams } from 'react-router-dom'
import { ProjectDetailPage as SharedProjectDetailPage } from '@donkeywork/workspace'
import { useWorkspaceNav } from '@/hooks/useWorkspaceNav'

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>()
  const nav = useWorkspaceNav()
  return <SharedProjectDetailPage projectId={id!} nav={nav} />
}
