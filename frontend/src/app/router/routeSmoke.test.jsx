import { describe, it, expect, vi, afterEach } from 'vitest'
import { act, render, cleanup } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import { Suspense } from 'react'
import i18n from '../../i18n'
import { createQueryClient } from '../../lib/query/queryClient'
import { ToastProvider } from '../../components/feedback/ToastProvider'
import { ROUTES } from './routes'
import { ROLES } from '../../features/auth/roles'

// Testing gap T-03 (audit §10 item 3): a route render smoke test over the
// REAL route registry, mounting every registered route with a mocked
// authenticated session, so an undefined-JSX-component crash (F-02/F-03 —
// exactly the class of bug that broke /app/student/units and
// /app/system/errors|backups) is caught here instead of only by manual
// clicking. This intentionally bypasses RoleGuard/PermissionGuard (those are
// separately covered by roleMatrix.test.js, T-06) — it renders each route's
// Component directly to isolate "does the component itself crash" from
// "is this role allowed here".

let mockAuth = { status: 'authenticated', user: { id: 'smoke-user', fullName: 'Smoke Test' }, role: ROLES.STUDENT }
vi.mock('../../features/auth/AuthContext', () => ({ useAuth: () => mockAuth }))

// Generic auto-mock: any method on any of these API client objects resolves
// to an empty-but-well-shaped value instead of hitting the network, so every
// page's data-fetch settles harmlessly and any render-time crash is real.
// Resolves to `[]`, not `{}`: every real *Api.js list method wraps its
// response through the shared `toItems()` helper, which is GUARANTEED to
// return an array (never a bare object) — matching that contract here avoids
// a false-positive crash in any page that calls .find()/.map()/.filter() on
// list data, which toObject()-style single-item consumers tolerate just as
// safely (an empty array is still falsy-ish and every field access on it is
// simply undefined, exactly like on `{}`).
function autoMockApi() {
  return new Proxy({}, { get: () => vi.fn(() => Promise.resolve([])) })
}
vi.mock('../../features/student/studentApi', async (importOriginal) => ({ ...(await importOriginal()), studentApi: autoMockApi() }))
vi.mock('../../features/teacher/teacherApi', async (importOriginal) => ({ ...(await importOriginal()), teacherApi: autoMockApi() }))
vi.mock('../../features/parent/parentApi', async (importOriginal) => ({ ...(await importOriginal()), parentApi: autoMockApi() }))
vi.mock('../../features/school/schoolApi', async (importOriginal) => ({ ...(await importOriginal()), schoolApi: autoMockApi() }))
vi.mock('../../features/system/systemApi', async (importOriginal) => ({ ...(await importOriginal()), systemApi: autoMockApi() }))
vi.mock('../../features/notifications/notificationsApi', async (importOriginal) => ({ ...(await importOriginal()), notificationsApi: autoMockApi() }))
vi.mock('../../features/files/filesApi', async (importOriginal) => ({ ...(await importOriginal()), filesApi: autoMockApi() }))
vi.mock('../../features/vision/visionApi', async (importOriginal) => ({ ...(await importOriginal()), visionApi: autoMockApi() }))

function concretePath(pattern) {
  return pattern.replace(/:[A-Za-z0-9_]+/g, 'smoke-test-id')
}

function roleFor(route) {
  if (route.roles && route.roles.length > 0) return route.roles[0]
  return ROLES.STUDENT
}

afterEach(() => {
  cleanup()
})

describe.each(ROUTES.map((r) => [r.path, r]))('route smoke: %s', (path, route) => {
  it('renders without throwing', async () => {
    mockAuth = { status: 'authenticated', user: { id: 'smoke-user', fullName: 'Smoke Test' }, role: roleFor(route) }
    const client = createQueryClient()
    const Component = route.Component

    await act(async () => {
      render(
        <I18nextProvider i18n={i18n}>
          <QueryClientProvider client={client}>
            <ToastProvider>
              <MemoryRouter initialEntries={[concretePath(path)]}>
                <Suspense fallback={null}>
                  <Routes>
                    <Route path={path} element={<Component {...(route.props || {})} />} />
                    {/* Catch-all so a RedirectPage route's <Navigate> lands somewhere in this
                        single-route test tree instead of logging "no routes matched" —
                        the real target route is registered app-wide; that wiring is proven
                        separately by redirectRoutes.test.js and RedirectPage.test.jsx. */}
                    <Route path="*" element={null} />
                  </Routes>
                </Suspense>
              </MemoryRouter>
            </ToastProvider>
          </QueryClientProvider>
        </I18nextProvider>,
      )
      await new Promise((resolve) => setTimeout(resolve, 30))
    })

    expect(true).toBe(true) // reaching here means no render-time throw occurred
  })
})
