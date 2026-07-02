// Mirrors the backend's EnglishNameValidator regex (English letters with internal
// spaces/hyphen/apostrophe/dot as separators). Client-side check only — the backend is the
// authoritative gate and re-validates on every create/reset request regardless of this check.
const ENGLISH_NAME_PATTERN = /^[A-Za-z]+(?:[ '.-]+[A-Za-z]+)*$/

export function isEnglishName(value) {
  return ENGLISH_NAME_PATTERN.test((value ?? '').trim())
}
