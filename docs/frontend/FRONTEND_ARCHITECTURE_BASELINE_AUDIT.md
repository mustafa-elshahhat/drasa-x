# DerasaX Frontend — Architecture Baseline Audit (Phase 0)

Date: 2026-06-30
Scope: `frontend/` only. Read-only evidence gathered before any migration code was written.
Purpose: the safety map the migration plan (`DERASAX_FRONTEND_ARCHITECTURE_TS_TAILWIND_PROTOTYPE_PLAN.md`) requires before Phase 1.

---

## 1. Stack & tooling

- React 19.2, Vite 7.3, React Router 7.18, `@tanstack/react-query` 5.10, i18next 23 + react-i18next 14, Recharts 3.8, lucide-react, framer-motion, react-hook-form 7 + zod 3, `@microsoft/signalr` 8.
- **No `typescript` dependency, no `.ts`/`.tsx` files, no `tsconfig*`, no `src/vite-env.d.ts`.** Type hints today are JSDoc `@typedef` (e.g. `AppRoute`, `NavItem`). `@types/react` / `@types/react-dom` are present (editor IntelliSense only).
- **No Tailwind / PostCSS anywhere.** Styling is 10 hand-written CSS files under `src/styles/`.
- `vite.config.js` (71 lines): `defineConfig` from `vitest/config`; plugins `react()` + `VitePWA(...)`; the **Vitest `test` block is embedded** (no standalone vitest config). Coverage `include` is hardcoded to `src/**/*.{js,jsx}`.
- `eslint.config.js` (49 lines): flat config, three blocks all keyed on `**/*.{js,jsx}`; no typescript-eslint parser.
- `playwright.config.js` (50 lines): `testDir ./e2e`, `workers:1`, `retries:2`, runs against `npm run preview` @ `:4173`. Backend-dependent specs self-skip when no backend is detected.
- Scripts: `dev / build / lint / preview / test / test:watch / test:coverage / test:e2e`. **No `typecheck`.**
- `index.html` loads `/src/main.jsx`. `vercel.json` enforces a strict CSP (`script-src 'self'`, `connect-src 'self' https: wss:`, `font-src 'self' data:`).

### PWA safety (must be preserved verbatim)
`vite.config.js` workbox block:
- `globPatterns: ['**/*.{js,css,html,svg,png,woff2}']` — only static shell precached.
- `runtimeCaching`: any `url.pathname.startsWith('/api/')` GET → `handler: 'NetworkOnly'` (no auth/tenant payload ever cached).
- `navigateFallbackDenylist: [/^\/api\//]`, `navigateFallback: 'index.html'`, `registerType: 'prompt'`, dev SW disabled.

---

## 2. Routing & guards

- **Single source of truth:** `src/app/router/routes.js` (214 lines) → `ROUTES` array of **122 entries** + `findRouteByPath()`. `AppRoute` shape: `path, titleKey?, requiresAuth?, anonymousOnly?, layout?('public'), roles?(string[]), permission?(string), Component?(lazy), props?({view})`.
- `src/app/App.jsx` (84 lines) partitions `ROUTES` at render: `marketingRoutes` (`layout==='public'` → `<PublicLayout>`), `flatPublicRoutes` (`!requiresAuth && layout!=='public'`, wrapped in `<AnonymousOnly>` when `anonymousOnly`), `appRoutes` (`requiresAuth && path!=='/app'` → children of `/app`, path made relative), `appIndex` (`/app`). `/app` is wrapped `<ProtectedRoute><AppShell/></ProtectedRoute>`. Static fallbacks: `/forbidden`, `/session-expired`, `/suspended`, `*`.
- `guardElement(route, el)`: `roles` → `<RoleGuard>` (takes precedence); else `permission` → `<PermissionGuard>`. They are **not combined**.
- Guards: `src/features/auth/guards.jsx` — `ProtectedRoute`, `AnonymousOnly`, `RoleGuard`, `PermissionGuard`, `TenantStatusGuard`. Behavior: `loading`→`FullPageLoader`; `suspended`→`/suspended`; unauth→`/login` (with `from` state); role/permission fail→`/forbidden`; anon authed→`/app`.
- Roles model: `src/features/auth/roles.js` — `ROLES` (Student/Teacher/Parent/SchoolAdmin/SystemAdmin), `PERMISSIONS` (7), `roleHasPermission`, `homeRouteForRole`.
- Composition root: `src/app/providers.jsx` — `I18nextProvider → QueryClientProvider → BrowserRouter → ErrorBoundary → ToastProvider → AuthProvider(onSessionEnd=queryClient.clear) → NotificationsProvider`. Cache cleared on session end (tenant isolation).

### Route map by area (counts)
| Area | Count | Component | Guard |
|---|---|---|---|
| Public marketing | 5 | per-page (`HomePage`…) | none (`layout:'public'`) |
| Auth | 1 | `LoginPage` | `anonymousOnly` |
| Account/shell | 3 | `AppHome`, `ProfilePage`, `SecurityPage` | `requiresAuth` (+`MANAGE_OWN_ACCOUNT`) |
| Shared communication | 4 | `CommunicationPage` (`view`) | `requiresAuth` |
| Vision (CV) | 4 | `VisionStaffPage`/`StudentVisionPage`/`ParentVisionPage` | role-scoped |
| Student | 33 | `StudentPortalPage` (`view`) | `roles:[STUDENT]` |
| Teacher | 17 | `TeacherPortalPage` (`view`) | `roles:[TEACHER, SCHOOL_ADMIN]` |
| Parent | 12 | `ParentPortalPage` (`view`) | `roles:[PARENT]` |
| School admin | 25 | `SchoolAdminPortalPage` (`view`) | `roles:[SCHOOL_ADMIN]` |
| System admin | 18 | `SystemAdminPortalPage` (`view`) | `roles:[SYSTEM_ADMIN]` |

Note: several routes share one `view` (e.g. `/units` and `/units/:unitId` both `view:'units'`; the page reads params internally). The split must keep this — multiple routes may lazy-load the same page module.

---

## 3. Portal dispatcher map (the core debt)

All five portals + `CommunicationPage` use the identical pattern: a single default export `function Portal({ view })` with a `switch(view)` at the bottom returning one inline-defined sub-page per case; the `view` is injected statically from `routes.js` (`props:{view}`). This keeps each file huge.

| File | Lines | Views | Notes |
|---|---|---|---|
| `pages/student/StudentPortalPage.jsx` | **3350** | 25 | 222 inline `style={{}}`; ALL production mock fallbacks live here |
| `pages/school/SchoolAdminPortalPage.jsx` | 706 | 25 | class-driven, clean |
| `pages/teacher/TeacherPortalPage.jsx` | 725 | 12 | class-driven, clean |
| `pages/system/SystemAdminPortalPage.jsx` | 680 | 18 | class-driven, clean |
| `pages/parent/ParentPortalPage.jsx` | 512 | 11 | class-driven, clean |
| `pages/app/CommunicationPage.jsx` | 433 | 4 | smallest/cleanest |

`pages/vision/` is already split (`VisionStaffPage` 329, `StudentVisionPage` 49, `ParentVisionPage` 34).

---

## 4. Component inventory (`src/components/`)

Subfolders: `ui/`, `form/`, `feedback/`, `files/`, `viz/`, `domain/`, `chat/`, `quiz/`, `data/` (several have `index.js` barrels) + `ErrorBoundary.jsx`.

Mapping to the plan's requested primitives:
| Requested | Status | Existing |
|---|---|---|
| Button | ✅ | `ui/Button.jsx` |
| Card | ✅ | exported from `ui/PageHeader.jsx` |
| PageHeader | ✅ | `ui/PageHeader.jsx` |
| Badge | ⚠ rename | `ui/Chip.jsx` |
| Avatar | ✅ | `ui/Avatar.jsx` |
| Tabs | ✅ | `ui/Tabs.jsx` |
| Dialog | ✅ family | `ui/Modal.jsx`, `ConfirmDialog.jsx`, `FormModal.jsx` |
| Dropdown | ❌ | (none — add wrapper) |
| Drawer | ❌ | (none — add wrapper) |
| DataTable | ✅ | `ui/DataTable.jsx` (+ `data/ResourceTable`, `Crud`, `DetailList`) |
| MetricCard / StatGrid | ⚠ rename | `ui/Metric.jsx` |
| LoadingState | ⚠ unify | `ui/Spinner.jsx`, `Skeleton.jsx`, `FullPageLoader.jsx` |
| ErrorState / EmptyState | ✅ | `ui/states.jsx` |
| QueryState | ⚠ rename | `ui/QueryBoundary.jsx` |
| FormField/TextField/SelectField/DateField | ⚠ wrap | `form/fields.jsx` |
| SectionHeader | ❌ | (add) |
| PageShell / PageSection | ❌ | (add) |

Other primitives: `Alert`, `SearchInput`, `Pagination`, `Toggle`, `Toolbar`, `Stepper`, `ChartWrapper`, `NotEnoughData`. Domain: `SubjectCard`, `UnitCard`, `LessonRow`, `QuizCard`, `ChildCard`, `LeaderboardRow`, `PricingPlanCard`, `UsageBars`, `ServiceStatusList`, `Thumb`. Viz: `Ring`, `ProgressBar`, `Bars`, `Heatmap`, `StreakStrip`. Chat: `ChatBubble`, `AITutorMessage`, `MessageThread`. Quiz: `OptionCard`, `QuizProgress`, `Timer`, `QuestionNavigator`, `QuizResult`.

**Conclusion:** the primitive library is already substantial. Phase 3 is mostly typed *wrappers/aliases* + a `src/shared/` home, not net-new UI.

## 5. Feature modules (`src/features/`)

API/logic layer only — no per-feature `components/hooks/mappers/types` yet:
`auth/` (AuthContext, guards, roles, authApi), `student/` (studentApi, studentUtils, studentSchemas), `teacher/` (teacherApi, teacherUtils), `parent/` (parentApi), `school/` (schoolApi), `system/` (systemApi), `notifications/` (notificationsApi, NotificationsProvider, NotificationBell), `vision/` (visionApi), `files/` (filesApi), plus `phase17/`/`phase18/` (test-only contracts).

---

## 6. CSS inventory (`src/styles/`)

| File | Lines | Role |
|---|---|---|
| `app.css` | **1971** | monolith: `:root` tokens (L7–61) + `[data-role]` accents (L64–67) + all app/portal styles |
| `public-threecards.css` | 208 | marketing |
| `public-request-demo.css` | 112 | marketing |
| `public-hero.css` | 109 | marketing |
| `public-navbar.css` | 85 | marketing |
| `public-pages.css` | 72 | marketing |
| `public-footer.css` | 70 | marketing |
| `public.css` | 49 | marketing |
| `fonts.css` | 21 | `@fontsource` Inter+Cairo (self-hosted; CSP-safe) |
| `public-faq.css` | 20 | marketing |

Tokens are a faithful 1:1 port of the prototype `T` object (teal `#0c7288`, status palette, radii 16/11/9, three shadows, `--header-h:64px`, `--sidebar-w:250px`). Per-role accents via `[data-role]` (Teacher=purple, Parent=orange, SchoolAdmin=blue, SystemAdmin=green; Student inherits brand teal). RTL is built on **logical properties** (`margin-inline`, `inset-inline-*`) with `[dir='rtl']` overrides only for physical flips.

---

## 7. Mock / fallback data inventory

**Entirely confined to `pages/student/StudentPortalPage.jsx`.** No `VITE_ENABLE_DEMO_DATA`, no `src/test/fixtures/`, no `src/demo/` yet. The other four portals + CommunicationPage have NO production mock fallbacks.

- L433–447: `mockSubjects` used as a live fallback when the subjects API returns empty (`baseItems = rawItems?.length ? rawItems : mockSubjects`). **Core violation.**
- L74–87: `AssignedLessonCard.THEMES` hardcoded teacher names + default `'Mr. Osama Refat'`.
- L355–417: `getSubjectTheme` — per-subject hardcoded `teacher` + fake `progress` (62/48/35/71/54/80) shipped via `t('student.mock.*', '<hardcoded default>')`.
- L315: inline hardcoded subject array in dashboard JSX ("Render mock subjects matching prototype if none loaded").
- L709–715: `mathMocks`. L959–1014: `mockMetas` (fake durations/resource counts). L2464–2497: fallback quiz reviews.

No hardcoded user/tenant IDs in production source (only in `*.test.jsx`).

## 8. Inline-style inventory

236 `style={{` occurrences across 12 files — **222 in `StudentPortalPage.jsx`**. The other portals are class-driven (1–2 each). Inline-style cleanup is essentially a student-portal task.

---

## 9. Tests

- 48 vitest files (co-located `*.test.{js,jsx}`), config embedded in `vite.config.js`, setup `src/test/setup.js` (jest-dom, jest-axe `toHaveNoViolations`, `matchMedia`/`scrollTo` polyfills). Global a11y suite `src/test/a11y.test.jsx`.
- Route-registry tests in `src/app/router/` (`publicRoutes`, `teacherRoutes`, `parentRoutes`, `schoolRoutes`, `systemRoutes`, `communicationRoutes`) assert on `ROUTES`. One `*PortalPage.test.jsx` per portal.
- 44 Playwright specs under `e2e/` (phase8…phase20), single worker, run against preview build; backend-dependent specs self-skip.

---

## 10. Prototype design extraction

Source of truth: `derasax-presentation-prototype/project/DerasaX Prototype.dc.html` (the readable design source; the `.html` is a bundled runtime). Tokens (`this.T`) are already ported into `app.css` `:root` verbatim. Layout = sticky 64px topbar + 270px left sidebar (off-canvas drawer when narrow) + content; "Need help?" sidebar card; "Derasa**X**" wordmark (X tinted teal). Role accents match `navConfig()`. RTL via `dir` on containers + `fontAr` (Cairo). Fonts in prototype come from Google Fonts; **production intentionally self-hosts via `@fontsource` (do not reintroduce Google Fonts — CSP)**. Detailed token map → Phase 9 `PROTOTYPE_DESIGN_AUDIT.md`.

---

## 11. Risk register

| Risk | Likelihood | Mitigation |
|---|---|---|
| Tailwind preflight resets existing CSS-driven UI | High | Omit preflight — import `tailwindcss/theme` + `tailwindcss/utilities` only; `legacy.css` keeps the existing reset |
| TS/JS coexistence breaks lint/test/build | Med | `allowJs:true`, `checkJs:false`, `skipLibCheck:true`; add typescript-eslint block; verify per phase |
| Route split breaks 48 unit + route-registry tests | Med | Keep `ROUTES`/`findRouteByPath` exports + every path/guard identical; update (not delete) portal tests |
| Student mock removal makes empty UI when backend down | Expected | Honest empty/loading/error states + gated `VITE_ENABLE_DEMO_DATA` demo fixture |
| PWA `/api/` caching rule lost in vite.config TS port | Med | Copy workbox block byte-for-byte; assert in report |
| Large diff destabilizes a portal | Med | One portal at a time, verify (lint/typecheck/test/build) after each |
| E2E needs backend (often down) | Expected | Run e2e, document exactly what ran/skipped/failed; rely on unit + build otherwise |

## 12. Proposed migration sequence

0 Audit (this doc) → 1 TS+Tailwind tooling → 2 typed route split + App.tsx → 3 shared UI (`src/shared`) → 4 CSS split → 5 Student split + mock cleanup → 6 Teacher+Parent → 7 School+System → 8 Communication+Vision → 9 prototype-alignment doc + primitive adoption → 10 production-data close → 11 tests/a11y → 12 docs + final report. Verify after every phase. No Git commits at any point.
