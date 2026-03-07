import { ProjectsPage as SharedProjectsPage } from '@donkeywork/workspace'
import { useWorkspaceNav } from '@/hooks/useWorkspaceNav'

export function ProjectsPage() {
  const nav = useWorkspaceNav()
  return <SharedProjectsPage nav={nav} />
}
