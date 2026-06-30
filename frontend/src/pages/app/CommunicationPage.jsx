import CommunicationNotificationsPage from './communication/CommunicationNotificationsPage'
import CommunicationPreferencesPage from './communication/CommunicationPreferencesPage'
import CommunicationMessagesPage from './communication/CommunicationMessagesPage'
import CommunicationThreadPage from './communication/CommunicationThreadPage'

// TEMPORARY compatibility facade (Phase 8 split). The communication dispatcher was
// split into the real, lazily-loaded page modules above; routes.shared.tsx loads
// them directly. Kept only for the legacy view-based test harness. SignalR realtime
// (NotificationsProvider) and the message/thread contracts are unchanged.
const VIEWS = {
  preferences: CommunicationPreferencesPage,
  messages: CommunicationMessagesPage,
  thread: CommunicationThreadPage,
}

export default function CommunicationPage({ view }) {
  const Page = VIEWS[view] || CommunicationNotificationsPage
  return <Page />
}
