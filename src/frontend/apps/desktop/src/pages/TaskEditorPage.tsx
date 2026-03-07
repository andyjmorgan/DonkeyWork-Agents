import { TaskEditorPage as SharedTaskEditorPage } from '@donkeywork/workspace'
import { useDesktopWorkspaceNav } from '../hooks/useDesktopWorkspaceNav'
import type { Page, PageParams } from '../types'

interface Props {
  taskId?: string
  isNew?: boolean
  onNavigate: (page: Page, params?: PageParams) => void
}

export function TaskEditorPage({ taskId, isNew, onNavigate }: Props) {
  const nav = useDesktopWorkspaceNav(onNavigate)
  return <SharedTaskEditorPage taskId={taskId} isNew={isNew} nav={nav} />
}
