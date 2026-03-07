import { useParams } from 'react-router-dom'
import { TaskEditorPage as SharedTaskEditorPage } from '@donkeywork/workspace'
import { useWorkspaceNav } from '@/hooks/useWorkspaceNav'

export function TaskEditorPage() {
  const { taskId } = useParams<{ taskId: string }>()
  const nav = useWorkspaceNav()
  const isNew = taskId === 'new'
  return <SharedTaskEditorPage taskId={isNew ? undefined : taskId} isNew={isNew} nav={nav} />
}
