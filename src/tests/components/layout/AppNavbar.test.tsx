import { screen } from '@testing-library/react'
import AppNavbar from '../../../components/layout/AppNavbar'
import { users } from '../../mocks/fixtures'
import { renderWithAuth } from '../../testUtils'

describe('AppNavbar', () => {
  it('shows Student navigation without organizer or Admin tools', () => {
    renderWithAuth(<AppNavbar />, { user: users.student })

    expect(screen.getByRole('link', { name: 'My registrations' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'Apply to organize' })).toBeVisible()
    expect(screen.queryByRole('link', { name: 'Manage events' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Users' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Reports' })).not.toBeInTheDocument()
  })

  it('shows Organizer navigation without Student or Admin tools', () => {
    renderWithAuth(<AppNavbar />, { user: users.organizer })

    expect(screen.getByRole('link', { name: 'Manage events' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'Explore events' })).toBeVisible()
    expect(screen.queryByRole('link', { name: 'Request an Organizer' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'My registrations' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Users' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Reports' })).not.toBeInTheDocument()
  })

  it('shows Admin navigation without Organizer tools', () => {
    renderWithAuth(<AppNavbar />, { user: users.admin })

    expect(screen.getByRole('link', { name: 'Reports' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'Users' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'Applications' })).toBeVisible()
    expect(screen.queryByRole('link', { name: 'Explore events' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Request an Organizer' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Manage events' })).not.toBeInTheDocument()
  })
})
