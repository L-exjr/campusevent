import { screen } from '@testing-library/react'
import Breadcrumbs from '../../../components/layout/Breadcrumbs'
import { renderWithAuth } from '../../testUtils'
import { users } from '../../mocks/fixtures'

describe('Breadcrumbs', () => {
  it('links Home to the public landing page when logged out', () => {
    renderWithAuth(<Breadcrumbs />, { initialEntries: ['/events'] })

    expect(screen.getByRole('link', { name: 'Home' })).toHaveAttribute('href', '/')
  })

  it.each([
    [users.student, '/student'],
    [users.organizer, '/organizer'],
    [users.admin, '/admin'],
  ])('links Home to the authenticated role home', (user, destination) => {
    renderWithAuth(<Breadcrumbs />, { user, initialEntries: ['/events/event-12345678901234567890'] })

    expect(screen.getByRole('link', { name: 'Home' })).toHaveAttribute('href', destination)
    expect(screen.getByText('Events')).toBeInTheDocument()
    expect(screen.getByText('Event')).toBeInTheDocument()
  })
})
