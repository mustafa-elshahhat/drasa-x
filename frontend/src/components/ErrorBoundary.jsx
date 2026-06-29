import { Component } from 'react'
import { telemetry } from '../lib/telemetry/telemetry'
import { config } from '../config/env'

// Application-level error boundary (Phase 7 §11). Catches render-time crashes,
// reports them to telemetry, and shows a safe fallback. In development it shows
// the message to aid debugging; in production it shows only generic copy and a
// support reference — never a stack trace.
export class ErrorBoundary extends Component {
  constructor(props) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error }
  }

  componentDidCatch(error, info) {
    telemetry.captureError(error, { componentStack: info?.componentStack, boundary: this.props.name || 'app' })
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null })
    if (this.props.onReset) this.props.onReset()
  }

  render() {
    if (!this.state.hasError) return this.props.children
    if (this.props.fallback) return this.props.fallback(this.state.error, this.handleReset)

    return (
      <div className="ui-error-boundary" role="alert">
        <h1>Something went wrong</h1>
        <p>An unexpected error occurred. You can try again, or return to the home page.</p>
        {config.isDev && this.state.error && (
          <pre className="ui-error-boundary__dev">{String(this.state.error?.message || this.state.error)}</pre>
        )}
        <div className="ui-error-boundary__actions">
          <button type="button" className="ui-btn ui-btn--primary" onClick={this.handleReset}>
            Try again
          </button>
          <a className="ui-btn ui-btn--secondary" href="/app">
            Go home
          </a>
        </div>
      </div>
    )
  }
}
