import { NotesPage as SharedNotesPage } from '@donkeywork/workspace'
import { useDesktopWorkspaceNav } from '../hooks/useDesktopWorkspaceNav'
import type { Page, PageParams } from '../types'

interface Props {
  onNavigate: (page: Page, params?: PageParams) => void
}

export function NotesPage({ onNavigate }: Props) {
  const nav = useDesktopWorkspaceNav(onNavigate)
  return <SharedNotesPage nav={nav} />
}
