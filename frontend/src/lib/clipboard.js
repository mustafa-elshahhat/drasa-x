// Copy-to-clipboard helper shared by any "one-time secret" display (generated login
// credentials, etc.). Prefers the async Clipboard API; falls back to a hidden textarea +
// execCommand('copy') for contexts where navigator.clipboard is unavailable (e.g. non-HTTPS,
// some test/embedded environments).
export async function copyToClipboard(text) {
  if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text)
      return true
    } catch {
      // fall through to the legacy path
    }
  }
  try {
    const textarea = document.createElement('textarea')
    textarea.value = text
    textarea.setAttribute('readonly', '')
    textarea.style.position = 'fixed'
    textarea.style.opacity = '0'
    document.body.appendChild(textarea)
    textarea.select()
    const ok = document.execCommand('copy')
    document.body.removeChild(textarea)
    return ok
  } catch {
    return false
  }
}
