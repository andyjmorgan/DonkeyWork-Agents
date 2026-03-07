import { TasksPage as SharedTasksPage } from '@donkeywork/workspace'
import { useWorkspaceNav } from '@/hooks/useWorkspaceNav'

export function TasksPage() {
  const nav = useWorkspaceNav()
  return <SharedTasksPage nav={nav} />
}
