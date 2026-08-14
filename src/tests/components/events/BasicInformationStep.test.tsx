import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import BasicInformationStep from '../../../components/events/create-event/BasicInformationStep'
import type { EventInput } from '../../../types'
import { DEFAULT_EVENT_IMAGE } from '../../../api/imageStorage'

const values: EventInput = {
  title: '',
  description: '',
  date: '',
  capacity: 50,
  category: 'Art & Exhibition',
  location: '',
  format: 'physical',
  meetingUrl: null,
  salesStartsAt: null,
  salesEndsAt: null,
  imageUrl: null,
  isPublished: true,
  priceMinor: 0,
  currency: 'GHS',
}

describe('BasicInformationStep', () => {
  it('renders the improved schedule and optional social fields', () => {
    render(
      <BasicInformationStep
        values={values}
        eventDate=""
        eventTime=""
        imagePreview={DEFAULT_EVENT_IMAGE}
        onValuesChange={vi.fn()}
        onEventDateChange={vi.fn()}
        onEventTimeChange={vi.fn()}
        onImageChange={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Basic information' })).toBeVisible()
    expect(screen.getByLabelText('Event title')).toBeVisible()
    expect(screen.getByLabelText('Description')).toBeVisible()
    expect(screen.getByLabelText('Category')).toBeVisible()
    expect(screen.getByLabelText('Start date')).toBeVisible()
    expect(screen.getByLabelText('Start time')).toBeVisible()
    expect(screen.getByLabelText('Cover image')).toBeVisible()
    expect(screen.getByLabelText('End date')).toBeVisible()
    expect(screen.getByLabelText('End time')).toBeVisible()
    expect(screen.getByText(/Event social links/i)).toBeVisible()
  })

  it('reports field changes to the wizard controller', async () => {
    const user = userEvent.setup()
    const onValuesChange = vi.fn()
    const onEventDateChange = vi.fn()
    const onEventTimeChange = vi.fn()

    render(
      <BasicInformationStep
        values={values}
        eventDate=""
        eventTime=""
        imagePreview={DEFAULT_EVENT_IMAGE}
        onValuesChange={onValuesChange}
        onEventDateChange={onEventDateChange}
        onEventTimeChange={onEventTimeChange}
        onImageChange={vi.fn()}
      />,
    )

    await user.type(screen.getByLabelText('Event title'), 'A')
    await user.selectOptions(screen.getByLabelText('Category'), 'Startup & Tech')
    await user.type(screen.getByLabelText('Start date'), '2030-08-20')
    fireEvent.change(screen.getByLabelText('Start time'), { target: { value: '18:30' } })

    expect(onValuesChange).toHaveBeenCalledWith({ title: 'A' })
    expect(onValuesChange).toHaveBeenCalledWith({ category: 'Startup & Tech' })
    expect(onEventDateChange).toHaveBeenLastCalledWith('2030-08-20')
    expect(onEventTimeChange).toHaveBeenLastCalledWith('18:30')
  })

  it('shows errors supplied by step-local validation', () => {
    render(
      <BasicInformationStep
        values={values}
        eventDate=""
        eventTime=""
        imagePreview={DEFAULT_EVENT_IMAGE}
        errors={{
          title: 'Event titles must contain at least 3 characters.',
          image: 'Choose a valid image.',
        }}
        onValuesChange={vi.fn()}
        onEventDateChange={vi.fn()}
        onEventTimeChange={vi.fn()}
        onImageChange={vi.fn()}
      />,
    )

    expect(screen.getByLabelText('Event title')).toBeInvalid()
    expect(screen.getByText('Event titles must contain at least 3 characters.')).toBeVisible()
    expect(screen.getByText('Choose a valid image.')).toBeVisible()
  })
})
