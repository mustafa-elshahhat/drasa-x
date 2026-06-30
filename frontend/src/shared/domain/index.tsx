// =============================================================================
// shared/domain — DerasaX domain-specific composite cards/rows.
//
// These are prototype-aligned, DerasaX-specific components (subject/unit/quiz
// cards, leaderboard/lesson rows, pricing & usage widgets). They live in
// src/components/domain (the implementation layer); this facade is the single
// import surface for pages/features so `src/components` stays internal.
//
// They are re-exported as-is (the underlying `.jsx` components carry no prop
// types yet); we deliberately do NOT invent prop types that could misrepresent
// the runtime shape. Type-hardening these is a tracked follow-up.
// =============================================================================
export {
  Thumb,
  SubjectCard,
  UnitCard,
  LessonRow,
  QuizCard,
  ChildCard,
  LeaderboardRow,
  PricingPlanCard,
  UsageBars,
  ServiceStatusList,
} from '../../components/domain'
