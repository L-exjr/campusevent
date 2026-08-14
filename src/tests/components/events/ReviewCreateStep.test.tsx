import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ReviewCreateStep from '../../../components/events/create-event/ReviewCreateStep'
import { DEFAULT_EVENT_IMAGE } from '../../../api/imageStorage'
import type { EventInput } from '../../../types'

const values: EventInput = {
  title: 'Hybrid engineering forum',
  description: 'A forum that attendees can join on campus or online.',
  date: '2030-08-20T18:30',
  endDate: '2030-08-20T20:30',
  capacity: 200,
  category: 'Startup & Tech',
  location: 'Engineering Auditorium',
  format: 'hybrid',
  meetingUrl: 'https://meet.example.test/engineering-forum',
  virtualPlatform: 'googleMeet',
  ticketingEnabled: true,
  registrationsEnabled: false,
  votingEnabled: false,
  salesStartsAt: '2030-08-01T09:00',
  salesEndsAt: '2030-08-19T18:30',
  imageUrl: null,
  isPublished: true,
  priceMinor: 12500,
  currency: 'GHS',
}

describe('ReviewCreateStep', () => {
  it('shows a complete summary grouped by the preceding steps', () => {
    render(
      <ReviewCreateStep
        values={values}
        registrationMode="paid"
        imagePreview={DEFAULT_EVENT_IMAGE}
        onEdit={vi.fn()}
        onPublishedChange={vi.fn()}
        onBack={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Basic information' })).toBeVisible()
    expect(screen.getByText('Hybrid engineering forum')).toBeVisible()
    expect(screen.getByRole('heading', { name: 'Venue' })).toBeVisible()
    expect(screen.getByText('Engineering Auditorium')).toBeVisible()
    expect(screen.getByText('https://meet.example.test/engineering-forum')).toBeVisible()
    expect(screen.getByRole('heading', { name: 'Event tools' })).toBeVisible()
    expect(screen.getByText('Paid tickets')).toBeVisible()
    expect(screen.getByText('GHS 125.00')).toBeVisible()
    expect(screen.getByRole('heading', { name: 'Publishing' })).toBeVisible()
  })

  it('jumps to the selected edit step', async () => {
    const user = userEvent.setup()
    const onEdit = vi.fn()
    render(
      <ReviewCreateStep
        values={values}
        registrationMode="paid"
        imagePreview={DEFAULT_EVENT_IMAGE}
        onEdit={onEdit}
        onPublishedChange={vi.fn()}
        onBack={vi.fn()}
      />,
    )

    const editButtons = screen.getAllByRole('button', { name: 'Edit' })
    await user.click(editButtons[0])
    await user.click(editButtons[1])
    await user.click(editButtons[2])

    expect(onEdit.mock.calls).toEqual([[1], [2], [3]])
  })

  it('offers draft and publish as one publishing choice', async () => {
    const user = userEvent.setup()
    const onPublishedChange = vi.fn()
    render(
      <ReviewCreateStep
        values={values}
        registrationMode="paid"
        imagePreview={DEFAULT_EVENT_IMAGE}
        onEdit={vi.fn()}
        onPublishedChange={onPublishedChange}
        onBack={vi.fn()}
      />,
    )

    expect(screen.getByRole('radio', { name: /publish now/i })).toBeChecked()
    expect(screen.getByRole('button', { name: 'Publish event' })).toBeEnabled()
    await user.click(screen.getByRole('radio', { name: /save as draft/i }))
    expect(onPublishedChange).toHaveBeenCalledWith(false)
  })

  it('disables final confirmation when creation is blocked', () => {
    render(
      <ReviewCreateStep
        values={values}
        registrationMode="paid"
        imagePreview={DEFAULT_EVENT_IMAGE}
        confirmationDisabled
        onEdit={vi.fn()}
        onPublishedChange={vi.fn()}
        onBack={vi.fn()}
      />,
    )

    expect(screen.getByRole('button', { name: 'Publish event' })).toBeDisabled()
    expect(screen.getByText(/cannot be created until/i)).toBeVisible()
  })
})
