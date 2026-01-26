import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AppLayout } from '@/components/layout'
import { AgentsPage, AgentEditorPage, ApiKeysPage, SecretsPage, LoginPage, LoginCallbackPage, NotFoundPage, ProfilePage, OAuthCallbackPage } from '@/pages'
import { useAuthStore } from '@/store/auth'

export default function App() {
  const { isAuthenticated } = useAuthStore()

  return (
    <BrowserRouter>
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
    </BrowserRouter>
  )
}
