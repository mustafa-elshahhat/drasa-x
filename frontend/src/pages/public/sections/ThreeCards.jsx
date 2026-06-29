import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import eventImg from '../../../assets/public/images/event.jpg'
import activityImg from '../../../assets/public/images/activity.jpg'
import newsImg from '../../../assets/public/images/news.jpg'
import modalVideo from '../../../assets/public/videos/Inspiring_Connecting_Empowering_Experiences_Video.mp4'
import '../../../styles/public-threecards.css'

// Card identity is static; all copy is resolved from i18n at render time so the
// cards translate and flip with the document direction. Images are ES imports
// (the original public app referenced them via hardcoded /src/assets strings,
// which 404 in a production build). The modal uses a local <video> element
// (the original used an <iframe>) so it stays within the strict CSP.
const CARD_DEFS = [
  { id: 1, key: 'events', image: eventImg, path: '/events' },
  { id: 2, key: 'activities', image: activityImg, path: '/activities' },
  { id: 3, key: 'news', image: newsImg, path: '/news' },
]

export function ThreeCards() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [selectedId, setSelectedId] = useState(null)

  const cards = CARD_DEFS.map((c) => ({
    ...c,
    title: t(`public.cards.${c.key}.title`),
    subtitle: t(`public.cards.${c.key}.subtitle`),
    fullDescription: t(`public.cards.${c.key}.fullDescription`),
    keyPoints: t(`public.cards.${c.key}.keyPoints`, { returnObjects: true }),
  }))

  const selected = cards.find((c) => c.id === selectedId) || null

  const goPrev = () => setSelectedId((id) => (id > 1 ? id - 1 : id))
  const goNext = () => setSelectedId((id) => (id < cards.length ? id + 1 : id))
  const explore = (path) => {
    setSelectedId(null)
    navigate(path)
  }

  return (
    <div className="public-cards">
      {cards.map((card, index) => (
        <button
          type="button"
          key={card.id}
          onClick={() => setSelectedId(card.id)}
          className={`public-card ${index === 1 ? 'public-card--middle' : ''}`}
        >
          <img src={card.image} className="public-card__image" alt={card.title} />
          <span className="public-card__title">{card.title}</span>
        </button>
      ))}

      {selected && (
        <div
          className="public-modal-overlay"
          onClick={() => setSelectedId(null)}
          role="presentation"
        >
          <div
            className="public-modal"
            onClick={(e) => e.stopPropagation()}
            role="dialog"
            aria-modal="true"
            aria-label={selected.title}
          >
            <div className="public-modal__wrapper">
              <div className="public-modal__media-col">
                <video
                  className="public-modal__video"
                  src={modalVideo}
                  controls
                  preload="metadata"
                  aria-label={t('public.cards.videoTitle')}
                />
              </div>
              <div className="public-modal__text-col">
                <h2>{selected.title}</h2>
                <p className="public-modal__subtitle">{selected.subtitle}</p>
                <p className="public-modal__desc">{selected.fullDescription}</p>

                {Array.isArray(selected.keyPoints) && selected.keyPoints.length > 0 && (
                  <>
                    <p className="public-modal__kp-title">{t('public.cards.keyPointsTitle')}</p>
                    <div className="public-modal__kp">
                      {selected.keyPoints.map((point) => (
                        <span key={point} className="public-modal__kp-tag">
                          {point}
                        </span>
                      ))}
                    </div>
                  </>
                )}

                <div className="public-modal__actions">
                  {selected.id > 1 && (
                    <button type="button" className="public-modal__nav" onClick={goPrev}>
                      {t('public.cards.back')}
                    </button>
                  )}
                  <button
                    type="button"
                    className="public-modal__explore"
                    onClick={() => explore(selected.path)}
                  >
                    {t('public.cards.explore')}
                  </button>
                  {selected.id < cards.length && (
                    <button type="button" className="public-modal__nav" onClick={goNext}>
                      {t('public.cards.next')}
                    </button>
                  )}
                </div>
              </div>
            </div>
            <button
              type="button"
              className="public-modal__close"
              onClick={() => setSelectedId(null)}
              aria-label={t('actions.close', 'Close')}
            >
              &times;
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
