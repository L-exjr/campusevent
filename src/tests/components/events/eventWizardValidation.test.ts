import {
  buildCreateEventPayload,
  validateEventWizard,
  type EventWizardSnapshot,
} from '../../../components/events/create-event/eventWizardValidation'

const hybridPaid: EventWizardSnapshot = {
  eventDate: '2030-08-20',
  eventTime: '18:30',
  eventEndDate: '2030-08-20',
  eventEndTime: '20:30',
  registrationMode: 'paid',
  values: {
    title: ' Hybrid forum ',
    description: ' A complete hybrid event description. ',
    date: '2030-08-20T18:30',
    capacity: 100,
    category: 'Startup & Tech',
    location: ' Great Hall ',
    format: 'hybrid',
    meetingUrl: ' https://meet.example.test/hybrid ',
    virtualPlatform: 'googleMeet',
    ticketingEnabled: true,
    registrationsEnabled: false,
    votingEnabled: true,
    salesStartsAt: '2030-08-01T09:00',
    salesEndsAt: '2030-08-19T18:30',
    imageUrl: null,
    isPublished: true,
    priceMinor: 5000,
    currency: 'GHS',
  },
}

describe('eventWizardValidation', () => {
  it('accepts a complete hybrid paid event', () => {
    expect(validateEventWizard(hybridPaid, '2029-01-01T00:00')).toEqual({
      basic: {},
      venue: {},
      tools: {},
    })
  })

  it('requires both hybrid venue fields and the existing paid sales window', () => {
    const errors = validateEventWizard(
      {
        ...hybridPaid,
        values: {
          ...hybridPaid.values,
          location: '',
          meetingUrl: null,
          salesEndsAt: '2030-07-31T09:00',
        },
      },
      '2029-01-01T00:00',
    )

    expect(errors.venue.location).toBeTruthy()
    expect(errors.venue.meetingUrl).toBeTruthy()
    expect(errors.tools.salesEndsAt).toBe('Ticket sales must end after they start.')
  })

  it('normalizes the expanded event payload', () => {
    expect(buildCreateEventPayload(hybridPaid, 'https://images.example.test/event.jpg')).toEqual({
      title: 'Hybrid forum',
      description: 'A complete hybrid event description.',
      date: '2030-08-20T18:30',
      endDate: '2030-08-20T20:30',
      capacity: 100,
      category: 'Startup & Tech',
      location: 'Great Hall',
      format: 'hybrid',
      meetingUrl: 'https://meet.example.test/hybrid',
      virtualPlatform: 'googleMeet',
      latitude: null,
      longitude: null,
      instagramUrl: null,
      twitterUrl: null,
      facebookUrl: null,
      websiteUrl: null,
      ticketingEnabled: true,
      registrationsEnabled: false,
      votingEnabled: true,
      salesStartsAt: '2030-08-01T09:00',
      salesEndsAt: '2030-08-19T18:30',
      imageUrl: 'https://images.example.test/event.jpg',
      isPublished: true,
      priceMinor: 5000,
      currency: 'GHS',
    })
  })
})
