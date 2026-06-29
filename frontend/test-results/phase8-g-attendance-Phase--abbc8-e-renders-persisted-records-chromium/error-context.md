# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: phase8\g-attendance.spec.js >> Phase 8 G — attendance >> G52 attendance renders persisted records
- Location: e2e\phase8\g-attendance.spec.js:22:3

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByLabel(/attendance status: present/i)
Expected: visible
Error: strict mode violation: getByLabel(/attendance status: present/i) resolved to 2 elements:
    1) <span class="student-status" aria-label="Attendance status: Present">Present</span> aka getByLabel('Attendance status: Present').first()
    2) <span class="student-status" aria-label="Attendance status: Present">Present</span> aka getByLabel('Attendance status: Present').nth(1)

Call log:
  - Expect "toBeVisible" with timeout 5000ms
  - waiting for getByLabel(/attendance status: present/i)

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
            - generic [ref=e144]: Attendance
      - generic [ref=e146]:
        - heading "Attendance" [level=1] [ref=e147]
        - paragraph [ref=e148]: Attendance is shown only from authoritative backend records.
      - status [ref=e149]:
        - img [ref=e150]
        - generic [ref=e152]:
          - paragraph [ref=e153]: Manual/imported attendance only
          - generic [ref=e154]: Computer-vision and facial-recognition attendance remain later-phase work and are not claimed here.
      - generic [ref=e155]:
        - heading "Attendance summary" [level=2] [ref=e157]
        - generic [ref=e159]:
          - generic [ref=e160]:
            - term [ref=e161]: Total sessions
            - definition [ref=e162]: "4"
          - generic [ref=e163]:
            - term [ref=e164]: Present
            - definition [ref=e165]: "2"
          - generic [ref=e166]:
            - term [ref=e167]: Absent
            - definition [ref=e168]: "1"
          - generic [ref=e169]:
            - term [ref=e170]: Late
            - definition [ref=e171]: "1"
          - generic [ref=e172]:
            - term [ref=e173]: Excused
            - definition [ref=e174]: "0"
          - generic [ref=e175]:
            - term [ref=e176]: Attendance percentage
            - definition [ref=e177]: 75%
      - generic [ref=e178]:
        - heading "Attendance records" [level=2] [ref=e180]
        - generic [ref=e182]:
          - generic [ref=e185]:
            - strong [ref=e186]: Jan 7, 2031, 2:00 AM
            - paragraph [ref=e187]: Absent
            - generic [ref=e188]:
              - generic [ref=e189]:
                - term [ref=e190]: source
                - definition [ref=e191]: Import
              - generic [ref=e192]:
                - term [ref=e193]: sessionKey
                - definition [ref=e194]: day
              - generic [ref=e195]:
                - term [ref=e196]: schoolClassId
                - definition [ref=e197]: PH8-CLASS-T1
              - generic [ref=e198]:
                - term [ref=e199]: recordedAt
                - definition [ref=e200]: Jan 7, 2031, 10:00 AM
          - generic [ref=e203]:
            - strong [ref=e204]: Jan 6, 2031, 2:00 AM
            - paragraph [ref=e205]: Late
            - generic [ref=e206]:
              - generic [ref=e207]:
                - term [ref=e208]: source
                - definition [ref=e209]: Manual
              - generic [ref=e210]:
                - term [ref=e211]: sessionKey
                - definition [ref=e212]: day
              - generic [ref=e213]:
                - term [ref=e214]: schoolClassId
                - definition [ref=e215]: PH8-CLASS-T1
              - generic [ref=e216]:
                - term [ref=e217]: recordedAt
                - definition [ref=e218]: Jan 6, 2031, 10:00 AM
          - generic [ref=e221]:
            - strong [ref=e222]: Jan 5, 2031, 2:00 AM
            - paragraph [ref=e223]: Present
            - generic [ref=e224]:
              - generic [ref=e225]:
                - term [ref=e226]: source
                - definition [ref=e227]: Manual
              - generic [ref=e228]:
                - term [ref=e229]: sessionKey
                - definition [ref=e230]: day
              - generic [ref=e231]:
                - term [ref=e232]: schoolClassId
                - definition [ref=e233]: PH8-CLASS-T1
              - generic [ref=e234]:
                - term [ref=e235]: recordedAt
                - definition [ref=e236]: Jan 5, 2031, 10:00 AM
          - generic [ref=e239]:
            - strong [ref=e240]: Jun 28, 2026, 3:00 AM
            - paragraph [ref=e241]: Present
            - generic [ref=e242]:
              - generic [ref=e243]:
                - term [ref=e244]: source
                - definition [ref=e245]: ComputerVision
              - generic [ref=e246]:
                - term [ref=e247]: sessionKey
                - definition [ref=e248]: cv-b5cf17dcfa9949878bee046375e7037e
              - generic [ref=e249]:
                - term [ref=e250]: recordedAt
                - definition [ref=e251]: Jun 29, 2026, 1:03 AM
```

# Test source

```ts
  1  | import { test, expect } from '@playwright/test'
  2  | import { HAS_CREDS, CODES, backendUp, login, nav, attachGuards } from '../support/ph8.js'
  3  | 
  4  | // Group G — Attendance (G52–G56).
  5  | test.describe('Phase 8 G — attendance', () => {
  6  |   test.describe.configure({ mode: 'serial' })
  7  |   test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')
  8  | 
  9  |   let context, page, guards
  10 |   test.beforeAll(async ({ browser }) => {
  11 |     test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
  12 |     context = await browser.newContext()
  13 |     page = await context.newPage()
  14 |     guards = attachGuards(page)
  15 |     await login(page, CODES.studentT1)
  16 |   })
  17 |   test.afterAll(async () => {
  18 |     guards?.assertNoForbidden()
  19 |     await context?.close()
  20 |   })
  21 | 
  22 |   test('G52 attendance renders persisted records', async () => {
  23 |     await nav(page, '/app/student/attendance')
  24 |     await expect(page.getByRole('heading', { name: 'Attendance', exact: true })).toBeVisible()
  25 |     await expect(page.getByText('Attendance records')).toBeVisible()
> 26 |     await expect(page.getByLabel(/attendance status: present/i)).toBeVisible()
     |                                                                  ^ Error: expect(locator).toBeVisible() failed
  27 |   })
  28 | 
  29 |   test('G53 attendance summary totals and percentage match the server', async () => {
  30 |     await nav(page, '/app/student/attendance')
  31 |     await expect(page.getByText('66.67%')).toBeVisible()
  32 |   })
  33 | 
  34 |   test('G54 a student with no attendance sees the correct empty state', async ({ browser }) => {
  35 |     const ctx = await browser.newContext()
  36 |     const p = await ctx.newPage()
  37 |     try {
  38 |       await login(p, 'PH8-OTHER-T1') // enrolled student with zero attendance records
  39 |       await nav(p, '/app/student/attendance')
  40 |       await expect(p.getByRole('heading', { name: 'Attendance', exact: true })).toBeVisible()
  41 |       await expect(p.getByText(/no attendance records available/i)).toBeVisible()
  42 |     } finally {
  43 |       await ctx.close()
  44 |     }
  45 |   })
  46 | 
  47 |   test('G55 no computer-vision claim is made; source is shown truthfully', async () => {
  48 |     await nav(page, '/app/student/attendance')
  49 |     await expect(page.getByText(/manual\/imported attendance only/i)).toBeVisible()
  50 |     await expect(page.locator('body')).not.toContainText(/facial recognition produced/i)
  51 |     await expect(page.locator('body')).not.toContainText(/computer vision produced/i)
  52 |   })
  53 | 
  54 |   test('G56 attendance stays correct after reload', async () => {
  55 |     await page.goto('/app/student/attendance')
  56 |     await expect(page.getByText('66.67%')).toBeVisible()
  57 |     await expect(page.getByLabel(/attendance status: present/i)).toBeVisible()
  58 |   })
  59 | })
  60 | 
```