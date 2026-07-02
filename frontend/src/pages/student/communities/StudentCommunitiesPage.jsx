import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { MessageSquare, Users } from 'lucide-react'
import { useStudentContext } from '../../../features/student/helpers'
import { DetailList } from '../../../shared/data-display'
import { QuizCard } from '../../../shared/domain'
import { TextareaField } from '../../../shared/form'
import { Button, Card, PageHeader } from '../../../shared/ui'
import { QueryBoundary, ErrorState } from '../../../shared/feedback'
import { useStudentQuery } from '../../../features/student/helpers'
import { Loading } from '../../../features/student/Loading'
import { studentApi } from '../../../features/student/studentApi'
import { displayValue, formatDate, getField, itemId } from '../../../features/student/studentUtils'
import { STALE, queryKeys } from '../../../lib/query/keys'

function CommunitiesPage({ userId, locale }) {
  const { t } = useTranslation()
  const { communityId } = useParams()
  const query = useStudentQuery(queryKeys.student.communities(userId), (signal) => studentApi.communities(signal), { staleTime: STALE.medium })
  if (communityId) return <CommunityDetails userId={userId} communityId={communityId} locale={locale} />
  return (
    <>
      <PageHeader title={t('student.communities.title')} description={t('student.communities.description')} />
      <QueryBoundary query={query} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.communities')} emptyIcon={Users}>
        {(items) => (
          <div className="ui-grid ui-grid--auto">
            {items.map((item) => (
              <QuizCard key={itemId(item)} to={`/app/student/communities/${itemId(item)}`} icon={Users} title={displayValue(item)} meta={displayValue(item, ['description', 'Description'])} />
            ))}
          </div>
        )}
      </QueryBoundary>
    </>
  )
}

function PostComment({ userId, communityId, postId, t }) {
  const qc = useQueryClient()
  const [body, setBody] = useState('')
  const comment = useMutation({
    mutationFn: () => studentApi.commentOnPost(postId, body.trim()),
    onSuccess: () => { setBody(''); qc.invalidateQueries({ queryKey: queryKeys.student.communityPosts(userId, communityId) }) },
  })
  return (
    <form className="cluster" onSubmit={(event) => { event.preventDefault(); if (body.trim()) comment.mutate() }}>
      <TextareaField label={t('student.communities.addComment')} value={body} onChange={(e) => setBody(e.target.value)} maxLength={1000} />
      <Button type="submit" variant="secondary" loading={comment.isPending} disabled={!body.trim()}>{t('student.communities.comment')}</Button>
      {comment.isError && <ErrorState error={comment.error} />}
    </form>
  )
}

function CommunityDetails({ userId, communityId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const [content, setContent] = useState('')
  const community = useStudentQuery(queryKeys.student.community(userId, communityId), (signal) => studentApi.community(communityId, signal), { staleTime: STALE.medium })
  const posts = useStudentQuery(queryKeys.student.communityPosts(userId, communityId), (signal) => studentApi.communityPosts(communityId, signal))
  const invalidateCommunity = () => qc.invalidateQueries({ queryKey: queryKeys.student.community(userId, communityId) })
  const join = useMutation({ mutationFn: () => studentApi.joinCommunity(communityId), onSuccess: invalidateCommunity })
  const leave = useMutation({ mutationFn: () => studentApi.leaveCommunity(communityId), onSuccess: invalidateCommunity })
  const post = useMutation({ mutationFn: () => studentApi.createCommunityPost(communityId, content), onSuccess: () => { setContent(''); qc.invalidateQueries({ queryKey: queryKeys.student.communityPosts(userId, communityId) }) } })
  return (
    <>
      <PageHeader
        title={displayValue(community.data) || t('student.communities.details')}
        description={t('student.communities.safeRendering')}
        actions={
          <span className="cluster">
            <Button onClick={() => join.mutate()} loading={join.isPending}>{t('student.communities.join')}</Button>
            <Button variant="secondary" onClick={() => leave.mutate()} loading={leave.isPending}>{t('student.communities.leave')}</Button>
          </span>
        }
      />
      {(community.isError || join.isError || leave.isError) && <ErrorState error={community.error || join.error || leave.error} onRetry={community.refetch} />}
      <div className="ui-split">
        <Card title={t('student.communities.posts')}>
          <form className="stack" onSubmit={(event) => { event.preventDefault(); if (content.trim()) post.mutate() }}>
            <TextareaField label={t('student.communities.newPost')} value={content} onChange={(e) => setContent(e.target.value)} maxLength={2000} />
            <Button type="submit" loading={post.isPending} disabled={!content.trim()}>{t('actions.submit')}</Button>
          </form>
          {post.isError && <ErrorState error={post.error} />}
          <QueryBoundary query={posts} loadingFallback={<Loading />} emptyWhen={(d) => !d?.length} emptyTitle={t('student.empty.posts')} emptyIcon={MessageSquare}>
            {(items) => (
              <ul className="ui-list">
                {items.map((item) => {
                  const postId = itemId(item)
                  const commentsCount = getField(item, 'commentsCount') ?? 0
                  return (
                    <li className="ui-list__item" key={postId}>
                      <div className="ui-list__body">
                        <div className="ui-list__title">{displayValue(item, ['authorName', 'AuthorName']) || t('student.communities.posts')}</div>
                        <div className="ui-list__meta">{displayValue(item, ['body', 'Body', 'content', 'Content'])}</div>
                        <div className="ui-list__meta ui-muted">{formatDate(getField(item, 'createdAt'), locale)} · {t('student.communities.commentsCount', { count: commentsCount })}</div>
                        <PostComment userId={userId} communityId={communityId} postId={postId} t={t} />
                      </div>
                    </li>
                  )
                })}
              </ul>
            )}
          </QueryBoundary>
        </Card>
        {community.data && <Card title={t('student.details')}><DetailList item={community.data} locale={locale} /></Card>}
      </div>
    </>
  )
}

// =============================================================================
// Competitions
// =============================================================================

export default function StudentCommunitiesPage(props) {
  const { userId, locale } = useStudentContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <CommunitiesPage userId={userId} locale={locale} {...props} />
}
