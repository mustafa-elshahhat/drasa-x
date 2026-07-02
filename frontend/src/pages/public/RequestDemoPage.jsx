import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import flag from '../../assets/public/images/flag.png'
import '../../styles/public-request-demo.css'

const EMPTY = {
  firstName: '',
  lastName: '',
  company: '',
  jobTitle: '',
  country: '',
  city: '',
  email: '',
  phone: '',
  message: '',
}

const CONTACT_EMAIL = 'info@derasax.com'

// Marketing "request a demo" page. There is intentionally NO backend demo
// endpoint (inventing one is out of scope), so this form cannot itself
// deliver the request anywhere — it never did, and previously showed a fake
// "your request has been received" message with no data going anywhere
// (D10). Instead, submitting builds a real `mailto:` draft (to the same
// address the public footer already advertises) and hands the visitor a
// genuine link to actually send it via their own email client.
function buildMailto(formData) {
  const subject = `Demo request${formData.company ? ` — ${formData.company}` : ''}`
  const lines = [
    ['Name', [formData.firstName, formData.lastName].filter(Boolean).join(' ')],
    ['Company', formData.company],
    ['Job title', formData.jobTitle],
    ['Country', formData.country],
    ['City', formData.city],
    ['Email', formData.email],
    ['Phone', formData.phone],
    ['Message', formData.message],
  ].filter(([, value]) => value)
  const body = lines.map(([label, value]) => `${label}: ${value}`).join('\n')
  return `mailto:${CONTACT_EMAIL}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`
}

export default function RequestDemoPage() {
  const { t } = useTranslation()
  useDocumentTitle({ titleKey: 'public.requestDemo.title' })
  const [formData, setFormData] = useState(EMPTY)
  const [submitted, setSubmitted] = useState(false)

  const handleChange = (e) =>
    setFormData((prev) => ({ ...prev, [e.target.name]: e.target.value }))
  const handleSubmit = (e) => {
    e.preventDefault()
    setSubmitted(true)
  }

  return (
    <main className="public-demo" role="main">
      <div className="public-demo__caption">
        <h1>{t('public.requestDemo.title')}</h1>
        <p>{t('public.requestDemo.subtitle')}</p>
      </div>

      <div className="public-demo__card">
        {submitted ? (
          <div className="public-demo__success" role="status">
            <p>{t('public.requestDemo.success')}</p>
            <a className="public-demo__submit" href={buildMailto(formData)}>
              {t('public.requestDemo.sendEmail')}
            </a>
          </div>
        ) : (
          <form className="public-demo__form" onSubmit={handleSubmit}>
            <div className="public-demo__row">
              <div className="public-demo__field">
                <label htmlFor="rd-firstName">{t('public.requestDemo.firstName')}*</label>
                <input
                  id="rd-firstName"
                  type="text"
                  name="firstName"
                  placeholder={t('public.requestDemo.placeholders.firstName')}
                  value={formData.firstName}
                  onChange={handleChange}
                  required
                />
              </div>
              <div className="public-demo__field">
                <label htmlFor="rd-lastName">{t('public.requestDemo.lastName')}*</label>
                <input
                  id="rd-lastName"
                  type="text"
                  name="lastName"
                  placeholder={t('public.requestDemo.placeholders.lastName')}
                  value={formData.lastName}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>

            <div className="public-demo__row">
              <div className="public-demo__field">
                <label htmlFor="rd-company">{t('public.requestDemo.company')}*</label>
                <input
                  id="rd-company"
                  type="text"
                  name="company"
                  placeholder={t('public.requestDemo.placeholders.company')}
                  value={formData.company}
                  onChange={handleChange}
                  required
                />
              </div>
              <div className="public-demo__field">
                <label htmlFor="rd-jobTitle">{t('public.requestDemo.jobTitle')}*</label>
                <input
                  id="rd-jobTitle"
                  type="text"
                  name="jobTitle"
                  placeholder={t('public.requestDemo.placeholders.jobTitle')}
                  value={formData.jobTitle}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>

            <div className="public-demo__row">
              <div className="public-demo__field">
                <label htmlFor="rd-country">{t('public.requestDemo.country')}*</label>
                <div className="public-demo__field-box">
                  <img src={flag} className="public-demo__flag" alt="" aria-hidden="true" />
                  <select
                    id="rd-country"
                    name="country"
                    value={formData.country}
                    onChange={handleChange}
                  >
                    <option value="">{t('public.requestDemo.selectCountry')}</option>
                    <option value="Egypt">{t('public.requestDemo.egypt')}</option>
                  </select>
                </div>
              </div>
              <div className="public-demo__field">
                <label htmlFor="rd-city">{t('public.requestDemo.city')}*</label>
                <input
                  id="rd-city"
                  type="text"
                  name="city"
                  placeholder={t('public.requestDemo.placeholders.city')}
                  value={formData.city}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>

            <div className="public-demo__row">
              <div className="public-demo__field">
                <label htmlFor="rd-email">{t('public.requestDemo.email')}*</label>
                <input
                  id="rd-email"
                  type="email"
                  name="email"
                  placeholder={t('public.requestDemo.placeholders.email')}
                  value={formData.email}
                  onChange={handleChange}
                  required
                />
              </div>
              <div className="public-demo__field">
                <label htmlFor="rd-phone">{t('public.requestDemo.phone')}*</label>
                <div className="public-demo__field-box">
                  <img src={flag} className="public-demo__flag" alt="" aria-hidden="true" />
                  <span className="public-demo__code">+20</span>
                  <input
                    id="rd-phone"
                    type="tel"
                    name="phone"
                    placeholder={t('public.requestDemo.placeholders.phone')}
                    value={formData.phone}
                    onChange={handleChange}
                  />
                </div>
              </div>
            </div>

            <div className="public-demo__field">
              <label htmlFor="rd-message">{t('public.requestDemo.message')}*</label>
              <textarea
                id="rd-message"
                name="message"
                placeholder={t('public.requestDemo.placeholders.message')}
                value={formData.message}
                onChange={handleChange}
              />
            </div>

            <button type="submit" className="public-demo__submit">
              {t('public.requestDemo.submit')}
            </button>
          </form>
        )}
      </div>
    </main>
  )
}
