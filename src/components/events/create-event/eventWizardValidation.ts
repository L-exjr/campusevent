import type { EventInput } from '../../../types'
import type { BasicInformationErrors } from './BasicInformationStep'
import type { EventToolsErrors, RegistrationMode } from './EventToolsStep'
import type { VenueErrors } from './VenueStep'

export interface EventWizardSnapshot {
  values: EventInput
  eventDate: string
  eventTime: string
  eventEndDate?: string
  eventEndTime?: string
  registrationMode: RegistrationMode
}

export interface EventWizardErrors {
  basic: BasicInformationErrors
  venue: VenueErrors
  tools: EventToolsErrors
}

export function combineDateTime(date?: string, time?: string) {
  return date && time ? `${date}T${time}` : ''
}

function isValidHttpUrl(value: string | null | undefined) {
  return Boolean(value?.trim() && /^https?:\/\//i.test(value.trim()))
}

export function validateBasicInformation(
  snapshot: EventWizardSnapshot,
  minimumDateTime: string,
  imageError?: string | null,
): BasicInformationErrors {
  const errors: BasicInformationErrors = {}
  const title = snapshot.values.title.trim()
  const description = snapshot.values.description.trim()
  const dateTime = combineDateTime(snapshot.eventDate, snapshot.eventTime)
  const start = new Date(dateTime).getTime()
  const end = new Date(combineDateTime(snapshot.eventEndDate, snapshot.eventEndTime)).getTime()
  const minimum = Math.max(new Date(minimumDateTime).getTime(), Date.now())

  if (title.length < 3 || title.length > 200) {
    errors.title = 'Event titles must contain between 3 and 200 characters.'
  }
  if (description.length < 10 || description.length > 5000) {
    errors.description = 'Event descriptions must contain between 10 and 5000 characters.'
  }
  if (!snapshot.eventDate || !Number.isFinite(start) || start < minimum) {
    errors.eventDate = 'Choose a future event date.'
  }
  if (!snapshot.eventTime) {
    errors.eventTime = 'Choose the start time.'
  }
  if (!snapshot.eventEndDate || !Number.isFinite(end)) errors.eventEndDate = 'Choose an event end date.'
  if (!snapshot.eventEndTime || (Number.isFinite(start) && Number.isFinite(end) && end <= start)) {
    errors.eventEndTime = 'Event end must be after the start.'
  }
  if (imageError) errors.image = imageError

  return errors
}

export function validateVenue(snapshot: EventWizardSnapshot): VenueErrors {
  const errors: VenueErrors = {}
  const needsLocation = snapshot.values.format !== 'virtual'
  const needsMeetingUrl = snapshot.values.format !== 'physical'

  if (needsLocation && !snapshot.values.location.trim()) {
    errors.location = 'Enter the physical venue.'
  }
  if (needsMeetingUrl && !isValidHttpUrl(snapshot.values.meetingUrl)) {
    errors.meetingUrl = 'Enter a valid meeting link beginning with http:// or https://.'
  }
  if (needsMeetingUrl && !snapshot.values.virtualPlatform) errors.meetingUrl = 'Choose a streaming platform and enter its link.'

  return errors
}

export function validateEventTools(snapshot: EventWizardSnapshot): EventToolsErrors {
  const errors: EventToolsErrors = {}
  const { values, registrationMode } = snapshot

  if (values.ticketingEnabled && values.registrationsEnabled) {
    errors.capacity = 'Ticketing and registrations cannot both be enabled.'
  }

  if (!Number.isInteger(values.capacity) || values.capacity < 1 || values.capacity > 100000) {
    errors.capacity = 'Event capacity must be between 1 and 100000.'
  }

  if (values.ticketingEnabled && registrationMode === 'paid') {
    if (!Number.isInteger(values.priceMinor) || values.priceMinor <= 0) {
      errors.priceMinor = 'Enter a ticket price greater than zero.'
    }

    const salesStart = values.salesStartsAt
      ? new Date(values.salesStartsAt).getTime()
      : Number.NaN
    const salesEnd = values.salesEndsAt
      ? new Date(values.salesEndsAt).getTime()
      : Number.NaN
    const eventStart = new Date(
      combineDateTime(snapshot.eventDate, snapshot.eventTime),
    ).getTime()

    if (!Number.isFinite(salesStart)) {
      errors.salesStartsAt = 'Choose when ticket sales open.'
    }
    if (!Number.isFinite(salesEnd)) {
      errors.salesEndsAt = 'Choose when ticket sales close.'
    } else if (Number.isFinite(salesStart) && salesStart >= salesEnd) {
      errors.salesEndsAt = 'Ticket sales must end after they start.'
    } else if (Number.isFinite(eventStart) && salesEnd > eventStart) {
      errors.salesEndsAt = 'Ticket sales must end no later than the event.'
    }
  }

  return errors
}

export function validateEventWizard(
  snapshot: EventWizardSnapshot,
  minimumDateTime: string,
  imageError?: string | null,
): EventWizardErrors {
  return {
    basic: validateBasicInformation(snapshot, minimumDateTime, imageError),
    venue: validateVenue(snapshot),
    tools: validateEventTools(snapshot),
  }
}

export function hasErrors(errors: object) {
  return Object.keys(errors).length > 0
}

export function firstInvalidStep(errors: EventWizardErrors): 1 | 2 | 3 | null {
  if (hasErrors(errors.basic)) return 1
  if (hasErrors(errors.venue)) return 2
  if (hasErrors(errors.tools)) return 3
  return null
}

export function buildCreateEventPayload(
  snapshot: EventWizardSnapshot,
  imageUrl: string | null,
): EventInput {
  const { values, registrationMode } = snapshot

  return {
    title: values.title.trim(),
    description: values.description.trim(),
    date: combineDateTime(snapshot.eventDate, snapshot.eventTime),
    endDate: combineDateTime(snapshot.eventEndDate, snapshot.eventEndTime),
    capacity: values.capacity,
    category: values.category,
    location: values.format === 'virtual' ? 'Online' : values.location.trim(),
    format: values.format,
    meetingUrl: values.format === 'physical' ? null : values.meetingUrl?.trim() || null,
    virtualPlatform: values.format === 'physical' ? null : values.virtualPlatform,
    latitude: values.format === 'virtual' ? null : values.latitude ?? null,
    longitude: values.format === 'virtual' ? null : values.longitude ?? null,
    instagramUrl: values.instagramUrl?.trim() || null,
    twitterUrl: values.twitterUrl?.trim() || null,
    facebookUrl: values.facebookUrl?.trim() || null,
    websiteUrl: values.websiteUrl?.trim() || null,
    ticketingEnabled: Boolean(values.ticketingEnabled),
    registrationsEnabled: Boolean(values.registrationsEnabled),
    votingEnabled: Boolean(values.votingEnabled),
    salesStartsAt: values.ticketingEnabled && registrationMode === 'paid' ? values.salesStartsAt : null,
    salesEndsAt: values.ticketingEnabled && registrationMode === 'paid' ? values.salesEndsAt : null,
    imageUrl,
    isPublished: values.isPublished ?? true,
    priceMinor: values.ticketingEnabled && registrationMode === 'paid' ? values.priceMinor : 0,
    currency: values.currency,
  }
}
