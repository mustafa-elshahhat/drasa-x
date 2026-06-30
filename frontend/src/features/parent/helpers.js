import { useQuery } from '@tanstack/react-query'
import { STALE } from '../../lib/query/keys'

export const ATTENDANCE_TONE = { present: 'success', late: 'warning', absent: 'danger', excused: 'info' }

// All parent reads are relationship-authorized server-side; the UI only renders
// what the backend returns and never fabricates child data. React Query forbids a
// query fn resolving to `undefined`; normalize to `null`.
export function useParentQuery(key, fn, options = {}) {
  return useQuery({
    queryKey: key,
    queryFn: async ({ signal }) => (await fn(signal)) ?? null,
    staleTime: options.staleTime ?? STALE.short,
    enabled: options.enabled ?? true,
  })
}
