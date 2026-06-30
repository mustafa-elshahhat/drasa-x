import { lazy } from 'react'
import type { AppRoute } from './route.types'

// Public marketing pages (Phase 20 frontend unification) render inside the
// light-themed PublicLayout; tagged with `layout: 'public'`.
const HomePage = lazy(() => import('../../pages/public/HomePage.jsx'))
const RequestDemoPage = lazy(() => import('../../pages/public/RequestDemoPage.jsx'))
const EventsPage = lazy(() => import('../../pages/public/EventsPage.jsx'))
const ActivitiesPage = lazy(() => import('../../pages/public/ActivitiesPage.jsx'))
const NewsPage = lazy(() => import('../../pages/public/NewsPage.jsx'))

export const publicRoutes: AppRoute[] = [
  { path: '/', titleKey: 'app.name', layout: 'public', Component: HomePage },
  { path: '/request-demo', titleKey: 'public.requestDemo.title', layout: 'public', Component: RequestDemoPage },
  { path: '/events', titleKey: 'public.events.title', layout: 'public', Component: EventsPage },
  { path: '/activities', titleKey: 'public.activities.title', layout: 'public', Component: ActivitiesPage },
  { path: '/news', titleKey: 'public.news.title', layout: 'public', Component: NewsPage },
]
