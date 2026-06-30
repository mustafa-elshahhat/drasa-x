# Phase 13.6 — Route & Regression Tests

**Goal:** lock the post-migration architecture so a future change can't silently reintroduce the old
`PortalPage` + `props.view` dispatcher, drop a guard, or change the route surface. **No existing test
was deleted, skipped, or weakened** — these are additive (and strengthen the prior assertions).

---

## 1. New test files

| File | Tests | Purpose |
|---|---:|---|
| `src/app/router/routeArchitecture.test.js` | 20 | route registry regression guards |
| `src/shared/sharedUi.test.tsx` | 13 | shared-UI smoke coverage **via the `src/shared` facade** |

## 2. `routeArchitecture.test.js` — what it asserts

Runtime (`ROUTES`):

- **Exact route count `=== 122`** (not `>=`). The verified current count is locked; an intentional
  add/remove must update the constant in the same change.
- Every route has a non-empty `path` and a (lazy) `Component`.
- **No duplicate paths.**
- **No production route carries a `props.view` dispatcher prop** (`'view' in r.props` is false for all).
- **Every `/app` route has `requiresAuth === true`** (no authenticated surface left unguarded).
- No route is both `requiresAuth` and `anonymousOnly`.
- Public marketing routes (`/`, `/request-demo`, `/events`, `/activities`, `/news`) are
  `layout: 'public'` and **not** auth-gated.
- **Role-protected areas preserve their role** — every `/app/{student,teacher,parent,school,system}`
  route that declares `roles` contains the expected `ROLES.*` value.
- Every declared role is a recognized backend role (`ALL_ROLES`).
- **Permission routes keep their permission** — `/app/profile` and `/app/security` keep
  `PERMISSIONS.MANAGE_OWN_ACCOUNT`.

Source-level (reads each `routes.*.tsx` via `node:fs`, comments stripped):

- **No `*PortalPage` import** in any route file.
- **No `view:` prop** declared in any route file.

> This source guard is stronger than a runtime check: it fails even if someone re-adds a PortalPage
> dispatcher import without wiring it into `ROUTES`.

### Real defect this phase caught

The exact-count assertion **failed at first (received 122, expected 120)** — exposing that the Phase
13.0 text-grep had undercounted: `routes.shared.tsx` has **7** routes, two of which (`/app/profile`,
`/app/security`) are multi-line objects a single-line grep missed. The authoritative
`ROUTES.length = 122` was confirmed and the baseline doc + test corrected. (Demonstrates the guard
working as intended.)

## 3. `sharedUi.test.tsx` — what it covers (through `src/shared`)

Complements the pre-existing `shared.test.tsx` (Badge / SectionHeader / StatGrid / MetricCard /
LoadingState / QueryState / PageShell). Adds:

- **Button** — variant class (`.ui-btn--primary`, `--danger`) + children; loading state disables +
  `aria-busy`.
- **Card** — title / description / children.
- **PageHeader** — title / description / actions slot.
- **EmptyState** — honest empty title + message, `role="status"`.
- **ErrorState** — `role="alert"`, retry button fires `onRetry`.
- **DataTable** — empty (`role=status` + empty text), loading (`aria-busy`), error (`role=alert`, no
  `<table>`), and populated (rows render).
- **Dialog** — renders title/children when `open`, nothing when closed.
- **Drawer** — renders as `role="dialog"` when open, closes on `Escape`.
- **RTL smoke** — a representative shared layout component (`Drawer`, logical `start`/`end` edges)
  mounts and exposes its dialog + Arabic content under `dir="rtl"`.

All shared-UI tests import from `./ui`, `./feedback`, `./data-display` — i.e. they exercise the same
public facade pages now use, so a facade regression is caught here.

## 4. Suite totals

| | Before Phase 13 | After Phase 13.6 |
|---|---:|---:|
| Test files | 50 | **52** |
| Tests | 243 | **276** |

No prior test modified; `routeInventory.test.js` (the older `>= 120` inventory test) is **kept as-is**
alongside the new exact-count guard.

## 5. How to run

```
cd frontend
npm test                                   # full suite (276 tests)
npx vitest run src/app/router/routeArchitecture.test.js   # route guards only
npx vitest run src/shared/sharedUi.test.tsx               # shared-UI smoke only
```
