// Numeric enum maps (the backend binds enums numerically — no JsonStringEnumConverter).
// Shared by the school-admin page modules; values match the backend ordinals.
export const GUARDIAN = ['Mother', 'Father', 'Guardian', 'Sibling', 'Other'] // value == index
export const CLASS_ROLE = ['SubjectTeacher', 'HomeroomTeacher', 'Assistant'] // value == index
export const AUDIENCE = { Students: 1, Parents: 2, Teachers: 4, All: 7 }
export const REQ_STATUS = { Open: 0, InProgress: 1, Resolved: 2, Rejected: 3, Closed: 4 }
export const SUPPORT_STATUS = { Pending: 1, Approved: 2, Rejected: 3, Completed: 4 }
export const SETTING_TYPE = { String: 0, Number: 1, Boolean: 2, Json: 3 }
export const USER_ROLES = ['Student', 'Teacher', 'Parent']
