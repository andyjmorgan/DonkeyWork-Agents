import { useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AppLayout } from '@/components/layout'
import { OrchestrationsPage, OrchestrationEditorPage, ApiKeysPage, CredentialsPage, OAuthClientsPage, ConnectedAccountsPage, ExecutionsPage, ExecutionDetailPage, LoginPage, LoginCallbackPage, NotFoundPage, ProfilePage, OAuthCallbackPage, ProjectsPage, ProjectDetailPage, TasksPage, NotesPage, NoteEditorPage, TaskEditorPage, MilestoneDetailPage, FilesPage, McpServersPage, A2aServersPage, ResearchPage, ResearchEditorPage, AgentChatPage, ConversationsPage, SkillsPage, SkillDetailPage, SandboxSettingsPage, AgentDefinitionsPage, AgentBuilderPage, PromptsPage, RecordingsPage } from '@/pages'
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

function AuthGuard({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, hasHydrated } = useAuthStore()

  useEffect(() => {
    if (hasHydrated) return

    // Force rehydration if it hasn't happened yet
    useAuthStore.persist.rehydrate()

    // Safety timeout: if hasHydrated is still false after 1s, force it
    const timer = setTimeout(() => {
      if (!useAuthStore.getState().hasHydrated) {
        useAuthStore.setState({ hasHydrated: true })
      }
    }, 1000)

    return () => clearTimeout(timer)
  }, [hasHydrated])

  if (!hasHydrated) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-foreground" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

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

          {/* Editor pages - full screen, no layout wrapper */}
          <Route path="/orchestrations/:id/edit" element={<AuthGuard><OrchestrationEditorPage /></AuthGuard>} />
          <Route path="/agent-definitions/:id/edit" element={<AuthGuard><AgentBuilderPage /></AuthGuard>} />

          {/* Regular pages with layout */}
          <Route element={<AuthGuard><AppLayout /></AuthGuard>}>
            <Route path="/" element={<Navigate to="/workspace" replace />} />
            <Route path="/orchestrations" element={<OrchestrationsPage />} />
            <Route path="/agent-definitions" element={<AgentDefinitionsPage />} />
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
            <Route path="/skills/:name" element={<SkillDetailPage />} />
            <Route path="/tasks/:taskId" element={<TaskEditorPage />} />
            <Route path="/api-keys" element={<ApiKeysPage />} />
            <Route path="/recordings" element={<RecordingsPage />} />
            <Route path="/credentials" element={<CredentialsPage />} />
            <Route path="/oauth-clients" element={<OAuthClientsPage />} />
            <Route path="/connected-accounts" element={<ConnectedAccountsPage />} />
            <Route path="/sandbox-settings" element={<SandboxSettingsPage />} />
            <Route path="/prompts" element={<PromptsPage />} />
            <Route path="/mcp-servers" element={<McpServersPage />} />
            <Route path="/a2a-servers" element={<A2aServersPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
        </TokenRefreshManager>
      </TooltipProvider>
      </NavigateBridge>
    </BrowserRouter>
    </ChatConfigProvider>
    </PlatformProvider>
  )
}
