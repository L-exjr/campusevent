import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { MemoryRouter } from 'react-router-dom'
import LoginPage from '../../../pages/auth/LoginPage'
import AuthProvider from '../../../context/AuthProvider'
import { server } from '../../mocks/server'

describe('LoginPage', () => {
  it('shows the API failure as an accessible error state', async () => {
    server.use(
      http.post('http://localhost:5080/api/auth/login', () =>
        HttpResponse.json(
          { error: 'The email or password is incorrect.' },
          { status: 401 },
        )),
    )
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <AuthProvider>
          <LoginPage />
        </AuthProvider>
      </MemoryRouter>,
    )

    await user.type(screen.getByLabelText('Email address'), 'wrong@example.test')
    await user.type(screen.getByLabelText('Password'), 'bad-password')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'The email or password is incorrect.',
    )
  })
})
