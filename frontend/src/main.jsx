import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './app/App'
import { AppProviders } from './app/providers'
import { registerPwa } from './app/pwa'
import './i18n'
import './styles/app.css'

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <AppProviders>
      <App />
    </AppProviders>
  </React.StrictMode>
)

// Register the service worker (no-op in dev; prompt strategy in prod).
registerPwa()
