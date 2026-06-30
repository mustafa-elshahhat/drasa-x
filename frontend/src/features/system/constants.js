// Numeric enum maps (the backend binds enums numerically — no JsonStringEnumConverter).
// Shared by the system-admin page modules; values match the backend ordinals.
export const TENANT_STATUS = ['Active', 'Suspended', 'Archived'] // value == index
export const TENANT_TONE = ['success', 'danger', 'muted']
export const CURRICULUM = [{ value: 0, label: 'National' }]
export const SETTING_TYPE = { String: 0, Number: 1, Boolean: 2, Json: 3 }
export const SUPPORT_STATUS = { Completed: 4 }
