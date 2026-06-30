// =============================================================================
// Frontend role & permission model (Phase 7 §5) — now TypeScript.
//
// IMPORTANT: this mirrors the backend role model ONLY to drive navigation and
// UX (what to render). It is NEVER authorization — every protected backend
// endpoint independently enforces access from the signed access token. Hiding a
// link does not protect data.
//
// The five roles come straight from the backend AuthModel.Role claim. Runtime
// behavior is unchanged from the previous roles.js; only types were added.
// =============================================================================

export const ROLES = Object.freeze({
  STUDENT: 'Student',
  TEACHER: 'Teacher',
  PARENT: 'Parent',
  SCHOOL_ADMIN: 'SchoolAdmin',
  SYSTEM_ADMIN: 'SystemAdmin',
} as const)

/** A recognized backend role value (e.g. 'Student'). */
export type Role = (typeof ROLES)[keyof typeof ROLES]

export const ALL_ROLES: readonly Role[] = Object.freeze(Object.values(ROLES))

/**
 * Representative, frontend-side permission catalog used by PermissionGuard and
 * navigation filtering. These are coarse capability flags — the backend remains
 * the source of truth. Names are stable identifiers, not user-facing strings.
 */
export const PERMISSIONS = Object.freeze({
  VIEW_OWN_LEARNING: 'view_own_learning',
  VIEW_CLASS_ANALYTICS: 'view_class_analytics',
  MANAGE_ASSIGNMENTS: 'manage_assignments',
  VIEW_CHILD_PROGRESS: 'view_child_progress',
  MANAGE_SCHOOL: 'manage_school',
  MANAGE_PLATFORM: 'manage_platform',
  MANAGE_OWN_ACCOUNT: 'manage_own_account',
} as const)

/** A frontend permission flag (e.g. 'manage_school'). */
export type Permission = (typeof PERMISSIONS)[keyof typeof PERMISSIONS]

/** Role -> permissions the frontend will optimistically render for. */
const ROLE_PERMISSIONS: Record<Role, Permission[]> = Object.freeze({
  [ROLES.STUDENT]: [PERMISSIONS.VIEW_OWN_LEARNING, PERMISSIONS.MANAGE_OWN_ACCOUNT],
  [ROLES.TEACHER]: [
    PERMISSIONS.VIEW_CLASS_ANALYTICS,
    PERMISSIONS.MANAGE_ASSIGNMENTS,
    PERMISSIONS.MANAGE_OWN_ACCOUNT,
  ],
  [ROLES.PARENT]: [PERMISSIONS.VIEW_CHILD_PROGRESS, PERMISSIONS.MANAGE_OWN_ACCOUNT],
  [ROLES.SCHOOL_ADMIN]: [
    PERMISSIONS.MANAGE_SCHOOL,
    PERMISSIONS.VIEW_CLASS_ANALYTICS,
    PERMISSIONS.MANAGE_ASSIGNMENTS,
    PERMISSIONS.MANAGE_OWN_ACCOUNT,
  ],
  [ROLES.SYSTEM_ADMIN]: [
    PERMISSIONS.MANAGE_PLATFORM,
    PERMISSIONS.MANAGE_SCHOOL,
    PERMISSIONS.MANAGE_OWN_ACCOUNT,
  ],
})

/** Permissions granted to a role (empty array for unknown roles). */
export function permissionsForRole(role: string | null | undefined): Permission[] {
  return (ROLE_PERMISSIONS as Record<string, Permission[]>)[role ?? ''] || []
}

/** True if `role` holds `permission`. */
export function roleHasPermission(role: string | null | undefined, permission: string): boolean {
  return permissionsForRole(role).includes(permission as Permission)
}

/** True when `role` is one of the recognized backend roles. */
export function isKnownRole(role: string | null | undefined): role is Role {
  return ALL_ROLES.includes(role as Role)
}

/** The home/landing route a role should be sent to after login. */
export function homeRouteForRole(role: string | null | undefined): string {
  switch (role) {
    case ROLES.STUDENT:
      return '/app/student'
    case ROLES.TEACHER:
      return '/app/teacher'
    case ROLES.PARENT:
      return '/app/parent'
    case ROLES.SCHOOL_ADMIN:
      return '/app/school'
    case ROLES.SYSTEM_ADMIN:
      return '/app/system'
    default:
      return '/app'
  }
}
