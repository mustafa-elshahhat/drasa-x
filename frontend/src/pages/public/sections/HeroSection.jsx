import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import hero1 from '../../../assets/public/images/herosection.png'
import hero2 from '../../../assets/public/images/herosection-2.png'
import hero3 from '../../../assets/public/images/herosection-3.png'
import hero4 from '../../../assets/public/images/herosection-4.png'
import '../../../styles/public-hero.css'

const images = [hero1, hero2, hero3, hero4]

// Marketing hero with a 4-image carousel (rotates every 3s). The brand word is
// pulled from i18n (app.name) so the h1 reads "Welcome to DerasaX" in English
// and the Arabic equivalent in RTL.
export function HeroSection() {
  const { t } = useTranslation()
  const [currentImageIndex, setCurrentImageIndex] = useState(0)

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentImageIndex((prev) => (prev + 1) % images.length)
    }, 3000)
    return () => clearInterval(timer)
  }, [])

  return (
    <section className="public-hero">
      <div className="public-hero__inner">
        <div className="public-hero__content">
          <h1 className="public-hero__title">
            {t('public.hero.welcomePrefix')}{' '}
            <span className="public-hero__highlight">{t('app.name')}</span>
          </h1>
          <h2 className="public-hero__subtitle">{t('public.hero.subtitle')}</h2>
          <p className="public-hero__description">{t('public.hero.description')}</p>
        </div>
        <div className="public-hero__media">
          <div className="public-hero__media-frame">
            <div className="public-hero__shape-line" aria-hidden="true" />
            <div className="public-hero__shape-block" aria-hidden="true" />
            <img
              src={images[currentImageIndex]}
              alt={t('public.hero.imageAlt')}
              className="public-hero__img"
            />
          </div>
        </div>
      </div>
    </section>
  )
}
