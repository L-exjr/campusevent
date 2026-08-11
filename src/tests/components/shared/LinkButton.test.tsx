import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import LinkButton from '../../../components/shared/LinkButton'

describe('LinkButton', () => {
  it('uses same-tab router navigation without opening a popup', async () => {
    const user = userEvent.setup()
    const open = vi.spyOn(window, 'open').mockReturnValue(null)

    render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route path="/" element={<LinkButton to="/request-organizer">Plan an event</LinkButton>} />
          <Route path="/request-organizer" element={<h1>Organizer request</h1>} />
        </Routes>
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: 'Plan an event' }))

    expect(await screen.findByRole('heading', { name: 'Organizer request' })).toBeVisible()
    expect(open).not.toHaveBeenCalled()
  })
})
