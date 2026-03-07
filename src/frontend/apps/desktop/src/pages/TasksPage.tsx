import { TasksPage as SharedTasksPage } from '@donkeywork/workspace'
import { useDesktopWorkspaceNav } from '../hooks/useDesktopWorkspaceNav'
import type { Page, PageParams } from '../types'

interface Props {
  onNavigate: (page: Page, params?: PageParams) => void
}

export function TasksPage({ onNavigate }: Props) {
  const nav = useDesktopWorkspaceNav(onNavigate)
  return <SharedTasksPage nav={nav} />
}
