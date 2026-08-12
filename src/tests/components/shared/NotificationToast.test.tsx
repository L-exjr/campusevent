import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import NotificationToast from '../../../components/shared/NotificationToast'

describe('NotificationToast', () => {
  it('announces a successful update and can be dismissed', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()

    render(
      <NotificationToast
        message="Event created successfully."
        onClose={onClose}
      />,
    )

    expect(screen.getByRole('status')).toHaveTextContent('Event created successfully.')
    await user.click(screen.getByRole('button', { name: 'Close' }))
    expect(onClose).toHaveBeenCalledOnce()
  })
})
