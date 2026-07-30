import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it } from 'vitest'
import PasswordInput from '../../components/auth/PasswordInput'

function Harness() {
  const [value, setValue] = useState('secret-password')
  return <PasswordInput id="password" value={value} onChange={setValue} autoComplete="current-password" />
}

describe('PasswordInput', () => {
  it('toggles visibility and updates its accessible label', async () => {
    const user = userEvent.setup()
    render(<Harness />)
    const input = screen.getByDisplayValue('secret-password')
    const toggle = screen.getByRole('button', { name: 'Show password' })
    expect(input).toHaveAttribute('type', 'password')
    await user.click(toggle)
    expect(input).toHaveAttribute('type', 'text')
    expect(screen.getByRole('button', { name: 'Hide password' })).toBeInTheDocument()
  })
})
