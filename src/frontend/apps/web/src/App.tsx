import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AppLayout } from '@/components/layout'
import { OrchestrationsPage, OrchestrationEditorPage, ApiKeysPage, CredentialsPage, OAuthClientsPage, ConnectedAccountsPage, ExecutionsPage, ExecutionDetailPage, LoginPage, LoginCallbackPage, NotFoundPage, ProfilePage, OAuthCallbackPage, ProjectsPage, ProjectDetailPage, TasksPage, NotesPage, NoteEditorPage, TaskEditorPage, MilestoneDetailPage, FilesPage, McpServersPage, ResearchPage, ResearchEditorPage, AgentChatPage, ConversationsPage, SkillsPage, SandboxCredentialsPage } from '@/pages'
import { useAuthStore } from '@donkeywork/stores'
import { useTokenRefresh } from '@/hooks/useTokenRefresh'
import { Toaster } from '@/components/ui/toaster'
import { TooltipProvider } from '@donkeywork/ui'
import { NotificationListener } from '@/components/providers/NotificationListener'
import { PlatformProvider } from '@donkeywork/platform'
import { ChatConfigProvider, type ChatConfig } from '@donkeywork/chat'
import { JsonViewer } from '@/components/ui/json-viewer'
import { webPlatformConfig } from './platform/web-platform'
import { NavigateBridge } from './platform/NavigateBridge'

const chatConfig: ChatConfig = {
  renderJson: (data, opts) => (
    <JsonViewer data={data} collapsed={opts.collapsed} className={opts.className} />
  ),
}

function TokenRefreshManager({ children }: { children: React.ReactNode }) {
  useTokenRefresh()
  return <>{children}</>
}

export default function App() {
  const { isAuthenticated } = useAuthStore()

  return (
    <PlatformProvider config={webPlatformConfig}>
    <ChatConfigProvider config={chatConfig}>
    <BrowserRouter>
      <NavigateBridge>
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
                <Route path="/chat" element={<Navigate to="/agent-chat" replace />} />
                <Route path="/conversations" element={<ConversationsPage />} />
                <Route path="/agent-chat" element={<AgentChatPage />} />
                <Route path="/agent-chat/:conversationId" element={<AgentChatPage />} />
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
                <Route path="/skills" element={<SkillsPage />} />
                <Route path="/tasks/:taskId" element={<TaskEditorPage />} />
                <Route path="/api-keys" element={<ApiKeysPage />} />
                <Route path="/credentials" element={<CredentialsPage />} />
                <Route path="/oauth-clients" element={<OAuthClientsPage />} />
                <Route path="/connected-accounts" element={<ConnectedAccountsPage />} />
                <Route path="/sandbox-credentials" element={<SandboxCredentialsPage />} />
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
      </NavigateBridge>
    </BrowserRouter>
    </ChatConfigProvider>
    </PlatformProvider>
  )
}
