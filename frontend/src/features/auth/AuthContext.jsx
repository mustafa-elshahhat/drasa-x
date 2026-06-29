/* eslint-disable react-refresh/only-export-components */
// =============================================================================
// Centralized authentication / session state (Phase 7 §4).
//
//   * Single source of truth for { status, user, role, expiresOn }.
//   * status: 'loading' | 'authenticated' | 'anonymous' | 'suspended'.
//   * On boot, attempts a silent refresh to restore the session BEFORE any
//     protected route renders — so protected content never flashes.
//   * Access token lives in memory (apiClient); the refresh token is HttpOnly.
//   * Proactively refreshes shortly before the access token expires.
//   * No hardcoded user/role/tenant — identity comes only from backend responses.
//   * On session loss (failed refresh on a 401) it transitions to anonymous with
//     reason 'expired', and notifies subscribers so caches can be cleared.
// =============================================================================
import { createContext, useContext, useEffect, useState, useCallback, useRef } from 'react'
import { setOnAuthLost, refreshOnce, setAccessToken } from '../../lib/api/client'
import * as authApi from './authApi'

const AuthContext = createContext(null)

// Refresh this many ms before the access token's expiry.
const REFRESH_SKEW_MS = 60_000

export function AuthProvider({ children, onSessionEnd }) {
  const [status, setStatus] = useState('loading')
  const [user, setUser] = useState(null)
  const [role, setRole] = useState(null)
  const [expiresOn, setExpiresOn] = useState(null)
  const [reason, setReason] = useState(null) // 'expired' | null
  const refreshTimer = useRef(null)
  const onSessionEndRef = useRef(onSessionEnd)
  // Keep the latest callback without writing the ref during render.
  useEffect(() => {
    onSessionEndRef.current = onSessionEnd
  }, [onSessionEnd])

  const clearTimer = useCallback(() => {
    if (refreshTimer.current) {
      clearTimeout(refreshTimer.current)
      refreshTimer.current = null
    }
  }, [])

  const applySession = useCallback((session) => {
    setUser(session.user)
    setRole(session.role)
    setExpiresOn(session.expiresOn)
    setReason(null)
    setStatus('authenticated')
  }, [])

  const endSession = useCallback(
    (nextStatus, nextReason = null) => {
      clearTimer()
      setAccessToken(null)
      setUser(null)
      setRole(null)
      setExpiresOn(null)
      setReason(nextReason)
      setStatus(nextStatus)
      // Notify subscribers (e.g. React Query) to drop all cached data so no
      // cross-user / cross-tenant data survives a logout or expiry.
      if (onSessionEndRef.current) onSessionEndRef.current()
    },
    [clearTimer]
  )

  // Schedule a proactive silent refresh just before expiry.
  const scheduleRefresh = useCallback(
    (expiry) => {
      clearTimer()
      if (!expiry) return
      const ms = new Date(expiry).getTime() - Date.now() - REFRESH_SKEW_MS
      const delay = Math.max(ms, 5_000)
      refreshTimer.current = setTimeout(async () => {
        const data = await refreshOnce()
        if (data?.token) {
          const session = authApi.toSession(data)
          applySession(session)
        }
        // On failure we do nothing here; the next authed request's 401 path
        // (onAuthLost) will end the session cleanly.
      }, delay)
    },
    [applySession, clearTimer]
  )

  useEffect(() => {
    if (status === 'authenticated' && expiresOn) scheduleRefresh(expiresOn)
    return clearTimer
  }, [status, expiresOn, scheduleRefresh, clearTimer])

  // Wire the apiClient "auth lost" hook (failed refresh after a 401).
  useEffect(() => {
    setOnAuthLost(() => endSession('anonymous', 'expired'))
  }, [endSession])

  // Boot: restore the session silently before deciding anonymous.
  useEffect(() => {
    let cancelled = false
    ;(async () => {
      const session = await authApi.restore()
      if (cancelled) return
      if (session) applySession(session)
      else endSession('anonymous')
    })()
    return () => {
      cancelled = true
    }
  }, [applySession, endSession])

  const login = useCallback(
    async (userId, password) => {
      const result = await authApi.login(userId, password)
      if (result.ok) {
        applySession(result.session)
        return { ok: true, session: result.session }
      }
      const err = result.error
      if (err?.status === 403 && err?.errorCode === 'TENANT_SUSPENDED') {
        endSession('suspended')
        return { ok: false, suspended: true, error: err }
      }
      // Account disabled/locked and invalid credentials all surface generically.
      return { ok: false, error: err }
    },
    [applySession, endSession]
  )

  const logout = useCallback(async () => {
    await authApi.logout()
    endSession('anonymous')
  }, [endSession])

  const value = {
    status,
    user,
    role,
    expiresOn,
    reason,
    isAuthenticated: status === 'authenticated',
    isLoading: status === 'loading',
    login,
    logout,
    changePassword: authApi.changePassword,
  }
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within <AuthProvider>')
  return ctx
}
