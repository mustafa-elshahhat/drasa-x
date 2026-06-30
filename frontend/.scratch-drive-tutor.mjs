import { chromium } from '@playwright/test'

const BASE_URL = 'http://127.0.0.1:5173'
const SHOT_DIR = 'C:/Users/musta/AppData/Local/Temp/claude/D--projects-drasa-x/ca52952d-6cbf-4a09-9292-ff9dd79478d6/scratchpad'

const browser = await chromium.launch()
const page = await browser.newPage()
const consoleErrors = []
page.on('console', (msg) => { if (msg.type() === 'error') consoleErrors.push(msg.text()) })
page.on('pageerror', (err) => consoleErrors.push(String(err)))

async function shot(name) {
  await page.screenshot({ path: `${SHOT_DIR}/${name}.png`, fullPage: true })
  console.log(`[screenshot] ${name}.png`)
}

try {
  console.log('--- login ---')
  await page.goto(`${BASE_URL}/login`)
  await page.getByLabel(/login code/i).fill('STU-T1')
  await page.getByLabel(/^password/i).fill('Local@Dev123')
  await page.getByRole('button', { name: /sign in/i }).click()
  await page.waitForURL((u) => !u.pathname.endsWith('/login'), { timeout: 15000 })
  console.log('logged in, url =', page.url())

  console.log('--- navigate to AI tutor ---')
  await page.goto(`${BASE_URL}/app/student/ai-tutor`)
  await page.waitForSelector('text=/ai tutor|المعلم الذكي/i', { timeout: 15000 })
  await shot('01-tutor-empty')

  console.log('--- ask: SQUARES (no subject selected) ---')
  await page.fill('#dxtutorin', 'SQUARES')
  const resp1P = page.waitForResponse((r) => r.url().includes('/api/v1/ai/tutor') && r.request().method() === 'POST', { timeout: 30000 })
  await page.press('#dxtutorin', 'Enter')
  const resp1 = await resp1P
  const json1 = await resp1.json().catch(() => ({}))
  console.log('SQUARES response:', JSON.stringify({ status: json1.status, grounded: json1.grounded, citationCount: (json1.citations || []).length, answerPreview: (json1.answer || '').slice(0, 120) }, null, 2))
  await page.waitForTimeout(500)
  await shot('02-tutor-squares')

  console.log('--- select Mathematics, ask: Water Cycle (expect autoroute to Science) ---')
  await page.goto(`${BASE_URL}/app/student/ai-tutor`)
  await page.waitForSelector('#dxtutorin', { timeout: 15000 })
  const subjectOptions = await page.locator('select').first().locator('option').allTextContents()
  console.log('available subject options:', subjectOptions)
  const mathOption = subjectOptions.find((o) => /math/i.test(o))
  if (mathOption) await page.locator('select').first().selectOption({ label: mathOption })
  await page.fill('#dxtutorin', 'Water Cycle')
  const resp2P = page.waitForResponse((r) => r.url().includes('/api/v1/ai/tutor') && r.request().method() === 'POST', { timeout: 30000 })
  await page.press('#dxtutorin', 'Enter')
  const resp2 = await resp2P
  const json2 = await resp2.json().catch(() => ({}))
  console.log('Water Cycle response:', JSON.stringify({ status: json2.status, grounded: json2.grounded, detectedSubject: json2.detectedSubject, subjectAutoRouted: json2.subjectAutoRouted, citationCount: (json2.citations || []).length, answerPreview: (json2.answer || '').slice(0, 120) }, null, 2))
  await page.waitForTimeout(500)
  await shot('03-tutor-watercycle')

  console.log('--- ask: linear equation (expect curriculum-backed if indexed) ---')
  await page.goto(`${BASE_URL}/app/student/ai-tutor`)
  await page.waitForSelector('#dxtutorin', { timeout: 15000 })
  await page.fill('#dxtutorin', 'What is a linear equation?')
  const resp3P = page.waitForResponse((r) => r.url().includes('/api/v1/ai/tutor') && r.request().method() === 'POST', { timeout: 30000 })
  await page.press('#dxtutorin', 'Enter')
  const resp3 = await resp3P
  const json3 = await resp3.json().catch(() => ({}))
  console.log('linear equation response:', JSON.stringify({ status: json3.status, grounded: json3.grounded, citationCount: (json3.citations || []).length }, null, 2))
  await page.waitForTimeout(500)
  await shot('04-tutor-linear-equation')

  console.log('--- console errors ---')
  console.log(consoleErrors.length ? consoleErrors.join('\n') : '(none)')
} catch (err) {
  console.error('DRIVE SCRIPT FAILED:', err)
  await shot('99-failure')
  process.exitCode = 1
} finally {
  await browser.close()
}
