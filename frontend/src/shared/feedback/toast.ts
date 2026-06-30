// =============================================================================
// shared/feedback/toast — toast provider + hook re-export.
//
// Kept in its own non-component (.ts) module so the `useToast` hook can be part
// of the `shared` public surface without tripping react-refresh's
// only-export-components rule on the JSX-bearing feedback barrel. The provider is
// mounted in app/providers.jsx; pages call `useToast()` to enqueue toasts.
// =============================================================================
export { ToastProvider, useToast } from '../../components/feedback/ToastProvider'
