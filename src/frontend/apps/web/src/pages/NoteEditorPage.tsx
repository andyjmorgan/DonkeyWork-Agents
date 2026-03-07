import { useParams } from 'react-router-dom'
import { NoteEditorPage as SharedNoteEditorPage } from '@donkeywork/workspace'
import { useWorkspaceNav } from '@/hooks/useWorkspaceNav'

export function NoteEditorPage() {
  const { noteId } = useParams<{ noteId: string }>()
  const nav = useWorkspaceNav()
  return <SharedNoteEditorPage noteId={noteId!} nav={nav} />
}
