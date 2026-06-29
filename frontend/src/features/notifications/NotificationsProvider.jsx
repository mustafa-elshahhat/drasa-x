/* eslint-disable react-refresh/only-export-components */
// =============================================================================
// Phase 13 — real-time notification delivery (SignalR). When authenticated, opens
// ONE tenant/user-scoped hub connection to DerasaX-backend's /hubs/notifications
// (the access token is sent via accessTokenFactory; the backend places the client
// in its user_{id} / {tenant}_{role} groups). On a "ReceiveNotification" push it
// invalidates the unread-count + inbox queries so the bell and centre update
// WITHOUT a page refresh. Real-time is best-effort: if the hub can't connect the
// inbox still works (the bell falls back to a slow poll, see NotificationBell).
// No fake online/presence state — `connected` reflects the real hub state.
// =============================================================================
import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { useAuth } from '../auth/AuthContext'
import { getAccessToken, BACKEND } from '../../lib/api/client'
import { queryKeys } from '../../lib/query/keys'

const RealtimeContext = createContext({ connected: false })

export function useNotificationsRealtime() {
  return useContext(RealtimeContext)
}

export function NotificationsProvider({ children }) {
  const { status, user } = useAuth()
  const queryClient = useQueryClient()
  const [connected, setConnected] = useState(false)
  const connRef = useRef(null)
  const userId = user?.id

  const invalidate = useCallback(() => {
    if (!userId) return
    queryClient.invalidateQueries({ queryKey: queryKeys.notifications.unread(userId) })
    queryClient.invalidateQueries({ queryKey: queryKeys.notifications.list(userId) })
  }, [queryClient, userId])

  useEffect(() => {
    if (status !== 'authenticated' || !userId) return undefined
    let cancelled = false
    let local = null

    ;(async () => {
      try {
        const signalR = await import('@microsoft/signalr')
        const conn = new signalR.HubConnectionBuilder()
          .withUrl(`${BACKEND}/hubs/notifications`, { accessTokenFactory: () => getAccessToken() || '' })
          .withAutomaticReconnect()
          .build()
        conn.on('ReceiveNotification', () => invalidate())
        conn.onreconnected(() => {
          if (!cancelled) {
            setConnected(true)
            invalidate()
          }
        })
        conn.onclose(() => {
          if (!cancelled) setConnected(false)
        })
        await conn.start()
        if (cancelled) {
          await conn.stop().catch(() => {})
          return
        }
        local = conn
        connRef.current = conn
        setConnected(true)
      } catch {
        // Real-time unavailable (e.g. hub unreachable in a unit-test/offline env).
        // The inbox remains fully functional via the bell's documented fallback poll.
        if (!cancelled) setConnected(false)
      }
    })()

    return () => {
      cancelled = true
      setConnected(false)
      const conn = connRef.current || local
      connRef.current = null
      if (conn) conn.stop().catch(() => {})
    }
  }, [status, userId, invalidate])

  return <RealtimeContext.Provider value={{ connected }}>{children}</RealtimeContext.Provider>
}
