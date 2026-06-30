# Phase 13.0 — Baseline Audit & Inventory

**Date:** 2026-06-30
**Branch:** main (no commits made; all work is local uncommitted changes)
**Scope:** `frontend/` only. The migration report was **verified, not trusted** — every
number below was re-measured from the working tree.

---

## 1. Verification of the migration report's claims

| Report claim | Verified? | Evidence |
|---|---|---|
| TS tooling + Tailwind v4 present | ✅ TRUE | `tsconfig.json` (allowJs, strict), `vite.config.ts` (`@tailwindcss/vite`), `package.json` deps |
| Entry/config/router are TS/TSX | ✅ TRUE | `src/main.tsx`, `src/app/App.tsx`, `src/app/router/routes.tsx` + 9 typed `routes.*.tsx` |
| Mega route registry split into typed files | ✅ TRUE | `routes.public/auth/shared/vision/student/teacher/parent/school/system.tsx` |
| PortalPage + `props.view` removed from production routing | ✅ TRUE (with nuance) | **No route file references `props.view` or a `*PortalPage` dispatcher.** See §3 |
| CSS split into tokens/base/layout/components/features/utilities/legacy/tailwind | ✅ TRUE | `src/styles/*.css` (18 files) |
| Prototype tokens preserved | ✅ TRUE | `tokens.css` + `@theme` in `tailwind.css` mirror the prototype teal/per-role accents |
| Unit tests / build / typecheck green | ✅ TRUE | see §9 |
| Backend-dependent E2E self-skip | ✅ TRUE | 224 skipped (backend down), 21 backend-independent passed |

**New finding the report did NOT mention:** the `src/shared` facade layer exists and is
fully typed, but **no page/feature/layout imports from it** — it is dead surface area.
This is the central gap Phase 13 closes (see §5).

---

## 2. Current frontend structure

```
src/
  app/            App.tsx, providers.jsx, pwa.js, router/ (typed split routes + tests)
  assets/
  components/     IMPLEMENTATION layer: ui/ data/ domain/ viz/ quiz/ chat/ form/ files/ feedback/
  config/
  demo/           demo-gated mock data (isDemoEnabled gate)
  features/       auth, communication, files, notifications, parent/student/teacher/school/system, vision, portal, phase17/18
  i18n/
  layouts/        AppShell, Header, Sidebar, Breadcrumbs, Public* 
  lib/            query/keys, security, telemetry
  navigation/     navConfig
  pages/          120 files (10 role/area folders; ~93 lazy page modules + tests) serving 122 routes
  shared/         FACADE layer: ui/ form/ data-display/ feedback/ layout/ navigation/ (typed re-exports) — CURRENTLY UNUSED
  styles/         18 CSS files (tokens/base/utilities/components/layout/legacy/features + public-*)
  test/
```

## 3. Route count & dispatcher check

- **Exact production route count: `122`** (5 public + 1 auth + **7 shared** + 4 vision + 33 student
  + 17 teacher + 12 parent + 25 school + 18 system). `routes.tsx` is a pure concatenation of the
  nine typed arrays — no dynamic generation, so the count is exact. (The authoritative number is
  `ROUTES.length = 122`; an initial text-grep undercounted shared to 5 because two route objects —
  `/app/profile`, `/app/security` — span multiple lines. The Phase 13.6 regression test asserts the
  exact `122`.)
- **`props.view` in production routes: NONE.** `grep "props:"` / `"view"` across `routes.*.tsx`
  returns zero `props.view` usages. The only `props` passed are small static variant props such as
  `{ mode: 'recommendations' }` / `{ mode: 'progress' }` on `StudentProgressPage` — a legitimate
  page prop, **not** the old mega-dispatcher `view`.
- **`*PortalPage` dispatcher in production routes: NONE.** No `routes.*.tsx` imports a `*PortalPage`.
  The `*PortalPage.jsx` files (`StudentPortalPage`, `TeacherPortalPage`, `ParentPortalPage`,
  `SchoolAdminPortalPage`, `SystemAdminPortalPage`) still exist but are referenced **only by their
  own `*.test.jsx` files** (kept as Phase 9–12 contract tests). They are not in the route graph and
  are not lazy-imported by any router file. Confirmed via `grep -rn PortalPage` excluding the page
  files themselves.

> Conclusion: production routing is fully on real lazy page modules. The dispatcher pattern is gone
> from the route graph; the legacy dispatcher files survive only as test fixtures.

## 4. File-type distribution (`src/`)

| Ext | Count |
|---|---|
| `.js`  | 76 |
| `.jsx` | 202 |
| `.ts`  | 4 |
| `.tsx` | 19 |
| **Total** | **301** |

TS/TSX share: 23 of 301 (~7.6%). The migrated surface (entry, router, shared facades, lib types)
is TS; the page/feature bodies remain `.jsx` under `allowJs` (intended gradual migration).

## 5. Shared-UI adoption (the core gap)

| Metric | Count |
|---|---|
| Files in `pages/` importing directly from `src/components` | **95** |
| Import **lines** from `src/components` in `pages/` | **388** |
| Files in `features/` importing from `src/components` | 8 (14 lines) |
| Files in `layouts/` importing from `src/components` | 1 (1 line) |
| **Total direct `src/components` import lines (pages+features+layouts)** | **403** |
| Import lines from `src/shared` in pages/features/layouts | **0** |

The only importer of `src/shared` is `src/shared/shared.test.tsx` (inside the facade itself).
`routes.tsx` matched a naive grep but only because it imports `./routes.shared` — a false positive.

### Most-imported `src/components` modules (from pages/features/layouts)

| Module | Lines | Already in `src/shared`? |
|---|---|---|
| `ui/states` (ErrorState, EmptyState) | 94 | ✅ `shared/feedback` |
| `ui/PageHeader` (PageHeader, Card) | 75 | ✅ `shared/ui` |
| `ui/Alert` | 48 | ✅ `shared/ui` |
| `ui/Button` | 40 | ✅ `shared/ui` |
| `form/fields` | 31 | ✅ `shared/form` |
| `data/DetailList` | 25 | ❌ **needs facade** |
| `ui/Chip` | 12 | ✅ `shared/ui` (Badge/Chip) |
| `ui/QueryBoundary` | 10 | ⚠️ exists as `QueryState` (no `QueryBoundary` alias) |
| `ui/Metric` | 10 | ✅ `shared/data-display` |
| `ui/Spinner` | 9 | ✅ `shared/ui` |
| `data/ResourceTable` | 7 | ❌ **needs facade** |
| `domain/QuizCard` | 6 | ❌ no `shared/domain` |
| `ui/Avatar` | 5 | ✅ `shared/ui` |
| `viz/Ring` | 4 | ❌ no `shared/charts` |
| `domain/UnitCard` | 4 | ❌ no `shared/domain` |
| `ui/NotEnoughData` | 3 | ❌ needs facade |
| `feedback/ToastProvider` (+useToast) | 3 | ❌ needs facade |
| `viz/Heatmap` | 2 | ❌ no `shared/charts` |
| `domain/{Thumb,LeaderboardRow,ChildCard}` | 2 each | ❌ no `shared/domain` |
| `ui/{Toggle,Stepper,FullPageLoader}` | 1 each | ❌ needs facade |
| `viz/{StreakStrip,ProgressBar}` | 1 each | ❌ no `shared/charts` |
| `quiz/OptionCard`, `chat/MessageThread`, `files/{FileUpload,FileDownloadButton}` | 1 each | ❌ needs facade |

The high-frequency generic primitives are already covered by `shared/`; the gap is (a) nobody imports
them through `shared`, and (b) a handful of additional facades (`DetailList`, `ResourceTable`,
`NotEnoughData`, `Toggle`, `Stepper`, `FullPageLoader`, `QueryBoundary` alias, `domain`, `charts`,
`quiz`, `chat`, `files`, `ToastProvider`/`useToast`) are needed so `src/components` can become a
purely-internal layer. This drives Phase 13.1 / 13.2.

## 6. Inline-style inventory

| Form | Count |
|---|---|
| `style={{ ... }}` (object literal) | **236** |
| `style={ var }` (single-brace, computed) | **5** |
| **Total inline-style attributes** | **241** |

### Classification of the 236 object-literal styles

| Class | Count | Notes |
|---|---|---|
| **Static** (fixed layout / token color / spacing — convertible to utilities/classes) | **~198** | almost entirely in 9 prototype-ported student pages |
| **Dynamic / library-required** (data-derived color, `${progress}%` width, chart `{height}`, ternary) | **~38** | must remain inline |

(The automated classifier reported 201/35; manual correction moves the three `{ height }` chart-dimension
shorthands — `ChartWrapper` ×2, `Bars` ×1 — from "static" to "dynamic", giving ~198/38.)

The 5 single-brace `style={var}` usages are all dynamic component-internal styles
(`Avatar` computed avatar style, `DataTable` conditional `textAlign`, `Metric` computed accent) — these
stay.

### Static inline styles by file (conversion targets)

| File | Static | Dynamic |
|---|---|---|
| `pages/student/quizzes/StudentQuizResultPage.jsx` | 36 | 4 |
| `pages/student/homework/StudentHomeworkPage.jsx` | 29 | 4 |
| `pages/student/quizzes/StudentQuizzesPage.jsx` | 29 | 3 |
| `pages/student/lessons/StudentLessonsPage.jsx` | 28 | 0 |
| `pages/student/subjects/StudentSubjectDetailsPage.jsx` | 23 | 6 |
| `pages/student/units/StudentUnitsPage.jsx` | 22 | 1 |
| `pages/student/tutor/StudentTutorPage.jsx` | 10 | 1 |
| `pages/student/quizzes/StudentQuizAttemptPage.jsx` | 8 | 1 |
| `pages/student/dashboard/StudentDashboardPage.jsx` | 3 | 6 |
| `pages/student/subjects/StudentSubjectsPage.jsx` | 3 | 3 |
| 11 other pages (1 each) + `components/ui/ChartWrapper` (2) | ~13 | — |

> Because the baseline shows static styles are the **majority** (~84%), the Phase 13.3 70%-reduction
> target is in principle achievable, but the conversions live in demo-gated student pages with **no
> visual-regression test coverage**, so they will be done via **byte-equivalent Tailwind-utility /
> token-class conversions** verified by build + tests, never by re-layout.

## 7. CSS file inventory (before)

| File | Bytes | Lines |
|---|---:|---:|
| `legacy.css` | 28,301 | 396 |
| `components.css` | 25,881 | 791 |
| `layout.css` | 11,781 | 188 |
| `features.css` | 10,960 | 510 |
| `public-threecards.css` | 4,412 | 208 |
| `tailwind.css` | 2,591 | 77 |
| `tokens.css` | 2,635 | 72 |
| `public-request-demo.css` | 2,453 | 112 |
| `public-hero.css` | 2,273 | 109 |
| `public-navbar.css` | 1,857 | 85 |
| `public-pages.css` | 1,795 | 72 |
| `public-footer.css` | 1,548 | 70 |
| `base.css` | 1,254 | 45 |
| `public.css` | (49 lines) | 49 |
| `fonts.css` | 976 | 21 |
| `public-faq.css` | 477 | 20 |
| `utilities.css` | 232 | 5 |
| **Total** | **~131 KB** | **2,830** |

`legacy.css` (28 KB / 396 lines) and `components.css` (25.9 KB / 791 lines) are the burn-down targets
for Phase 13.5.

## 8. Stop-condition scan (none triggered at baseline)

- Production routes do **not** depend on dispatcher behavior (§3) — safe to proceed.
- No CSS deletions planned that aren't replaced/verified.
- Inline-style cleanup is byte-equivalent only.
- TS hardening is scoped to boundaries (no broad unsafe API assumptions).

## 9. Baseline verification results

All commands run from `frontend/` against the current working tree (logs in `verification/`).

| Command | Result | Evidence |
|---|---|---|
| `npm run lint` | ✅ PASS (exit 0, 0 errors) | `verification/baseline-lint.log` |
| `npm run typecheck` | ✅ PASS (exit 0) | `verification/baseline-typecheck.log` |
| `npm test` | ✅ PASS — **50 files, 243 tests** | `verification/baseline-test.log` |
| `npm run build` | ✅ PASS (exit 0; PWA precache 221 entries) | `verification/baseline-build.log` |
| `npm run test:e2e` | ✅ RAN — **21 passed, 224 skipped, 0 failed** | `verification/baseline-e2e.log` |

### E2E backend status (true environment blocker)

- The local DerasaX backend (`http://localhost:5155/health/live`) is **down** (probed `:5155`, `:5000`,
  `:8080` — all closed).
- The 224 skipped specs are all backend-dependent (`authenticated`, `phase8/9/10/11/12-…`,
  `phase15–19` live-stack) and **self-skip** via `backendUp()` — they do not fail.
- The **21 backend-independent specs all pass** (foundation redirect/RTL/no-direct-AI, phase17/18
  session-protection smoke, phase20 public-marketing). **No migration-related E2E failure.**
- This is a **true environment blocker**, documented per the task rules: backend-independent evidence
  is green and no skip masks a migration regression.

---

## 10. Phase 13 execution plan (derived from this baseline)

1. **13.1/13.2 (highest value, lowest risk):** expand `src/shared` to cover every UI module pages use
   (add `DetailList`, `ResourceTable`, `NotEnoughData`, `Toggle`, `Stepper`, `FullPageLoader`,
   `QueryBoundary` alias, and new `domain`/`charts`/`quiz`/`chat`/`files` facades + `ToastProvider`/
   `useToast`), then swap the 403 direct `src/components` import lines in pages/features/layouts to
   `src/shared` (sibling dirs → relative prefix unchanged, named imports preserved). Add a lint/CI
   guard against new direct imports.
2. **13.4:** strengthen types at boundaries (route metadata already typed; add `Role`/`Permission`/
   `ApiResult`/`PagedResult`/`SelectOption`/`MetricItem`/`TableColumn`/`QueryState`/`EmptyStateProps`/
   etc.; convert safe shared/lib modules to TS).
3. **13.3:** byte-equivalent inline-style → Tailwind-utility/token-class conversion on the static
   student-page styles; keep the ~38 dynamic.
4. **13.5:** conservative legacy/components CSS burn-down (move page-specific selectors, dedupe, token
   substitution) — no unverified deletions.
5. **13.6:** route-inventory regression tests (exact count 122, no `props.view`, no PortalPage in graph,
   auth/role/permission preserved) + shared-UI smoke tests.
6. **13.7:** re-run full verification, write final report.
