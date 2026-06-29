import { useTranslation } from 'react-i18next'
import { GraduationCap } from 'lucide-react'
import instagram from '../assets/public/icons/instgram.svg'
import linkedin from '../assets/public/icons/linkedin.svg'
import facebook from '../assets/public/icons/facebook.svg'
import twitter from '../assets/public/icons/twitter.svg'
import emailIcon from '../assets/public/icons/email.svg'
import mobileIcon from '../assets/public/icons/mobile.svg'
import locationIcon from '../assets/public/icons/location.svg'
import vectorIcon from '../assets/public/icons/vector.svg'
import '../styles/public-footer.css'

// Public marketing footer. Rebranded to DerasaX, fully internationalized.
// Social icons carry translated alt text; the small contact glyphs are
// decorative (alt="" aria-hidden).
export function PublicFooter() {
  const { t } = useTranslation()
  return (
    <footer className="public-footer">
      <div className="public-footer__container">
        <div className="public-footer__brand">
          <div className="public-footer__logo">
            <GraduationCap size={28} aria-hidden="true" />
            <span className="public-footer__name">{t('app.name')}</span>
          </div>
          <p className="public-footer__tagline">{t('public.footer.tagline')}</p>
          <p className="public-footer__contact-line">{t('public.footer.email')}</p>
          <p className="public-footer__contact-line">{t('public.footer.phone')}</p>
          <div className="public-footer__socials">
            <img src={instagram} alt={t('public.footer.social.instagram')} />
            <img src={linkedin} alt={t('public.footer.social.linkedin')} />
            <img src={facebook} alt={t('public.footer.social.facebook')} />
            <img src={twitter} alt={t('public.footer.social.twitter')} />
          </div>
        </div>

        <div className="public-footer__section">
          <h3>{t('public.footer.pagesTitle')}</h3>
          <ul>
            <li>{t('public.footer.pages.home')}</li>
            <li>{t('public.footer.pages.about')}</li>
            <li>{t('public.footer.pages.product')}</li>
            <li>{t('public.footer.pages.contact')}</li>
          </ul>
        </div>

        <div className="public-footer__section">
          <h3>{t('public.footer.servicesTitle')}</h3>
          <ul>
            <li>{t('public.footer.services.aboutUs')}</li>
            <li>{t('public.footer.services.faq')}</li>
            <li>{t('public.footer.services.team')}</li>
            <li>{t('public.footer.services.terms')}</li>
          </ul>
        </div>

        <div className="public-footer__section">
          <h3>{t('public.footer.contactTitle')}</h3>
          <ul>
            <li><img src={emailIcon} alt="" aria-hidden="true" />{t('public.footer.email')}</li>
            <li><img src={mobileIcon} alt="" aria-hidden="true" />{t('public.footer.address')}</li>
            <li><img src={locationIcon} alt="" aria-hidden="true" />{t('public.footer.phone')}</li>
            <li><img src={vectorIcon} alt="" aria-hidden="true" />{t('public.footer.zip')}</li>
          </ul>
        </div>
      </div>
    </footer>
  )
}
