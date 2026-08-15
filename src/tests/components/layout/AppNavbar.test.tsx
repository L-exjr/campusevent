import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import AppNavbar from '../../../components/layout/AppNavbar'
import { users } from '../../mocks/fixtures'
import { renderWithAuth } from '../../testUtils'

describe('AppNavbar', () => {
  it('toggles the mobile menu and closes it after navigation', async () => {
    const user = userEvent.setup()
    renderWithAuth(<AppNavbar />)

    const toggle = screen.getByRole('button', { name: 'Open navigation' })
    const navigation = document.querySelector('#main-navigation')

    await user.click(toggle)
    expect(screen.getByRole('button', { name: 'Close navigation' })).toHaveAttribute('aria-expanded', 'true')
    await waitFor(() => expect(navigation).toHaveClass('show'))

    await user.click(screen.getByRole('link', { name: 'Explore events' }))
    expect(screen.getByRole('button', { name: 'Open navigation' })).toHaveAttribute('aria-expanded', 'false')
    expect(navigation).not.toHaveClass('show')

    await user.click(toggle)
    await user.click(screen.getByRole('button', { name: 'Close navigation' }))
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    expect(navigation).not.toHaveClass('show')
  })

  it('shows the unified ordinary-user navigation', async () => {
    const user = userEvent.setup()
    renderWithAuth(<AppNavbar />, { user: users.student })

    expect(screen.getByRole('link', { name: 'My registrations' })).toBeVisible()
    await user.click(screen.getByRole('button', { name: /Sam Student/i }))
    expect(screen.getByRole('link', { name: 'Request an Organizer' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'Manage events' })).toBeVisible()
    expect(screen.queryByRole('link', { name: 'Users' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Reports' })).not.toBeInTheDocument()
  })

  it('gives legacy Organizer accounts the same ordinary-user navigation', async () => {
    const user = userEvent.setup()
    renderWithAuth(<AppNavbar />, { user: users.organizer })

    expect(screen.getByRole('link', { name: 'Manage events' })).toBeVisible()
    await user.click(screen.getByRole('button', { name: /Olivia Organizer/i }))
    expect(screen.getByRole('link', { name: 'Booking requests' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'Explore events' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'Request an Organizer' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'My registrations' })).toBeVisible()
    expect(screen.queryByRole('link', { name: 'Users' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Reports' })).not.toBeInTheDocument()
  })

  it('shows Admin primary navigation and secondary operations', async () => {
    const user = userEvent.setup()
    renderWithAuth(<AppNavbar />, { user: users.admin })

    expect(screen.getByRole('link', { name: 'Reports' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'Users' })).toBeVisible()
    await user.click(screen.getByRole('button', { name: /Ada Admin/i }))
    expect(screen.getByRole('link', { name: 'Verification requests' })).toBeVisible()
    expect(screen.queryByRole('link', { name: 'Explore events' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Request an Organizer' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Manage events' })).not.toBeInTheDocument()
  })

  it('collapses the mobile menu after choosing a destination', async () => {
    const user = userEvent.setup()
    renderWithAuth(<AppNavbar />, { user: users.student })

    const toggle = screen.getByRole('button', { name: 'Open navigation' })
    await user.click(toggle)
    expect(toggle).toHaveAttribute('aria-expanded', 'true')

    await user.click(screen.getByRole('link', { name: 'My registrations' }))
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
  })
})
