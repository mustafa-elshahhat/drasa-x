# DerasaX Frontend Route Map (before / after)

All **URLs, guards, roles, permissions, and titleKeys are unchanged**. Only the component
each route resolves to changed: from one giant `PortalPage` + `props.view` dispatcher per role
to real, lazily code-split page modules.

## Registry structure

- Before: a single `src/app/router/routes.js` (122 entries, JSDoc-typed).
- After: typed `src/app/router/route.types.ts` + per-area files
  `routes.{public,auth,shared,vision,student,teacher,parent,school,system}.tsx`, aggregated by
  `routes.tsx` (still exports `ROUTES` + `findRouteByPath` from the same `./router/routes` path).

## Dispatcher → page-module map

| Area | Before (Component / `props.view`) | After (real lazy modules) | Routes |
|---|---|---|---|
| Student | `StudentPortalPage` + 25 views | 21 modules under `pages/student/<area>/` | 33 |
| Teacher | `TeacherPortalPage` + 12 views | 12 modules under `pages/teacher/<area>/` | 17 |
| Parent | `ParentPortalPage` + 11 views | 11 modules under `pages/parent/<area>/` | 12 |
| School admin | `SchoolAdminPortalPage` + 25 views | 22 modules under `pages/school/<area>/` | 25 |
| System admin | `SystemAdminPortalPage` + 18 views | 18 modules under `pages/system/<area>/` | 18 |
| Communication | `CommunicationPage` + 4 views | 4 modules under `pages/app/communication/` | 4 |

Each old dispatcher remains on disk as a **thin compatibility facade** (`view → module`) imported
**only by its existing test** — no production route references it.

### Notable preserved mappings
- Routes that shared a view still load the same module and read params internally
  (e.g. `/app/student/units` and `/app/student/units/:unitId` → `StudentUnitsPage`).
- `StudentProgressPage` serves `/progress`, `/recommendations`, `/engagement` via `props:{mode}`
  (a real page param, not a view dispatcher).
- `SchoolUsersPage` serves `/users` (`props:{role:null,canCreate:true}`), `/students`, `/teachers`,
  `/parents` (`props:{role:'…'}`).
- `/app/student/profile`, `/app/parent/profile` still use the shared `pages/app/ProfilePage`.

## Areas unchanged (already split before migration)

| Area | Routes | Components |
|---|---|---|
| Public marketing (`layout:'public'`) | 5 | `HomePage`, `RequestDemoPage`, `EventsPage`, `ActivitiesPage`, `NewsPage` |
| Auth | 1 | `LoginPage` (`anonymousOnly`) |
| Account/shell | 3 | `AppHome`, `ProfilePage`, `SecurityPage` |
| Vision (CV) | 4 | `VisionStaffPage` (Teacher/SchoolAdmin), `StudentVisionPage`, `ParentVisionPage` |
| Status/fallback | 4 | `Forbidden`, `SessionExpired`, `SuspendedTenant`, `NotFound` (in `App.tsx`) |

## Guards (unchanged — `App.tsx` `guardElement`)
`roles` → `<RoleGuard>` (takes precedence); else `permission` → `<PermissionGuard>`. `/app`
wrapped `<ProtectedRoute><AppShell/></ProtectedRoute>`. Role scoping preserved exactly:
Student-only, Teacher+SchoolAdmin, Parent-only, SchoolAdmin-only, SystemAdmin-only.
