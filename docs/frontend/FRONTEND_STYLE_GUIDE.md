# DerasaX Frontend Style Guide

Conventions for the migrated, TypeScript-enabled, Tailwind-backed frontend.

## TypeScript (gradual)
- `.ts/.tsx` and `.jsx` coexist (`tsconfig.json`: `allowJs:true`, `checkJs:false`, `strict:true`,
  `noEmit:true`, bundler resolution, `jsx:react-jsx`). `npm run typecheck` = `tsc --noEmit`.
- New/boundary code is typed: route registry + types, `App.tsx`/`main.tsx`, shared UI props,
  `vite.config.ts`. Page bodies migrated structurally remain `.jsx` (typed at their import seams).
- Lint covers TS via `typescript-eslint` (`eslint.config.js`); keep the `^[A-Z_]` unused-var
  convention. Do not disable lint/typecheck rules to hide errors.

## Styling
- **Tokens are the source of truth** — `src/styles/tokens.css` (`:root` + `[data-role]` accents).
  Never hardcode brand/role/spacing values; reference the CSS variables (or the Tailwind tokens
  in `@theme`, e.g. `bg-brand`, `text-ink`, `rounded-card`, `shadow-card`).
- CSS is split: `tokens → base → components → layout → legacy → features` (+ `utilities`
  placeholder), imported in source order by `styles/tailwind.css`. `legacy.css`/`features.css`
  are temporary and should shrink; do not add new large global blocks there.
- **Tailwind is additive, no preflight** (the existing reset lives in `base.css`). Prefer Tailwind
  utilities backed by DerasaX tokens for new/migrated UI; keep existing `ui-*` classes working.
- RTL: use **logical properties** / Tailwind logical utilities (`start-*`/`end-*`,
  `margin-inline`, `inset-inline-*`). Add `[dir='rtl']` overrides only for true physical flips.
- Keep `style={{}}` only for runtime-computed values (widths, dynamic colors); otherwise use
  classes/utilities.

## Components
- Import shared primitives from `src/shared/{ui,form,data-display,feedback,layout,navigation}`
  (typed facades over `src/components/*`). Don't duplicate primitives; extend the shared layer.
- A file that exports a non-component (hook/const) plus a component trips
  `react-refresh/only-export-components`. Split them: hooks/utils in a `.js` (e.g.
  `features/<x>/helpers.js`), shared components in a `.jsx` (e.g. `features/<x>/components.jsx`).
- Default-export route pages as **named** function components (not an HOC call expression) so
  fast-refresh works; resolve portal context via `features/portal/context.usePortalContext()`.

## Feature & page organization
- Page orchestration lives under `pages/<area>/...`; domain logic (API wrappers, hooks, mappers,
  shared components, constants, types) under `features/<feature>/...`.
- Role portal pages resolve `{userId, locale}` themselves (via `usePortalContext` /
  `useStudentContext`) and render honest `Loading`/`Empty`/`Error`/`QueryState` — never a blank
  screen or a fabricated value.

## Production-data safety (hard rule)
- No mock/sample data on the production path. Sample data lives only in `src/demo/*` and renders
  only behind `isDemoEnabled()` (`VITE_ENABLE_DEMO_DATA=true`, **forced off in production**).
- Only `src/config/env.js` reads `import.meta.env`; everything else imports the validated `config`.
- The PWA never caches `/api/**` (NetworkOnly in `vite.config.ts` workbox) — do not weaken it.

## Verification (run from `frontend/`)
`npm run lint` · `npm run typecheck` · `npm test` · `npm run build` · `npm run test:e2e`.
Keep all green; never delete/skip/weaken a test to pass.
