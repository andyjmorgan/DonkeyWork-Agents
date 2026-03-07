import { NotesPage as SharedNotesPage } from '@donkeywork/workspace'
import { useWorkspaceNav } from '@/hooks/useWorkspaceNav'

export function NotesPage() {
  const nav = useWorkspaceNav()
  return <SharedNotesPage nav={nav} />
}
