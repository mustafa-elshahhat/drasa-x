# Phase 13.5 — Legacy CSS Burn-down

**Goal:** reduce legacy CSS debt and prevent new global-CSS sprawl, **without** any visual change.

**Hard reality (same cascade constraint proven in Phase 13.3):** the hand-authored CSS is imported
**un-layered** and is byte-identical-cascade by design. There are **no visual-regression tests** on
these screens. So this phase removes only **provably-dead** selectors and makes **byte-equivalent**
edits; structural moves and class→utility rewrites are deferred (see §5) because they would risk
silent regressions — a documented stop condition.

---

## 1. Before / after sizes

| File | Before (lines / bytes) | After (lines / bytes) | Δ |
|---|---|---|---|
| `legacy.css` | 396 / 28,301 | **385 / 27,568** | −11 lines / −733 B |
| `components.css` | 791 / 25,881 | **781 / 25,048** | −10 lines / −833 B |
| Other style files | unchanged | unchanged | — |

(Full `src/styles` inventory in `BASELINE_AUDIT.md` §7. `legacy.css` had **no new CSS added**.)

## 2. How dead selectors were identified (safely)

A scan (`scratchpad/css-analyze.mjs`) extracted every class selector from `legacy.css` and
`components.css` (209 + 160 classes) and tested each against **all** non-CSS source text. A class was
flagged dead only if **neither its full name nor its dynamic prefix** (before a trailing `__x`/`--x`)
appears anywhere in `src/`. Every flagged class was then **manually re-verified with grep** (incl.
dynamic-construction roots like `` `is-medal-${rank}` ``) before deletion.

This caught and **rejected** false positives: `is-medal-2` / `is-medal-3` were flagged but are built
dynamically (`` `is-medal-${rank}` `` in `LeaderboardRow.jsx`) → **kept**.

## 3. Deleted selectors (all verified zero-usage in app + tests)

### `legacy.css` (7 classes / 11 rules)

| Selector(s) | Why dead |
|---|---|
| `.ui-cols-2`, `.ui-cols-3`, `.ui-cols-4` (+ their refs in two `@media` blocks) | grid column-count helpers — **0** occurrences anywhere; superseded by `.ui-grid--auto` / explicit grids |
| `.ui-thread`, `.ui-thread__msg`, `.ui-thread__msg--mine`, `.ui-thread__body` | old message-thread styles — **0** occurrences; replaced by the `.chat-thread` / `.chat-bubble` component |

(One now-empty `@media (min-width: 561px) and (max-width: 900px)` block was removed entirely; the
`@media (max-width: 900px)` rule kept `.ui-split, .ui-split--even`.)

### `components.css` (4 classes / 10 rules)

| Selector(s) | Why dead |
|---|---|
| `.student-metric` (+ ` strong`, ` span`, `:hover strong`) | old dashboard tile — **0** occurrences; dashboards now use `.stats-card*` / shared `MetricCard`. (Also removes one of the four descendant `… span` rules flagged as a cascade hazard in Phase 13.3.) |
| `.student-row-link` (+ `:hover`) | **0** occurrences |
| `.student-kv` (+ ` dt`, ` dd`) | **0** occurrences |
| `.student-answer` | **0** occurrences |

A stale comment that referenced the removed `.student-metric` was corrected (the kept
`.student-dashboard > .ui-card` treatment it describes is unchanged).

## 4. Token / hardcoded-value review

A scan compared every hardcoded hex in both files against the `tokens.css` palette:

- The **only** exact token matches were `#fff` / `#ffffff` (= `--surface`). These are **not** substituted:
  in context they are **white text on colored fills** (`.ui-thread__msg--mine`, `.chat-bubble--mine`,
  medal ranks, `.ui-btn--dark`), which is semantically "white", **not** the surface token. Aliasing
  them to `var(--surface)` would be wrong-by-meaning and fragile. Left as literal `#fff` (intentional).
- The other hardcoded hexes (`#dc2626`, `#9ca3af`, `#16a34a`, `#eef1f5`, `#9a7000`, `#6a26c4`,
  `#c44e22`, `#b0b6c0`, `#cd7f32`, `#7b1fa2`, `#f3e5f5`, …) are **one-off prototype shades** (medal
  metals, realtime status dots, chip tints) with **no** matching global token. Substituting would
  require inventing tokens — i.e. *adding* token surface, not reducing — so they are left as-is.

So no color substitutions were made: there is no byte-equivalent, semantically-correct one available.

## 5. Remaining legacy CSS & justified deferrals

`legacy.css` is **not** dead weight — it is the **prototype component library** (chips, badges, tabs,
toggle, stepper, toolbar, the `viz-*` charts, `domain-*` cards/rows, `chat-*`, `quiz-*`) that backs the
shared components wired up in Phase 13.1. Each remaining selector maps to a live component.

Deliberately **not** done this phase (would risk visual regressions with no test coverage):

1. **Moving page-specific selectors** (e.g. `.student-subject-details__stats-grid` in `legacy.css`,
   the `.student-dashboard*` block in `components.css`) into a page/feature CSS file. Because the CSS
   is un-layered, **reordering** rules can change the cascade for any equal-specificity collision; the
   migration explicitly preserved byte-identical source order. Without visual tests, a move is unsafe.
2. **Converting one-off layout classes to Tailwind utilities.** Phase 13.3 proved that un-layered
   classes out-rank Tailwind's `utilities` layer, so this is not a safe 1:1 swap.
3. **Collapsing repeated patterns into shared variants** — valuable, but a behavioral refactor of
   live components better done with a visual baseline in place.

Recommended safe follow-up: introduce a Playwright/visual-snapshot baseline for the student + shared
screens, *then* perform the moves/utility conversions above with regression coverage.

## 6. Preserved behavior

- Dark/light: the palette is light-prototype only; no dark-mode rules touched.
- RTL/LTR: all kept rules use logical properties (`inset-inline-*`, `margin-inline-*`); the
  `[dir="rtl"] .domain-row__chev` flip is untouched.
- `prefers-reduced-motion` block untouched.
- `npm run build` ✅ (CSS bundles cleanly); `vitest` ✅ 243/243 (unit tests run with `css:false`, and
  no test referenced a removed class — verified).
