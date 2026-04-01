const MAX_RETRIES = 10;
const MAX_DELAY_MS = 30_000;

export { MAX_RETRIES };

export function getReconnectDelay(attempt: number): number {
  if (attempt === 0) return 0;
  const base = Math.min(MAX_DELAY_MS, Math.pow(2, attempt - 1) * 1000);
  const jitter = 0.8 + Math.random() * 0.4;
  return Math.round(base * jitter);
}

export interface ReconnectGuardParams {
  intentionalClose: boolean;
  conversationId: string | null;
  isAuthenticated: boolean;
  attemptCount: number;
  isMounted: boolean;
}

export function shouldAttemptReconnect(params: ReconnectGuardParams): boolean {
  if (params.intentionalClose) return false;
  if (!params.conversationId) return false;
  if (!params.isAuthenticated) return false;
  if (params.attemptCount >= MAX_RETRIES) return false;
  if (!params.isMounted) return false;
  return true;
}
