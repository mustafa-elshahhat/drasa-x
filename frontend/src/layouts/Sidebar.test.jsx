import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { renderWithProviders } from '../test/utils'
import { Sidebar } from './Sidebar'
import { ROLES } from '../features/auth/roles'

describe('Sidebar (role-aware navigation)', () => {
  it('renders an accessible navigation landmark', () => {
    renderWithProviders(<Sidebar role={ROLES.STUDENT} />)
    expect(screen.getByRole('navigation')).toBeInTheDocument()
  })

  it('shows a student their learning + account links, not teacher/admin links', () => {
    renderWithProviders(<Sidebar role={ROLES.STUDENT} />)
    expect(screen.getByText('Dashboard')).toBeInTheDocument()
    expect(screen.getByText('Profile')).toBeInTheDocument()
    expect(screen.queryByText('Class analytics')).toBeNull()
    expect(screen.queryByText('Platform administration')).toBeNull()
  })

  it('shows a system admin the platform area', () => {
    renderWithProviders(<Sidebar role={ROLES.SYSTEM_ADMIN} />)
    expect(screen.getByText('Platform administration')).toBeInTheDocument()
    expect(screen.queryByText('Dashboard')).toBeNull()
  })
})
