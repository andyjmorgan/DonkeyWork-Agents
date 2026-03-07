import { NoteEditorPage as SharedNoteEditorPage } from '@donkeywork/workspace'
import { useDesktopWorkspaceNav } from '../hooks/useDesktopWorkspaceNav'
import type { Page, PageParams } from '../types'

interface Props {
  noteId: string
  onNavigate: (page: Page, params?: PageParams) => void
}

export function NoteEditorPage({ noteId, onNavigate }: Props) {
  const nav = useDesktopWorkspaceNav(onNavigate)
  return <SharedNoteEditorPage noteId={noteId} nav={nav} />
}
