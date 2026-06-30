# DerasaX Prototype Design Audit (Phase 9)

Date: 2026-06-30
Prototype source of truth: `derasax-presentation-prototype/project/DerasaX Prototype.dc.html`
(the `.html` sibling is a bundled runtime; `.dc.html` is the readable design source).

Purpose: document the prototype's design language and how it maps onto the migrated
frontend's tokens and shared components, so future UI work stays prototype-aligned.

## Key finding: the design language is already faithfully ported

The prototype's design tokens (its `this.T` object) were transcribed verbatim into the
production CSS **before** this migration. Phase 4 moved that token block, unchanged, into
`src/styles/tokens.css` (the single source of truth) and proved (byte-identical built CSS)
that the migration changed **no** visual output. The architecture migration therefore did
not need to re-skin anything — it organized the existing prototype-aligned UI into shared
primitives + real page modules. Phase 1 additionally exposed the same tokens to Tailwind via
`@theme`, so new/migrated UI uses token-backed utilities (`bg-brand`, `text-ink`, …).

## Token map: prototype `T` → `tokens.css` / Tailwind `@theme`

| Prototype `T` | tokens.css var | Tailwind utility token | Value |
|---|---|---|---|
| `teal` | `--brand` | `--color-brand` | `#0c7288` |
| `tealD` | `--brand-strong` | `--color-brand-strong` | `#0a5c6e` |
| `tealSoft` | `--brand-soft` | `--color-brand-soft` | `#e5f0f2` |
| `bg` | `--bg` | `--color-page` | `#f1f5f8` |
| `card` | `--surface` | `--color-surface` | `#ffffff` |
| `ink` | `--text` | `--color-ink` | `#1d2026` |
| `muted` | `--text-dim` | `--color-muted` | `#6e7485` |
| `line` | `--border` | `--color-line` | `#e9eaf0` |
| `orange/green/red/purple/amber/blue` | `--orange/--success/--danger/--purple/--warning/--info` | matching `--color-*` | prototype hexes |
| `r / rs / rx` | `--radius / --radius-sm / --radius-xs` | `--radius-card/soft/xs` | `16 / 11 / 9 px` |
| `sh / shL` | `--shadow / --shadow-lg` | `--shadow-card / --shadow-pop` | prototype shadows |
| `font / fontAr` | `--font / --font-ar` | `--font-sans / --font-arabic` | Inter / Cairo first |

Fonts intentionally diverge from the prototype: production **self-hosts** Inter + Cairo via
`@fontsource` (`styles/fonts.css`) to keep a strict `font-src 'self'` CSP, instead of the
prototype's Google Fonts link. Inter is listed before Segoe UI (prototype had Segoe first).

## Per-role accent map (matches the prototype `navConfig` colors)

Set via `data-role` on the app shell → `--accent` (kept in `tokens.css`):

| Role | Accent | Source |
|---|---|---|
| Student | teal `#0c7288` (brand default) | prototype `student` |
| Teacher | purple `#8a38f5` | prototype `teacher` |
| Parent | orange `#ff6636` | prototype `parent` |
| School Admin | blue `#2f6fed` | prototype `school` |
| System Admin | green `#00a824` | prototype `system` |

Primary buttons stay brand-teal everywhere (matching the prototype); only surfaces, metric
tiles, and the sidebar pick up the per-role accent. The student subject theming
(`features/student/theme.js`) keeps the prototype's per-subject color/icon, with sample
teacher/progress surfaced only in demo mode.

## Pattern → shared component map

| Prototype pattern | DerasaX component(s) |
|---|---|
| App shell (sticky 64px topbar + 270px sidebar + content) | `layouts/AppShell` + `styles/layout.css` |
| Brand wordmark "Derasa**X**" (X tinted teal) | `app-header__brand*` (layout.css) |
| Sidebar role pill + "Need help?" card | `app-sidebar__role` / `app-sidebar__help*` |
| `chip(text, tone)` | `shared/ui` `Badge` (= `Chip`) |
| `metric()` KPI tile | `shared/data-display` `MetricCard` (= `Metric`), `StatGrid` |
| Content cards | `shared/ui` `Card`, `PageHeader`, `SectionHeader` |
| `tabs()` underline strip | `shared/ui` `Tabs` |
| Data tables (stack-on-mobile) | `shared/data-display` `DataTable`, `components/data/ResourceTable` |
| Forms | `shared/form` (`TextField`/`SelectField`/`DateField`/`FormField`/…) |
| Loading / empty / error | `shared/feedback` `LoadingState`/`EmptyState`/`ErrorState`/`QueryState` |
| Off-canvas / menus | `shared/ui` `Drawer`, `Dropdown` |

## RTL / LTR (preserved)

Direction is driven by `<html dir/lang>` set in `src/i18n/direction.js`
(`applyDocumentDirection`), with `RTL_LANGUAGES = ['ar', …]`. All layout CSS uses **logical
properties** (`margin-inline`, `inset-inline-*`, `border-inline-*`); physical flips use
`[dir='rtl']` overrides only where needed (drawer transform, chevrons via `scaleX(-1)`).
New shared primitives use Tailwind logical utilities (`start-*`/`end-*`). Arabic swaps to the
Cairo font via `[lang='ar'] body, [dir='rtl'] body`. None of this changed during the migration.

## Alignment status

- Migrated pages render byte-for-byte the same prototype-aligned markup (the split relocated
  code verbatim; the only intentional visual change is honest empty/zero states replacing
  former mock data when demo mode is off).
- New/migrated UI consumes the shared, token-backed primitives rather than one-off CSS.
- Public marketing, auth, and portal areas share the same DerasaX brand tokens while keeping
  their distinct layouts (marketing `PublicLayout` vs the `/app` shell), as the prototype does.
