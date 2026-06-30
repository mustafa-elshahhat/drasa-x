# Phase 13.1 — Shared UI Adoption Map

**Goal:** make `src/shared` the real public UI import surface so `src/pages`,
`src/features`, and `src/layouts` import their UI from `shared/*` and `src/components`
becomes a purely-internal implementation layer.

**Baseline (measured):** 403 direct `src/components` import lines across pages/features/layouts;
**0** imports from `src/shared`. The facade existed but was unused.

---

## 1. Layering decision

```
pages / features / layouts          ← consume UI ONLY through src/shared
        │
        ▼
src/shared/*  (typed facade: ui, form, data-display, feedback, layout,
               navigation, domain, charts, quiz, chat, files)
        │
        ▼
src/components/*  (implementation layer — prototype-aligned .jsx components)
```

`src/components` stays the implementation home (and keeps being imported **internally**, e.g.
`components/data/ResourceTable` → `components/ui/DataTable`). Only the *consumer boundary*
(pages/features/layouts) is moved onto `shared`.

## 2. Old → new import mapping (applied in Phase 13.2)

Because `src/components` and `src/shared` are **siblings under `src/`**, the relative `../` prefix
of every import is unchanged — only the `components/<module>` segment is swapped for
`shared/<category>`. Named imports are preserved verbatim (the facade re-exports the same names),
so no JSX changes.

| Old import (`src/components/…`) | New import (`src/shared/…`) | Facade status |
|---|---|---|
| `ui/Button` | `shared/ui` | existed |
| `ui/PageHeader` (`PageHeader`, `Card`) | `shared/ui` | existed |
| `ui/Alert` | `shared/ui` | existed |
| `ui/Chip` | `shared/ui` (`Chip` = `Badge`) | existed |
| `ui/Avatar` | `shared/ui` | existed |
| `ui/Spinner` | `shared/ui` | existed |
| `ui/Skeleton` | `shared/ui` | existed |
| `ui/Modal` | `shared/ui` (`Modal` = `Dialog`) | existed |
| `ui/ConfirmDialog`, `ui/FormModal` | `shared/ui` | existed |
| `ui/Tabs` | `shared/ui` | existed |
| `ui/Toggle` | `shared/ui` | **added (typed `ToggleProps`)** |
| `ui/Stepper` | `shared/ui` | **added (typed `StepperProps`)** |
| `ui/SearchInput` | `shared/ui` | **added (typed `SearchInputProps`)** |
| `ui/Toolbar` | `shared/ui` | **added (typed `ToolbarProps`)** |
| `ui/Metric` | `shared/data-display` (`Metric` = `MetricCard`) | existed |
| `ui/DataTable` | `shared/data-display` | existed |
| `data/DetailList` | `shared/data-display` | **added (typed `DetailListProps`)** |
| `data/ResourceTable` | `shared/data-display` | **added (typed `ResourceTableProps<Row>`)** |
| `data/Crud` | `shared/data-display` | **added (re-export; rich impl contract)** |
| `form/fields` (TextField, SelectField, …) | `shared/form` | existed |
| `ui/states` (`ErrorState`, `EmptyState`) | `shared/feedback` | existed |
| `ui/QueryBoundary` | `shared/feedback` (`QueryBoundary` alias of `QueryState`) | **alias added** |
| `ui/NotEnoughData` | `shared/feedback` | **added (typed `NotEnoughDataProps`)** |
| `ui/FullPageLoader` | `shared/feedback` | **added (typed `FullPageLoaderProps`)** |
| `ui/Pagination` | `shared/navigation` | existed |
| `feedback/ToastProvider` (`useToast`) | `shared/feedback/toast` | **added (separate non-component module)** |
| `domain/*` (QuizCard, UnitCard, ChildCard, LeaderboardRow, Thumb, …) | `shared/domain` | **new facade** |
| `viz/*` (Ring, Heatmap, ProgressBar, StreakStrip, Bars) | `shared/charts` | **new facade** |
| `quiz/*` (OptionCard, …) | `shared/quiz` | **new facade** |
| `chat/*` (MessageThread, …) | `shared/chat` | **new facade** |
| `files/*` (FileUpload, FileDownloadButton) | `shared/files` | **new facade** |

### Renames preserved by back-compat aliases (no JSX edits required)

| Facade canonical name | Alias kept | Reason |
|---|---|---|
| `Badge` | `Chip` | pages use `<Chip>` |
| `Dialog` | `Modal` | pages use `<Modal>` |
| `MetricCard` | `Metric` | pages use `<Metric>` |
| `QueryState` | `QueryBoundary` | pages use `<QueryBoundary>` |

## 3. New / extended facade files

**New category facades**

- `src/shared/domain/index.tsx` — re-exports all `components/domain/*`
- `src/shared/charts/index.tsx` — re-exports all `components/viz/*`
- `src/shared/quiz/index.tsx` — re-exports all `components/quiz/*`
- `src/shared/chat/index.tsx` — re-exports all `components/chat/*`
- `src/shared/files/index.tsx` — re-exports `FileUpload`, `FileDownloadButton`
- `src/shared/feedback/toast.ts` — `ToastProvider` + `useToast` (kept separate so the hook export
  doesn't break react-refresh on the JSX feedback barrel)

**Extended existing facades**

- `shared/ui` — added `Toggle`, `Stepper`, `SearchInput`, `Toolbar` (typed)
- `shared/data-display` — added `DetailList`, `ResourceTable`, `Crud` (typed)
- `shared/feedback` — added `NotEnoughData`, `FullPageLoader`, `QueryBoundary` alias
- `shared/index.ts` — root barrel now also re-exports `domain`/`charts`/`quiz`/`chat`/`files`

> The new domain/charts/quiz/chat/files facades are **thin re-exports** (no duplication). The
> underlying `.jsx` carry no prop types yet; per the task rule "do not create fake types that hide
> unknown shapes", they are exposed as-is and flagged as a TS-hardening follow-up (Phase 13.4 notes).

## 4. Components intentionally kept as direct `src/components` imports (exceptions)

| Module | Where | Why kept direct |
|---|---|---|
| `components/feedback/ToastProvider` (`ToastProvider`) | `app/providers.jsx` (mount point) + 2 **test** files | App-infra provider mounted at the root; tests wrap components with it. `providers.jsx` is app wiring, not a page; test files test the implementation directly. (`useToast` for pages routes through `shared/feedback/toast`.) |
| `components/ErrorBoundary` | `app/` (not pages/features/layouts) | App-level error boundary; outside the consumer-boundary scope. |
| internal `components/*` → `components/*` imports | within `src/components` | Implementation layer composing itself (e.g. `ResourceTable` → `DataTable`). Out of scope by design. |
| `*.test.jsx` importing `components/*` | `src/components/**`, page/feature tests | Tests of the implementation layer must import the implementation; not weakened or redirected (per task rule). |

All other page/feature/layout UI imports are migrated to `src/shared` in Phase 13.2.

## 5. Adoption guard (Phase 13.2)

The guard is an **ESLint rule** (runs inside `npm run lint`, no extra command): a
`no-restricted-imports` block in `eslint.config.js` scoped to `src/pages/**`,
`src/features/**`, `src/layouts/**` (test files exempt) forbids any import matching
`**/components/**`. New direct-import regressions fail the lint gate. See
`SHARED_UI_ADOPTION_RESULT.md`.
