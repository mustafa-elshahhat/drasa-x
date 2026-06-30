import { useQuery } from '@tanstack/react-query'
import { STALE } from '../../lib/query/keys'

// All school-admin reads are tenant-scoped + SchoolAdmin-guarded server-side; the
// UI only renders what the backend returns and never fabricates statistics or ids.
export function useSchoolQuery(key, fn, options = {}) {
  return useQuery({
    queryKey: key,
    queryFn: ({ signal }) => fn(signal),
    staleTime: options.staleTime ?? STALE.short,
    enabled: options.enabled ?? true,
  })
}
