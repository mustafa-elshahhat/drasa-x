// =============================================================================
// shared/files — durable file upload/download UI. Implementations live in
// src/components/files (they own the signed-token + per-purpose validation
// wiring); this is the single import surface for pages/features.
// =============================================================================
export { FileUpload } from '../../components/files/FileUpload'
export { FileDownloadButton } from '../../components/files/FileDownloadButton'
