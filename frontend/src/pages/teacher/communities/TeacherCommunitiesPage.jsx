import { useMemo, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { SelectField, TextField, TextareaField } from '../../../shared/form'
import { Alert, Button, Card, Chip, FormModal, PageHeader } from '../../../shared/ui'
import { EmptyState, ErrorState } from '../../../shared/feedback'
import { Loading } from '../../../features/teacher/components'
import { useTeacherQuery } from '../../../features/teacher/helpers'
import { useAuth } from '../../../features/auth/AuthContext'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { displayValue, formatDate, getField, itemId } from '../../../features/teacher/teacherUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

// Numeric enums (the backend binds enums numerically — no JsonStringEnumConverter):
// CommunityVisibility{Public=0,TenantOnly=1,ClassOnly=2}, CommunityMemberRole{Member=0,Moderator=1,
// Owner=2}, ReportStatus{Open=0,Reviewed=1,Dismissed=2,ActionTaken=3}. This page is brand new
// (greenfield) — a Teacher can create a community (becoming its Owner automatically) but
// `communities()` lists EVERY tenant community, not just the caller's own, so management actions
// (edit/archive/add member/moderate) are gated in the UI by the caller's actual membership role on
// that specific community (fetched lazily once "Manage" is expanded) and otherwise surface a real
// 403 via ErrorState rather than being silently hidden everywhere.
const VISIBILITY = ['Public', 'TenantOnly', 'ClassOnly']
const VISIBILITY_TONE = { 0: 'success', 1: 'brand', 2: 'warning' }
const MEMBER_ROLE = ['Member', 'Moderator', 'Owner']
const MEMBER_ROLE_TONE = { 0: 'muted', 1: 'info', 2: 'brand' }
const REPORT_STATUS = ['Open', 'Reviewed', 'Dismissed', 'ActionTaken']

function visibilityOptions(t) {
  return VISIBILITY.map((v, i) => ({ value: i, label: t(`teacher.communities.visibilityValue.${v}`) }))
}
function memberRoleOptions(t) {
  return MEMBER_ROLE.map((v, i) => ({ value: i, label: t(`teacher.communities.roleValue.${v}`) }))
}
function reportStatusOptions(t) {
  return REPORT_STATUS.map((v, i) => ({ value: i, label: t(`teacher.communities.reportStatusValue.${v}`) }))
}

function communityEditFields(t, gradeOptions) {
  return [
    { name: 'name', label: t('teacher.communities.name'), required: true },
    { name: 'description', label: t('teacher.communities.descriptionField'), type: 'textarea' },
    { name: 'visibility', label: t('teacher.communities.visibility'), type: 'select', options: visibilityOptions(t) },
    { name: 'eligibleGradeId', label: t('teacher.communities.eligibleGrade'), type: 'select', options: [{ value: '', label: t('teacher.communities.none') }, ...gradeOptions] },
  ]
}

function CreateCommunityCard({ userId, classOptions, gradeOptions, t }) {
  const qc = useQueryClient()
  const EMPTY = { name: '', description: '', visibility: 1, schoolClassId: '', eligibleGradeId: '' }
  const [form, setForm] = useState(EMPTY)
  const create = useMutation({
    mutationFn: () => teacherApi.createCommunity({
      name: form.name.trim(),
      description: form.description.trim() || null,
      visibility: Number(form.visibility),
      schoolClassId: form.schoolClassId || null,
      eligibleGradeId: form.eligibleGradeId || null,
    }),
    onSuccess: () => { setForm(EMPTY); qc.invalidateQueries({ queryKey: queryKeys.teacher.communities(userId) }) },
  })
  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))

  return (
    <Card title={t('teacher.communities.create')}>
      {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
      <div className="ui-formgrid ui-formgrid--2">
        <TextField label={t('teacher.communities.name')} value={form.name} onChange={set('name')} />
        <SelectField label={t('teacher.communities.visibility')} value={form.visibility} onChange={set('visibility')} options={visibilityOptions(t)} />
        <SelectField
          label={t('teacher.communities.classField')}
          value={form.schoolClassId}
          onChange={set('schoolClassId')}
          options={[{ value: '', label: t('teacher.communities.none') }, ...classOptions]}
          hint={t('teacher.communities.classHint')}
        />
        <SelectField
          label={t('teacher.communities.eligibleGrade')}
          value={form.eligibleGradeId}
          onChange={set('eligibleGradeId')}
          options={[{ value: '', label: t('teacher.communities.none') }, ...gradeOptions]}
        />
      </div>
      <TextareaField label={t('teacher.communities.descriptionField')} value={form.description} onChange={set('description')} />
      <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!form.name.trim()}>{t('teacher.communities.create')}</Button>
    </Card>
  )
}

function CommunityManagePanel({ userId, locale, communityId, studentById, isBypass, onEditCommunity, archive, t }) {
  const qc = useQueryClient()
  const members = useTeacherQuery(queryKeys.teacher.communityMembers(userId, communityId), (signal) => teacherApi.communityMembers(communityId, signal))
  const posts = useTeacherQuery(queryKeys.teacher.communityPosts(userId, communityId), (signal) => teacherApi.communityPosts(communityId, signal))

  const memberItems = Array.isArray(members.data) ? members.data : []
  const postItems = Array.isArray(posts.data) ? posts.data : []
  const myMembership = memberItems.find((m) => getField(m, 'userId') === userId)
  const myRoleValue = getField(myMembership, 'role')
  const isMember = isBypass || Boolean(myMembership)
  const isManager = isBypass || myRoleValue === 1 || myRoleValue === 2
  const isOwner = isBypass || myRoleValue === 2

  const memberIds = new Set(memberItems.map((m) => getField(m, 'userId')))
  const availableStudents = [...studentById.values()].filter((s) => !memberIds.has(itemId(s, ['studentId', 'StudentId', 'id', 'Id'])))

  const [newMemberId, setNewMemberId] = useState('')
  const [newMemberRole, setNewMemberRole] = useState(0)
  const addMember = useMutation({
    mutationFn: () => teacherApi.addCommunityMember(communityId, { userId: newMemberId, role: Number(newMemberRole) }),
    onSuccess: () => {
      setNewMemberId(''); setNewMemberRole(0)
      qc.invalidateQueries({ queryKey: queryKeys.teacher.communityMembers(userId, communityId) })
    },
  })

  const [postContent, setPostContent] = useState('')
  const [postPhotoUrl, setPostPhotoUrl] = useState('')
  const createPost = useMutation({
    mutationFn: () => teacherApi.createCommunityPost(communityId, { content: postContent.trim(), photoUrl: postPhotoUrl.trim() || null }),
    onSuccess: () => {
      setPostContent(''); setPostPhotoUrl('')
      qc.invalidateQueries({ queryKey: queryKeys.teacher.communityPosts(userId, communityId) })
    },
  })

  const [moderatingPostId, setModeratingPostId] = useState(null)
  const moderate = useMutation({
    mutationFn: ({ postId, body }) => teacherApi.moderatePost(postId, body),
    onSuccess: () => {
      setModeratingPostId(null)
      qc.invalidateQueries({ queryKey: queryKeys.teacher.communityPosts(userId, communityId) })
    },
  })

  return (
    <div className="stack" style={{ marginTop: 12 }}>
      {members.isError && <ErrorState error={members.error} onRetry={members.refetch} />}
      {!members.isLoading && !members.isError && !isMember && (
        <Alert variant="info" title={t('teacher.communities.notManagerNote')} />
      )}
      {isManager && (
        <div className="cluster">
          {archive.isError && <ErrorState error={archive.error} onRetry={() => archive.reset()} />}
          <Button variant="secondary" onClick={onEditCommunity}>{t('teacher.communities.edit')}</Button>
          {isOwner && <Button variant="danger" onClick={() => archive.mutate()} loading={archive.isPending}>{t('teacher.communities.archive')}</Button>}
        </div>
      )}

      <Card title={t('teacher.communities.members')}>
        {members.isLoading && <Loading />}
        {!members.isLoading && !members.isError && (
          memberItems.length === 0 ? <EmptyState title={t('teacher.communities.noMembers')} /> : (
            <ul className="ui-list">
              {memberItems.map((m, idx) => {
                const uid = getField(m, 'userId')
                const student = studentById.get(uid)
                return (
                  <li className="ui-list__item" key={uid || idx}>
                    <div className="ui-list__body">
                      <div className="ui-list__title">{student ? displayValue(student, ['fullName', 'FullName']) : uid}</div>
                    </div>
                    <Chip tone={MEMBER_ROLE_TONE[getField(m, 'role')] ?? 'muted'}>
                      {t(`teacher.communities.roleValue.${MEMBER_ROLE[getField(m, 'role')] || 'Member'}`)}
                    </Chip>
                  </li>
                )
              })}
            </ul>
          )
        )}
        {isManager && (
          <>
            {addMember.isError && <ErrorState error={addMember.error} onRetry={() => addMember.reset()} />}
            <div className="ui-form-row">
              <SelectField
                label={t('teacher.communities.addMember')}
                value={newMemberId}
                onChange={(e) => setNewMemberId(e.target.value)}
                options={[{ value: '', label: t('teacher.communities.chooseMember') }, ...availableStudents.map((s) => ({ value: itemId(s, ['studentId', 'StudentId', 'id', 'Id']), label: displayValue(s, ['fullName', 'FullName']) || itemId(s) }))]}
              />
              <SelectField label={t('teacher.communities.role')} value={newMemberRole} onChange={(e) => setNewMemberRole(e.target.value)} options={memberRoleOptions(t)} />
              <Button onClick={() => addMember.mutate()} loading={addMember.isPending} disabled={!newMemberId}>{t('actions.add')}</Button>
            </div>
          </>
        )}
      </Card>

      <Card title={t('teacher.communities.posts')}>
        {/* No comment-list or report-list endpoint exists on the backend (PostDto only carries
            commentsCount; ModeratePostAsync blindly resolves ALL open reports on a post rather than
            targeting one by id) — so there is no per-comment moderation or report browser here. */}
        {posts.isLoading && <Loading />}
        {posts.isError && <ErrorState error={posts.error} onRetry={posts.refetch} />}
        {!posts.isLoading && !posts.isError && (
          postItems.length === 0 ? <EmptyState title={t('teacher.empty.posts')} /> : (
            <ul className="ui-list">
              {postItems.map((p, idx) => {
                const pid = itemId(p)
                const uid = getField(p, 'userId')
                const student = studentById.get(uid)
                return (
                  <li className="ui-list__item" key={pid || idx}>
                    <div className="ui-list__body">
                      <div className="ui-list__title">{student ? displayValue(student, ['fullName', 'FullName']) : uid}</div>
                      <p>{getField(p, 'content')}</p>
                      <div className="ui-list__meta ui-muted">
                        {t('teacher.communities.commentsCount', { count: getField(p, 'commentsCount') ?? 0 })} · {formatDate(getField(p, 'createdAt'), locale)}
                      </div>
                    </div>
                    {isManager && <Button variant="secondary" onClick={() => setModeratingPostId(pid)}>{t('teacher.communities.moderate')}</Button>}
                  </li>
                )
              })}
            </ul>
          )
        )}
        {isMember && (
          <>
            {createPost.isError && <ErrorState error={createPost.error} onRetry={() => createPost.reset()} />}
            <form className="stack" onSubmit={(e) => { e.preventDefault(); if (postContent.trim()) createPost.mutate() }}>
              <TextareaField label={t('teacher.communities.postContent')} value={postContent} onChange={(e) => setPostContent(e.target.value)} />
              <TextField label={t('teacher.communities.postPhotoUrl')} value={postPhotoUrl} onChange={(e) => setPostPhotoUrl(e.target.value)} />
              <Button type="submit" loading={createPost.isPending} disabled={!postContent.trim()}>{t('teacher.communities.post')}</Button>
            </form>
          </>
        )}
      </Card>

      <FormModal
        open={Boolean(moderatingPostId)}
        onClose={() => setModeratingPostId(null)}
        title={t('teacher.communities.moderateTitle')}
        fields={[
          { name: 'status', label: t('teacher.communities.reportStatus'), type: 'select', options: reportStatusOptions(t) },
          { name: 'removePost', label: t('teacher.communities.removePost'), type: 'checkbox' },
        ]}
        initialValues={{ status: 1, removePost: false }}
        onSubmit={(values) => moderate.mutate({ postId: moderatingPostId, body: { status: Number(values.status), removePost: !!values.removePost } })}
        submitting={moderate.isPending}
        error={moderate.error}
        submitLabel={t('teacher.communities.moderate')}
      />
    </div>
  )
}

function CommunityRow({ userId, locale, item, studentById, gradeOptions, isBypass, t }) {
  const qc = useQueryClient()
  const id = itemId(item)
  const [editing, setEditing] = useState(false)
  const [managing, setManaging] = useState(false)
  const invalidateList = () => qc.invalidateQueries({ queryKey: queryKeys.teacher.communities(userId) })

  const update = useMutation({
    mutationFn: (values) => teacherApi.updateCommunity(id, {
      name: values.name.trim(),
      description: values.description.trim() || null,
      visibility: Number(values.visibility),
      eligibleGradeId: values.eligibleGradeId || null,
    }),
    onSuccess: () => { setEditing(false); invalidateList() },
  })
  const archive = useMutation({ mutationFn: () => teacherApi.archiveCommunity(id), onSuccess: invalidateList })

  const visibilityValue = getField(item, 'visibility')

  return (
    <div className="student-list__item">
      <div className="cluster mb-2">
        <strong className="domain-row__title">{displayValue(item)}</strong>
        <Chip tone={VISIBILITY_TONE[visibilityValue] ?? 'muted'}>{t(`teacher.communities.visibilityValue.${VISIBILITY[visibilityValue] || 'TenantOnly'}`)}</Chip>
        <span className="ui-muted">{t('teacher.communities.memberCount', { count: getField(item, 'memberCount') ?? 0 })}</span>
      </div>
      {getField(item, 'description') && <p className="ui-muted">{getField(item, 'description')}</p>}
      <div className="cluster">
        <Button variant="secondary" onClick={() => setManaging((m) => !m)}>
          {managing ? t('teacher.communities.hideManage') : t('teacher.communities.manage')}
        </Button>
      </div>
      {managing && (
        <CommunityManagePanel
          userId={userId}
          locale={locale}
          communityId={id}
          studentById={studentById}
          isBypass={isBypass}
          onEditCommunity={() => setEditing(true)}
          archive={archive}
          t={t}
        />
      )}
      <FormModal
        open={editing}
        onClose={() => setEditing(false)}
        title={t('teacher.communities.editTitle')}
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
        submitLabel={t('actions.save')}
      />
    </div>
  )
}

function CommunitiesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { role } = useAuth()
  const isBypass = role === 'SchoolAdmin'
  const query = useTeacherQuery(queryKeys.teacher.communities(userId), (signal) => teacherApi.communities(signal))
  const classes = useTeacherQuery(queryKeys.teacher.classes(userId), (signal) => teacherApi.classes(signal), { staleTime: STALE.medium })
  const grades = useTeacherQuery(queryKeys.teacher.grades(userId), (signal) => teacherApi.grades(signal), { staleTime: STALE.long })
  const students = useTeacherQuery(queryKeys.teacher.students(userId), (signal) => teacherApi.myStudents(signal), { staleTime: STALE.medium })

  const studentById = useMemo(() => {
    const map = new Map()
    for (const s of Array.isArray(students.data) ? students.data : []) map.set(itemId(s, ['studentId', 'StudentId', 'id', 'Id']), s)
    return map
  }, [students.data])

  const classOptions = (Array.isArray(classes.data) ? classes.data : []).map((c) => ({ value: itemId(c), label: displayValue(c) || itemId(c) }))
  const gradeOptions = (Array.isArray(grades.data) ? grades.data : []).map((g) => ({ value: itemId(g), label: displayValue(g) || itemId(g) }))
  const items = Array.isArray(query.data) ? query.data : []

  return (
    <>
      <PageHeader title={t('teacher.communities.title')} description={t('teacher.communities.description')} />
      <CreateCommunityCard userId={userId} classOptions={classOptions} gradeOptions={gradeOptions} t={t} />
      {query.isLoading && <Loading />}
      {query.isError && <ErrorState error={query.error} onRetry={query.refetch} />}
      {!query.isLoading && !query.isError && items.length === 0 && <EmptyState title={t('teacher.empty.communities')} />}
      {items.length > 0 && (
        <div className="student-list">
          {items.map((item, idx) => (
            <CommunityRow key={itemId(item) || idx} userId={userId} locale={locale} item={item} studentById={studentById} gradeOptions={gradeOptions} isBypass={isBypass} t={t} />
          ))}
        </div>
      )}
    </>
  )
}

export default function TeacherCommunitiesPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <CommunitiesPage userId={userId} locale={locale} {...props} />
}
