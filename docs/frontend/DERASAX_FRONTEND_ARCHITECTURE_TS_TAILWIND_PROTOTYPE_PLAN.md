# DerasaX Frontend Architecture + TypeScript + Tailwind + Prototype Alignment Plan

Repository: `https://github.com/mustafa-elshahhat/drasa-x`  
Target app: `frontend/`  
Prototype source of truth: `derasax-presentation-prototype/project/DerasaX Prototype.html`  
Document purpose: execution plan + coding-agent prompt for converting the current unified DerasaX frontend into a maintainable, typed, Tailwind-based, prototype-aligned frontend without changing backend contracts or product behavior.

---

## 0. Executive Decision

This is a **frontend maintainability and design-system migration phase**, not a product rewrite.

The intended outcome is:

- Existing routes keep working.
- Existing auth, role guards, permission guards, i18n, API calls, tenant behavior, and PWA safety remain intact.
- Oversized portal files are split into real page modules.
- Shared UI primitives are extracted correctly.
- Feature-specific components, hooks, mappers, API modules, and types are organized by feature.
- TypeScript is introduced gradually in high-value boundaries first.
- Tailwind CSS is introduced correctly through Vite and backed by DerasaX design tokens.
- The visual system is aligned to the bundled DerasaX prototype.
- The production frontend stops relying on mock/fallback data as real data.
- CSS is split into tokens/base/layout/components/features instead of a single growing global file.
- All verification commands pass or true environment blockers are documented with exact evidence.

---

## 1. Confirmed Current Frontend Facts

Based on direct repository inspection:

- `frontend/package.json` uses React 19, Vite 7, React Router 7, React Query, i18next, Recharts, Lucide, Vitest, and Playwright.
- `frontend/package.json` currently has no Tailwind dependency and no `typescript` dependency.
- `frontend/vite.config.js` is JavaScript and currently uses `@vitejs/plugin-react` and `vite-plugin-pwa`.
- Vitest coverage currently includes `src/**/*.{js,jsx}` and excludes `tsx/ts` until updated.
- `frontend/src/app/router/routes.js` is a central route registry.
- Many portal routes currently point to dispatcher components such as:
  - `StudentPortalPage.jsx`
  - `TeacherPortalPage.jsx`
  - `ParentPortalPage.jsx`
  - `SchoolAdminPortalPage.jsx`
  - `SystemAdminPortalPage.jsx`
- Many routes pass `props: { view: '...' }`, which keeps the files large and weakens route-level code splitting.
- `frontend/src/app/App.jsx` renders routes from metadata and wraps authenticated routes with the existing guards and app shell.
- The prototype file is a bundled HTML artifact and must be inspected/unpacked/read before visual migration.

---

## 2. Non-Negotiable Project Rules

### 2.1 Git rules

- Do not create Git commits.
- Do not push.
- Do not amend, rebase, squash, tag, or rewrite history.
- Keep all work as local uncommitted changes.

### 2.2 Scope rules

This phase is frontend-only unless a tiny documentation or frontend test-config change is required.

Do not modify:

- Backend code.
- AI service code.
- Database migrations.
- Docker/infrastructure scripts unrelated to frontend verification.
- API endpoints.
- Auth contracts.
- Role/permission semantics.
- Tenant-isolation assumptions.

### 2.3 Behavior rules

Do not change:

- Public route URLs.
- Authenticated route URLs.
- Role guards.
- Permission guards.
- Suspended/session-expired/forbidden behavior.
- API payload contracts.
- i18n language behavior.
- RTL/LTR switching behavior.
- PWA rule that backend `/api/**` responses are never cached.

### 2.4 Test rules

Forbidden:

- Deleting tests to make the suite pass.
- Skipping tests without a documented true blocker.
- Weakening assertions.
- Disabling lint/typecheck rules to hide errors.
- Removing routes/features because migration is hard.

### 2.5 Production data rules

Forbidden in production UI:

- Mock teacher/student names presented as real data.
- Fake progress/points/grades/subjects shown as backend results.
- Hardcoded tenant/user IDs.
- LocalStorage as authoritative store for educational records.
- Browser calls directly to AI services.

Allowed only under explicit demo/test boundaries:

- `src/test/fixtures/`
- `src/demo/`
- `VITE_ENABLE_DEMO_DATA=true`
- Storybook/demo-only assets, if introduced later.

---

## 3. Target Frontend Architecture

Recommended target structure:

```text
frontend/src/
  app/
    App.tsx
    main.tsx
    providers/
    router/
      route.types.ts
      routes.tsx
      routes.public.tsx
      routes.auth.tsx
      routes.shared.tsx
      routes.student.tsx
      routes.teacher.tsx
      routes.parent.tsx
      routes.school.tsx
      routes.system.tsx
  layouts/
    AppShell.tsx
    PublicLayout.tsx
    shell/
  pages/
    public/
    auth/
    app/
    status/
    student/
    teacher/
    parent/
    school/
    system/
    vision/
  features/
    auth/
      api/
      components/
      guards/
      hooks/
      types/
    student/
      api/
      components/
      hooks/
      mappers/
      types/
    teacher/
    parent/
    school-admin/
    system-admin/
    communication/
    vision/
    gamification/
  shared/
    ui/
    form/
    data-display/
    feedback/
    navigation/
    layout/
    hooks/
    utils/
    types/
  styles/
    tailwind.css
    tokens.css
    base.css
    layout.css
    utilities.css
    legacy.css
    components/
    features/
  test/
    fixtures/
    setup.ts
```

Notes:

- `pages/` should contain page orchestration only.
- `features/` should contain domain-specific UI, hooks, API wrappers, mappers, schemas, and types.
- `shared/` should contain only truly reusable cross-feature primitives.
- `styles/legacy.css` is temporary and should shrink over time.
- Old `.jsx` files may coexist during migration, but new files should be `.ts` or `.tsx`.

---

## 4. Design System Direction

### 4.1 Prototype source

The visual reference is:

```text
derasax-presentation-prototype/project/DerasaX Prototype.html
```

Before implementing visual migration:

- Inspect the bundled HTML.
- Unpack or render it if necessary.
- Extract DerasaX colors, typography feel, spacing, cards, shell, navigation, dashboards, buttons, data display, Arabic/English layout behavior, and responsive patterns.
- Document extracted design tokens in `docs/frontend/PROTOTYPE_DESIGN_AUDIT.md`.

### 4.2 Token categories

Create stable tokens for:

- Brand colors.
- Semantic colors.
- Role accent colors.
- Surface/background layers.
- Text colors.
- Border colors.
- Spacing scale.
- Radius scale.
- Shadows/elevation.
- Font families.
- Font sizes.
- Layout widths.
- Sidebar/header dimensions.
- Z-index layers.
- Motion durations.

### 4.3 Tailwind and tokens

Tailwind utilities should consume DerasaX tokens rather than introducing unrelated visual values.

Recommended approach:

- Keep CSS variables in `styles/tokens.css`.
- Import Tailwind in `styles/tailwind.css`.
- Use Tailwind utilities for layout, spacing, typography, and component variants.
- Use CSS variables for brand/role/theme values.
- Keep a small number of semantic component classes when repeated patterns are clearer than long utility strings.

---

## 5. TypeScript Migration Strategy

This is gradual migration, not a risky full rewrite.

### First TypeScript targets

1. Route metadata and route arrays.
2. Shared UI component props.
3. API DTOs and typed client responses.
4. React Query keys and hook outputs.
5. Form payloads and validation schemas.
6. Mapper functions from backend DTOs to UI view models.
7. Test setup and high-value tests.

### Required TypeScript setup

Add or update:

```text
frontend/tsconfig.json
frontend/tsconfig.node.json or equivalent if needed
frontend/src/vite-env.d.ts
frontend/vite.config.ts
frontend/src/main.tsx
frontend/src/app/App.tsx
```

Add scripts:

```json
{
  "typecheck": "tsc --noEmit"
}
```

Update Vitest coverage include:

```text
src/**/*.{js,jsx,ts,tsx}
```

Update test setup from:

```text
src/test/setup.js
```

to either:

```text
src/test/setup.ts
```

or keep JS temporarily while supporting TS test files.

---

## 6. Tailwind Migration Strategy

### Required package changes

Inside `frontend/`, add Tailwind using the current Vite plugin approach:

```bash
npm install tailwindcss @tailwindcss/vite
npm install -D typescript
```

If the project already has a compatible TypeScript dependency after inspection, do not duplicate it.

### Vite config migration

Move from:

```text
vite.config.js
```

to:

```text
vite.config.ts
```

Preserve all existing PWA safety behavior, especially:

- Only static app shell assets are precached.
- `/api/**` uses `NetworkOnly`.
- The browser never caches authenticated backend API responses.

Add Tailwind plugin alongside React and PWA.

### CSS entry

Create:

```text
src/styles/tailwind.css
```

with:

```css
@import "tailwindcss";
@import "./tokens.css";
@import "./base.css";
@import "./layout.css";
@import "./utilities.css";
@import "./legacy.css";
```

Then update the app entry to import this instead of directly importing one giant app stylesheet.

---

## 7. Phase Plan

## Phase 0 — Baseline Audit and Safety Map

### Objective
Build an evidence-based migration map before touching code.

### Tasks

- Inspect `frontend/package.json`.
- Inspect `frontend/vite.config.js`.
- Inspect `frontend/src/app/App.jsx`.
- Inspect `frontend/src/app/router/routes.js`.
- Inspect `frontend/src/styles/app.css` and other global CSS files.
- Inspect all portal dispatcher files.
- Inspect existing shared UI components.
- Inspect existing feature API modules.
- Inspect prototype file and document what can be extracted.
- Identify all routes that use `props.view`.
- Identify large files and approximate line counts.
- Identify inline style clusters.
- Identify mock/fallback production data.
- Identify repeated UI patterns suitable for shared components.

### Required output

Create:

```text
docs/frontend/FRONTEND_ARCHITECTURE_BASELINE_AUDIT.md
```

Must include:

- Current route map.
- Current portal dispatcher map.
- CSS inventory.
- Component inventory.
- Mock/fallback data inventory.
- Inline style inventory.
- Prototype design extraction notes.
- Risk register.
- Proposed migration sequence.

### Completion gate

No code migration starts until this audit exists.

---

## Phase 1 — Tooling Foundation: TypeScript + Tailwind

### Objective
Enable TS and Tailwind without changing route behavior or visuals yet.

### Tasks

- Add `typescript` as a dev dependency if missing.
- Add `tailwindcss` and `@tailwindcss/vite`.
- Convert `vite.config.js` to `vite.config.ts`.
- Preserve React plugin.
- Preserve PWA plugin and all existing runtime caching safety rules.
- Add Tailwind Vite plugin.
- Add `tsconfig.json` with Vite-compatible settings.
- Add `src/vite-env.d.ts`.
- Add `npm run typecheck`.
- Update Vitest coverage include for `ts/tsx`.
- Create `src/styles/tailwind.css`.
- Create `src/styles/tokens.css`, `base.css`, `layout.css`, `utilities.css`, `legacy.css`.
- Move existing `app.css` content into `legacy.css` temporarily without losing styles.
- Import `tailwind.css` from the app entry.

### Verification

```bash
cd frontend
npm install
npm run lint
npm run typecheck
npm test
npm run build
```

### Completion gate

The app builds and tests pass with TS/Tailwind enabled, before functional refactoring begins.

---

## Phase 2 — Router Type Safety and Route File Split

### Objective
Turn routing into a typed, modular route registry while preserving all URLs and guards.

### Tasks

- Create `src/app/router/route.types.ts`.
- Convert route metadata to typed structures.
- Split routes into:
  - `routes.public.tsx`
  - `routes.auth.tsx`
  - `routes.shared.tsx`
  - `routes.student.tsx`
  - `routes.teacher.tsx`
  - `routes.parent.tsx`
  - `routes.school.tsx`
  - `routes.system.tsx`
  - `routes.vision.tsx` if useful
- Create `routes.tsx` that only combines route groups.
- Convert `App.jsx` to `App.tsx`.
- Preserve `ProtectedRoute`, `RoleGuard`, `PermissionGuard`, and `AnonymousOnly` behavior.
- Preserve route metadata fields: path, titleKey, requiresAuth, anonymousOnly, layout, roles, permission, Component, props where still temporary.
- Add a route inventory assertion or test if practical.

### Verification

```bash
cd frontend
npm run lint
npm run typecheck
npm test
npm run build
```

### Completion gate

All existing routes still resolve, and route files are grouped without changing behavior.

---

## Phase 3 — Shared UI Foundation

### Objective
Create reusable UI primitives before splitting portals deeply.

### Tasks

Create or normalize shared primitives:

```text
shared/ui/Button.tsx
shared/ui/Card.tsx
shared/ui/PageHeader.tsx
shared/ui/SectionHeader.tsx
shared/ui/Badge.tsx
shared/ui/Avatar.tsx
shared/ui/Tabs.tsx
shared/ui/Dialog.tsx
shared/ui/Drawer.tsx
shared/ui/Dropdown.tsx
shared/form/FormField.tsx
shared/form/TextField.tsx
shared/form/SelectField.tsx
shared/form/DateField.tsx
shared/data-display/DataTable.tsx
shared/data-display/MetricCard.tsx
shared/data-display/StatGrid.tsx
shared/feedback/LoadingState.tsx
shared/feedback/ErrorState.tsx
shared/feedback/EmptyState.tsx
shared/feedback/QueryState.tsx
shared/layout/PageShell.tsx
shared/layout/PageSection.tsx
```

### Rules

- Shared components must be generic and role-neutral.
- Shared components must use typed props.
- Shared components must support RTL/LTR naturally.
- Shared components must use DerasaX tokens/Tailwind utilities.
- Do not move domain-specific logic into shared components.
- Do not create abstraction layers that are not used.

### Verification

- Add or update component tests where existing test style supports it.
- Run lint/typecheck/test/build.

### Completion gate

At least the high-frequency UI patterns can be consumed by portal pages without changing product behavior.

---

## Phase 4 — CSS Architecture and Token Migration

### Objective
Stop `app.css` from being the uncontrolled global styling sink.

### Tasks

- Move design variables into `styles/tokens.css`.
- Move reset/base app styles into `styles/base.css`.
- Move shell/page layout rules into `styles/layout.css`.
- Move true utilities into `styles/utilities.css`.
- Move remaining old styles into `styles/legacy.css` temporarily.
- Create `styles/components/` for reusable non-Tailwind component classes only when needed.
- Create `styles/features/` for feature-specific legacy CSS during migration.
- Remove duplicate color/spacing values when tokens exist.
- Replace repeated inline styles with Tailwind classes or component variants.
- Keep dynamic inline styles only where values are computed at runtime.

### Completion gate

- `legacy.css` exists but is clearly temporary.
- New or migrated pages do not add new large global CSS blocks.
- Tokens are the source of truth for brand, role, theme, and layout values.

---

## Phase 5 — Student Portal Split

### Objective
Replace `StudentPortalPage.jsx + props.view` with real page modules.

### Target pages

Create actual page components for all student routes, including at minimum:

```text
pages/student/dashboard/StudentDashboardPage.tsx
pages/student/subjects/StudentSubjectsPage.tsx
pages/student/subjects/StudentSubjectDetailsPage.tsx
pages/student/units/StudentUnitsPage.tsx
pages/student/lessons/StudentLessonsPage.tsx
pages/student/materials/StudentMaterialPage.tsx
pages/student/homework/StudentHomeworkPage.tsx
pages/student/quizzes/StudentQuizzesPage.tsx
pages/student/quizzes/StudentQuizDetailsPage.tsx
pages/student/quizzes/StudentQuizAttemptPage.tsx
pages/student/quizzes/StudentQuizResultPage.tsx
pages/student/tutor/StudentTutorPage.tsx
pages/student/recommendations/StudentRecommendationsPage.tsx
pages/student/progress/StudentProgressPage.tsx
pages/student/attendance/StudentAttendancePage.tsx
pages/student/engagement/StudentEngagementPage.tsx
pages/student/communities/StudentCommunitiesPage.tsx
pages/student/competitions/StudentCompetitionsPage.tsx
pages/student/office-hours/StudentOfficeHoursPage.tsx
pages/student/notifications/StudentNotificationsPage.tsx
pages/student/announcements/StudentAnnouncementsPage.tsx
pages/student/suggestions/StudentSuggestionsPage.tsx
pages/student/badges/StudentBadgesPage.tsx
pages/student/settings/StudentSettingsPage.tsx
```

### Feature extraction

Move reusable/domain-specific student code to:

```text
features/student/api/
features/student/hooks/
features/student/components/
features/student/mappers/
features/student/types/
```

### Required fixes

- Replace incorrect side-effect-in-state-initializer patterns with `useEffect`.
- Remove production mock data fallbacks.
- Use typed query hooks.
- Use `LoadingState`, `ErrorState`, `EmptyState` consistently.
- Use shared UI components where appropriate.
- Route registry must lazy-load the actual page component, not the student dispatcher.

### Completion gate

No normal student route should require `StudentPortalPage` as a view dispatcher.

---

## Phase 6 — Teacher and Parent Portal Split

### Objective
Apply the same page/feature split to teacher and parent portals.

### Teacher tasks

Create actual teacher page modules for dashboard, classes, subjects, units, lessons, students, quizzes, quiz generator, assignments, submissions, gradebook, pain points, office hours, communities, competitions, badges, announcements, notifications, settings, and profile-linked routes where applicable.

Move code to:

```text
features/teacher/api/
features/teacher/hooks/
features/teacher/components/
features/teacher/mappers/
features/teacher/types/
```

### Parent tasks

Create actual parent page modules for dashboard, children, child details, progress, grades, attendance, engagement, messages, document requests, announcements, notifications, settings, and profile-linked routes where applicable.

Move code to:

```text
features/parent/api/
features/parent/hooks/
features/parent/components/
features/parent/mappers/
features/parent/types/
```

### Completion gate

No normal teacher or parent route should require a role portal dispatcher with `props.view`.

---

## Phase 7 — School Admin and System Admin Portal Split

### Objective
Split large admin portal files while preserving sensitive admin behavior.

### School admin tasks

Create actual page modules for dashboard, users, students, teachers, parents, relationships, grades, classes, subjects, curriculum, announcements, document requests, communities, competitions, reports, AI usage, storage, subscription, support, audit log, settings, and school profile.

Move code to:

```text
features/school-admin/api/
features/school-admin/hooks/
features/school-admin/components/
features/school-admin/mappers/
features/school-admin/types/
```

### System admin tasks

Create actual page modules for platform dashboard, tenants, tenant details, onboarding, plans, subscriptions, usage, AI usage, storage, support tickets, feature flags, global announcements, health, errors, audit logs, security events, backups, and settings.

Move code to:

```text
features/system-admin/api/
features/system-admin/hooks/
features/system-admin/components/
features/system-admin/mappers/
features/system-admin/types/
```

### Extra safety

- Do not weaken admin-only route guards.
- Do not expose system-admin views to tenant roles.
- Do not fake health/security/backup data as real if backend is unavailable.

### Completion gate

No school/system admin route should depend on a giant dispatcher page.

---

## Phase 8 — Shared Communication and Vision Cleanup

### Objective
Remove remaining dispatcher patterns where practical and align shared app surfaces.

### Tasks

- Review `CommunicationPage` and split if it uses view dispatching for notifications, preferences, messages, and message thread.
- Review vision pages for shared staff/student/parent patterns.
- Extract reusable communication components to `features/communication/`.
- Extract reusable vision components to `features/vision/`.
- Preserve SignalR behavior and message/thread contracts.
- Preserve CV/vision route guards.

### Completion gate

Shared surfaces follow the same architecture rules as portal surfaces.

---

## Phase 9 — Prototype Design Alignment

### Objective
Apply the prototype design language across the migrated architecture.

### Tasks

- Create `docs/frontend/PROTOTYPE_DESIGN_AUDIT.md` if not created in Phase 0.
- Map prototype patterns to actual components:
  - App shell.
  - Sidebar.
  - Header/topbar.
  - Dashboard hero/summary area.
  - Metric cards.
  - Content cards.
  - Data tables.
  - Forms.
  - Tabs.
  - Empty states.
  - Status pages.
  - Public marketing pages.
  - Auth pages.
- Implement design through shared components/tokens, not copy-pasted page CSS.
- Preserve responsive behavior.
- Preserve Arabic RTL and English LTR behavior.
- Ensure public/auth/portal areas share the same DerasaX brand without forcing identical layouts where the prototype differentiates them.

### Completion gate

All migrated pages use the shared prototype-aligned UI primitives instead of one-off CSS.

---

## Phase 10 — Production Data Cleanup

### Objective
Ensure the UI is honest and production-safe.

### Tasks

- Remove fake production fallbacks from pages.
- Replace fake data with empty/loading/error states.
- Move demo data into `src/test/fixtures/` or `src/demo/`.
- Add a clearly gated demo-data mode only if necessary.
- Remove hardcoded user/teacher/student IDs.
- Remove hardcoded progress, grades, points, and recommendations from production views.
- Ensure every API failure is displayed as a real error or degraded state, not hidden behind fake values.

### Completion gate

Production UI never presents mock data as real backend data.

---

## Phase 11 — Tests, Accessibility, and Regression Coverage

### Objective
Make the refactor measurable and safe.

### Tasks

- Add/update route smoke tests.
- Add/update shared UI tests.
- Add/update QueryState/EmptyState/ErrorState tests.
- Add/update role route guard tests if current test structure supports it.
- Add basic accessibility checks where current test stack supports it.
- Update Playwright specs only when required by stable UI semantics.
- Do not weaken e2e coverage.
- Ensure TypeScript files are covered by lint/typecheck/test/build.

### Required verification

```bash
cd frontend
npm run lint
npm run typecheck
npm test
npm run build
npm run test:e2e
```

If e2e requires backend and backend is unavailable, document:

- Exact command run.
- Exact error/blocker.
- Which tests were not executed.
- Which unit/component checks cover the changed frontend behavior.

---

## Phase 12 — Documentation and Final Report

### Objective
Leave future maintainers with an accurate map.

### Required docs

Create/update:

```text
docs/frontend/FRONTEND_ARCHITECTURE_BASELINE_AUDIT.md
docs/frontend/PROTOTYPE_DESIGN_AUDIT.md
docs/frontend/FRONTEND_MIGRATION_REPORT.md
docs/frontend/FRONTEND_ROUTE_MAP.md
docs/frontend/FRONTEND_STYLE_GUIDE.md
```

### Final report must include

- Final status: PASSED / PASSED WITH TRUE BLOCKERS / FAILED.
- What changed.
- What did not change.
- Route migration map before/after.
- Old dispatcher files and their replacement pages.
- Shared components created.
- Feature modules created.
- CSS split map.
- TypeScript setup and converted file inventory.
- Tailwind setup and token map.
- Prototype alignment summary.
- Removed mock/fallback data inventory.
- Verification commands and results.
- Remaining blockers, if any, with exact evidence.
- Confirmation that no Git commit/push/history mutation was performed.

---

## 8. Global Definition of Done

This phase is done only when:

- `frontend/` has TypeScript enabled.
- `npm run typecheck` exists and passes.
- Tailwind is installed through the Vite plugin and used by new/migrated UI.
- Route registry is split and typed.
- Main portal dispatchers are removed or reduced to temporary compatibility facades with no normal route dependency.
- Student, teacher, parent, school admin, and system admin pages are split into real route modules.
- Shared UI primitives exist and are used by migrated pages.
- Feature-specific code lives under `features/<feature>/`.
- CSS is split into tokens/base/layout/utilities/legacy/components/features.
- New styling uses tokens/Tailwind rather than new giant CSS blocks.
- Production mock/fallback data is removed or explicitly gated.
- Prototype design audit exists and was used to drive UI decisions.
- Public, auth, shared app, and role portals remain reachable.
- Existing behavior is preserved unless a change is explicitly documented as a bug fix.
- Lint, typecheck, unit tests, and build pass.
- E2E passes or true environment blockers are documented.
- No tests are deleted/skipped/weakened to force success.
- No Git commit/push/history mutation is performed.

---

# Coding-Agent Execution Prompt

Use the following prompt to execute this plan in the repository.

```text
You are a senior frontend architect and migration engineer working inside the DerasaX repository.

Objective:
Execute the full frontend migration plan in `docs/frontend/DERASAX_FRONTEND_ARCHITECTURE_TS_TAILWIND_PROTOTYPE_PLAN.md` from start to finish. The goal is to convert the existing unified DerasaX frontend into a maintainable, TypeScript-enabled, Tailwind-based, prototype-aligned frontend while preserving all current product behavior, backend contracts, auth/role/permission guards, i18n behavior, route URLs, tenant safety assumptions, PWA safety rules, and tests.

Repository:
https://github.com/mustafa-elshahhat/drasa-x

Working area:
`frontend/`

Plan file to follow exactly:
`docs/frontend/DERASAX_FRONTEND_ARCHITECTURE_TS_TAILWIND_PROTOTYPE_PLAN.md`

Prototype source of truth:
`derasax-presentation-prototype/project/DerasaX Prototype.html`

Permanent project rule:
Do not create Git commits. Do not push. Do not amend, rebase, squash, tag, or rewrite Git history. Keep all changes as local uncommitted changes only.

Hard constraints:
- This is a frontend-only phase.
- Do not modify backend code, AI service code, database migrations, or unrelated infrastructure.
- Do not change API endpoint contracts.
- Do not change route URLs.
- Do not weaken ProtectedRoute, RoleGuard, PermissionGuard, AnonymousOnly, tenant assumptions, or suspended/session-expired/forbidden behavior.
- Do not break public marketing, auth, shared app, student, teacher, parent, school admin, system admin, communication, or vision routes.
- Do not remove PWA safety rules that prevent caching `/api/**` responses.
- Do not delete, skip, or weaken tests to make verification pass.
- Do not hide errors by disabling lint/typecheck rules.
- Do not present mock/demo data as real production backend data.
- Do not rewrite the app from scratch.
- Do not introduce a different visual identity from the DerasaX prototype.

Required execution method:
1. Read the plan file first.
2. Produce a concise implementation plan that lists all phases and the files likely to be touched.
3. Then execute phases in order.
4. Before editing any important file, read it first.
5. After each phase, update a running status section in `docs/frontend/FRONTEND_MIGRATION_REPORT.md`.
6. Run verification after every major phase or whenever a risky migration is completed.
7. If a command fails, inspect the exact error and make one targeted fix. If the same failure repeats twice, stop and document the blocker instead of looping.

Required phases:
Phase 0 — Baseline Audit and Safety Map
- Inspect package, Vite config, app shell, router, portal files, CSS, shared components, feature modules, tests, and prototype.
- Create `docs/frontend/FRONTEND_ARCHITECTURE_BASELINE_AUDIT.md`.
- Do not start migration before the audit exists.

Phase 1 — Tooling Foundation: TypeScript + Tailwind
- Add TypeScript and Tailwind using the Vite plugin approach.
- Convert Vite config safely to TS.
- Add `tsconfig.json`, `src/vite-env.d.ts`, and `npm run typecheck`.
- Create `src/styles/tailwind.css`, `tokens.css`, `base.css`, `layout.css`, `utilities.css`, and `legacy.css`.
- Preserve all PWA runtime-caching safety behavior.
- Update Vitest coverage/config to include TS/TSX.

Phase 2 — Router Type Safety and Route File Split
- Convert route metadata to typed TS/TSX.
- Split routes into public/auth/shared/student/teacher/parent/school/system/vision route files.
- Convert `App.jsx` to `App.tsx` without changing behavior.
- Keep temporary `props.view` only where a portal has not yet been split.

Phase 3 — Shared UI Foundation
- Create shared UI primitives for Button, Card, PageHeader, SectionHeader, Badge, Avatar, Tabs, Dialog, Drawer, Dropdown, FormField, TextField, SelectField, DateField, DataTable, MetricCard, StatGrid, LoadingState, ErrorState, EmptyState, QueryState, PageShell, and PageSection where useful.
- Keep shared components generic and typed.
- Use DerasaX tokens and Tailwind utilities.

Phase 4 — CSS Architecture and Token Migration
- Split the old global CSS into tokens/base/layout/utilities/legacy/components/features.
- Move repeated values to tokens.
- Stop adding new large blocks to global CSS.
- Replace fixed inline styles with Tailwind/classes/component variants where safe.

Phase 5 — Student Portal Split
- Replace `StudentPortalPage.jsx + props.view` with real lazy-loaded student page modules.
- Extract student API/hooks/components/mappers/types under `features/student/`.
- Fix hook misuse and production mock fallback patterns found during audit.
- Remove normal route dependency on the student dispatcher.

Phase 6 — Teacher and Parent Portal Split
- Split teacher and parent portal dispatchers into real page modules.
- Extract teacher and parent feature code under `features/teacher/` and `features/parent/`.
- Preserve authorization and data access assumptions.

Phase 7 — School Admin and System Admin Portal Split
- Split school admin and system admin dispatchers into real page modules.
- Extract admin feature code under `features/school-admin/` and `features/system-admin/`.
- Do not weaken admin-only behavior or expose system-admin views to tenant roles.

Phase 8 — Shared Communication and Vision Cleanup
- Remove remaining dispatcher patterns where practical.
- Extract communication and vision shared logic/components.
- Preserve SignalR and vision route behavior.

Phase 9 — Prototype Design Alignment
- Inspect/unpack/render the bundled prototype as needed.
- Create `docs/frontend/PROTOTYPE_DESIGN_AUDIT.md`.
- Map prototype patterns to shared components and tokens.
- Align public/auth/portal UI to the prototype design language through reusable primitives, not copy-pasted page CSS.
- Preserve Arabic RTL and English LTR.

Phase 10 — Production Data Cleanup
- Remove or gate production mock/fallback data.
- Move demo values to test/demo fixtures.
- Use honest loading/empty/error states.

Phase 11 — Tests, Accessibility, and Regression Coverage
- Add/update route smoke tests and shared UI tests as appropriate.
- Keep current tests meaningful.
- Add TypeScript-aware test coverage.
- Add accessibility checks where current stack supports it.

Phase 12 — Documentation and Final Report
- Create/update:
  - `docs/frontend/FRONTEND_ARCHITECTURE_BASELINE_AUDIT.md`
  - `docs/frontend/PROTOTYPE_DESIGN_AUDIT.md`
  - `docs/frontend/FRONTEND_MIGRATION_REPORT.md`
  - `docs/frontend/FRONTEND_ROUTE_MAP.md`
  - `docs/frontend/FRONTEND_STYLE_GUIDE.md`
- Final report must list changed files, before/after route map, CSS split, TypeScript setup, Tailwind setup, prototype alignment, verification results, blockers, and confirmation that no Git commits were made.

Verification commands:
Run these from `frontend/` when applicable:

```bash
npm run lint
npm run typecheck
npm test
npm run build
npm run test:e2e
```

If backend is required for E2E and unavailable, document the exact blocker. Do not claim E2E passed if it did not run.

Definition of done:
- All phases in the plan are complete or true blockers are documented.
- TypeScript is enabled and `npm run typecheck` passes.
- Tailwind is installed through Vite and used by migrated UI.
- Route files are split and typed.
- Normal portal routes no longer depend on giant `PortalPage + props.view` dispatchers.
- Shared UI primitives and feature modules are organized.
- CSS is split and tokenized.
- Prototype-aligned design language is applied through shared components.
- Production mock/fallback data is removed or explicitly gated.
- Lint, typecheck, unit tests, and build pass.
- E2E passes or exact true blockers are documented.
- No tests were deleted/skipped/weakened.
- No backend/AI/database contracts were changed.
- No Git commits/pushes/history changes were made.

Final response format:
Return a concise final report with:
A. Final status
B. Phases completed
C. Files/directories changed
D. Route split summary
E. Shared UI/design-system summary
F. TypeScript/Tailwind summary
G. Prototype alignment summary
H. Verification results
I. True blockers, if any
J. Confirmation of no Git commits/history changes
```
