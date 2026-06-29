# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: phase8\h-communities.spec.js >> Phase 8 H — communities >> H57 eligible (tenant-scoped) communities are listed
- Location: e2e\phase8\h-communities.spec.js:23:3

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
            - generic [ref=e144]: Communities
      - generic [ref=e146]:
        - heading "Communities" [level=1] [ref=e147]
        - paragraph [ref=e148]: Tenant-scoped communities available to the student.
      - generic [ref=e149]:
        - link "E2E-PH14-GNO-1782684199999" [ref=e151] [cursor=pointer]:
          - /url: /app/student/communities/f9c0dd75-c3f0-4397-a188-ea9f00abe293
          - img [ref=e152]
          - generic [ref=e155]: E2E-PH14-GNO-1782684199999
        - link "E2E-PH14-GOK-1782684199977" [ref=e157] [cursor=pointer]:
          - /url: /app/student/communities/06f4a47a-f752-4b1d-9561-5f71579cc4d7
          - img [ref=e158]
          - generic [ref=e161]: E2E-PH14-GOK-1782684199977
        - link "Phase 8 Math Club" [ref=e163] [cursor=pointer]:
          - /url: /app/student/communities/E2E-PH8-COMM-T1
          - img [ref=e164]
          - generic [ref=e167]: Phase 8 Math Club
```

# Test source

```ts
  1  | import { test, expect, request as pwRequest } from '@playwright/test'
  2  | import { HAS_CREDS, CODES, FIX, BACKEND, backendUp, login, nav, resetE2E, attachGuards, apiLogin } from '../support/ph8.js'
  3  | 
  4  | // Group H — Communities & comments (H57–H62).
  5  | test.describe('Phase 8 H — communities', () => {
  6  |   test.describe.configure({ mode: 'serial' })
  7  |   test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')
  8  | 
  9  |   let context, page, guards
  10 |   test.beforeAll(async ({ browser }) => {
  11 |     test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
  12 |     await resetE2E() // clear STU-T1 membership/posts
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
  23 |   test('H57 eligible (tenant-scoped) communities are listed', async () => {
  24 |     await nav(page, '/app/student/communities')
  25 |     await expect(page.getByRole('heading', { name: /^communities$/i })).toBeVisible()
  26 |     await expect(page.getByText('Phase 8 Math Club').first()).toBeVisible()
> 27 |     expect(await page.locator('.student-row-link').count()).toBe(1)
     |                                                             ^ Error: expect(received).toBe(expected) // Object.is equality
  28 |   })
  29 | 
  30 |   test('H58 community details render info and the membership action', async () => {
  31 |     await nav(page, `/app/student/communities/${FIX.commT1}`)
  32 |     await expect(page.getByRole('heading', { name: /phase 8 math club/i })).toBeVisible()
  33 |     await expect(page.getByText(/community posts/i)).toBeVisible()
  34 |     await expect(page.getByRole('button', { name: /join community/i })).toBeVisible()
  35 |   })
  36 | 
  37 |   test('H59 joining a community persists across reload', async () => {
  38 |     await page.goto(`/app/student/communities/${FIX.commT1}`)
  39 |     const joinP = page.waitForResponse((r) => r.url().includes(`/communities/${FIX.commT1}/join`) && r.request().method() === 'POST')
  40 |     await page.getByRole('button', { name: /join community/i }).click()
  41 |     expect((await joinP).status()).toBe(200)
  42 |     // The membership is durably persisted (verified against the backend directly).
  43 |     const token = await apiLogin(CODES.studentT1)
  44 |     const ctx = await pwRequest.newContext()
  45 |     try {
  46 |       const res = await ctx.get(`${BACKEND}/api/v1/communities/${FIX.commT1}`, { headers: { Authorization: `Bearer ${token}` } })
  47 |       const json = await res.json()
  48 |       expect((json?.data ?? json)?.memberCount).toBe(2)
  49 |     } finally {
  50 |       await ctx.dispose()
  51 |     }
  52 |   })
  53 | 
  54 |   test('H60 a member can create a post and it is shown', async () => {
  55 |     await page.goto(`/app/student/communities/${FIX.commT1}`)
  56 |     const body = 'Phase 8 community post from the live acceptance run.'
  57 |     await page.getByLabel(/new post/i).fill(body)
  58 |     const postP = page.waitForResponse((r) => r.url().includes(`/communities/${FIX.commT1}/posts`) && r.request().method() === 'POST')
  59 |     await page.getByRole('button', { name: /submit/i }).click()
  60 |     expect((await postP).status()).toBe(201)
  61 |     await expect(page.getByText(body).first()).toBeVisible()
  62 |   })
  63 | 
  64 |   test('H61 a member can add a material comment and it is shown', async () => {
  65 |     await page.goto(`/app/student/materials/${FIX.materialT1}`)
  66 |     const body = 'Phase 8 material comment from the live acceptance run.'
  67 |     await page.getByLabel(/add a comment/i).fill(body)
  68 |     const cP = page.waitForResponse((r) => r.url().includes(`/lesson-materials/${FIX.materialT1}/comments`) && r.request().method() === 'POST')
  69 |     await page.getByRole('button', { name: /submit/i }).click()
  70 |     expect((await cP).status()).toBe(201)
  71 |     await expect(page.getByText(body).first()).toBeVisible()
  72 |   })
  73 | 
  74 |   test('H62 a cross-tenant community is denied and user content renders as safe text', async () => {
  75 |     // Cross-tenant community id leaks nothing — the backend returns 404.
  76 |     const token = await apiLogin(CODES.studentT1)
  77 |     const ctx = await pwRequest.newContext()
  78 |     try {
  79 |       const res = await ctx.get(`${BACKEND}/api/v1/communities/${FIX.commT2}`, { headers: { Authorization: `Bearer ${token}` } })
  80 |       expect(res.status()).toBe(404)
  81 |     } finally {
  82 |       await ctx.dispose()
  83 |     }
  84 |     // The UI surfaces a usable error state (no foreign-tenant content rendered).
  85 |     await page.goto(`/app/student/communities/${FIX.commT2}`)
  86 |     await expect(page.locator('[role="alert"]').first()).toBeVisible()
  87 |     await expect(page.getByText('Phase 8 Math Club')).toHaveCount(0)
  88 | 
  89 |     // Existing posts render as escaped text (no raw HTML / injected script).
  90 |     await page.goto(`/app/student/communities/${FIX.commT1}`)
  91 |     await expect(page.getByText(/welcome to the phase 8 math club/i).first()).toBeVisible()
  92 |     await expect(page.locator('main script:not([src])')).toHaveCount(0)
  93 |   })
  94 | })
  95 | 
```