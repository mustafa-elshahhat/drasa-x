// =============================================================================
// Query-key factory (Phase 7 §8). Every key is scoped to the current user so a
// cached payload can never be read by a different user/tenant. Combined with a
// full cache clear on logout/expiry (see providers), this guarantees no
// cross-user or cross-tenant stale data survives an account switch.
//
// The frontend never knows the raw tenant id (the backend derives it from the
// token); scoping by the authenticated user id is the browser-side isolation
// boundary, and the tenant boundary is enforced server-side on every request.
// =============================================================================

const ANON = '__anon__'

/** Build a user-scoped query key. */
export function scopedKey(userId, parts) {
  return ['u', userId || ANON, ...(Array.isArray(parts) ? parts : [parts])]
}

/** Stable key builders for the representative Phase 7 data. */
export const queryKeys = {
  session: (userId) => scopedKey(userId, ['session']),
  profile: (userId) => scopedKey(userId, ['profile']),
  student: {
    root: (userId) => scopedKey(userId, ['student']),
    dashboard: (userId) => scopedKey(userId, ['student', 'dashboard']),
    subjects: (userId) => scopedKey(userId, ['student', 'subjects']),
    subject: (userId, subjectId) => scopedKey(userId, ['student', 'subjects', subjectId]),
    units: (userId, subjectId) => scopedKey(userId, ['student', 'units', subjectId]),
    lessons: (userId, unitId) => scopedKey(userId, ['student', 'lessons', unitId]),
    materials: (userId, lessonId) => scopedKey(userId, ['student', 'materials', lessonId]),
    comments: (userId, materialId) => scopedKey(userId, ['student', 'comments', materialId]),
    homework: (userId) => scopedKey(userId, ['student', 'homework']),
    homeworkSubmission: (userId, homeworkId) => scopedKey(userId, ['student', 'homework', homeworkId, 'submission']),
    quizzes: (userId) => scopedKey(userId, ['student', 'quizzes']),
    attempt: (userId, attemptId) => scopedKey(userId, ['student', 'attempt', attemptId]),
    attemptResult: (userId, attemptId) => scopedKey(userId, ['student', 'attempt', attemptId, 'result']),
    previousAttempts: (userId, quizId) => scopedKey(userId, ['student', 'quizzes', quizId, 'attempts']),
    progress: (userId) => scopedKey(userId, ['student', 'progress']),
    attendance: (userId) => scopedKey(userId, ['student', 'attendance']),
    communities: (userId) => scopedKey(userId, ['student', 'communities']),
    community: (userId, communityId) => scopedKey(userId, ['student', 'communities', communityId]),
    communityPosts: (userId, communityId) => scopedKey(userId, ['student', 'communities', communityId, 'posts']),
    competitions: (userId) => scopedKey(userId, ['student', 'competitions']),
    competition: (userId, competitionId) => scopedKey(userId, ['student', 'competitions', competitionId]),
    competitionSubmission: (userId, competitionId) => scopedKey(userId, ['student', 'competitions', competitionId, 'submission']),
    leaderboard: (userId, competitionId) => scopedKey(userId, ['student', 'competitions', competitionId, 'leaderboard']),
    officeHours: (userId) => scopedKey(userId, ['student', 'office-hours']),
    notifications: (userId) => scopedKey(userId, ['student', 'notifications']),
    announcements: (userId) => scopedKey(userId, ['student', 'announcements']),
    badges: (userId) => scopedKey(userId, ['student', 'badges']),
    points: (userId) => scopedKey(userId, ['student', 'points']),
    pointsLedger: (userId) => scopedKey(userId, ['student', 'points', 'ledger']),
    pointsLeaderboard: (userId, gradeId) => scopedKey(userId, ['student', 'points', 'leaderboard', gradeId || 'all']),
  },
  teacher: {
    root: (userId) => scopedKey(userId, ['teacher']),
    dashboard: (userId) => scopedKey(userId, ['teacher', 'dashboard']),
    classes: (userId) => scopedKey(userId, ['teacher', 'classes']),
    classStudents: (userId, classId) => scopedKey(userId, ['teacher', 'classes', classId, 'students']),
    subjects: (userId) => scopedKey(userId, ['teacher', 'subjects']),
    units: (userId, subjectId) => scopedKey(userId, ['teacher', 'units', subjectId]),
    lessons: (userId, unitId) => scopedKey(userId, ['teacher', 'lessons', unitId]),
    materials: (userId, lessonId) => scopedKey(userId, ['teacher', 'materials', lessonId]),
    students: (userId) => scopedKey(userId, ['teacher', 'students']),
    studentProgress: (userId, studentId) => scopedKey(userId, ['teacher', 'students', studentId, 'progress']),
    studentPoints: (userId, studentId) => scopedKey(userId, ['teacher', 'students', studentId, 'points']),
    quizzes: (userId) => scopedKey(userId, ['teacher', 'quizzes']),
    quiz: (userId, quizId) => scopedKey(userId, ['teacher', 'quizzes', quizId]),
    quizSubmissions: (userId, quizId) => scopedKey(userId, ['teacher', 'quizzes', quizId, 'submissions']),
    quizAssignments: (userId, quizId) => scopedKey(userId, ['teacher', 'quizzes', quizId, 'assignments']),
    quizAnalytics: (userId, quizId) => scopedKey(userId, ['teacher', 'quizzes', quizId, 'analytics']),
    submission: (userId, attemptId) => scopedKey(userId, ['teacher', 'submissions', attemptId]),
    notifications: (userId) => scopedKey(userId, ['teacher', 'notifications']),
    officeHours: (userId) => scopedKey(userId, ['teacher', 'office-hours']),
  },
  parent: {
    root: (userId) => scopedKey(userId, ['parent']),
    dashboard: (userId) => scopedKey(userId, ['parent', 'dashboard']),
    children: (userId) => scopedKey(userId, ['parent', 'children']),
    child: (userId, childId) => scopedKey(userId, ['parent', 'children', childId]),
    childProgress: (userId, childId) => scopedKey(userId, ['parent', 'children', childId, 'progress']),
    childAttendance: (userId, childId) => scopedKey(userId, ['parent', 'children', childId, 'attendance']),
    childPoints: (userId, childId) => scopedKey(userId, ['parent', 'children', childId, 'points']),
    documentRequests: (userId) => scopedKey(userId, ['parent', 'document-requests']),
    conversations: (userId) => scopedKey(userId, ['parent', 'conversations']),
    notifications: (userId) => scopedKey(userId, ['parent', 'notifications']),
    announcements: (userId) => scopedKey(userId, ['parent', 'announcements']),
  },
  // Phase 11 — School Admin Portal. Every key is user-scoped (and therefore tenant-scoped,
  // since the backend derives the tenant from the same authenticated token). A `role` arg is
  // threaded into the user-list keys so Students/Teachers/Parents views never share a cache.
  school: {
    root: (userId) => scopedKey(userId, ['school']),
    dashboard: (userId) => scopedKey(userId, ['school', 'dashboard']),
    profile: (userId) => scopedKey(userId, ['school', 'profile']),
    subscription: (userId) => scopedKey(userId, ['school', 'subscription']),
    usage: (userId) => scopedKey(userId, ['school', 'usage']),
    academicYears: (userId) => scopedKey(userId, ['school', 'academic-years']),
    terms: (userId) => scopedKey(userId, ['school', 'terms']),
    grades: (userId) => scopedKey(userId, ['school', 'grades']),
    classes: (userId) => scopedKey(userId, ['school', 'classes']),
    subjects: (userId) => scopedKey(userId, ['school', 'subjects']),
    users: (userId, role) => scopedKey(userId, ['school', 'users', role || 'all']),
    relationships: (userId) => scopedKey(userId, ['school', 'relationships']),
    subjectAssignments: (userId) => scopedKey(userId, ['school', 'teacher-subject-assignments']),
    classAssignments: (userId) => scopedKey(userId, ['school', 'teacher-class-assignments']),
    announcements: (userId) => scopedKey(userId, ['school', 'announcements']),
    documentRequests: (userId) => scopedKey(userId, ['school', 'document-requests']),
    communities: (userId) => scopedKey(userId, ['school', 'communities']),
    competitions: (userId) => scopedKey(userId, ['school', 'competitions']),
    reports: (userId) => scopedKey(userId, ['school', 'reports']),
    aiUsage: (userId) => scopedKey(userId, ['school', 'ai-usage']),
    support: (userId) => scopedKey(userId, ['school', 'support']),
    audit: (userId) => scopedKey(userId, ['school', 'audit']),
    settings: (userId) => scopedKey(userId, ['school', 'settings']),
  },
  // Phase 15 — computer-vision attendance + engagement. User-scoped (and therefore
  // tenant-scoped, since the backend derives the tenant from the same token).
  vision: {
    root: (userId) => scopedKey(userId, ['vision']),
    sessions: (userId) => scopedKey(userId, ['vision', 'sessions']),
    session: (userId, sessionId) => scopedKey(userId, ['vision', 'sessions', sessionId]),
    candidates: (userId, sessionId) => scopedKey(userId, ['vision', 'sessions', sessionId, 'candidates']),
    summary: (userId, sessionId) => scopedKey(userId, ['vision', 'sessions', sessionId, 'summary']),
    frames: (userId, sessionId) => scopedKey(userId, ['vision', 'sessions', sessionId, 'frames']),
    myEngagement: (userId) => scopedKey(userId, ['vision', 'my-engagement']),
    childEngagement: (userId, childId) => scopedKey(userId, ['vision', 'children', childId, 'engagement']),
  },
  // Phase 13 — communication (notifications, preferences, messaging). User-scoped; the backend
  // enforces the tenant + per-user boundary on every request and the SignalR hub groups.
  notifications: {
    root: (userId) => scopedKey(userId, ['notifications']),
    list: (userId) => scopedKey(userId, ['notifications', 'list']),
    unread: (userId) => scopedKey(userId, ['notifications', 'unread']),
    preferences: (userId) => scopedKey(userId, ['notifications', 'preferences']),
    conversations: (userId) => scopedKey(userId, ['notifications', 'conversations']),
    conversation: (userId, id) => scopedKey(userId, ['notifications', 'conversations', id]),
    messages: (userId, id) => scopedKey(userId, ['notifications', 'conversations', id, 'messages']),
  },
  // Phase 12 — System Admin (platform) Portal. Every key is user-scoped. The SystemAdmin is
  // platform-scope (no tenant); these reads aggregate across tenants from real backend contracts.
  system: {
    root: (userId) => scopedKey(userId, ['system']),
    dashboard: (userId) => scopedKey(userId, ['system', 'dashboard']),
    tenants: (userId, status) => scopedKey(userId, ['system', 'tenants', status || 'all']),
    tenant: (userId, tenantId) => scopedKey(userId, ['system', 'tenants', tenantId]),
    tenantSubscription: (userId, tenantId) => scopedKey(userId, ['system', 'tenants', tenantId, 'subscription']),
    tenantUsage: (userId, tenantId) => scopedKey(userId, ['system', 'tenants', tenantId, 'usage']),
    plans: (userId) => scopedKey(userId, ['system', 'plans']),
    subscriptions: (userId) => scopedKey(userId, ['system', 'subscriptions']),
    usage: (userId) => scopedKey(userId, ['system', 'usage']),
    aiUsage: (userId) => scopedKey(userId, ['system', 'ai-usage']),
    storage: (userId) => scopedKey(userId, ['system', 'storage']),
    support: (userId) => scopedKey(userId, ['system', 'support']),
    featureFlags: (userId) => scopedKey(userId, ['system', 'feature-flags']),
    announcements: (userId) => scopedKey(userId, ['system', 'announcements']),
    audit: (userId) => scopedKey(userId, ['system', 'audit']),
    security: (userId) => scopedKey(userId, ['system', 'security']),
    operational: (userId) => scopedKey(userId, ['system', 'operational-status']),
    settings: (userId) => scopedKey(userId, ['system', 'settings']),
  },
}

/** Recommended stale-times (ms). Documented centrally so cache policy is consistent. */
export const STALE = {
  short: 30_000, // volatile data
  medium: 5 * 60_000, // typical lists
  long: 30 * 60_000, // rarely-changing reference data
}
