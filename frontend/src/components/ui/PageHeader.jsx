// Page header with title, optional description and actions slot (Phase 7 §10).
export function PageHeader({ title, description, actions }) {
  return (
    <header className="ui-page-header">
      <div className="ui-page-header__text">
        <h1 className="ui-page-header__title">{title}</h1>
        {description && <p className="ui-page-header__desc">{description}</p>}
      </div>
      {actions && <div className="ui-page-header__actions">{actions}</div>}
    </header>
  )
}

export function Card({ title, description, children, className = '' }) {
  return (
    <section className={`ui-card ${className}`}>
      {(title || description) && (
        <div className="ui-card__header">
          {title && <h2 className="ui-card__title">{title}</h2>}
          {description && <p className="ui-card__desc">{description}</p>}
        </div>
      )}
      <div className="ui-card__body">{children}</div>
    </section>
  )
}
