import { screen } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import ProtectedRoute from '../../../components/routing/ProtectedRoute'
import { renderWithAuth } from '../../testUtils'
import { users } from '../../mocks/fixtures'

function GuardedRoutes() {
  return (
    <Routes>
      <Route element={<ProtectedRoute allowedRoles={['organizer']} />}>
        <Route path="/organizer-only" element={<h1>Organizer secrets</h1>} />
      </Route>
      <Route element={<ProtectedRoute allowedRoles={['admin']} />}>
        <Route path="/admin-only" element={<h1>Admin secrets</h1>} />
      </Route>
      <Route path="/login" element={<h1>Sign in</h1>} />
      <Route path="/unauthorized" element={<h1>Unauthorized</h1>} />
    </Routes>
  )
}

describe('ProtectedRoute', () => {
  it.each(['/organizer-only', '/admin-only'])(
    'shows unauthorized instead of protected content when a Student visits %s',
    (path) => {
      renderWithAuth(<GuardedRoutes />, { user: users.student, initialEntries: [path] })

      expect(screen.getByRole('heading', { name: 'Unauthorized' })).toBeVisible()
      expect(screen.queryByText(/secrets/i)).not.toBeInTheDocument()
    },
  )

  it('redirects an unauthenticated visitor to login', () => {
    renderWithAuth(<GuardedRoutes />, { initialEntries: ['/organizer-only'] })

    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeVisible()
    expect(screen.queryByText('Organizer secrets')).not.toBeInTheDocument()
  })
})
