import { useQuery } from '@tanstack/react-query'
import { STALE } from '../../lib/query/keys'

// All system-admin reads are platform-scope + SystemAdminOnly server-side; the UI
// only renders what the backend returns and never fabricates statistics or ids.
export function useSystemQuery(key, fn, options = {}) {
  return useQuery({
    queryKey: key,
    queryFn: ({ signal }) => fn(signal),
    staleTime: options.staleTime ?? STALE.short,
    enabled: options.enabled ?? true,
  })
}
