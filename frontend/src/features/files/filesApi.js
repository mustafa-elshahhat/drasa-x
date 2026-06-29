// =============================================================================
// Phase 16 — durable file storage API client (browser → DerasaX-backend ONLY).
//
// All upload/download goes through the canonical `api`/`apiFetch` client, so the
// browser never touches object storage or the AI service directly, and never
// sees a raw storage URL. Downloads are backend-mediated: we fetch the bytes
// (auth + 401-refresh handled by apiFetch), then hand the browser a blob to save.
// =============================================================================
import { api, apiFetch } from '../../lib/api/client'
import { problemFromResponse } from '../../lib/api/problemDetails'
import { toObject } from '../student/studentSchemas'

const BASE = '/api/v1/files'

/** Parse the download filename from a Content-Disposition header, if present. */
export function filenameFromResponse(res, fallback = 'download') {
  const cd = res.headers.get('content-disposition') || ''
  const star = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(cd)
  if (star?.[1]) {
    try {
      return decodeURIComponent(star[1].replace(/^["']|["']$/g, ''))
    } catch {
      /* fall through */
    }
  }
  const plain = /filename="?([^";]+)"?/i.exec(cd)
  return plain?.[1] || fallback
}

/** Trigger a browser "save as" for a blob. No-op outside a DOM (tests/SSR). */
export function saveBlob(blob, name) {
  if (typeof document === 'undefined' || typeof URL === 'undefined' || !URL.createObjectURL) return
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = name
  document.body.appendChild(a)
  a.click()
  a.remove()
  setTimeout(() => URL.revokeObjectURL(url), 0)
}

/**
 * Backend-mediated download of a relative file path → browser save.
 * Throws a normalized ApiError on a non-2xx response (e.g. 403/404/expired token).
 */
export async function downloadToBrowser(path, fallbackName = 'download') {
  if (/^https?:\/\//i.test(path)) throw new Error('downloadToBrowser requires a backend-relative path')
  const res = await apiFetch(path, { method: 'GET' })
  if (!res.ok) throw await problemFromResponse(res)
  const blob = await res.blob()
  const name = filenameFromResponse(res, fallbackName)
  saveBlob(blob, name)
  return name
}

export const filesApi = {
  /** Upload a self-service file (returns the stored-file metadata). */
  async upload({ file, purpose, relatedEntityType, relatedEntityId }, signal) {
    const form = new FormData()
    form.append('File', file)
    form.append('Purpose', purpose || 'Other')
    if (relatedEntityType) form.append('RelatedEntityType', relatedEntityType)
    if (relatedEntityId) form.append('RelatedEntityId', relatedEntityId)
    return toObject(await api.upload(`${BASE}/upload`, form, { signal }))
  },

  /** Client-safe metadata (no storage key / URL). */
  async metadata(fileId, signal) {
    return toObject(await api.get(`${BASE}/${fileId}/metadata`, { signal }))
  },

  /** Issue a short-lived signed download token + relative redeem URL. */
  async signedDownload(fileId) {
    return toObject(await api.post(`${BASE}/${fileId}/signed-download`, null))
  },

  /** Soft-delete a file (owner/admin). */
  async remove(fileId) {
    return api.del(`${BASE}/${fileId}`)
  },

  /** Backend-mediated download by file id. */
  async download(fileId, fallbackName = 'download') {
    return downloadToBrowser(`${BASE}/${fileId}/download`, fallbackName)
  },
}

/** Allowed purposes for the generic upload UI (relationship-sensitive ones use dedicated flows). */
export const FILE_PURPOSES = {
  MessageAttachment: 'MessageAttachment',
  CommunityAttachment: 'CommunityAttachment',
  CompetitionAttachment: 'CompetitionAttachment',
  SubmissionAttachment: 'SubmissionAttachment',
  ProfileImage: 'ProfileImage',
  Other: 'Other',
}
