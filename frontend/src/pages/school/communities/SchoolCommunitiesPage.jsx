import { useMemo, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { SelectField, TextField, TextareaField } from '../../../shared/form'
import { Alert, Button, Card, Chip, FormModal } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { displayValue, formatDate, getField, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

// Numeric enums (the backend binds enums numerically — no JsonStringEnumConverter):
// CommunityVisibility{Public=0,TenantOnly=1,ClassOnly=2}, CommunityMemberRole{Member=0,Moderator=1,
// Owner=2}, ReportStatus{Open=0,Reviewed=1,Dismissed=2,ActionTaken=3}.
const VISIBILITY = ['Public', 'TenantOnly', 'ClassOnly']
const VISIBILITY_TONE = { 0: 'success', 1: 'brand', 2: 'warning' }
const MEMBER_ROLE = ['Member', 'Moderator', 'Owner']
const MEMBER_ROLE_TONE = { 0: 'muted', 1: 'info', 2: 'brand' }
const REPORT_STATUS = ['Open', 'Reviewed', 'Dismissed', 'ActionTaken']

function visibilityOptions(t) {
  return VISIBILITY.map((v, i) => ({ value: i, label: t(`school.communities.visibilityValue.${v}`) }))
}
function memberRoleOptions(t) {
  return MEMBER_ROLE.map((v, i) => ({ value: i, label: t(`school.communities.roleValue.${v}`) }))
}
function reportStatusOptions(t) {
  return REPORT_STATUS.map((v, i) => ({ value: i, label: t(`school.communities.reportStatusValue.${v}`) }))
}

function communityEditFields(t, gradeOptions) {
  return [
    { name: 'name', label: t('school.common.name'), required: true },
    { name: 'description', label: t('school.communities.description'), type: 'textarea' },
    { name: 'visibility', label: t('school.communities.visibility'), type: 'select', options: visibilityOptions(t) },
    { name: 'eligibleGradeId', label: t('school.communities.eligibleGrade'), type: 'select', options: [{ value: '', label: t('school.common.none') }, ...gradeOptions] },
  ]
}

function CreateCommunityCard({ userId, classOptions, gradeOptions, t }) {
  const qc = useQueryClient()
  const EMPTY = { name: '', description: '', visibility: 1, schoolClassId: '', eligibleGradeId: '' }
  const [form, setForm] = useState(EMPTY)
  const create = useMutation({
    mutationFn: () => schoolApi.createCommunity({
      name: form.name.trim(),
      description: form.description.trim() || null,
      visibility: Number(form.visibility),
      schoolClassId: form.schoolClassId || null,
      eligibleGradeId: form.eligibleGradeId || null,
    }),
    onSuccess: () => { setForm(EMPTY); qc.invalidateQueries({ queryKey: queryKeys.school.communities(userId) }) },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))

  return (
    <Card title={t('school.communities.create')}>
      {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
      <div className="ui-formgrid ui-formgrid--2">
        <TextField label={t('school.common.name')} value={form.name} onChange={set('name')} />
        <SelectField label={t('school.communities.visibility')} value={form.visibility} onChange={set('visibility')} options={visibilityOptions(t)} />
        <SelectField
          label={t('school.common.class')}
          value={form.schoolClassId}
          onChange={set('schoolClassId')}
          options={[{ value: '', label: t('school.common.none') }, ...classOptions]}
          hint={t('school.communities.classHint')}
        />
        <SelectField
          label={t('school.communities.eligibleGrade')}
          value={form.eligibleGradeId}
          onChange={set('eligibleGradeId')}
          options={[{ value: '', label: t('school.common.none') }, ...gradeOptions]}
        />
      </div>
      <TextareaField label={t('school.communities.description')} value={form.description} onChange={set('description')} />
      <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.name.trim()}>{t('school.communities.create')}</Button>
    </Card>
  )
}

function CommunityManagePanel({ userId, locale, communityId, userById, t }) {
  const qc = useQueryClient()
  const members = useSchoolQuery(queryKeys.school.communityMembers(userId, communityId), (s) => schoolApi.communityMembers(communityId, s))
  const posts = useSchoolQuery(queryKeys.school.communityPosts(userId, communityId), (s) => schoolApi.communityPosts(communityId, s))

  const memberItems = Array.isArray(members.data) ? members.data : []
  const postItems = Array.isArray(posts.data) ? posts.data : []
  const memberIds = new Set(memberItems.map((m) => getField(m, 'userId')))
  const availableUsers = [...userById.values()].filter((u) => !memberIds.has(itemId(u)))

  const [newMemberId, setNewMemberId] = useState('')
  const [newMemberRole, setNewMemberRole] = useState(0)
  const addMember = useMutation({
    mutationFn: () => schoolApi.addCommunityMember(communityId, { userId: newMemberId, role: Number(newMemberRole) }),
    onSuccess: () => {
      setNewMemberId(''); setNewMemberRole(0)
      qc.invalidateQueries({ queryKey: queryKeys.school.communityMembers(userId, communityId) })
    },
  })

  const [moderatingPostId, setModeratingPostId] = useState(null)
  const moderate = useMutation({
    mutationFn: ({ postId, body }) => schoolApi.moderatePost(postId, body),
    onSuccess: () => {
      setModeratingPostId(null)
      qc.invalidateQueries({ queryKey: queryKeys.school.communityPosts(userId, communityId) })
    },
  })

  return (
    <div className="stack" style={{ marginTop: 12 }}>
      <Card title={t('school.communities.members')}>
        {members.isLoading && <Loading />}
        {members.isError && <ErrorState error={members.error} onRetry={members.refetch} />}
        {!members.isLoading && !members.isError && (
          memberItems.length === 0 ? <EmptyState title={t('school.communities.noMembers')} /> : (
            <ul className="ui-list">
              {memberItems.map((m, idx) => {
                const uid = getField(m, 'userId')
                const user = userById.get(uid)
                return (
                  <li className="ui-list__item" key={uid || idx}>
                    <div className="ui-list__body">
                      <div className="ui-list__title">{user ? displayValue(user, ['fullName', 'FullName']) : uid}</div>
                    </div>
                    <Chip tone={MEMBER_ROLE_TONE[getField(m, 'role')] ?? 'muted'}>
                      {t(`school.communities.roleValue.${MEMBER_ROLE[getField(m, 'role')] || 'Member'}`)}
                    </Chip>
                  </li>
                )
              })}
            </ul>
          )
        )}
        {addMember.isError && <ErrorState error={addMember.error} onRetry={() => addMember.reset()} />}
        <div className="ui-form-row">
          <SelectField
            label={t('school.communities.addMember')}
            value={newMemberId}
            onChange={(e) => setNewMemberId(e.target.value)}
            options={[{ value: '', label: t('school.common.choose') }, ...availableUsers.map((u) => ({ value: itemId(u), label: displayValue(u, ['fullName', 'FullName']) || itemId(u) }))]}
          />
          <SelectField label={t('school.communities.role')} value={newMemberRole} onChange={(e) => setNewMemberRole(e.target.value)} options={memberRoleOptions(t)} />
          <Button onClick={() => addMember.mutate()} loading={addMember.isPending} disabled={!newMemberId}>{t('actions.add')}</Button>
        </div>
      </Card>

      <Card title={t('school.communities.posts')}>
        {/* No comment-list or report-list endpoint exists on the backend (PostDto only carries
            commentsCount; ModeratePostAsync blindly resolves ALL open reports on a post rather than
            targeting one by id) — so there is no per-comment moderation or report browser here.
            "Moderate" below is the full extent of what the real contract supports. */}
        {posts.isLoading && <Loading />}
        {posts.isError && <ErrorState error={posts.error} onRetry={posts.refetch} />}
        {!posts.isLoading && !posts.isError && (
          postItems.length === 0 ? <EmptyState title={t('school.empty.posts')} /> : (
            <ul className="ui-list">
              {postItems.map((p, idx) => {
                const pid = itemId(p)
                const uid = getField(p, 'userId')
                const user = userById.get(uid)
                return (
                  <li className="ui-list__item" key={pid || idx}>
                    <div className="ui-list__body">
                      <div className="ui-list__title">{user ? displayValue(user, ['fullName', 'FullName']) : uid}</div>
                      <p>{getField(p, 'content')}</p>
                      <div className="ui-list__meta ui-muted">
                        {t('school.communities.commentsCount', { count: getField(p, 'commentsCount') ?? 0 })} · {formatDate(getField(p, 'createdAt'), locale)}
                      </div>
                    </div>
                    <Button variant="secondary" onClick={() => setModeratingPostId(pid)}>{t('school.communities.moderate')}</Button>
                  </li>
                )
              })}
            </ul>
          )
        )}
      </Card>

      <FormModal
        open={Boolean(moderatingPostId)}
        onClose={() => setModeratingPostId(null)}
        title={t('school.communities.moderateTitle')}
        fields={[
          { name: 'status', label: t('school.communities.reportStatus'), type: 'select', options: reportStatusOptions(t) },
          { name: 'removePost', label: t('school.communities.removePost'), type: 'checkbox' },
        ]}
        initialValues={{ status: 1, removePost: false }}
        onSubmit={(values) => moderate.mutate({ postId: moderatingPostId, body: { status: Number(values.status), removePost: !!values.removePost } })}
        submitting={moderate.isPending}
        error={moderate.error}
        submitLabel={t('school.communities.moderate')}
      />
    </div>
  )
}

function CommunityRow({ userId, locale, item, userById, gradeOptions, t }) {
  const qc = useQueryClient()
  const id = itemId(item)
  const [editing, setEditing] = useState(false)
  const [managing, setManaging] = useState(false)
  const invalidateList = () => qc.invalidateQueries({ queryKey: queryKeys.school.communities(userId) })

  const update = useMutation({
    mutationFn: (values) => schoolApi.updateCommunity(id, {
      name: values.name.trim(),
      description: values.description.trim() || null,
      visibility: Number(values.visibility),
      eligibleGradeId: values.eligibleGradeId || null,
    }),
    onSuccess: () => { setEditing(false); invalidateList() },
  })
  const archive = useMutation({ mutationFn: () => schoolApi.archiveCommunity(id), onSuccess: invalidateList })

  const visibilityValue = getField(item, 'visibility')

  return (
    <div className="student-list__item">
      <div className="cluster mb-2">
        <strong className="domain-row__title">{displayValue(item)}</strong>
        <Chip tone={VISIBILITY_TONE[visibilityValue] ?? 'muted'}>{t(`school.communities.visibilityValue.${VISIBILITY[visibilityValue] || 'TenantOnly'}`)}</Chip>
        <span className="ui-muted">{t('school.communities.memberCount', { count: getField(item, 'memberCount') ?? 0 })}</span>
      </div>
      {getField(item, 'description') && <p className="ui-muted">{getField(item, 'description')}</p>}
      {archive.isError && <ErrorState error={archive.error} onRetry={() => archive.reset()} />}
      <div className="cluster">
        <Button variant="secondary" onClick={() => setEditing(true)}>{t('school.common.edit')}</Button>
        <Button variant="danger" onClick={() => archive.mutate()} loading={archive.isPending}>{t('school.common.archive')}</Button>
        <Button variant="secondary" onClick={() => setManaging((m) => !m)}>
          {managing ? t('school.communities.hideManage') : t('school.communities.manage')}
        </Button>
      </div>
      {managing && <CommunityManagePanel userId={userId} locale={locale} communityId={id} userById={userById} t={t} />}
      <FormModal
        open={editing}
        onClose={() => setEditing(false)}
        title={t('school.communities.editTitle')}
        fields={editing ? communityEditFields(t, gradeOptions) : []}
        initialValues={{
          name: getField(item, 'name') || '',
          description: getField(item, 'description') || '',
          visibility: getField(item, 'visibility') ?? 1,
          eligibleGradeId: getField(item, 'eligibleGradeId') || '',
        }}
        onSubmit={(values) => update.mutate(values)}
        submitting={update.isPending}
        error={update.error}
        submitLabel={t('school.common.save')}
      />
    </div>
  )
}

function CommunitiesPage({ userId, locale }) {
  const { t } = useTranslation()
  const query = useSchoolQuery(queryKeys.school.communities(userId), (s) => schoolApi.communities(s))
  const classes = useSchoolQuery(queryKeys.school.classes(userId), (s) => schoolApi.classes(s), { staleTime: STALE.medium })
  const grades = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s), { staleTime: STALE.long })
  const users = useSchoolQuery(queryKeys.school.users(userId, undefined), (s) => schoolApi.users(undefined, s), { staleTime: STALE.medium })

  const userById = useMemo(() => {
    const map = new Map()
    for (const u of Array.isArray(users.data) ? users.data : []) map.set(itemId(u), u)
    return map
  }, [users.data])

  const classOptions = (Array.isArray(classes.data) ? classes.data : []).map((c) => ({ value: itemId(c), label: displayValue(c) || itemId(c) }))
  const gradeOptions = (Array.isArray(grades.data) ? grades.data : []).map((g) => ({ value: itemId(g), label: displayValue(g) || itemId(g) }))
  const items = Array.isArray(query.data) ? query.data : []

  return (
    <>
      <Head view="communities" />
      <Alert title={t('school.communities.noteTitle')}>{t('school.notes.communities')}</Alert>
      <CreateCommunityCard userId={userId} classOptions={classOptions} gradeOptions={gradeOptions} t={t} />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {!query.isLoading && !query.isError && items.length === 0 && <EmptyState title={t('school.empty.communities')} />}
      {items.length > 0 && (
        <div className="student-list">
          {items.map((item, idx) => (
            <CommunityRow key={itemId(item) || idx} userId={userId} locale={locale} item={item} userById={userById} gradeOptions={gradeOptions} t={t} />
          ))}
        </div>
      )}
    </>
  )
}

export default function SchoolCommunitiesPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <CommunitiesPage userId={userId} locale={locale} {...props} />
}
