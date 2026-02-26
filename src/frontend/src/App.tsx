import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AppLayout } from '@/components/layout'
import { OrchestrationsPage, OrchestrationEditorPage, ApiKeysPage, CredentialsPage, OAuthClientsPage, ConnectedAccountsPage, ExecutionsPage, ExecutionDetailPage, LoginPage, LoginCallbackPage, NotFoundPage, ProfilePage, OAuthCallbackPage, ProjectsPage, ProjectDetailPage, TasksPage, NotesPage, NoteEditorPage, TaskEditorPage, MilestoneDetailPage, ChatPage, ConversationsPage, FilesPage, McpServersPage, ResearchPage, ResearchEditorPage, AgentChatPage } from '@/pages'
import { useAuthStore } from '@/store/auth'
import { useTokenRefresh } from '@/hooks/useTokenRefresh'
import { Toaster } from '@/components/ui/toaster'
import { TooltipProvider } from '@/components/ui/tooltip'
import { NotificationListener } from '@/components/providers/NotificationListener'

function TokenRefreshManager({ children }: { children: React.ReactNode }) {
  useTokenRefresh()
  return <>{children}</>
}

export default function App() {
  const { isAuthenticated } = useAuthStore()

  return (
    <BrowserRouter>
      <TooltipProvider>
        <TokenRefreshManager>
          <Toaster />
          {/* Real-time notifications - only when authenticated */}
          {isAuthenticated && <NotificationListener />}
          <Routes>
          {/* Public routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/login/callback" element={<LoginCallbackPage />} />
          <Route path="/oauth/callback" element={<OAuthCallbackPage />} />

          {/* Protected routes */}
          {isAuthenticated ? (
            <>
              {/* Editor page - full screen, no layout wrapper */}
              <Route path="/orchestrations/:id/edit" element={<OrchestrationEditorPage />} />

              {/* Regular pages with layout */}
              <Route element={<AppLayout />}>
                <Route path="/" element={<Navigate to="/orchestrations" replace />} />
                <Route path="/orchestrations" element={<OrchestrationsPage />} />
                <Route path="/chat" element={<ChatPage />} />
                <Route path="/agent-chat" element={<AgentChatPage />} />
                <Route path="/conversations" element={<ConversationsPage />} />
                <Route path="/executions" element={<ExecutionsPage />} />
                <Route path="/executions/:executionId" element={<ExecutionDetailPage />} />
                <Route path="/workspace" element={<ProjectsPage />} />
                <Route path="/workspace/:id" element={<ProjectDetailPage />} />
                <Route path="/workspace/:projectId/milestones/:milestoneId" element={<MilestoneDetailPage />} />
                <Route path="/tasks" element={<TasksPage />} />
                <Route path="/notes" element={<NotesPage />} />
                <Route path="/notes/:noteId" element={<NoteEditorPage />} />
                <Route path="/research" element={<ResearchPage />} />
                <Route path="/research/:researchId" element={<ResearchEditorPage />} />
                <Route path="/files" element={<FilesPage />} />
                <Route path="/tasks/:taskId" element={<TaskEditorPage />} />
                <Route path="/api-keys" element={<ApiKeysPage />} />
                <Route path="/credentials" element={<CredentialsPage />} />
                <Route path="/oauth-clients" element={<OAuthClientsPage />} />
                <Route path="/connected-accounts" element={<ConnectedAccountsPage />} />
                <Route path="/mcp-servers" element={<McpServersPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="*" element={<NotFoundPage />} />
              </Route>
            </>
          ) : (
            <Route path="*" element={<Navigate to="/login" replace />} />
          )}
        </Routes>
        </TokenRefreshManager>
      </TooltipProvider>
    </BrowserRouter>
  )
}
