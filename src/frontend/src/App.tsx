import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AppLayout } from '@/components/layout'
import { AgentsPage, AgentEditorPage, ApiKeysPage, SecretsPage, ExecutionsPage, ExecutionDetailPage, LoginPage, LoginCallbackPage, NotFoundPage, ProfilePage, OAuthCallbackPage, ProjectsPage, ProjectDetailPage, TodosPage, NotesPage, NoteEditorPage, MilestoneDetailPage } from '@/pages'
import { useAuthStore } from '@/store/auth'
import { useTokenRefresh } from '@/hooks/useTokenRefresh'
import { Toaster } from '@/components/ui/toaster'

function TokenRefreshManager({ children }: { children: React.ReactNode }) {
  useTokenRefresh()
  return <>{children}</>
}

export default function App() {
  const { isAuthenticated } = useAuthStore()

  return (
    <BrowserRouter>
      <TokenRefreshManager>
        <Toaster />
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/login/callback" element={<LoginCallbackPage />} />
          <Route path="/oauth/callback" element={<OAuthCallbackPage />} />

          {/* Protected routes */}
          {isAuthenticated ? (
            <>
              {/* Editor page - full screen, no layout wrapper */}
              <Route path="/agents/:id/edit" element={<AgentEditorPage />} />

              {/* Regular pages with layout */}
              <Route element={<AppLayout />}>
                <Route path="/" element={<Navigate to="/agents" replace />} />
                <Route path="/agents" element={<AgentsPage />} />
                <Route path="/executions" element={<ExecutionsPage />} />
                <Route path="/executions/:executionId" element={<ExecutionDetailPage />} />
                <Route path="/projects" element={<ProjectsPage />} />
                <Route path="/projects/:id" element={<ProjectDetailPage />} />
                <Route path="/projects/:projectId/milestones/:milestoneId" element={<MilestoneDetailPage />} />
                <Route path="/todos" element={<TodosPage />} />
                <Route path="/notes" element={<NotesPage />} />
                <Route path="/notes/:noteId" element={<NoteEditorPage />} />
                <Route path="/api-keys" element={<ApiKeysPage />} />
                <Route path="/secrets" element={<SecretsPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="*" element={<NotFoundPage />} />
              </Route>
            </>
          ) : (
            <Route path="*" element={<Navigate to="/login" replace />} />
          )}
        </Routes>
      </TokenRefreshManager>
    </BrowserRouter>
  )
}
