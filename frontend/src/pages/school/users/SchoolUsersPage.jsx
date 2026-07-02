import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { SelectField, TextField } from '../../../shared/form'
import { Alert, Button, CredentialsPanel, Modal } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { isEnglishName } from '../../../lib/validation/englishName'
import { Head, List } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { toObject } from '../../../features/student/studentSchemas'
import { displayValue, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

// role is always a fixed value now (Student/Teacher/Parent) — each school-admin
// role page (Students/Teachers/Parents) renders this with its own role, and the
// account-creation modal never exposes a role selector (School Admin "Users" page
// removed; accounts are created from the role-specific page only).
function UsersPage({ userId, locale, role, canCreate }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const view = `${role.toLowerCase()}s`
  const detailBase = `/app/school/${view}`
  const query = useSchoolQuery(queryKeys.school.users(userId, role), (s) => schoolApi.users(role, s), { staleTime: STALE.medium })
  const grades = useSchoolQuery(queryKeys.school.grades(userId), (s) => schoolApi.grades(s), { staleTime: STALE.medium, enabled: Boolean(canCreate) })
  const [modalOpen, setModalOpen] = useState(false)
  const [fullName, setFullName] = useState('')
  const [gradeId, setGradeId] = useState('')
  const [credential, setCredential] = useState(null)
  const [credentialName, setCredentialName] = useState('')
  const [showCredential, setShowCredential] = useState(false)
  const nameError = fullName.trim() && !isEnglishName(fullName)
    ? t('validation.englishNameOnly', 'Full name must be written in English letters only.')
    : null
  const closeModal = () => {
    setModalOpen(false)
    setFullName('')
    setGradeId('')
  }
  const create = useMutation({
    mutationFn: () => schoolApi.createUser({ fullName: fullName.trim(), role, gradeId: role === 'Student' ? gradeId || null : null }),
    onSuccess: (data) => {
      setCredential(toObject(data))
      setCredentialName(fullName.trim())
      setShowCredential(true)
      closeModal()
      qc.invalidateQueries({ queryKey: queryKeys.school.users(userId, role) })
    },
  })
  const gradeItems = Array.isArray(grades.data) ? grades.data : []
  const columns = [
    { key: 'fullName', header: t('school.common.name') },
    { key: 'loginCode', header: t('school.common.loginCode') },
    { key: 'role', header: t('school.common.role'), kind: 'role' },
  ]
  const addLabel = t('school.users.add', { role: t(`roles.${role}`) })
  return (
    <>
      <Head view={view} />
      {canCreate && (
        <>
          <div className="cluster mb-3">
            <Button onClick={() => setModalOpen(true)}>{addLabel}</Button>
          </div>
          {credential && (
            <Alert variant="success" title={t('school.credential.title')}>
              {t('school.credential.body')}{' '}
              <Button type="button" variant="ghost" onClick={() => setShowCredential(true)}>
                {t('credentials.view', 'View credentials')}
              </Button>
            </Alert>
          )}
          <Modal
            open={modalOpen}
            onClose={closeModal}
            title={addLabel}
            footer={
              <>
                <Button type="button" variant="ghost" onClick={closeModal}>{t('actions.cancel', 'Cancel')}</Button>
                <Button onClick={() => create.mutate()} loading={create.isPending} disabled={!fullName.trim() || Boolean(nameError)}>{addLabel}</Button>
              </>
            }
          >
            {create.isError && <ErrorState error={create.error} onRetry={() => create.reset()} />}
            <TextField label={t('school.common.name')} value={fullName} onChange={(e) => setFullName(e.target.value)} error={nameError} />
            {role === 'Student' && (
              <SelectField label={t('school.common.grade')} value={gradeId} onChange={(e) => setGradeId(e.target.value)}
                options={[{ value: '', label: t('school.common.choose') }, ...gradeItems.map((g) => ({ value: itemId(g), label: displayValue(g, ['name', 'Name']) || itemId(g) }))]} />
            )}
          </Modal>
        </>
      )}

      {credential && (
        <CredentialsPanel
          open={showCredential}
          onClose={() => setShowCredential(false)}
          fullName={credentialName}
          role={credential.role}
          loginCode={credential.loginCode}
          temporaryPassword={credential.temporaryPassword}
        />
      )}
      <List
        query={query}
        columns={columns}
        empty={t('school.empty.users')}
        locale={locale}
        rowActions={(item) => <Link className="ui-btn ui-btn--secondary" to={`${detailBase}/${encodeURIComponent(itemId(item))}`}>{t('school.common.open')}</Link>}
      />
    </>
  )
}

export default function SchoolUsersPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <UsersPage userId={userId} locale={locale} {...props} />
}
