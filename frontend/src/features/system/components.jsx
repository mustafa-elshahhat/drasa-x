import { useTranslation } from 'react-i18next'
import { Spinner, PageHeader, Card } from '../../shared/ui'
import { ResourceTable, DetailList } from '../../shared/data-display'

// Shared system-admin presentational helpers (components only).

export function Loading() {
  const { t } = useTranslation()
  return <Spinner label={t('states.loading')} />
}

/** Operational posture card (Health / Error monitoring / Backups deferral notes). */
export function PostureCard({ posture, locale }) {
  const { t } = useTranslation()
  if (!posture) return null
  return (
    <Card title={posture.configured ? t('system.posture.configured') : t('system.posture.notConfigured')}>
      <DetailList item={posture} locale={locale} />
    </Card>
  )
}

/** Page header sourced from the `system.pages.<view>.*` i18n keys. */
export function Head({ view }) {
  const { t } = useTranslation()
  return <PageHeader title={t(`system.pages.${view}.title`)} description={t(`system.pages.${view}.description`)} />
}

/** Resource table bound to a query's loading/error/empty/data states. */
export function List({ query, columns, empty, rowActions, locale }) {
  return (
    <ResourceTable
      rows={Array.isArray(query.data) ? query.data : []}
      columns={columns}
      rowActions={rowActions}
      loading={query.isLoading}
      error={query.error}
      onRetry={() => query.refetch()}
      emptyTitle={empty}
      locale={locale}
    />
  )
}
