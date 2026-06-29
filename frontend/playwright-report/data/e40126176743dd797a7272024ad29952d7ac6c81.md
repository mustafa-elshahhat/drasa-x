# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: phase10\b-parent-children.spec.js >> Phase 10 — parent child monitoring >> PH10-B3 parent views the child academic progress from the backend
- Location: e2e\phase10\b-parent-children.spec.js:29:3

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByText('Quiz results')
Expected: visible
Error: strict mode violation: getByText('Quiz results') resolved to 2 elements:
    1) <h2 class="ui-card__title">Quiz results</h2> aka getByRole('heading', { name: 'Quiz results', exact: true })
    2) <h3 class="ui-state__title">No quiz results yet.</h3> aka getByRole('heading', { name: 'No quiz results yet.' })

Call log:
  - Expect "toBeVisible" with timeout 5000ms
  - waiting for getByText('Quiz results')

```

# Page snapshot

```yaml
- generic [ref=e3]:
  - link "Skip to main content" [ref=e4] [cursor=pointer]:
    - /url: "#main-content"
  - complementary [ref=e5]:
    - navigation "Main navigation" [ref=e6]:
      - generic [ref=e7]:
        - img [ref=e8]
        - generic [ref=e11]: DerasaX
      - list [ref=e12]:
        - listitem [ref=e13]:
          - link "Parent dashboard" [ref=e14] [cursor=pointer]:
            - /url: /app/parent
            - img [ref=e15]
            - generic [ref=e17]: Parent dashboard
        - listitem [ref=e18]:
          - link "My children" [ref=e19] [cursor=pointer]:
            - /url: /app/parent/children
            - img [ref=e20]
            - generic [ref=e25]: My children
        - listitem [ref=e26]:
          - link "Document requests" [ref=e27] [cursor=pointer]:
            - /url: /app/parent/documents
            - img [ref=e28]
            - generic [ref=e31]: Document requests
        - listitem [ref=e32]:
          - link "Teacher conversations" [ref=e33] [cursor=pointer]:
            - /url: /app/parent/conversations
            - img [ref=e34]
            - generic [ref=e36]: Teacher conversations
        - listitem [ref=e37]:
          - link "Announcements" [ref=e38] [cursor=pointer]:
            - /url: /app/parent/announcements
            - img [ref=e39]
            - generic [ref=e42]: Announcements
        - listitem [ref=e43]:
          - link "Notifications" [ref=e44] [cursor=pointer]:
            - /url: /app/parent/notifications
            - img [ref=e45]
            - generic [ref=e48]: Notifications
        - listitem [ref=e49]:
          - link "Messages" [ref=e50] [cursor=pointer]:
            - /url: /app/messages
            - img [ref=e51]
            - generic [ref=e53]: Messages
        - listitem [ref=e54]:
          - link "Notifications" [ref=e55] [cursor=pointer]:
            - /url: /app/notifications
            - img [ref=e56]
            - generic [ref=e59]: Notifications
        - listitem [ref=e60]:
          - link "Profile" [ref=e61] [cursor=pointer]:
            - /url: /app/profile
            - img [ref=e62]
            - generic [ref=e66]: Profile
        - listitem [ref=e67]:
          - link "Security" [ref=e68] [cursor=pointer]:
            - /url: /app/security
            - img [ref=e69]
            - generic [ref=e72]: Security
  - generic [ref=e73]:
    - banner [ref=e74]:
      - generic [ref=e76]:
        - generic [ref=e77]: Phase 10 Linked Parent
        - generic [ref=e78]: Parent
      - link "Notifications" [ref=e79] [cursor=pointer]:
        - /url: /app/notifications
        - img [ref=e80]
      - generic [ref=e84]:
        - img [ref=e85]
        - generic [ref=e89]: Language
        - combobox "Language" [ref=e90]:
          - option "English" [selected]
          - option "العربية"
      - button "Sign out" [ref=e91] [cursor=pointer]:
        - img [ref=e92]
        - generic [ref=e95]: Sign out
    - main [ref=e96]:
      - navigation "Breadcrumb" [ref=e97]:
        - list [ref=e98]:
          - listitem [ref=e99]:
            - link "Dashboard" [ref=e100] [cursor=pointer]:
              - /url: /app
            - img [ref=e101]
          - listitem [ref=e103]:
            - link "Parent dashboard" [ref=e104] [cursor=pointer]:
              - /url: /app/parent
            - img [ref=e105]
          - listitem [ref=e107]:
            - link "My children" [ref=e108] [cursor=pointer]:
              - /url: /app/parent/children
            - img [ref=e109]
          - listitem [ref=e111]:
            - link "f8eecdb1-22f6-463b-8598-e37801ba4e63" [ref=e112] [cursor=pointer]:
              - /url: /app/parent/children/f8eecdb1-22f6-463b-8598-e37801ba4e63
            - img [ref=e113]
          - listitem [ref=e115]:
            - generic [ref=e116]: progress
      - generic [ref=e118]:
        - heading "Academic progress" [level=1] [ref=e119]
        - paragraph [ref=e120]: Persisted progress, results, and stored AI insights for your child.
      - generic [ref=e121]:
        - heading "Progress summary" [level=2] [ref=e123]
        - generic [ref=e125]:
          - generic [ref=e126]:
            - term [ref=e127]: studentId
            - definition [ref=e128]: f8eecdb1-22f6-463b-8598-e37801ba4e63
          - generic [ref=e129]:
            - term [ref=e130]: lessonsTracked
            - definition [ref=e131]: "0"
          - generic [ref=e132]:
            - term [ref=e133]: lessonsCompleted
            - definition [ref=e134]: "0"
          - generic [ref=e135]:
            - term [ref=e136]: averageLessonCompletion
            - definition [ref=e137]: "0"
          - generic [ref=e138]:
            - term [ref=e139]: quizAttempts
            - definition [ref=e140]: "0"
          - generic [ref=e141]:
            - term [ref=e142]: averageQuizPercentage
            - definition [ref=e143]: "0"
          - generic [ref=e144]:
            - term [ref=e145]: subjectsTracked
            - definition [ref=e146]: "1"
      - generic [ref=e147]:
        - heading "Subject progress" [level=2] [ref=e149]
        - generic [ref=e153]:
          - generic [ref=e154]:
            - term [ref=e155]: id
            - definition [ref=e156]: E2E-PH8-SP-T1
          - generic [ref=e157]:
            - term [ref=e158]: subjectId
            - definition [ref=e159]: PH8-SUBJECT-T1
          - generic [ref=e160]:
            - term [ref=e161]: completionPercentage
            - definition [ref=e162]: "40"
          - generic [ref=e163]:
            - term [ref=e164]: averageScore
            - definition [ref=e165]: "78"
          - generic [ref=e166]:
            - term [ref=e167]: lessonsCompleted
            - definition [ref=e168]: "2"
          - generic [ref=e169]:
            - term [ref=e170]: totalLessons
            - definition [ref=e171]: "5"
          - generic [ref=e172]:
            - term [ref=e173]: lastActivityAt
            - definition [ref=e174]: Jun 28, 2026, 1:01 AM
      - generic [ref=e175]:
        - heading "Quiz results" [level=2] [ref=e177]
        - status [ref=e179]:
          - img [ref=e180]
          - heading "No quiz results yet." [level=3] [ref=e183]
      - generic [ref=e184]:
        - heading "AI insights" [level=2] [ref=e186]
        - generic [ref=e187]:
          - paragraph [ref=e188]: These are stored AI outputs; no AI runs when you view this page.
          - generic [ref=e191]:
            - generic [ref=e192]:
              - term [ref=e193]: id
              - definition [ref=e194]: E2E-PH8-INSIGHT-T1
            - generic [ref=e195]:
              - term [ref=e196]: performance
              - definition [ref=e197]: "3"
            - generic [ref=e198]:
              - term [ref=e199]: confidenceScore
              - definition [ref=e200]: "0.8"
            - generic [ref=e201]:
              - term [ref=e202]: summary
              - definition [ref=e203]: "Phase 8 fixture insight: steady progress in algebra."
            - generic [ref=e204]:
              - term [ref=e205]: period
              - definition [ref=e206]: "1"
            - generic [ref=e207]:
              - term [ref=e208]: periodStart
              - definition [ref=e209]: 2026-06-21T22:01:42.62027Z
            - generic [ref=e210]:
              - term [ref=e211]: periodEnd
              - definition [ref=e212]: 2026-06-28T22:01:42.62027Z
            - generic [ref=e213]:
              - term [ref=e214]: generatedAt
              - definition [ref=e215]: Jun 29, 2026, 1:01 AM
            - generic [ref=e216]:
              - term [ref=e217]: source
              - definition [ref=e218]: stored-ai-output
      - generic [ref=e219]:
        - heading "Recommendations" [level=2] [ref=e221]
        - generic [ref=e225]:
          - generic [ref=e226]:
            - term [ref=e227]: id
            - definition [ref=e228]: E2E-PH8-REC-T1
          - generic [ref=e229]:
            - term [ref=e230]: title
            - definition [ref=e231]: Practice linear equations
          - generic [ref=e232]:
            - term [ref=e233]: body
            - definition [ref=e234]: Complete 5 practice problems on linear equations this week.
          - generic [ref=e235]:
            - term [ref=e236]: status
            - definition [ref=e237]: "0"
          - generic [ref=e238]:
            - term [ref=e239]: generatedAt
            - definition [ref=e240]: Jun 27, 2026, 1:01 AM
          - generic [ref=e241]:
            - term [ref=e242]: source
            - definition [ref=e243]: stored-ai-output
```

# Test source

```ts
  1  | // Phase 10 — Parent Portal child monitoring + relationship/tenant scoping (live).
  2  | import { test, expect } from '@playwright/test'
  3  | import { CODES10, CHILD_NAME, HAS_CREDS, backendUp, login, nav, attachGuards, apiLoginId } from '../support/ph10.js'
  4  | 
  5  | test.describe('Phase 10 — parent child monitoring', () => {
  6  |   test.beforeEach(async () => {
  7  |     test.skip(!HAS_CREDS, 'E2E credentials not provided')
  8  |     test.skip(!(await backendUp()), 'DerasaX-backend not running')
  9  |   })
  10 | 
  11 |   test('PH10-B1 parent sees only the linked child in the children list', async ({ page }) => {
  12 |     const guards = attachGuards(page)
  13 |     await login(page, CODES10.parentLinked)
  14 |     await nav(page, '/app/parent/children')
  15 |     await expect(page.getByText(new RegExp(CHILD_NAME))).toBeVisible()
  16 |     guards.assertNoForbidden()
  17 |   })
  18 | 
  19 |   test('PH10-B2 parent opens the linked child profile (real backend data)', async ({ page }) => {
  20 |     const guards = attachGuards(page)
  21 |     await login(page, CODES10.parentLinked)
  22 |     await nav(page, '/app/parent/children')
  23 |     await page.getByText(new RegExp(CHILD_NAME)).click()
  24 |     await expect(page).toHaveURL(/\/app\/parent\/children\/[^/]+$/)
  25 |     await expect(page.getByText('Progress summary')).toBeVisible()
  26 |     guards.assertNoForbidden()
  27 |   })
  28 | 
  29 |   test('PH10-B3 parent views the child academic progress from the backend', async ({ page }) => {
  30 |     const guards = attachGuards(page)
  31 |     const childId = await apiLoginId(CODES10.studentT1)
  32 |     await login(page, CODES10.parentLinked)
  33 |     await nav(page, `/app/parent/children/${childId}/progress`)
  34 |     await expect(page.getByRole('heading', { name: 'Academic progress' })).toBeVisible()
> 35 |     await expect(page.getByText('Quiz results')).toBeVisible()
     |                                                  ^ Error: expect(locator).toBeVisible() failed
  36 |     guards.assertNoForbidden()
  37 |   })
  38 | 
  39 |   test('PH10-B4 parent views the child attendance read model', async ({ page }) => {
  40 |     const guards = attachGuards(page)
  41 |     const childId = await apiLoginId(CODES10.studentT1)
  42 |     await login(page, CODES10.parentLinked)
  43 |     await nav(page, `/app/parent/children/${childId}/attendance`)
  44 |     await expect(page.getByText('Attendance summary')).toBeVisible()
  45 |     // STU-T1 has seeded attendance — at least the summary labels render.
  46 |     await expect(page.getByText('Total sessions')).toBeVisible()
  47 |     guards.assertNoForbidden()
  48 |   })
  49 | 
  50 |   test('PH10-B5 a parent with no children sees the empty state', async ({ page }) => {
  51 |     const guards = attachGuards(page)
  52 |     await login(page, CODES10.parentNoChild)
  53 |     await nav(page, '/app/parent/children')
  54 |     await expect(page.getByText('No children are linked to your account.')).toBeVisible()
  55 |     guards.assertNoForbidden()
  56 |   })
  57 | 
  58 |   test('PH10-B6 parent cannot open a same-tenant UNLINKED child (denied, no profile leak)', async ({ page }) => {
  59 |     const guards = attachGuards(page)
  60 |     const otherId = await apiLoginId(CODES10.otherT1)
  61 |     await login(page, CODES10.parentLinked)
  62 |     await nav(page, `/app/parent/children/${otherId}`)
  63 |     // The backend returns 403; the UI renders an error state, never the child profile.
  64 |     await expect(page.getByText('Something went wrong')).toBeVisible()
  65 |     await expect(page.getByText('Progress summary')).toHaveCount(0)
  66 |     guards.assertNoForbidden() // 4xx is expected; only 5xx/AI/archived would fail here
  67 |   })
  68 | 
  69 |   test('PH10-B7 parent cannot open a CROSS-TENANT child (hidden as not found)', async ({ page }) => {
  70 |     const guards = attachGuards(page)
  71 |     const crossId = await apiLoginId(CODES10.studentT2)
  72 |     await login(page, CODES10.parentLinked)
  73 |     await nav(page, `/app/parent/children/${crossId}`)
  74 |     await expect(page.getByText('Something went wrong')).toBeVisible()
  75 |     await expect(page.getByText('Progress summary')).toHaveCount(0)
  76 |     guards.assertNoForbidden()
  77 |   })
  78 | 
  79 |   test('PH10-B8 parent submits a document request for a linked child', async ({ page }) => {
  80 |     const guards = attachGuards(page)
  81 |     await login(page, CODES10.parentLinked)
  82 |     await nav(page, '/app/parent/documents')
  83 |     await expect(page.getByRole('heading', { name: 'Document requests' })).toBeVisible()
  84 |     await page.getByLabel('Child').selectOption({ label: CHILD_NAME })
  85 |     await page.getByLabel('Subject').fill('Official transcript request')
  86 |     await page.getByLabel('Details').fill('Please issue an official transcript for the term.')
  87 |     await page.getByRole('button', { name: /submit request/i }).click()
  88 |     await expect(page.getByText('Request submitted')).toBeVisible()
  89 |     guards.assertNoForbidden()
  90 |   })
  91 | })
  92 | 
```