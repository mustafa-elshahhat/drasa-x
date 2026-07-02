// Testing gap T-10 (route/detail/RBAC audit §10 item 10): "E2E for
// notification deep-links and message-thread opening from parent surfaces
// (phase13 covers shared surfaces only)." Existing phase13 specs only drive
// Student/Teacher through messaging + notifications — CODES13.parent was
// defined but never actually used in any assertion.
//
// Actors: CODES13.teacher/CODES13.parent do NOT work for the messaging half —
// PH13-PARENT is a deliberately unlinked "non-targeted role" fixture
// (DataSeederService.cs ~line 1260), so a message attempt correctly 403s
// ("You may only contact parents of students in your assigned classes.").
// Uses the base Phase 8 actors instead (TEACH-T1/ADMIN-T1, tenant-1) + the
// Phase 10 fixture PH10-PARENT-T1, a real, intentionally-linked parent of
// STU-T1 (a student in TEACH-T1's class) in the SAME tenant.
//
// Two genuinely different mechanisms, tested separately rather than
// conflated: (1) message-thread opening — verified directly, confirmed via
// reading ConversationService.cs that a real backend "new message"
// notification carries NO ActionUrl at all (StageNotificationAsync has no
// such parameter) — so a parent reaches a thread via the real
// /app/parent/conversations list, not a notification deep-link, exactly as
// the P2-1 fix wired it. (2) a notification deep-link — the ONLY backend path
// that actually sets a real ActionUrl is AnnouncementService (confirmed by
// reading its source), so that is the real flow exercised here, proving the
// sanitizer (safeInternalPath, F-11/P2-13) renders a working Link end-to-end
// against a genuine backend-supplied value, not just a mocked one.
import { test, expect } from '@playwright/test'
import { BACKEND, CODES, HAS_CREDS, backendUp, resetE2E, login, nav, attachGuards, apiLoginFull, startConversation, publishAnnouncement } from '../support/ph13.js'

const PARENT_LINKED = 'PH10-PARENT-T1'

test.describe('Phase 13 E / T-10 — parent message thread + notification deep-link', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run live E2E')

  let context, page, guards, convId
  const stamp = Date.now()
  const subject = `PH13 parent thread ${stamp}`
  const messageText = `PH13 parent deep-link E2E ${stamp}`
  const announcementTitle = `PH13 parent announcement ${stamp}`

  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
    await resetE2E()
    const { token: teacherToken } = await apiLoginFull(CODES.teacher)
    const parent = await apiLoginFull(PARENT_LINKED)
    convId = await startConversation(teacherToken, {
      participantUserId: parent.id,
      subject,
      firstMessage: messageText,
    })
    const adminToken = await apiLoginFull(CODES.schoolAdmin).then((r) => r.token)
    // targetAudience 2 = Parents (see publishAnnouncement's [Flags] doc).
    await publishAnnouncement(adminToken, { title: announcementTitle, body: 'Live E2E parent announcement.', targetAudience: 2 })

    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
  })
  test.afterAll(async () => { await context?.close() })

  test('E1 the parent conversations list links a row into the real shared thread', async () => {
    await login(page, PARENT_LINKED)
    await nav(page, '/app/parent/conversations')
    await expect(page.getByText(subject)).toBeVisible({ timeout: 15000 })
    await page.getByRole('link', { name: subject }).click()
    await expect(page).toHaveURL(new RegExp(`/app/messages/${convId}$`))
    await expect(page.getByText(messageText)).toBeVisible({ timeout: 15000 })
    guards.assertNoForbidden()
  })

  test('E2 a real backend-supplied notification deep-link renders a working sanitized Link', async () => {
    await nav(page, '/app/notifications')
    await expect(page.getByText(announcementTitle)).toBeVisible({ timeout: 15000 })

    const row = page.locator('li', { hasText: announcementTitle })
    const openLink = row.getByRole('link', { name: 'Open' })
    await expect(openLink).toBeVisible()
    const href = await openLink.getAttribute('href')
    // The sanitizer (safeInternalPath, F-11/P2-13) only ever renders a Link for
    // a real /app/... path — never an absolute/external URL — and this is the
    // real backend-supplied value (AnnouncementService.cs), not a mock.
    expect(href).toBe('/app/notifications')

    await openLink.click()
    await expect(page).toHaveURL(/\/app\/notifications$/)
    guards.assertNoForbidden()
  })
})
