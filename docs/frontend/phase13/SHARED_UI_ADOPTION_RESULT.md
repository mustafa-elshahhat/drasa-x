# Phase 13.2 — Shared UI Adoption Result

**Goal:** pages/features/layouts import UI through `src/shared`, not directly from `src/components`.

---

## 1. Result summary

| Metric | Before | After |
|---|---:|---:|
| Direct `src/components` import **lines** in production pages/features/layouts | **403** | **0** |
| Files in pages/features/layouts importing `src/components` (non-test) | 102 | **0** |
| Production pages/features/layouts importing `src/shared` | 0 | **102 files** |
| Merged `src/shared` import lines written | — | **279** |

> "Production" excludes `*.test.{jsx,tsx,js}`. All 403 direct component-import lines across 102
> non-test modules were rewritten to `src/shared/*`. The codemod merged same-target imports per
> contiguous block (403 source lines → 279 merged lines).

## 2. How the migration was done

A one-shot codemod (`scratchpad/migrate-shared.mjs`, kept out of the repo) walked
`src/pages`, `src/features`, `src/layouts` (skipping `*.test.*`) and for each
`import { … } from '<prefix>components/<dir>/<file>'`:

1. mapped `<dir>/<file>` → a `src/shared` category (table in `SHARED_UI_ADOPTION_MAP.md`),
2. **kept the `<prefix>` (`../…`) unchanged** — `components/` and `shared/` are siblings under
   `src/`, so depth is identical,
3. **kept the named specifiers verbatim** — the facade re-exports the same names (incl. the
   `Chip`/`Modal`/`Metric`/`QueryBoundary` back-compat aliases), so **no JSX changed**,
4. merged same-(prefix,category) imports in a contiguous block into one statement.

The codemod reported **0 unmapped component imports** — every consumed module had a facade.

### Example (StudentDashboardPage.jsx)
```diff
- import { Avatar } from '../../../components/ui/Avatar'
- import { ErrorState } from '../../../components/ui/states'
- import { Ring } from '../../../components/viz/Ring'
+ import { Avatar } from '../../../shared/ui'
+ import { ErrorState } from '../../../shared/feedback'
+ import { Ring } from '../../../shared/charts'
```
### Example (SchoolUsersPage.jsx — merge)
```diff
- import { Button } from '../../../components/ui/Button'
- import { Card } from '../../../components/ui/PageHeader'
- import { Alert } from '../../../components/ui/Alert'
+ import { Alert, Button, Card } from '../../../shared/ui'
```

## 3. Behavior preservation

- **No markup, props, classNames, or route behavior changed** — only import *paths* (and import
  grouping) changed. The facade components ARE the same `src/components` implementations (re-exported),
  so render output is identical.
- **No API calls changed.**
- Verified green after migration: `tsc --noEmit` ✅, `eslint .` ✅, `vitest run` ✅ **243/243**,
  `vite build` ✅. (Full logs in Phase 13.7 `verification/`.)

## 4. Remaining direct `src/components` imports (all justified)

Production pages/features/layouts now have **zero** direct component imports. The remaining
`src/components` importers in the repo are intentional and out of scope:

| Location | Imports | Justification |
|---|---|---|
| `src/app/providers.jsx` | `ToastProvider`, `ErrorBoundary` | App-root wiring (mounts the provider / error boundary). Not a page/feature/layout; not a UI-consumer boundary. |
| `src/app/**` (router, App) | (none of components) | — |
| `*.test.jsx` / `*.test.tsx` (page, feature, component tests) | various `components/*` | Tests test/wrap the **implementation**; redirecting them would weaken them (forbidden by task rules). |
| `src/components/**` internal | `components/*` → `components/*` | The implementation layer composing itself (e.g. `ResourceTable`→`DataTable`, `DetailList`→`Chip`). By design. |
| `src/shared/**` | `components/*` | The facade's entire purpose — it wraps the implementation. |

These are the **only** exceptions and each is structural, not a missed migration.

## 5. Regression guard (lint-enforced)

`eslint.config.js` gained a `no-restricted-imports` rule scoped to
`src/pages/**`, `src/features/**`, `src/layouts/**` (test files exempt) that errors on any
`**/components/**` import. Verified:

- `npm run lint` → **0 errors** on the migrated tree.
- Negative test: a probe file `src/pages/__guard_probe.jsx` importing
  `'../../components/ui/Button'` produced
  `error … import is restricted … Import UI through src/shared/*` (probe removed afterward).

So the boundary is now machine-enforced in the existing lint gate — no new command, no CI change.

## 6. Files changed (this phase)

- **102** production page/feature/layout modules — import lines rewritten to `src/shared/*`.
- `eslint.config.js` — added the architecture guard rule.
- (Facade files created/extended are listed in `SHARED_UI_ADOPTION_MAP.md` §3.)
