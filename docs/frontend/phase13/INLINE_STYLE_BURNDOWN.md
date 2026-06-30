# Phase 13.3 — Inline Style Burn-down

**Goal:** remove static inline styles (→ Tailwind utilities / token classes); keep only
truly dynamic / library-required inline styles. **Hard constraint:** no visual change.

---

## 1. Headline numbers

| Metric | Count |
|---|---:|
| Inline `style={{…}}` (object literal) — **before** | **236** |
| Inline `style={var}` (single-brace, computed) — before | 5 |
| **Total inline-style attributes — before** | **241** |
| Static `style={{…}}` converted to Tailwind utilities/classes | **153** |
| Inline `style={{…}}` — **after** | **83** |
| Inline `style={var}` — after | 5 |
| **Total inline-style attributes — after** | **88** |
| **Reduction (object-literal styles)** | **153 / 236 = 64.8%** |
| **Reduction (all inline-style attributes)** | **(241→88) = 63.5%** |

> The **70% target was not fully reached** — but every un-converted style is justified below,
> and the gap is a **confirmed CSS-cascade safety blocker**, not unfinished work. Pushing past
> ~65% would require visually-unsafe rewrites (the documented stop condition).

## 2. How conversions were done (byte-equivalent, cascade-safe)

A Babel-AST codemod (`scratchpad/inline-to-tw*.mjs`, not committed) located every `style={{…}}`
and:

1. Converted **only fully-static** objects (every value a string/number/empty-template literal).
2. Mapped each declaration to a **real Tailwind utility** where an exact token/scale match exists
   (`display:flex`→`flex`, `gap:12px`→`gap-3`, `color:var(--text-dim)`→`text-muted`,
   `marginBottom:20px`→`mb-5`, `fontWeight:800`→`font-extrabold`, `borderRadius:8px`→`rounded-lg`,
   `background:var(--surface-2)`→`bg-surface-2`), and to a **Tailwind arbitrary value**
   (byte-identical CSS) only for off-scale values (`fontSize:28px`→`text-[28px]`,
   `marginBottom:22px`→`mb-[22px]`, `margin:'0 0 6px'`→`[margin:0_0_6px]`, `lineHeight:1.7`→`leading-[1.7]`).
3. Merged the utilities into the element's existing string `className` (or added one).

### The cascade constraint that bounds this phase

The hand-authored DerasaX CSS (`components.css`, `legacy.css`, …) is imported **un-layered**, which
in the CSS cascade **outranks Tailwind's `utilities` layer**. Inline styles, by contrast, outrank
*everything*. So an inline style can only be safely moved to a utility when **no un-layered author
rule sets the same property on that element** — otherwise the class would now win and the value would
change.

Concretely (real evidence): `StudentHomeworkPage` renders
`<h1 className="student-dashboard__welcome-title" style={{ fontSize:'28px', … }}>`. The borrowed class
sets `font-size: 32px` (components.css). The page deliberately overrides it to `28px` **inline**.
Converting to `text-[28px]` (utilities layer) would lose to `.student-dashboard__welcome-title` (un-layered)
→ the title would jump to **32px**. That is a visual regression.

The codemod therefore converts an element **only** when:

- the element is **not** an anchor (`a`/`Link`/`NavLink`) — `base.css` has un-layered `a { color: … }`; **and**
- **none of the element's classes declare a CSS property in the same family** as any converted
  declaration. (A 425-class → property-family index was built from the CSS to enforce this. Classes
  that set nothing, or nothing overlapping, are safe.)

This guarantees **zero visual change**: every converted style was either on a class-less element
(only UA defaults, which author utilities beat) or on a classed element whose classes provably don't
touch those properties.

### Verification that nothing else outranks the conversions

- `base.css` has only `body{}` and `a{color}` bare-tag rules (handled by the anchor exclusion).
- No bare `h1/p/div/span/section` author rules exist in `components/legacy/features.css`
  (only `a.<class>:hover` forms, which carry a class token and are captured by the index).
- Only 4 descendant bare-tag selectors exist (`.student-metric span`, `.ui-field > span`,
  `.viz-heatmap__legend li`, `.domain-plan__features li`) — none target any converted element.

## 3. Classification of the 88 remaining inline styles

| Category | ≈Count | Disposition |
|---|---:|---|
| **Dynamic / library-required** (must stay inline) | **~43** | data-derived colors (`background: theme.color`), progress widths (`width: \`${pct}%\``), gradients, chart `style={{ height }}` (recharts/Bars dims), and the 5 `style={var}` computed component styles (`Avatar`, `DataTable` align, `Metric` accent) |
| **Static — cascade-blocked (borrowed-class overrides)** | **~22** | e.g. `.student-dashboard__welcome-title/__welcome-header/__welcome-subtitle`, hero/tutor cards — the page intentionally overrides a property the borrowed class sets; converting would regress (see §2) |
| **Static — anchors** (`<Link>`/`<a>` with `color`) | **~15** | `base.css a{color:var(--brand)}` is un-layered; a `text-muted` utility would lose → link recolors. Breadcrumb links keep `textDecoration:none + color` inline |
| **Static — `.ui-btn` width/decoration overrides** | ~4 | full-width buttons (`width:100%`) borrowing `.ui-btn`; correct fix is a `.ui-btn--block` modifier, not a utility (out of scope) |
| **Static — invalid-property styles (kept verbatim on purpose)** | 4 | objects containing the typo `justify: 'center'` (not a real CSS prop) or `lineBreak: 'none'`. Rewriting risks "fixing" a no-op and **changing layout** — a stop condition. Left exactly as-is |
| **Static — border/transition shorthands** | ~4 | `borderInlineStart:'3px solid var(--accent)'`, `transition` shorthands — low-value, no exact utility; left inline |

## 4. Files changed (13 page modules)

All in `frontend/src/pages/**` (class-level changes only; no markup/logic changes):

`student/dashboard/StudentDashboardPage`, `student/homework/StudentHomeworkPage`,
`student/lessons/StudentLessonsPage`, `student/units/StudentUnitsPage`,
`student/tutor/StudentTutorPage`, `student/subjects/StudentSubjectsPage`,
`student/subjects/StudentSubjectDetailsPage`, `student/quizzes/StudentQuizzesPage`,
`student/quizzes/StudentQuizResultPage`, `student/quizzes/StudentQuizAttemptPage`,
`student/attendance/StudentAttendancePage` *(+ scattered: `app/communication/CommunicationMessagesPage`,
`parent/children/ParentChildAttendancePage`, `system/tenants/SystemTenantDetailsPage`,
`teacher/students/TeacherStudentsPage`)*.

`components/*` files (`Spinner`, `Skeleton`, `ProgressBar`, `Bars`, `ChartWrapper`, `Thumb`,
`QuizProgress`) were **not** touched — their inline styles are dynamic (prop-driven sizes).

## 5. Blocker statement (why not 70%)

Reaching 70% would require converting the ~45 **static cascade-blocked** styles, each of which
overrides a property its element's un-layered class also sets (or is an anchor color). The only ways
to convert them are:

1. **Move the hand-authored CSS into cascade layers** so utilities can override it — a **global**
   change to the entire app's cascade, with broad blast radius and **no visual-regression test
   coverage**. (The migration deliberately kept this CSS un-layered for byte-identical cascade.)
2. **Give each page its own dedicated classes** instead of borrowing dashboard/`ui-btn` classes — a
   per-page restyle of demo-gated pages with no visual tests.

Both are exactly the documented **stop conditions** ("a visual cleanup would change user-facing
behavior or break prototype alignment"; "a CSS deletion/changes risks breaking screens without test
coverage"). So the burn-down **stops at the safe boundary (64.8%)** and the remainder is justified
rather than force-converted.

## 6. Verification

After all conversions: `tsc --noEmit` ✅, `eslint .` ✅ (incl. the architecture guard),
`vitest run` ✅ **243/243**, `vite build` ✅. No test was changed. (Logs in Phase 13.7.)
