// =============================================================================
// React Query client factory (Phase 7 §8). Centralizes cache ownership, retry,
// and stale-time policy. Auth errors are never retried (the client's refresh
// flow already handles a single 401 retry); other 4xx are not retried either.
// =============================================================================
import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '../api/problemDetails'
import { STALE } from './keys'

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: STALE.short,
        gcTime: STALE.medium,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          // Don't retry client errors (4xx) — they won't succeed on retry.
          if (error instanceof ApiError && error.status >= 400 && error.status < 500) return false
          return failureCount < 2
        },
      },
      mutations: {
        retry: false,
      },
    },
  })
}
