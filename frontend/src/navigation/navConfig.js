// =============================================================================
// Role-aware navigation definitions (Phase 7 §7). Each destination declares the
// roles allowed to SEE it. Filtering here is navigation/UX only — the backend
// authorizes every request regardless of what links are shown.
// =============================================================================
import {
  LayoutDashboard,
  GraduationCap,
  BarChart3,
  ClipboardList,
  Users,
  Building2,
  ShieldCheck,
  UserCircle,
  KeyRound,
  BookOpen,
  ClipboardCheck,
  Bot,
  TrendingUp,
  Bell,
  Trophy,
  Medal,
  CalendarClock,
  MessageSquare,
  FileText,
  CalendarDays,
  Layers,
  Link2,
  Megaphone,
  CreditCard,
  Settings,
  ScrollText,
  UserCog,
  ScanFace,
  Landmark,
  FileStack,
  Layers3,
  HardDrive,
  Lightbulb,
  Activity,
} from 'lucide-react'
import { ROLES } from '../features/auth/roles'

/**
 * @typedef {Object} NavItem
 * @property {string} key
 * @property {string} labelKey  i18n key
 * @property {string} to        route path
 * @property {Function} icon
 * @property {string[]} roles   roles allowed to see this item
 */

/** @type {NavItem[]} */
export const NAV_ITEMS = [
  // Per-role landing dashboards
  { key: 'student', labelKey: 'nav.dashboard', to: '/app/student', icon: LayoutDashboard, roles: [ROLES.STUDENT] },
  { key: 'student-subjects', labelKey: 'student.subjects.title', to: '/app/student/subjects', icon: BookOpen, roles: [ROLES.STUDENT] },
  { key: 'student-homework', labelKey: 'student.homework.title', to: '/app/student/homework', icon: ClipboardCheck, roles: [ROLES.STUDENT] },
  { key: 'student-quizzes', labelKey: 'student.quizzes.title', to: '/app/student/quizzes', icon: ClipboardList, roles: [ROLES.STUDENT] },
  { key: 'student-tutor', labelKey: 'student.tutor.title', to: '/app/student/ai-tutor', icon: Bot, roles: [ROLES.STUDENT] },
  { key: 'student-progress', labelKey: 'student.progress.title', to: '/app/student/progress', icon: TrendingUp, roles: [ROLES.STUDENT] },
  { key: 'student-engagement', labelKey: 'student.engagement.title', to: '/app/student/engagement', icon: Activity, roles: [ROLES.STUDENT] },
  { key: 'student-attendance', labelKey: 'student.attendance.title', to: '/app/student/attendance', icon: CalendarDays, roles: [ROLES.STUDENT] },
  { key: 'student-vision', labelKey: 'vision.studentTitle', to: '/app/student/vision', icon: ScanFace, roles: [ROLES.STUDENT] },
  { key: 'student-communities', labelKey: 'student.communities.title', to: '/app/student/communities', icon: Users, roles: [ROLES.STUDENT] },
  { key: 'student-competitions', labelKey: 'student.competitions.title', to: '/app/student/competitions', icon: Trophy, roles: [ROLES.STUDENT] },
  { key: 'student-office-hours', labelKey: 'student.officeHours.title', to: '/app/student/office-hours', icon: CalendarClock, roles: [ROLES.STUDENT] },
  { key: 'student-badges', labelKey: 'student.badges.title', to: '/app/student/badges', icon: Medal, roles: [ROLES.STUDENT] },
  { key: 'student-announcements', labelKey: 'student.announcements.title', to: '/app/student/announcements', icon: Megaphone, roles: [ROLES.STUDENT] },
  { key: 'student-suggestions', labelKey: 'student.suggestions.title', to: '/app/student/suggestions', icon: MessageSquare, roles: [ROLES.STUDENT] },
  { key: 'student-settings', labelKey: 'student.settings.title', to: '/app/student/settings', icon: Settings, roles: [ROLES.STUDENT] },
  // Teacher portal — Teacher ONLY. SchoolAdmin must not see these (SchoolAdmin
  // Teacher-portal removal); SchoolAdmin's own administrative pages live under
  // the school-* nav items below.
  { key: 'teacher', labelKey: 'teacher.dashboard.title', to: '/app/teacher', icon: BarChart3, roles: [ROLES.TEACHER] },
  { key: 'teacher-classes', labelKey: 'teacher.classes.title', to: '/app/teacher/classes', icon: Users, roles: [ROLES.TEACHER] },
  { key: 'teacher-subjects', labelKey: 'teacher.subjects.title', to: '/app/teacher/subjects', icon: BookOpen, roles: [ROLES.TEACHER] },
  { key: 'teacher-students', labelKey: 'teacher.students.title', to: '/app/teacher/students', icon: GraduationCap, roles: [ROLES.TEACHER] },
  { key: 'teacher-quizzes', labelKey: 'teacher.quizzes.title', to: '/app/teacher/quizzes', icon: ClipboardList, roles: [ROLES.TEACHER] },
  { key: 'teacher-generate', labelKey: 'teacher.generate.title', to: '/app/teacher/quiz-generate', icon: Bot, roles: [ROLES.TEACHER] },
  { key: 'teacher-homework', labelKey: 'teacher.homework.title', to: '/app/teacher/homework', icon: ClipboardCheck, roles: [ROLES.TEACHER] },
  { key: 'teacher-competitions', labelKey: 'teacher.competitions.title', to: '/app/teacher/competitions', icon: Trophy, roles: [ROLES.TEACHER] },
  { key: 'teacher-office-hours', labelKey: 'teacher.officeHours.title', to: '/app/teacher/office-hours', icon: CalendarClock, roles: [ROLES.TEACHER] },
  { key: 'teacher-communities', labelKey: 'teacher.communities.title', to: '/app/teacher/communities', icon: Users, roles: [ROLES.TEACHER] },
  { key: 'teacher-vision', labelKey: 'vision.title', to: '/app/vision', icon: ScanFace, roles: [ROLES.TEACHER] },
  { key: 'teacher-notifications', labelKey: 'teacher.notifications.title', to: '/app/teacher/notifications', icon: Bell, roles: [ROLES.TEACHER] },
  { key: 'parent', labelKey: 'parent.dashboard.title', to: '/app/parent', icon: BarChart3, roles: [ROLES.PARENT] },
  { key: 'parent-children', labelKey: 'parent.children.title', to: '/app/parent/children', icon: Users, roles: [ROLES.PARENT] },
  { key: 'parent-documents', labelKey: 'parent.documents.title', to: '/app/parent/documents', icon: FileText, roles: [ROLES.PARENT] },
  { key: 'parent-conversations', labelKey: 'parent.conversations.title', to: '/app/parent/conversations', icon: MessageSquare, roles: [ROLES.PARENT] },
  { key: 'parent-announcements', labelKey: 'parent.announcements.title', to: '/app/parent/announcements', icon: ClipboardList, roles: [ROLES.PARENT] },
  { key: 'parent-notifications', labelKey: 'parent.notifications.title', to: '/app/parent/notifications', icon: Bell, roles: [ROLES.PARENT] },
  { key: 'school', labelKey: 'nav.schoolAdmin', to: '/app/school', icon: Building2, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-profile', labelKey: 'school.pages.profile.title', to: '/app/school/profile', icon: Landmark, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-students', labelKey: 'school.pages.students.title', to: '/app/school/students', icon: GraduationCap, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-teachers', labelKey: 'school.pages.teachers.title', to: '/app/school/teachers', icon: Users, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-parents', labelKey: 'school.pages.parents.title', to: '/app/school/parents', icon: UserCog, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-relationships', labelKey: 'school.pages.relationships.title', to: '/app/school/relationships', icon: Link2, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-assignments', labelKey: 'school.pages.teacherAssignments.title', to: '/app/school/teacher-assignments', icon: ClipboardCheck, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-classes', labelKey: 'school.pages.classes.title', to: '/app/school/classes', icon: Layers, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-academic-years', labelKey: 'school.pages.academicYears.title', to: '/app/school/academic-years', icon: CalendarDays, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-terms', labelKey: 'school.pages.terms.title', to: '/app/school/terms', icon: FileStack, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-grades', labelKey: 'school.pages.grades.title', to: '/app/school/grades', icon: Layers3, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-curriculum', labelKey: 'school.pages.curriculum.title', to: '/app/school/curriculum', icon: BookOpen, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-announcements', labelKey: 'school.pages.announcements.title', to: '/app/school/announcements', icon: Megaphone, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-document-requests', labelKey: 'school.pages.documentRequests.title', to: '/app/school/document-requests', icon: FileText, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-communities', labelKey: 'school.pages.communities.title', to: '/app/school/communities', icon: Users, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-competitions', labelKey: 'school.pages.competitions.title', to: '/app/school/competitions', icon: Trophy, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-suggestions', labelKey: 'school.pages.suggestions.title', to: '/app/school/suggestions', icon: Lightbulb, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-gamification-rules', labelKey: 'school.pages.gamificationRules.title', to: '/app/school/gamification-rules', icon: Medal, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-reports', labelKey: 'school.pages.reports.title', to: '/app/school/reports', icon: BarChart3, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-ai-usage', labelKey: 'school.pages.aiUsage.title', to: '/app/school/ai-usage', icon: Bot, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-storage', labelKey: 'school.pages.storage.title', to: '/app/school/storage', icon: HardDrive, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-subscription', labelKey: 'school.pages.subscription.title', to: '/app/school/subscription', icon: CreditCard, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-support', labelKey: 'school.pages.support.title', to: '/app/school/support', icon: MessageSquare, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-audit', labelKey: 'school.pages.audit.title', to: '/app/school/audit-log', icon: ScrollText, roles: [ROLES.SCHOOL_ADMIN] },
  { key: 'school-settings', labelKey: 'school.pages.settings.title', to: '/app/school/settings', icon: Settings, roles: [ROLES.SCHOOL_ADMIN] },
  // System Admin (platform) portal — Phase 12
  { key: 'system', labelKey: 'nav.systemAdmin', to: '/app/system', icon: ShieldCheck, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-tenants', labelKey: 'system.pages.tenants.title', to: '/app/system/tenants', icon: Building2, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-onboarding', labelKey: 'system.pages.onboarding.title', to: '/app/system/onboarding', icon: UserCog, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-plans', labelKey: 'system.pages.plans.title', to: '/app/system/plans', icon: CreditCard, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-subscriptions', labelKey: 'system.pages.subscriptions.title', to: '/app/system/subscriptions', icon: CreditCard, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-usage', labelKey: 'system.pages.usage.title', to: '/app/system/usage', icon: BarChart3, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-ai-usage', labelKey: 'system.pages.aiUsage.title', to: '/app/system/ai-usage', icon: Bot, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-storage', labelKey: 'system.pages.storage.title', to: '/app/system/storage', icon: Layers, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-support', labelKey: 'system.pages.support.title', to: '/app/system/support', icon: MessageSquare, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-feature-flags', labelKey: 'system.pages.featureFlags.title', to: '/app/system/feature-flags', icon: ClipboardCheck, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-announcements', labelKey: 'system.pages.announcements.title', to: '/app/system/announcements', icon: Megaphone, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-health', labelKey: 'system.pages.health.title', to: '/app/system/health', icon: TrendingUp, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-errors', labelKey: 'system.pages.errorMonitoring.title', to: '/app/system/errors', icon: Bell, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-audit', labelKey: 'system.pages.audit.title', to: '/app/system/audit', icon: ScrollText, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-security', labelKey: 'system.pages.security.title', to: '/app/system/security', icon: KeyRound, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-backups', labelKey: 'system.pages.backups.title', to: '/app/system/backups', icon: FileText, roles: [ROLES.SYSTEM_ADMIN] },
  { key: 'system-settings', labelKey: 'system.pages.settings.title', to: '/app/system/settings', icon: Settings, roles: [ROLES.SYSTEM_ADMIN] },
  // Shared communication surfaces (Phase 13) — messages are tenant-role only (SystemAdmin is
  // platform-scope and the backend 403s it there — see routes.shared.tsx). Notifications are
  // genuinely open to every role (`NotificationsController` is plain `[Authorize]`), so its nav
  // visibility now matches what the header NotificationBell already does for SystemAdmin.
  { key: 'messages', labelKey: 'messages.title', to: '/app/messages', icon: MessageSquare, roles: [ROLES.STUDENT, ROLES.TEACHER, ROLES.PARENT, ROLES.SCHOOL_ADMIN] },
  { key: 'notifications-center', labelKey: 'notifications.title', to: '/app/notifications', icon: Bell, roles: [ROLES.STUDENT, ROLES.TEACHER, ROLES.PARENT, ROLES.SCHOOL_ADMIN, ROLES.SYSTEM_ADMIN] },
  // Shared, available to every authenticated role
  { key: 'profile', labelKey: 'nav.profile', to: '/app/profile', icon: UserCircle, roles: 'all' },
  { key: 'security', labelKey: 'nav.security', to: '/app/security', icon: KeyRound, roles: 'all' },
]

// A generic "home" entry resolved per role at render time.
export const DASHBOARD_ITEM = {
  key: 'dashboard',
  labelKey: 'nav.dashboard',
  icon: LayoutDashboard,
}

/** Items a given role is allowed to see. */
export function navItemsForRole(role) {
  return NAV_ITEMS.filter((item) => item.roles === 'all' || (Array.isArray(item.roles) && item.roles.includes(role)))
}
