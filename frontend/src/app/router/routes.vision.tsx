import { lazy } from 'react'
import type { AppRoute } from './route.types'
import { ROLES } from '../../features/auth/roles'

// Computer-vision attendance + engagement (Phase 15).
const VisionStaffPage = lazy(() => import('../../pages/vision/VisionStaffPage.jsx'))
const StudentVisionPage = lazy(() => import('../../pages/vision/StudentVisionPage.jsx'))
const ParentVisionPage = lazy(() => import('../../pages/vision/ParentVisionPage.jsx'))

export const visionRoutes: AppRoute[] = [
  { path: '/app/vision', titleKey: 'vision.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: VisionStaffPage },
  { path: '/app/vision/sessions/:sessionId', titleKey: 'vision.sessionTitleFallback', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: VisionStaffPage },
  { path: '/app/student/vision', titleKey: 'vision.studentTitle', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentVisionPage },
  { path: '/app/parent/children/:childId/vision', titleKey: 'vision.parentTitle', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentVisionPage },
]
