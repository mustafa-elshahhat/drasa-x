import { useQuery } from '@tanstack/react-query'
import { STALE } from '../../lib/query/keys'

// Shared teacher-portal query hook (extracted from the former dispatcher).
// React Query forbids a query fn resolving to `undefined`; normalize to `null`.
export function useTeacherQuery(key, fn, options = {}) {
  return useQuery({
    queryKey: key,
    queryFn: async ({ signal }) => (await fn(signal)) ?? null,
    staleTime: options.staleTime ?? STALE.short,
    enabled: options.enabled ?? true,
  })
}
