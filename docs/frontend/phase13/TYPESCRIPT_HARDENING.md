# Phase 13.4 — TypeScript Hardening

**Goal:** strengthen TypeScript at the architecture boundaries (not a blind full rewrite).
`allowJs` stays on; `.jsx` page/feature bodies remain JS under `checkJs:false`.

**`npm run typecheck` (`tsc --noEmit`): ✅ PASS** (exit 0) after every change below.

---

## 1. Converted files (JS → TS)

| File | From → To | Why (boundary) |
|---|---|---|
| `src/features/auth/roles.js` | → `roles.ts` | **Roles/permissions** — the most-imported guard module (18 importers). Now exports literal-typed `ROLES`/`PERMISSIONS` + `Role`/`Permission` union types and typed function signatures. Runtime behavior identical. |

No explicit `.js` extensions referenced `roles`, so the rename is import-safe; the
`roles.test.js` suite still passes unchanged.

## 2. New shared types

### `src/shared/types.ts` (new — canonical cross-cutting types)

| Type | Shape | Source of truth |
|---|---|---|
| `Role`, `Permission` | string-literal unions | re-exported from `features/auth/roles` |
| `AppRoute`, `RouteMeta`, `RouteComponent`, `RouteLayout` | route metadata | re-exported from `app/router/route.types` |
| `PagedResult<T>` | `{ items: T[]; totalCount?; pageSize?; pageNumber? }` | matches the backend list envelope (`items`/`pageSize`/`totalCount` observed in feature APIs) |
| `ApiResult<T>` | `{ ok: true; data: T } \| { ok: false; error: unknown }` | conservative mapper/hook envelope (error stays `unknown`) |
| `SelectOption` | `{ value: string\|number; label: ReactNode; disabled? }` | matches `SelectField`/`RadioGroup` `options` |
| `MetricItem` | `{ label; value; sub?; accent?; to? }` | matches `MetricCard` |
| `TableColumn<T>` | re-export of `DataTableColumn<Row>` under the canonical name | `shared/data-display` |
| `QueryState<T>` | `{ data?; isLoading; isError; error?; refetch }` | React-Query result shape `QueryState` consumes |

It also re-exports the shared UI prop types (`ButtonProps`, `CardProps`, `PageHeaderProps`,
`DialogProps`, `EmptyStateProps`, `ErrorStateProps`, `LoadingStateProps`, `DetailListProps`,
`ResourceTableProps`, `ToggleProps`, `StepperProps`, `FormFieldProps`, `PageShellProps`, …) so a
consumer can `import type { … } from '../shared/types'` from one place.

> `types.ts` is intentionally **not** added to the `shared/index.ts` runtime barrel — the barrel
> already re-exports the prop types from each category, so a wildcard re-export would duplicate them.
> Import shared types from `shared/types` (or the category facade) directly.

## 3. Improved types (existing surfaces)

- **`src/app/router/route.types.ts`** — `AppRoute.roles` tightened `string[]` → `Role[]`;
  `permission` `string` → `Permission`; added `RouteMeta` alias; clarified the `props` doc-comment
  (it is a small page-variant prop like `{ mode }`, **not** the removed `props.view` dispatcher).
  All nine `routes.*.tsx` files typecheck against the tightened shape (they pass `ROLES.*`/`PERMISSIONS.*`
  literals).
- **Shared UI facades (Phase 13.1)** — added typed prop interfaces for the newly-exposed primitives:
  `ToggleProps`, `StepperProps`, `SearchInputProps`, `ToolbarProps` (ui); `DetailListProps`,
  `ResourceColumn<Row>`/`ResourceTableProps<Row>`, `DetailField`, `ChipDescriptor` (data-display);
  `NotEnoughDataProps`, `FullPageLoaderProps`, and the `QueryBoundary` alias (feedback). These join the
  pre-existing `ButtonProps`, `CardProps`, `PageHeaderProps`, `DialogProps`, `DataTableColumn<Row>`,
  `MetricCardProps`, `QueryStateProps<T>`, `EmptyStateProps`, etc.

## 4. Requested types — coverage checklist

| Requested | Status |
|---|---|
| `AppRoute` | ✅ existed; doc-improved |
| `RouteMeta` | ✅ added (alias) |
| `Role` | ✅ added (`roles.ts`) |
| `Permission` | ✅ added (`roles.ts`) |
| `ApiResult<T>` | ✅ added (`types.ts`) |
| `PagedResult<T>` | ✅ added (`types.ts`) |
| `SelectOption` | ✅ added (`types.ts`) |
| `MetricItem` | ✅ added (`types.ts`) |
| `TableColumn<T>` | ✅ added (alias of `DataTableColumn<Row>`) |
| `QueryState<T>` | ✅ added (`types.ts`) |
| `EmptyStateProps` | ✅ existed (`shared/feedback`) |
| `PageHeaderProps` | ✅ existed (`shared/ui`) |
| `DialogProps` | ✅ existed (`shared/ui`) |

## 5. Deliberate decisions (allowJs / strictness / conservatism)

- **`allowJs` kept on, `checkJs:false`.** The 202 `.jsx` page/feature bodies stay JS — converting
  them blindly would force broad, unsafe assumptions about untyped API payloads. Gradual migration
  is the intended posture (`tsconfig.json` documents it).
- **No fabricated API DTOs.** `ApiResult.error` and uncertain payloads stay `unknown`; `PagedResult`
  optional fields stay optional. Per-feature mappers (which already use Zod where present, e.g.
  `studentSchemas`) remain the place to refine concrete shapes — Zod usage was **not** removed.
- **Form fields left as ref-forwarding re-exports.** `shared/form` re-exports the `forwardRef`
  field components as-is (not cast to `FC`) so react-hook-form's `register()` ref typing is
  preserved; `SelectOption` is provided in `types.ts` for callers building `options` arrays.
- **Domain/charts/quiz/chat/files facades** are thin re-exports of untyped `.jsx`; no speculative
  prop types were invented for them (would misrepresent runtime shapes).

## 6. Remaining JS/JSX areas & next safe TS candidates

Final distribution: **JS 75, JSX 202, TS 7, TSX 24** (was JS 76 / TS 4 / TSX 19).

Next safe conversion candidates (pure, well-bounded, no API guesswork), in priority order:

1. `src/lib/query/keys.js` → `keys.ts` — pure query-key factory; type as
   `(userId?: string, …) => readonly unknown[]`. (Large but mechanical; no `.js` importers.)
2. `src/features/auth/authModel.js` / `AuthContext` selectors — typed `Role`/session shape.
3. `src/i18n/direction.js`, `src/config/env.js` — small, pure utility modules.
4. `src/components/data/DetailList.jsx` & `ResourceTable.jsx` → `.tsx` using the `DetailField`/
   `ResourceColumn` types already defined in the facade (closes the loop on the data-display layer).
5. `src/navigation/navConfig.js` — type nav items against `Role`/`Permission`.

These were left for a follow-up to keep this phase's blast radius small and every change typecheck-verified.
