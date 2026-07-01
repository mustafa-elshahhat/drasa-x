// Numeric enum maps (the backend binds enums numerically — no JsonStringEnumConverter).
// Shared by the system-admin page modules; values match the backend ordinals.
export const TENANT_STATUS = ['Active', 'Suspended', 'Archived'] // value == index
export const TENANT_TONE = ['success', 'danger', 'muted']
export const CURRICULUM = [{ value: 0, label: 'National' }]
export const SETTING_TYPE = { String: 0, Number: 1, Boolean: 2, Json: 3 }
export const SUPPORT_STATUS = { Completed: 4 }
// SubscriptionPlan enum is 1-indexed on the backend (Free=1, Pro=2, Enterprise=3) — NOT
// 0-indexed like the other enum arrays above. Index this by value, never by position.
export const PLAN_TIER = [undefined, 'Free', 'Pro', 'Enterprise']
export const BILLING_PERIOD = ['Monthly', 'Quarterly', 'Annual', 'Custom'] // value == index
