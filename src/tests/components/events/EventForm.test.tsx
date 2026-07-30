import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import EventForm from '../../../components/events/EventForm'
import { renderWithAuth } from '../../testUtils'

describe('EventForm', () => {
  it('rejects missing required fields and shows validation guidance', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    renderWithAuth(
      <EventForm
        submitLabel="Create event"
        onSubmit={onSubmit}
        onCancel={vi.fn()}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Create event' }))

    expect(screen.getByLabelText('Event title')).toBeInvalid()
    expect(screen.getByLabelText('Description')).toBeInvalid()
    expect(screen.getByLabelText('Date and time')).toBeInvalid()
    expect(screen.getByLabelText('Location')).toBeInvalid()
    expect(screen.getByText('Enter an event title.')).toBeInTheDocument()
    expect(screen.getByText('Add a short description.')).toBeInTheDocument()
    expect(screen.getByText('Choose a future date and time.')).toBeInTheDocument()
    expect(screen.getByText('Enter a location.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })
})
