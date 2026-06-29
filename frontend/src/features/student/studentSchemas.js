import { z } from 'zod'

const unknownRecord = z.record(z.unknown())

export const apiEnvelopeSchema = z
  .object({
    statusCode: z.number().optional(),
    succeeded: z.boolean().optional(),
    message: z.string().nullable().optional(),
    data: z.unknown().optional(),
    errors: z.unknown().optional(),
  })
  .passthrough()

export const listSchema = z.union([
  z.array(unknownRecord),
  z.object({ items: z.array(unknownRecord).optional(), totalCount: z.number().optional() }).passthrough(),
  z.object({ data: z.array(unknownRecord).optional(), totalCount: z.number().optional() }).passthrough(),
  unknownRecord,
])

export const tutorResponseSchema = z
  .object({
    answer: z.string().optional(),
    Answer: z.string().optional(),
    grounded: z.boolean().optional(),
    Grounded: z.boolean().optional(),
    noAnswerReason: z.string().nullable().optional(),
    NoAnswerReason: z.string().nullable().optional(),
    citations: z.array(unknownRecord).optional(),
    Citations: z.array(unknownRecord).optional(),
    correlationId: z.string().optional(),
    CorrelationId: z.string().optional(),
  })
  .passthrough()

const attendanceRecordSchema = z
  .object({
    id: z.string().optional(),
    attendanceDate: z.string().optional(),
    AttendanceDate: z.string().optional(),
    status: z.string().optional(),
    Status: z.string().optional(),
    recordedAt: z.string().optional(),
    RecordedAt: z.string().optional(),
    source: z.string().optional(),
    Source: z.string().optional(),
    sessionKey: z.string().optional(),
    SessionKey: z.string().optional(),
    schoolClassId: z.string().nullable().optional(),
    SchoolClassId: z.string().nullable().optional(),
    notes: z.string().nullable().optional(),
    Notes: z.string().nullable().optional(),
  })
  .passthrough()

const attendanceSummarySchema = z
  .object({
    total: z.number().catch(0),
    present: z.number().catch(0),
    absent: z.number().catch(0),
    late: z.number().catch(0),
    excused: z.number().catch(0),
    attendancePercentage: z.number().catch(0),
  })
  .passthrough()

export const attendanceResponseSchema = z
  .object({
    summary: attendanceSummarySchema.optional(),
    records: z.array(attendanceRecordSchema).optional(),
  })
  .passthrough()

export function unwrapEnvelope(value) {
  const parsed = apiEnvelopeSchema.safeParse(value)
  if (parsed.success && Object.prototype.hasOwnProperty.call(parsed.data, 'data')) return parsed.data.data
  return value
}

export function toItems(value) {
  const data = unwrapEnvelope(value)
  const parsed = listSchema.safeParse(data)
  if (!parsed.success) return []
  const current = parsed.data
  if (Array.isArray(current)) return current
  if (Array.isArray(current.items)) return current.items
  if (Array.isArray(current.data)) return current.data
  return []
}

export function toObject(value) {
  const data = unwrapEnvelope(value)
  return data && typeof data === 'object' && !Array.isArray(data) ? data : null
}

export function normalizeTutorResponse(value) {
  const parsed = tutorResponseSchema.parse(value)
  return {
    answer: parsed.answer || parsed.Answer || '',
    grounded: Boolean(parsed.grounded ?? parsed.Grounded),
    noAnswerReason: parsed.noAnswerReason || parsed.NoAnswerReason || null,
    citations: parsed.citations || parsed.Citations || [],
    correlationId: parsed.correlationId || parsed.CorrelationId || '',
    raw: parsed,
  }
}

export function normalizeAttendance(value) {
  const data = unwrapEnvelope(value)
  const parsed = attendanceResponseSchema.parse(data)
  return {
    summary: parsed.summary || { total: 0, present: 0, absent: 0, late: 0, excused: 0, attendancePercentage: 0 },
    records: (parsed.records || []).map((row) => ({
      id: row.id || row.Id || '',
      attendanceDate: row.attendanceDate || row.AttendanceDate || '',
      status: row.status || row.Status || '',
      recordedAt: row.recordedAt || row.RecordedAt || '',
      source: row.source || row.Source || '',
      sessionKey: row.sessionKey || row.SessionKey || '',
      schoolClassId: row.schoolClassId || row.SchoolClassId || null,
      notes: row.notes || row.Notes || null,
    })),
  }
}
