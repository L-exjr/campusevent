import { screen } from '@testing-library/react'
import App from '../App'
import type { Role } from '../types'
import { users } from './mocks/fixtures'
import { renderWithAuth } from './testUtils'

describe('App role dashboards', () => {
  it.each<[Role, string, RegExp]>([
    ['student', '/student', /Good to see you, Sam/i],
    ['organizer', '/organizer', /Make it memorable, Olivia/i],
    ['admin', '/admin', /Reports dashboard/i],
  ])('renders the %s dashboard for its role', async (role, path, heading) => {
    renderWithAuth(<App />, { user: users[role], initialEntries: [path] })

    expect(await screen.findByRole('heading', { name: heading })).toBeVisible()
  })
})
