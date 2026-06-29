# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: phase8\i-competitions.spec.js >> Phase 8 I — competitions & leaderboards >> I63 eligible (tenant-scoped) competitions are listed
- Location: e2e\phase8\i-competitions.spec.js:23:3

# Error details

```
Error: expect(received).toBe(expected) // Object.is equality

Expected: 1
Received: 3
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
          - link "My learning" [ref=e14] [cursor=pointer]:
            - /url: /app/student
            - img [ref=e15]
            - generic [ref=e18]: My learning
        - listitem [ref=e19]:
          - link "My subjects" [ref=e20] [cursor=pointer]:
            - /url: /app/student/subjects
            - img [ref=e21]
            - generic [ref=e23]: My subjects
        - listitem [ref=e24]:
          - link "Homework" [ref=e25] [cursor=pointer]:
            - /url: /app/student/homework
            - img [ref=e26]
            - generic [ref=e30]: Homework
        - listitem [ref=e31]:
          - link "Quizzes" [ref=e32] [cursor=pointer]:
            - /url: /app/student/quizzes
            - img [ref=e33]
            - generic [ref=e36]: Quizzes
        - listitem [ref=e37]:
          - link "AI tutor" [ref=e38] [cursor=pointer]:
            - /url: /app/student/ai-tutor
            - img [ref=e39]
            - generic [ref=e42]: AI tutor
        - listitem [ref=e43]:
          - link "Progress" [ref=e44] [cursor=pointer]:
            - /url: /app/student/progress
            - img [ref=e45]
            - generic [ref=e48]: Progress
        - listitem [ref=e49]:
          - link "Communities" [ref=e50] [cursor=pointer]:
            - /url: /app/student/communities
            - img [ref=e51]
            - generic [ref=e56]: Communities
        - listitem [ref=e57]:
          - link "Competitions" [ref=e58] [cursor=pointer]:
            - /url: /app/student/competitions
            - img [ref=e59]
            - generic [ref=e65]: Competitions
        - listitem [ref=e66]:
          - link "Office hours" [ref=e67] [cursor=pointer]:
            - /url: /app/student/office-hours
            - img [ref=e68]
            - generic [ref=e72]: Office hours
        - listitem [ref=e73]:
          - link "Notifications" [ref=e74] [cursor=pointer]:
            - /url: /app/student/notifications
            - img [ref=e75]
            - generic [ref=e78]: Notifications
        - listitem [ref=e79]:
          - link "Anonymous suggestions" [ref=e80] [cursor=pointer]:
            - /url: /app/student/suggestions
            - img [ref=e81]
            - generic [ref=e83]: Anonymous suggestions
        - listitem [ref=e84]:
          - link "Messages" [ref=e85] [cursor=pointer]:
            - /url: /app/messages
            - img [ref=e86]
            - generic [ref=e88]: Messages
        - listitem [ref=e89]:
          - link "Notifications" [ref=e90] [cursor=pointer]:
            - /url: /app/notifications
            - img [ref=e91]
            - generic [ref=e94]: Notifications
        - listitem [ref=e95]:
          - link "Profile" [ref=e96] [cursor=pointer]:
            - /url: /app/profile
            - img [ref=e97]
            - generic [ref=e101]: Profile
        - listitem [ref=e102]:
          - link "Security" [ref=e103] [cursor=pointer]:
            - /url: /app/security
            - img [ref=e104]
            - generic [ref=e107]: Security
  - generic [ref=e108]:
    - banner [ref=e109]:
      - generic [ref=e111]:
        - generic [ref=e112]: Tenant1 Student
        - generic [ref=e113]: Student
      - link "5 unread notifications" [ref=e114] [cursor=pointer]:
        - /url: /app/notifications
        - img [ref=e115]
        - generic [ref=e118]: "5"
      - generic [ref=e120]:
        - img [ref=e121]
        - generic [ref=e125]: Language
        - combobox "Language" [ref=e126]:
          - option "English" [selected]
          - option "العربية"
      - button "Sign out" [ref=e127] [cursor=pointer]:
        - img [ref=e128]
        - generic [ref=e131]: Sign out
    - main [ref=e132]:
      - navigation "Breadcrumb" [ref=e133]:
        - list [ref=e134]:
          - listitem [ref=e135]:
            - link "Dashboard" [ref=e136] [cursor=pointer]:
              - /url: /app
            - img [ref=e137]
          - listitem [ref=e139]:
            - link "Student dashboard" [ref=e140] [cursor=pointer]:
              - /url: /app/student
            - img [ref=e141]
          - listitem [ref=e143]:
            - generic [ref=e144]: Competitions
      - generic [ref=e146]:
        - heading "Competitions" [level=1] [ref=e147]
        - paragraph [ref=e148]: Competition eligibility and scores are backend-authoritative.
      - generic [ref=e149]:
        - link "E2E-PH14-SUB-1782684199785" [ref=e151] [cursor=pointer]:
          - /url: /app/student/competitions/39ac6d79-98f6-4686-af07-40b01c9aca77
          - img [ref=e152]
          - generic [ref=e155]: E2E-PH14-SUB-1782684199785
        - link "E2E-PH14-UISUB-1782684202407" [ref=e157] [cursor=pointer]:
          - /url: /app/student/competitions/83536da9-752a-4185-8ea5-5999f03d9549
          - img [ref=e158]
          - generic [ref=e161]: E2E-PH14-UISUB-1782684202407
        - link "Phase 8 Math Olympiad" [ref=e163] [cursor=pointer]:
          - /url: /app/student/competitions/E2E-PH8-COMP-T1
          - img [ref=e164]
          - generic [ref=e167]: Phase 8 Math Olympiad
```

# Test source

```ts
  1  | import { test, expect, request as pwRequest } from '@playwright/test'
  2  | import { HAS_CREDS, CODES, FIX, BACKEND, backendUp, login, nav, resetE2E, attachGuards, captureRequest, apiLogin } from '../support/ph8.js'
  3  | 
  4  | // Group I — Competitions & leaderboards (I63–I67).
  5  | test.describe('Phase 8 I — competitions & leaderboards', () => {
  6  |   test.describe.configure({ mode: 'serial' })
  7  |   test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')
  8  | 
  9  |   let context, page, guards
  10 |   test.beforeAll(async ({ browser }) => {
  11 |     test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
  12 |     await resetE2E() // clear STU-T1 entries
  13 |     context = await browser.newContext()
  14 |     page = await context.newPage()
  15 |     guards = attachGuards(page)
  16 |     await login(page, CODES.studentT1)
  17 |   })
  18 |   test.afterAll(async () => {
  19 |     guards?.assertNoForbidden()
  20 |     await context?.close()
  21 |   })
  22 | 
  23 |   test('I63 eligible (tenant-scoped) competitions are listed', async () => {
  24 |     await nav(page, '/app/student/competitions')
  25 |     await expect(page.getByRole('heading', { name: /^competitions$/i })).toBeVisible()
  26 |     await expect(page.getByText('Phase 8 Math Olympiad').first()).toBeVisible()
> 27 |     expect(await page.locator('.student-row-link').count()).toBe(1)
     |                                                             ^ Error: expect(received).toBe(expected) // Object.is equality
  28 |   })
  29 | 
  30 |   test('I64 competition details render state and the entry action', async () => {
  31 |     await nav(page, `/app/student/competitions/${FIX.compT1}`)
  32 |     await expect(page.getByRole('heading', { name: /phase 8 math olympiad/i })).toBeVisible()
  33 |     await expect(page.getByText(/leaderboard/i)).toBeVisible()
  34 |     await expect(page.getByRole('button', { name: /enter competition/i })).toBeVisible()
  35 |   })
  36 | 
  37 |   test('I65 entering a competition is server-confirmed and sends no client authority', async () => {
  38 |     await page.goto(`/app/student/competitions/${FIX.compT1}`)
  39 |     const reqP = captureRequest(page, `/competitions/${FIX.compT1}/entries`, 'POST')
  40 |     const respP = page.waitForResponse((r) => r.url().includes(`/competitions/${FIX.compT1}/entries`) && r.request().method() === 'POST')
  41 |     await page.getByRole('button', { name: /enter competition/i }).click()
  42 |     expect((await respP).status()).toBe(201)
  43 |     const req = await reqP
  44 |     const body = req.body || {}
  45 |     for (const forbidden of ['rank', 'points', 'score', 'studentId', 'tenantId']) {
  46 |       expect(body, `entry request leaked ${forbidden}`).not.toHaveProperty(forbidden)
  47 |     }
  48 |   })
  49 | 
  50 |   test('I66 the leaderboard shows server-owned rank and points', async () => {
  51 |     await page.goto(`/app/student/leaderboard/${FIX.compT1}`)
  52 |     const board = page.locator('.ui-card', { hasText: /leaderboard/i })
  53 |     await expect(board).toBeVisible()
  54 |     await expect(board.getByText(/no leaderboard rows/i)).toHaveCount(0)
  55 |     await expect(board.getByText('95')).toBeVisible() // OTHER-T1's persisted score
  56 |   })
  57 | 
  58 |   test('I67 a duplicate entry carries no client authority and a cross-tenant id is denied', async () => {
  59 |     // Duplicate entry: still no client-supplied rank/points; rejected by the server.
  60 |     await page.goto(`/app/student/competitions/${FIX.compT1}`)
  61 |     const reqP = captureRequest(page, `/competitions/${FIX.compT1}/entries`, 'POST')
  62 |     const respP = page.waitForResponse((r) => r.url().includes(`/competitions/${FIX.compT1}/entries`) && r.request().method() === 'POST')
  63 |     await page.getByRole('button', { name: /enter competition/i }).click()
  64 |     expect((await respP).status()).toBe(409)
  65 |     const body = (await reqP).body || {}
  66 |     for (const forbidden of ['rank', 'points', 'score', 'studentId']) {
  67 |       expect(body).not.toHaveProperty(forbidden)
  68 |     }
  69 |     // Cross-tenant competition id leaks nothing (404).
  70 |     const token = await apiLogin(CODES.studentT1)
  71 |     const ctx = await pwRequest.newContext()
  72 |     try {
  73 |       const res = await ctx.get(`${BACKEND}/api/v1/competitions/${FIX.compT2}`, { headers: { Authorization: `Bearer ${token}` } })
  74 |       expect(res.status()).toBe(404)
  75 |     } finally {
  76 |       await ctx.dispose()
  77 |     }
  78 |   })
  79 | })
  80 | 
```