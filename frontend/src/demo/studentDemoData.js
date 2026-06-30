// =============================================================================
// Prototype-style SAMPLE data for the student experience.
//
// This file is the ONLY home for the formerly-inline student mock data. It is
// consumed solely behind `isDemoEnabled()` (VITE_ENABLE_DEMO_DATA=true, never in
// production). With demo mode off, none of these values reach the UI.
// =============================================================================

/** Sample subject cards (used when the subjects API returns nothing, demo only). */
export const DEMO_SUBJECTS = [
  { id: 'math', name: 'Mathematics', nameAr: 'الرياضيات', teacher: 'Mr. Osama Refat', units: 6, lessons: 24, progress: 62 },
  { id: 'phys', name: 'Physics', nameAr: 'الفيزياء', teacher: 'Dr. Ahmed Hassan', units: 5, lessons: 20, progress: 48 },
  { id: 'chem', name: 'Chemistry', nameAr: 'الكيمياء', teacher: 'Prof. Menna Sameh', units: 5, lessons: 18, progress: 35 },
  { id: 'bio', name: 'Biology', nameAr: 'الأحياء', teacher: 'Dr. Hazem Ahmed', units: 4, lessons: 16, progress: 71 },
  { id: 'hist', name: 'World History', nameAr: 'التاريخ', teacher: 'Dr. Mona Ali', units: 4, lessons: 14, progress: 54 },
  { id: 'eng', name: 'English', nameAr: 'اللغة الإنجليزية', teacher: 'Ms. Sara Fouad', units: 5, lessons: 22, progress: 80 },
]

/** Per-subject sample meta (teacher/progress/units/lessons) keyed by category. */
export const DEMO_SUBJECT_META = {
  math: { teacher: 'Mr. Osama Refat', units: 6, lessons: 24, progress: 62 },
  phys: { teacher: 'Dr. Ahmed Hassan', units: 5, lessons: 20, progress: 48 },
  chem: { teacher: 'Prof. Menna Sameh', units: 5, lessons: 18, progress: 35 },
  bio: { teacher: 'Dr. Hazem Ahmed', units: 4, lessons: 16, progress: 71 },
  hist: { teacher: 'Dr. Mona Ali', units: 4, lessons: 14, progress: 54 },
  eng: { teacher: 'Ms. Sara Fouad', units: 5, lessons: 22, progress: 80 },
  default: { teacher: 'Mr. Osama Refat', units: 5, lessons: 18, progress: 50 },
}

/** Sample per-unit progress, cycled by index (used when real progress absent). */
export const DEMO_UNIT_PROGRESS = [
  { total: 4, done: 4, progress: 100 },
  { total: 5, done: 3, progress: 60 },
  { total: 5, done: 1, progress: 20 },
  { total: 4, done: 0, progress: 0 },
]

/** Sample lesson duration/resource metadata, cycled by index. */
export const DEMO_LESSON_METAS = [
  { mins: 45, res: 5 },
  { mins: 30, res: 3 },
  { mins: 50, res: 4 },
  { mins: 40, res: 2 },
]

/** Sample quiz-result review rows (shown when an attempt has no stored answers). */
export const DEMO_QUIZ_REVIEWS = [
  { q: 'Evaluate ∫ 2x dx', correct: true },
  { q: 'The definite integral ∫₀¹ 3x² dx equals:', correct: true },
  { q: '∫ cos(x) dx =', correct: false },
  { q: 'Area under f(x)=x from 0 to 4 is:', correct: true },
]
