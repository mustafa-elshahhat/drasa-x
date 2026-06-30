import { Sigma, Atom, FlaskConical, Leaf, Globe, BookOpen } from 'lucide-react'
import { displayValue, itemId } from './studentUtils'
import { isDemoEnabled } from '../../demo/isDemoEnabled'
import { DEMO_SUBJECT_META } from '../../demo/studentDemoData'

// =============================================================================
// Presentational subject theming (color + icon by subject category). This is a
// visual concern, NOT data. The accompanying teacher/progress/units/lessons
// fields are sample values surfaced ONLY in demo mode; in production they are
// honest zeros/empties so no fabricated teacher or progress is ever shown.
// =============================================================================

const VISUALS = {
  math: { color: '#0c7288', icon: Sigma },
  phys: { color: '#8a38f5', icon: Atom },
  chem: { color: '#ff6636', icon: FlaskConical },
  bio: { color: '#00a824', icon: Leaf },
  hist: { color: '#2f6fed', icon: Globe },
  eng: { color: '#e0a000', icon: BookOpen },
  default: { color: '#0c7288', icon: BookOpen },
}

const EMPTY_META = { teacher: '', units: 0, lessons: 0, progress: 0 }

function categoryOf(subject) {
  const name = String(displayValue(subject) || '').toLowerCase()
  const id = String(itemId(subject) || '').toLowerCase()
  const has = (s) => id.includes(s) || name.includes(s)
  if (has('math') || name.includes('الرياضيات')) return 'math'
  if (has('phys') || name.includes('الفيزياء')) return 'phys'
  if (has('chem') || name.includes('الكيمياء')) return 'chem'
  if (has('bio') || name.includes('الأحياء')) return 'bio'
  if (has('hist') || has('history') || name.includes('التاريخ')) return 'hist'
  if (has('eng') || has('english') || name.includes('اللغة الإنجليزية')) return 'eng'
  return 'default'
}

/** Returns { color, icon } + (demo-only) { teacher, units, lessons, progress }. */
export function getSubjectTheme(subject) {
  const cat = categoryOf(subject)
  const visual = VISUALS[cat] || VISUALS.default
  const meta = isDemoEnabled() ? DEMO_SUBJECT_META[cat] || DEMO_SUBJECT_META.default : EMPTY_META
  return { ...visual, ...meta }
}
