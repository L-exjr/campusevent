import type {
  AdminAuditLog,
  AuthSession,
  BookingRequest,
  BookingRequestInput,
  BookingRequestStatus,
  CheckInResult,
  CertificateDownload,
  EmailDeadLetter,
  FailedImageCleanup,
  EventFilters,
  EventInput,
  EventItem,
  EventRegistrant,
  EventPaymentStatus,
  OrganizerApplication,
  OrganizerDetail,
  OrganizerDirectorySettings,
  OrganizerSummary,
  Page,
  PaymentInitialization,
  Registration,
  ReportsData,
  Role,
  StudentRegistration,
  Ticket,
  User,
  VotingCampaign,
  VotingCampaignInput,
  VotingPaymentInitialization,
  VotingPaymentStatus,
  Coupon,
  CouponInput,
  OrganizerAnalytics,
} from '../types'
import { EVENT_CATEGORIES } from '../types'
import type { EventManagementApi } from './EventManagementApi'
import bookingTransitionsContract from '../../contracts/booking-transitions.json'

type StoredEvent = Omit<EventItem, 'registeredCount' | 'imageUrl' | 'isPublished' | 'version' | 'priceMinor' | 'currency' | 'format' | 'meetingUrl' | 'salesStartsAt' | 'salesEndsAt'> & {
  imageUrl?: string | null
  isPublished?: boolean
  version?: number
  priceMinor?: number
  currency?: 'GHS'
  format?: EventItem['format']
  meetingUrl?: string | null
  salesStartsAt?: string | null
  salesEndsAt?: string | null
}
type StoredUser = Omit<User, 'imageUrl'> & { imageUrl?: string | null; password: string }
type DirectoryStoredUser = StoredUser & { directory?: OrganizerDirectorySettings }

interface MockDatabase {
  users: DirectoryStoredUser[]
  events: StoredEvent[]
  registrations: Registration[]
  votingCampaigns: VotingCampaign[]
  organizerApplications: OrganizerApplication[]
  bookingRequests: BookingRequest[]
  emailDeadLetters: EmailDeadLetter[]
  failedImageCleanups: FailedImageCleanup[]
  adminAuditLogs: AdminAuditLog[]
}

const DB_KEY = 'campus_events_mock_db'
const SESSION_KEY = 'campus_events_session'
const LEGACY_EMAIL_SUFFIX = '@campus.edu'
const EMAIL_SUFFIX = '@cevents.com'

function paginate<T>(items: T[], page = 1, pageSize = 20): Page<T> {
  const normalizedPage = Math.max(page, 1)
  const normalizedPageSize = Math.max(pageSize, 1)
  return {
    items: items.slice(
      (normalizedPage - 1) * normalizedPageSize,
      normalizedPage * normalizedPageSize,
    ),
    page: normalizedPage,
    pageSize: normalizedPageSize,
    totalCount: items.length,
    totalPages: Math.ceil(items.length / normalizedPageSize),
  }
}

const pause = (milliseconds = 420) =>
  new Promise<void>((resolve) => window.setTimeout(resolve, milliseconds))

function daysFromNow(days: number, hour: number) {
  const date = new Date()
  date.setDate(date.getDate() + days)
  date.setHours(hour, 0, 0, 0)
  return date.toISOString()
}

function createSeedDatabase(): MockDatabase {
  const joinedAt = daysFromNow(-120, 9)
  const users: DirectoryStoredUser[] = [
    {
      id: 'user-student-1',
      name: 'Maya Johnson',
      email: 'student@cevents.com',
      password: 'demo123',
      role: 'student',
      active: true,
      joinedAt,
      directory: { isVisible: true, bio: 'Campus event producer focused on technology, conferences, and hands-on learning.', bannerUrl: null, instagramUrl: null, twitterUrl: null, facebookUrl: null, websiteUrl: null, specialties: ['Startup & Tech', 'Conferences', 'Workshops & Training'] },
    },
    {
      id: 'user-student-2',
      name: 'Daniel Owusu',
      email: 'daniel@cevents.com',
      password: 'demo123',
      role: 'student',
      active: true,
      joinedAt,
    },
    {
      id: 'user-student-3',
      name: 'Leila Hassan',
      email: 'leila@cevents.com',
      password: 'demo123',
      role: 'student',
      active: true,
      joinedAt,
    },
    {
      id: 'user-organizer-1',
      name: 'Alex Morgan',
      email: 'organizer@cevents.com',
      password: 'demo123',
      role: 'organizer',
      active: true,
      joinedAt,
    },
    {
      id: 'user-organizer-2',
      name: 'Priya Patel',
      email: 'priya@cevents.com',
      password: 'demo123',
      role: 'organizer',
      active: true,
      joinedAt,
    },
    {
      id: 'user-admin-1',
      name: 'Jordan Lee',
      email: 'admin@cevents.com',
      password: 'demo123',
      role: 'admin',
      active: true,
      joinedAt,
    },
  ]

  const createdAt = daysFromNow(-30, 10)
  const events: StoredEvent[] = [
    {
      id: 'event-1',
      title: 'Future of AI Symposium',
      description:
        'A practical afternoon exploring responsible AI, emerging research, and the skills students need for an AI-enabled workplace.',
      date: daysFromNow(5, 14),
      capacity: 120,
      category: 'Startup & Tech',
      location: 'Innovation Hall',
      organizerId: 'user-organizer-1',
      organizerName: 'Alex Morgan',
      createdAt,
    },
    {
      id: 'event-2',
      title: 'Graduate Career Lab',
      description:
        'Bring your resume and leave with a clearer personal pitch, an improved profile, and feedback from industry mentors.',
      date: daysFromNow(9, 10),
      capacity: 60,
      category: 'Conferences',
      location: 'Business School, Room 204',
      organizerId: 'user-organizer-1',
      organizerName: 'Alex Morgan',
      createdAt,
    },
    {
      id: 'event-3',
      title: 'Campus Culture Night',
      description:
        'An evening of food, music, stories, and performances celebrating the many communities that make our campus home.',
      date: daysFromNow(14, 18),
      capacity: 250,
      category: 'Cultural Events',
      location: 'Central Courtyard',
      organizerId: 'user-organizer-2',
      organizerName: 'Priya Patel',
      createdAt,
    },
    {
      id: 'event-4',
      title: 'Research Writing Workshop',
      description:
        'A focused workshop on structuring arguments, working with sources, and editing academic writing for clarity.',
      date: daysFromNow(18, 13),
      capacity: 45,
      category: 'Education & Learning',
      location: 'Main Library, Seminar 3',
      organizerId: 'user-organizer-2',
      organizerName: 'Priya Patel',
      createdAt,
    },
    {
      id: 'event-5',
      title: 'Sunrise Wellness Walk',
      description:
        'Start the day with a relaxed guided walk, light mobility exercises, and a short conversation about sustainable habits.',
      date: daysFromNow(23, 7),
      capacity: 80,
      category: 'Health & Wellness',
      location: 'North Gate Trailhead',
      organizerId: 'user-organizer-1',
      organizerName: 'Alex Morgan',
      createdAt,
    },
    {
      id: 'event-6',
      title: 'Interfaculty Futsal Final',
      description:
        'Cheer on the finalists in the annual interfaculty futsal competition and stay for the awards presentation.',
      date: daysFromNow(29, 16),
      capacity: 180,
      category: 'Gaming & Esports',
      location: 'University Sports Centre',
      organizerId: 'user-organizer-2',
      organizerName: 'Priya Patel',
      createdAt,
    },
  ]

  const registrations: Registration[] = [
    {
      id: 'registration-1',
      eventId: 'event-1',
      studentId: 'user-student-1',
      registeredAt: daysFromNow(-6, 11),
      attended: false,
    },
    {
      id: 'registration-2',
      eventId: 'event-2',
      studentId: 'user-student-1',
      registeredAt: daysFromNow(-4, 9),
      attended: false,
    },
    {
      id: 'registration-3',
      eventId: 'event-1',
      studentId: 'user-student-2',
      registeredAt: daysFromNow(-5, 15),
      attended: true,
    },
    {
      id: 'registration-4',
      eventId: 'event-3',
      studentId: 'user-student-3',
      registeredAt: daysFromNow(-3, 12),
      attended: true,
    },
    {
      id: 'registration-5',
      eventId: 'event-3',
      studentId: 'user-student-2',
      registeredAt: daysFromNow(-2, 8),
      attended: false,
    },
  ]

  const organizerApplications: OrganizerApplication[] = [
    {
      id: 'organizer-application-1',
      userId: 'user-student-2',
      userName: 'Daniel Owusu',
      userEmail: 'daniel@cevents.com',
      reason:
        'I help lead our entrepreneurship society and want to organize practical founder workshops, mentoring sessions, and student showcase events.',
      status: 'pending',
      rejectionReason: null,
      submittedAt: daysFromNow(-2, 14),
      reviewedAt: null,
      reviewedByAdminId: null,
      reviewedByAdminName: null,
    },
  ]

  const bookingRequests: BookingRequest[] = [
    {
      id: 'booking-request-1',
      organizationName: 'Engineering Society',
      contactName: 'Ama Mensah',
      email: 'ama@example.com',
      phone: '+233 20 000 0000',
      eventType: 'Industry workshop',
      proposedDate: daysFromNow(35, 14),
      alternativeDates: undefined,
      flexibilityNote: 'Weekday afternoons are preferred.',
      estimatedAttendance: 80,
      preferredOrganizer: undefined,
      description: 'A practical engineering careers workshop with alumni and industry mentors.',
      status: 'submitted',
      assignedOrganizerId: null,
      requestedOrganizerId: null,
      requestedOrganizerName: null,
      assignedOrganizerName: null,
      organizerResponseNote: null,
      draftEventId: null,
      submittedAt: daysFromNow(-1, 10),
      updatedAt: daysFromNow(-1, 10),
    },
    {
      id: 'booking-request-2',
      organizationName: 'Community Health Club',
      contactName: 'Kojo Asare',
      email: 'kojo@example.com',
      phone: '+233 24 000 0000',
      eventType: 'Wellness seminar',
      proposedDate: daysFromNow(42, 11),
      alternativeDates: undefined,
      flexibilityNote: undefined,
      estimatedAttendance: 55,
      preferredOrganizer: 'Alex Morgan',
      description: 'A student wellness seminar focused on sustainable healthy routines.',
      status: 'sentToOrganizer',
      assignedOrganizerId: 'user-organizer-1',
      requestedOrganizerId: 'user-organizer-1',
      requestedOrganizerName: 'Alex Morgan',
      assignedOrganizerName: 'Alex Morgan',
      organizerResponseNote: null,
      draftEventId: null,
      submittedAt: daysFromNow(-3, 9),
      updatedAt: daysFromNow(-2, 9),
    },
  ]

  const emailDeadLetters: EmailDeadLetter[] = [
    {
      id: 'email-dead-letter-1',
      kind: 'EventReminder',
      aggregateId: 'registration-1',
      attemptCount: 8,
      lifetimeAttemptCount: 8,
      manualRetryCount: 0,
      lastRetriedAt: null,
      lastError: 'The email provider did not accept the message.',
      createdAt: daysFromNow(-1, 8),
      canRetry: true,
    },
  ]
  const failedImageCleanups: FailedImageCleanup[] = [{
    id: 'failed-image-1',
    bucket: 'event-images',
    objectKey: 'event-images/orphaned.webp',
    kind: 'Event',
    deleteAttemptCount: 8,
    lifetimeDeleteAttemptCount: 8,
    manualRetryCount: 0,
    lastRetriedAt: null,
    lastError: 'Storage provider unavailable.',
    createdAt: daysFromNow(-2, 8),
  }]

  return {
    users,
    events,
    registrations,
    votingCampaigns: [],
    organizerApplications,
    bookingRequests,
    emailDeadLetters,
    failedImageCleanups,
    adminAuditLogs: [],
  }
}

function migrateEmailDomain(email: string) {
  return email.toLowerCase().endsWith(LEGACY_EMAIL_SUFFIX)
    ? `${email.slice(0, -LEGACY_EMAIL_SUFFIX.length)}${EMAIL_SUFFIX}`
    : email
}

function getDatabase() {
  const saved = window.localStorage.getItem(DB_KEY)
  if (saved) {
    const database = JSON.parse(saved) as MockDatabase
    database.votingCampaigns ??= []
    database.organizerApplications ??= []
    database.bookingRequests ??= []
    database.emailDeadLetters ??= []
    database.adminAuditLogs ??= []
    database.users.forEach((user) => { user.imageUrl ??= null })
    database.events.forEach((event) => {
      event.imageUrl ??= null
      event.version ??= 1
    })

    const userEmails = database.users.map((user) => migrateEmailDomain(user.email))
    const applicationEmails = database.organizerApplications.map((application) =>
      migrateEmailDomain(application.userEmail),
    )
    const emailDomainChanged =
      database.users.some((user, index) => user.email !== userEmails[index]) ||
      database.organizerApplications.some(
        (application, index) => application.userEmail !== applicationEmails[index],
      )

    if (emailDomainChanged) {
      database.users.forEach((user, index) => {
        user.email = userEmails[index]
      })
      database.organizerApplications.forEach((application, index) => {
        application.userEmail = applicationEmails[index]
      })
      window.localStorage.setItem(DB_KEY, JSON.stringify(database))
    }

    return database
  }
  const database = createSeedDatabase()
  window.localStorage.setItem(DB_KEY, JSON.stringify(database))
  return database
}

function saveDatabase(database: MockDatabase) {
  window.localStorage.setItem(DB_KEY, JSON.stringify(database))
}

function publicUser(user: StoredUser): User {
  return {
    id: user.id,
    name: user.name,
    email: user.email,
    role: user.role,
    active: user.active,
    joinedAt: user.joinedAt,
    imageUrl: user.imageUrl ?? null,
  }
}

function eventWithCount(database: MockDatabase, event: StoredEvent): EventItem {
  return {
    ...event,
    imageUrl: event.imageUrl ?? null,
    isPublished: event.isPublished ?? true,
    version: event.version ?? 1,
    priceMinor: event.priceMinor ?? 0,
    currency: event.currency ?? 'GHS',
    format: event.format ?? 'physical',
    meetingUrl: event.meetingUrl ?? null,
    salesStartsAt: event.salesStartsAt ?? null,
    salesEndsAt: event.salesEndsAt ?? null,
    registeredCount: database.registrations.filter(
      (registration) => registration.eventId === event.id,
    ).length,
  }
}

function makeId(prefix: string) {
  return `${prefix}-${crypto.randomUUID()}`
}

function getCurrentUser(database: MockDatabase) {
  const savedSession = window.localStorage.getItem(SESSION_KEY)
  if (!savedSession) throw new Error('Authentication is required.')
  const session = JSON.parse(savedSession) as AuthSession
  const user = database.users.find((item) => item.id === session.user.id)
  if (!user?.active) throw new Error('Your account is unavailable.')
  return user
}

function appendAdminAudit(
  database: MockDatabase,
  actor: StoredUser,
  action: string,
  targetType: string,
  targetId: string,
  details: object,
) {
  database.adminAuditLogs.push({
    id: makeId('audit'),
    actorUserId: actor.id,
    actorName: actor.name,
    action,
    targetType,
    targetId,
    detailsJson: JSON.stringify(details),
    createdAt: new Date().toISOString(),
  })
}

const bookingTransitions = bookingTransitionsContract as Record<
  BookingRequestStatus,
  BookingRequestStatus[]
>

function ensureBookingTransition(current: BookingRequestStatus, target: BookingRequestStatus) {
  if (bookingTransitions[current].includes(target)) return
  throw new Error(`A booking request cannot move from ${current} to ${target}.`)
}

function normalizeEventInput(input: EventInput, requireFutureDate: boolean): EventInput {
  const title = input.title.trim()
  const description = input.description.trim()
  const location = input.location.trim()
  const meetingUrl = input.meetingUrl?.trim() || null
  const salesStartsAt = input.salesStartsAt ? new Date(input.salesStartsAt) : null
  const salesEndsAt = input.salesEndsAt ? new Date(input.salesEndsAt) : null
  const date = new Date(input.date)
  const endDate = input.endDate ? new Date(input.endDate) : new Date(date.getTime() + 60 * 60_000)
  const category = EVENT_CATEGORIES.find(
    (item) => item.toLowerCase() === input.category.trim().toLowerCase(),
  )

  if (title.length < 3) throw new Error('Event titles must contain at least 3 characters.')
  if (description.length < 10) throw new Error('Event descriptions must contain at least 10 characters.')
  if ((input.format === 'physical' || input.format === 'hybrid') && !location) {
    throw new Error('An in-person venue is required.')
  }
  if ((input.format === 'virtual' || input.format === 'hybrid') &&
      (!meetingUrl || !/^https?:\/\//i.test(meetingUrl))) {
    throw new Error('A valid online meeting link is required.')
  }
  if (!Number.isFinite(date.getTime())) throw new Error('Enter a valid event date and time.')
  if (!Number.isFinite(endDate.getTime()) || endDate <= date) throw new Error('Event end must be after the start.')
  if (input.ticketingEnabled && input.registrationsEnabled) throw new Error('Ticketing and registrations cannot both be enabled.')
  if (requireFutureDate && date.getTime() <= Date.now()) {
    throw new Error('New events must be scheduled in the future.')
  }
  if (!Number.isInteger(input.capacity) || input.capacity < 1 || input.capacity > 100000) {
    throw new Error('Event capacity must be between 1 and 100000.')
  }
  if (!category) throw new Error('Choose a supported event category.')
  if (input.priceMinor > 0 && (!salesStartsAt || !salesEndsAt ||
      !Number.isFinite(salesStartsAt.getTime()) || !Number.isFinite(salesEndsAt.getTime()))) {
    throw new Error('Paid events require a sales start and end time.')
  }
  if (input.priceMinor > 0 && salesStartsAt! >= salesEndsAt!) {
    throw new Error('Ticket sales must end after they start.')
  }
  if (input.priceMinor > 0 && salesEndsAt! > date) {
    throw new Error('Ticket sales must end no later than the event start.')
  }

  return {
    title,
    description,
    date: date.toISOString(),
    endDate: endDate.toISOString(),
    capacity: input.capacity,
    category,
    location: input.format === 'virtual' ? 'Online' : location,
    format: input.format,
    meetingUrl: input.format === 'physical' ? null : meetingUrl,
    virtualPlatform: input.format === 'physical' ? null : input.virtualPlatform,
    latitude: input.format === 'virtual' ? null : input.latitude ?? null,
    longitude: input.format === 'virtual' ? null : input.longitude ?? null,
    instagramUrl: input.instagramUrl?.trim() || null,
    twitterUrl: input.twitterUrl?.trim() || null,
    facebookUrl: input.facebookUrl?.trim() || null,
    websiteUrl: input.websiteUrl?.trim() || null,
    ticketingEnabled: Boolean(input.ticketingEnabled),
    registrationsEnabled: Boolean(input.registrationsEnabled),
    votingEnabled: Boolean(input.votingEnabled),
    salesStartsAt: input.priceMinor > 0 ? salesStartsAt!.toISOString() : null,
    salesEndsAt: input.priceMinor > 0 ? salesEndsAt!.toISOString() : null,
    imageUrl: input.imageUrl ?? null,
    isPublished: input.isPublished ?? true,
    version: input.version,
    priceMinor: input.priceMinor,
    currency: input.currency,
  }
}

export const mockApi: EventManagementApi = {
  // TODO: replace with POST /api/auth/login.
  async login(email: string, password: string): Promise<AuthSession> {
    await pause()
    const database = getDatabase()
    const user = database.users.find(
      (item) => item.email.toLowerCase() === email.trim().toLowerCase(),
    )
    if (!user || user.password !== password) {
      throw new Error('The email or password is incorrect.')
    }
    if (!user.active) throw new Error('This account has been deactivated.')
    const session = {
      token: `mock.jwt.${btoa(user.id)}`,
      expiresAt: new Date(Date.now() + 75 * 60 * 1000).toISOString(),
      user: publicUser(user),
    }
    window.localStorage.setItem(SESSION_KEY, JSON.stringify(session))
    return session
  },

  // TODO: replace with POST /api/auth/register.
  async register(name: string, email: string, password: string): Promise<AuthSession> {
    await pause()
    const database = getDatabase()
    if (
      database.users.some(
        (user) => user.email.toLowerCase() === email.trim().toLowerCase(),
      )
    ) {
      throw new Error('An account with this email already exists.')
    }
    const newUser: StoredUser = {
      id: makeId('user'),
      name: name.trim(),
      email: email.trim().toLowerCase(),
      password,
      role: 'student',
      active: true,
      joinedAt: new Date().toISOString(),
    }
    database.users.push(newUser)
    saveDatabase(database)
    const session = {
      token: `mock.jwt.${btoa(newUser.id)}`,
      expiresAt: new Date(Date.now() + 75 * 60 * 1000).toISOString(),
      user: publicUser(newUser),
    }
    window.localStorage.setItem(SESSION_KEY, JSON.stringify(session))
    return session
  },

  async googleLogin(): Promise<AuthSession> {
    throw new Error('Google sign-in requires the real API configuration.')
  },

  async forgotPassword(): Promise<string> {
    await pause()
    return 'If an account exists for that email, a password reset link has been sent.'
  },

  async resetPassword(): Promise<string> {
    await pause()
    return 'Your password has been reset successfully.'
  },

  async restoreSession(): Promise<AuthSession | null> {
    await pause(180)
    const saved = window.localStorage.getItem(SESSION_KEY)
    if (!saved) return null
    const session = JSON.parse(saved) as AuthSession
    if (!session.expiresAt || new Date(session.expiresAt).getTime() <= Date.now()) {
      window.localStorage.removeItem(SESSION_KEY)
      return null
    }
    const user = getDatabase().users.find((item) => item.id === session.user.id)
    if (!user?.active) {
      window.localStorage.removeItem(SESSION_KEY)
      return null
    }
    return { ...session, user: publicUser(user) }
  },

  async logout() {
    await pause(120)
    window.localStorage.removeItem(SESSION_KEY)
  },

  // TODO: replace with GET /api/events?search=&category=&date=.
  async getEvents(filters: EventFilters = {}, page = 1, pageSize = 20): Promise<Page<EventItem>> {
    await pause()
    const database = getDatabase()
    const query = filters.search?.trim().toLowerCase()
    const selectedDay = filters.date
      ? {
          start: new Date(`${filters.date}T00:00:00`).getTime(),
          end: new Date(`${filters.date}T23:59:59.999`).getTime(),
        }
      : null
    return paginate(database.events
      .filter((event) => new Date(event.date).getTime() >= Date.now())
      .filter(
        (event) =>
          !query ||
          event.title.toLowerCase().includes(query) ||
          event.description.toLowerCase().includes(query) ||
          event.location.toLowerCase().includes(query),
      )
      .filter(
        (event) => !filters.category || event.category === filters.category,
      )
      .filter((event) => {
        if (!selectedDay) return true
        const eventTime = new Date(event.date).getTime()
        return eventTime >= selectedDay.start && eventTime <= selectedDay.end
      })
      .sort((left, right) => left.date.localeCompare(right.date))
      .map((event) => eventWithCount(database, event)), page, pageSize)
  },

  // TODO: replace with GET /api/events/{id}.
  async getEvent(id: string): Promise<EventItem> {
    await pause()
    const database = getDatabase()
    const event = database.events.find((item) => item.id === id)
    if (!event) throw new Error('This event could not be found.')
    return eventWithCount(database, event)
  },

  async getManagementEvent(id: string): Promise<EventItem> {
    await pause()
    const database = getDatabase()
    const event = database.events.find((item) => item.id === id)
    if (!event) throw new Error('Event not found.')
    const currentUser = getCurrentUser(database)
    if (currentUser.role !== 'admin' && event.organizerId !== currentUser.id) {
      throw new Error('You do not have permission to manage this event.')
    }
    return eventWithCount(database, event)
  },

  // TODO: replace with POST /api/events/{id}/registrations.
  async registerForEvent(eventId: string, studentId: string) {
    await pause()
    const database = getDatabase()
    const student = getCurrentUser(database)
    if (student.id !== studentId || student.role !== 'student') {
      throw new Error('Only Students can register for events.')
    }
    const event = database.events.find((item) => item.id === eventId)
    if (!event) throw new Error('This event could not be found.')
    if (new Date(event.date).getTime() <= Date.now()) {
      throw new Error('Registration has closed for this event.')
    }
    if (
      database.registrations.some(
        (item) => item.eventId === eventId && item.studentId === studentId,
      )
    ) {
      throw new Error('You are already registered for this event.')
    }
    const count = database.registrations.filter(
      (item) => item.eventId === eventId,
    ).length
    if (count >= event.capacity) throw new Error('This event is at capacity.')
    database.registrations.push({
      id: makeId('registration'),
      eventId,
      studentId,
      registeredAt: new Date().toISOString(),
      attended: false,
    })
    saveDatabase(database)
  },

  async initializeEventPayment(eventId: string): Promise<PaymentInitialization> {
    const database = getDatabase()
    const student = getCurrentUser(database)
    await this.registerForEvent(eventId, student.id)
    const reference = `mock_${crypto.randomUUID()}`
    return {
      reference,
      authorizationUrl: `/payment/callback?reference=${reference}`,
      amountMinor: database.events.find((event) => event.id === eventId)?.priceMinor ?? 0,
      currency: 'GHS',
      expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
    }
  },

  async getPaymentStatus(reference: string): Promise<EventPaymentStatus> {
    await pause()
    return {
      reference,
      status: 'verified',
      amountMinor: 0,
      currency: 'GHS',
      registrationId: null,
      expiresAt: new Date().toISOString(),
    }
  },

  async getTicket(registrationId: string): Promise<Ticket> {
    await pause()
    const database = getDatabase()
    const student = getCurrentUser(database)
    const registration = database.registrations.find((item) => item.id === registrationId)
    if (!registration || registration.studentId !== student.id) {
      throw new Error('Registration not found.')
    }
    const event = database.events.find((item) => item.id === registration.eventId)!
    return {
      registrationId,
      eventId: event.id,
      eventTitle: event.title,
      studentName: student.name,
      token: `mock-ticket:${registrationId}:${event.id}:${student.id}`,
      expiresAt: new Date(new Date(event.date).getTime() + 86_400_000).toISOString(),
    }
  },

  async checkInTicket(eventId: string, token: string): Promise<CheckInResult> {
    await pause()
    const database = getDatabase()
    const parts = token.split(':')
    const registration = database.registrations.find((item) =>
      item.id === parts[1] && item.eventId === eventId)
    if (!registration) throw new Error('The ticket is invalid.')
    if (registration.attended) throw new Error('This ticket has already been checked in.')
    registration.attended = true
    saveDatabase(database)
    const student = database.users.find((item) => item.id === registration.studentId)!
    return {
      registrationId: registration.id,
      eventId,
      studentName: student.name,
      checkedInAt: new Date().toISOString(),
    }
  },

  async checkInTicketByCode(eventId: string, ticketCode: string): Promise<CheckInResult> {
    const database = getDatabase()
    const registration = database.registrations.find((item) =>
      item.eventId === eventId && item.id.slice(-8).toUpperCase() === ticketCode.replace(/^EMS-/, '').toUpperCase())
    if (!registration) throw new Error('Ticket code was not found.')
    return this.checkInTicket(eventId, `mock-ticket:${registration.id}:${eventId}:${registration.studentId}`)
  },

  async getCertificate(registrationId: string): Promise<CertificateDownload> {
    await pause()
    const database = getDatabase()
    const student = getCurrentUser(database)
    const registration = database.registrations.find((item) => item.id === registrationId)
    if (!registration || registration.studentId !== student.id) {
      throw new Error('Registration not found.')
    }
    const event = database.events.find((item) => item.id === registration.eventId)!
    if (!registration.attended) {
      throw new Error('A certificate is available only after attendance has been confirmed.')
    }
    if (new Date(event.date).getTime() > Date.now()) {
      throw new Error('A certificate is available only after the event has ended.')
    }
    const now = new Date()
    return {
      registrationId,
      downloadUrl: `data:application/pdf,Mock%20certificate%20for%20${encodeURIComponent(event.title)}`,
      generatedAt: now.toISOString(),
      expiresAt: new Date(now.getTime() + 5 * 60_000).toISOString(),
    }
  },

  async getVotingCampaign(eventId: string): Promise<VotingCampaign> {
    await pause()
    const campaign = getDatabase().votingCampaigns.find((item) => item.eventId === eventId)
    if (!campaign) throw new Error('Voting campaign not found.')
    return structuredClone(campaign)
  },

  async saveVotingCampaign(eventId: string, input: VotingCampaignInput): Promise<VotingCampaign> {
    await pause()
    const database = getDatabase()
    const event = database.events.find((item) => item.id === eventId)
    if (!event) throw new Error('Event not found.')
    const now = Date.now()
    const campaign: VotingCampaign = {
      id: makeId('voting-campaign'),
      eventId,
      eventTitle: event.title,
      opensAt: input.opensAt,
      closesAt: input.closesAt,
      isPublished: input.isPublished,
      status: !input.isPublished ? 'Draft' : now < new Date(input.opensAt).getTime()
        ? 'Scheduled' : now >= new Date(input.closesAt).getTime() ? 'Closed' : 'Open',
      canManage: true,
      resultsVisible: true,
      categories: input.categories.map((category) => ({
        id: makeId('voting-category'),
        name: category.name,
        description: category.description || null,
        mode: category.mode,
        pricePerVoteMinor: category.mode === 'paid' ? category.pricePerVoteMinor : 0,
        currency: 'GHS',
        hasVoted: false,
        nominees: category.nominees.map((nominee) => ({
          id: makeId('voting-nominee'),
          name: nominee.name,
          description: nominee.description || null,
          voteCount: 0,
        })),
      })),
    }
    database.votingCampaigns = database.votingCampaigns.filter((item) => item.eventId !== eventId)
    database.votingCampaigns.push(campaign)
    saveDatabase(database)
    return structuredClone(campaign)
  },

  async castFreeVote(categoryId: string, nomineeId: string): Promise<void> {
    await pause()
    const database = getDatabase()
    const category = database.votingCampaigns.flatMap((item) => item.categories)
      .find((item) => item.id === categoryId)
    if (!category) throw new Error('Voting category not found.')
    if (category.hasVoted) throw new Error('You have already voted in this category.')
    const nominee = category.nominees.find((item) => item.id === nomineeId)
    if (!nominee) throw new Error('Nominee not found.')
    category.hasVoted = true
    nominee.voteCount = (nominee.voteCount ?? 0) + 1
    saveDatabase(database)
  },

  async initializeVotingPayment(
    categoryId: string,
    nomineeId: string,
    quantity: number,
  ): Promise<VotingPaymentInitialization> {
    await pause()
    const category = getDatabase().votingCampaigns.flatMap((item) => item.categories)
      .find((item) => item.id === categoryId)
    if (!category) throw new Error('Voting category not found.')
    const reference = `vote_${makeId('mock')}`
    return {
      reference,
      authorizationUrl: `/voting/payment/callback?reference=${encodeURIComponent(reference)}`,
      categoryId,
      nomineeId,
      quantity,
      amountMinor: category.pricePerVoteMinor * quantity,
      currency: 'GHS',
      expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
    }
  },

  async getVotingPaymentStatus(reference: string): Promise<VotingPaymentStatus> {
    await pause()
    return {
      reference,
      status: 'verified',
      categoryId: '',
      nomineeId: '',
      quantity: 1,
      amountMinor: 0,
      currency: 'GHS',
      voteRecorded: true,
      expiresAt: new Date().toISOString(),
    }
  },

  async isRegisteredForEvent(eventId: string) {
    await pause()
    const database = getDatabase()
    const student = getCurrentUser(database)
    return database.registrations.some(
      (registration) => registration.eventId === eventId && registration.studentId === student.id,
    )
  },

  // TODO: replace with GET /api/registrations/me.
  async getStudentRegistrations(studentId: string, page = 1, pageSize = 20): Promise<Page<StudentRegistration>> {
    await pause()
    const database = getDatabase()
    return paginate(database.registrations
      .filter((item) => item.studentId === studentId)
      .map((registration) => {
        const event = database.events.find((item) => item.id === registration.eventId)
        if (!event) return null
        return { registration, event: eventWithCount(database, event) }
      })
      .filter((item): item is StudentRegistration => item !== null)
      .sort((left, right) => left.event.date.localeCompare(right.event.date)), page, pageSize)
  },

  async getMyOrganizerApplication(): Promise<OrganizerApplication | null> {
    await pause()
    const database = getDatabase()
    const user = getCurrentUser(database)
    return database.organizerApplications
      .filter((application) => application.userId === user.id)
      .sort((left, right) => right.submittedAt.localeCompare(left.submittedAt))[0] ?? null
  },

  async submitOrganizerApplication(reason: string): Promise<OrganizerApplication> {
    await pause()
    const database = getDatabase()
    const user = getCurrentUser(database)
    if (user.role !== 'student') throw new Error('Only Students can apply to become Organizers.')
    if (database.organizerApplications.some(
      (application) => application.userId === user.id && application.status === 'pending',
    )) {
      throw new Error('You already have a pending organizer application.')
    }

    const normalizedReason = reason.trim()
    if (normalizedReason.length < 20) {
      throw new Error('Tell us why you want to become an Organizer using at least 20 characters.')
    }
    if (normalizedReason.length > 2000) {
      throw new Error('Your reason cannot be longer than 2000 characters.')
    }

    const application: OrganizerApplication = {
      id: makeId('organizer-application'),
      userId: user.id,
      userName: user.name,
      userEmail: user.email,
      reason: normalizedReason,
      status: 'pending',
      rejectionReason: null,
      submittedAt: new Date().toISOString(),
      reviewedAt: null,
      reviewedByAdminId: null,
      reviewedByAdminName: null,
    }
    database.organizerApplications.push(application)
    saveDatabase(database)
    return application
  },

  async getPendingOrganizerApplications(page = 1, pageSize = 20, search = ''): Promise<Page<OrganizerApplication>> {
    await pause()
    const database = getDatabase()
    const user = getCurrentUser(database)
    if (user.role !== 'admin') throw new Error('Admin access is required.')
    const query = search.trim().toLowerCase()
    return paginate(database.organizerApplications
      .filter((application) => application.status === 'pending')
      .filter((application) => !query ||
        application.userName.toLowerCase().includes(query) ||
        application.userEmail.toLowerCase().includes(query) ||
        application.reason.toLowerCase().includes(query))
      .sort((left, right) => right.submittedAt.localeCompare(left.submittedAt)), page, pageSize)
  },

  async approveOrganizerApplication(id: string): Promise<OrganizerApplication> {
    await pause()
    const database = getDatabase()
    const admin = getCurrentUser(database)
    if (admin.role !== 'admin') throw new Error('Admin access is required.')
    const application = database.organizerApplications.find((item) => item.id === id)
    if (!application) throw new Error('Organizer application not found.')
    if (application.status !== 'pending') throw new Error('This application has already been reviewed.')
    const applicant = database.users.find((user) => user.id === application.userId)
    if (!applicant) throw new Error('User account not found.')
    if (!applicant.active) throw new Error('An inactive user cannot become an Organizer.')
    if (applicant.role !== 'student') throw new Error('The applicant is no longer a Student.')

    application.status = 'approved'
    application.reviewedAt = new Date().toISOString()
    application.reviewedByAdminId = admin.id
    application.reviewedByAdminName = admin.name
    application.rejectionReason = null
    applicant.role = 'organizer'
    appendAdminAudit(
      database,
      admin,
      'OrganizerApplicationApproved',
      'OrganizerApplication',
      application.id,
      { userId: applicant.id, status: 'approved' },
    )
    saveDatabase(database)
    return application
  },

  async rejectOrganizerApplication(id: string, reason?: string): Promise<OrganizerApplication> {
    await pause()
    const database = getDatabase()
    const admin = getCurrentUser(database)
    if (admin.role !== 'admin') throw new Error('Admin access is required.')
    const application = database.organizerApplications.find((item) => item.id === id)
    if (!application) throw new Error('Organizer application not found.')
    if (application.status !== 'pending') throw new Error('This application has already been reviewed.')
    const applicant = database.users.find((user) => user.id === application.userId)
    if (!applicant) throw new Error('User account not found.')
    if (!applicant.active) throw new Error('An inactive user application cannot be reviewed.')
    if (applicant.role !== 'student') throw new Error('The applicant is no longer a Student.')

    const normalizedReason = reason?.trim() || null
    if (normalizedReason && normalizedReason.length > 1000) {
      throw new Error('Rejection feedback cannot be longer than 1000 characters.')
    }
    application.status = 'rejected'
    application.reviewedAt = new Date().toISOString()
    application.reviewedByAdminId = admin.id
    application.reviewedByAdminName = admin.name
    application.rejectionReason = normalizedReason
    appendAdminAudit(
      database,
      admin,
      'OrganizerApplicationRejected',
      'OrganizerApplication',
      application.id,
      { userId: applicant.id, status: 'rejected' },
    )
    saveDatabase(database)
    return application
  },

  // TODO: replace with organizer-scoped GET /api/events/mine.
  async getOrganizerEvents(organizerId: string, upcomingOnly = false, page = 1, pageSize = 20): Promise<Page<EventItem>> {
    await pause()
    const database = getDatabase()
    const now = new Date().toISOString()
    return paginate(database.events
      .filter((event) => event.organizerId === organizerId && (!upcomingOnly || event.date > now))
      .sort((left, right) => left.date.localeCompare(right.date))
      .map((event) => eventWithCount(database, event)), page, pageSize)
  },

  // TODO: replace with POST /api/events.
  async createEvent(input: EventInput): Promise<EventItem> {
    await pause()
    const database = getDatabase()
    const organizer = getCurrentUser(database)
    if (organizer.role !== 'organizer' && organizer.role !== 'admin') {
      throw new Error('Only Organizers or Admins can create events.')
    }
    const normalized = normalizeEventInput(input, true)
    const event: StoredEvent = {
      ...normalized,
      id: makeId('event'),
      organizerId: organizer.id,
      organizerName: organizer.name,
      createdAt: new Date().toISOString(),
      version: 1,
      ticketTiers: normalized.ticketTiers?.map((tier) => ({
        id: tier.id ?? makeId('tier'), name: tier.name, priceMinor: tier.priceMinor,
        capacity: tier.capacity, sold: 0, isActive: true,
      })),
    }
    database.events.push(event)
    if (organizer.role === 'admin') {
      appendAdminAudit(
        database,
        organizer,
        'EventCreated',
        'Event',
        event.id,
        { title: event.title, isPublished: event.isPublished ?? true },
      )
    }
    saveDatabase(database)
    return eventWithCount(database, event)
  },

  // TODO: replace with PUT /api/events/{id}.
  async updateEvent(id: string, input: EventInput): Promise<EventItem> {
    await pause()
    const database = getDatabase()
    const event = database.events.find((item) => item.id === id)
    if (!event) throw new Error('This event could not be found.')
    if (input.version !== (event.version ?? 1)) {
      throw new Error('This event changed after you opened it. Refresh and try again.')
    }
    Object.assign(event, normalizeEventInput(input, false))
    event.version = (event.version ?? 1) + 1
    const actor = getCurrentUser(database)
    if (actor.role === 'admin') {
      appendAdminAudit(
        database,
        actor,
        'EventUpdated',
        'Event',
        event.id,
        { title: event.title, isPublished: event.isPublished, version: event.version },
      )
    }
    saveDatabase(database)
    return eventWithCount(database, event)
  },

  async transferEventOwnership(
    id: string,
    organizerId: string,
    version: number,
  ): Promise<EventItem> {
    await pause()
    const database = getDatabase()
    const actor = getCurrentUser(database)
    if (actor.role !== 'admin') throw new Error('Only Admins can transfer event ownership.')
    const event = database.events.find((item) => item.id === id)
    if (!event) throw new Error('Event not found.')
    if ((event.version ?? 1) !== version) {
      throw new Error('This event changed after you opened it. Refresh and try again.')
    }
    const organizer = database.users.find(
      (user) => user.id === organizerId && user.role !== 'admin' && user.active,
    )
    if (!organizer) {
      throw new Error('Event ownership can only be transferred to an active Organizer.')
    }
    if (event.organizerId === organizer.id) throw new Error('This Organizer already owns the event.')
    const previousOrganizerId = event.organizerId
    event.organizerId = organizer.id
    event.organizerName = organizer.name
    event.version = (event.version ?? 1) + 1
    database.bookingRequests
      .filter((request) => request.draftEventId === event.id && request.status === 'accepted')
      .forEach((request) => {
        request.assignedOrganizerId = organizer.id
        request.assignedOrganizerName = organizer.name
        request.updatedAt = new Date().toISOString()
      })
    appendAdminAudit(
      database,
      actor,
      'EventOwnershipTransferred',
      'Event',
      event.id,
      { previousOrganizerId, newOrganizerId: organizer.id },
    )
    saveDatabase(database)
    return eventWithCount(database, event)
  },

  // TODO: replace with DELETE /api/events/{id}.
  async deleteEvent(id: string) {
    await pause()
    const database = getDatabase()
    const actor = getCurrentUser(database)
    const event = database.events.find((item) => item.id === id)
    if (actor.role === 'admin' && event) {
      appendAdminAudit(
        database,
        actor,
        'EventDeleted',
        'Event',
        id,
        { title: event.title, organizerId: event.organizerId },
      )
    }
    database.events = database.events.filter((event) => event.id !== id)
    database.registrations = database.registrations.filter(
      (registration) => registration.eventId !== id,
    )
    saveDatabase(database)
  },

  // TODO: replace with GET /api/events/{id}/registrants.
  async getEventRegistrants(
    eventId: string,
    page = 1,
    pageSize = 20,
    search = '',
    attended?: boolean,
  ): Promise<Page<EventRegistrant>> {
    await pause()
    const database = getDatabase()
    const actor = getCurrentUser(database)
    const event = database.events.find((item) => item.id === eventId)
    if (!event) throw new Error('This event could not be found.')
    if (
      actor.role !== 'admin' &&
      (actor.role !== 'organizer' || event.organizerId !== actor.id)
    ) {
      throw new Error('You do not have permission to view this event’s registrants.')
    }
    const query = search.trim().toLowerCase()
    return paginate(database.registrations
      .filter((registration) => registration.eventId === eventId)
      .map((registration) => {
        const student = database.users.find(
          (user) => user.id === registration.studentId,
        )
        if (!student) return null
        return {
          registrationId: registration.id,
          userId: student.id,
          name: student.name,
          email: student.email,
          registeredAt: registration.registeredAt,
          attended: registration.attended,
        }
      })
      .filter((item): item is EventRegistrant => item !== null)
      .filter((item) =>
        (!query || item.name.toLowerCase().includes(query) || item.email.toLowerCase().includes(query)) &&
        (attended === undefined || item.attended === attended))
      .sort((left, right) => left.name.localeCompare(right.name)), page, pageSize)
  },

  // TODO: replace with PUT /api/events/{id}/attendance.
  async updateAttendance(eventId: string, attendance: Record<string, boolean>) {
    await pause()
    const database = getDatabase()
    database.registrations.forEach((registration) => {
      if (
        registration.eventId === eventId &&
        attendance[registration.id] !== undefined
      ) {
        registration.attended = attendance[registration.id]
      }
    })
    saveDatabase(database)
  },

  async exportEventRegistrants(eventId: string): Promise<Blob> {
    const rows = getDatabase().registrations.filter((item) => item.eventId === eventId)
    return new Blob([`Name,Email,Registration date,Checked in\n${rows.length ? 'Mock attendee,mock@example.test,,No' : ''}`], { type: 'text/csv' })
  },

  async getOrganizerAnalytics(): Promise<OrganizerAnalytics> {
    const database = getDatabase()
    const user = getCurrentUser(database)
    const eventIds = new Set(database.events.filter((item) => item.organizerId === user.id).map((item) => item.id))
    const registrations = database.registrations.filter((item) => eventIds.has(item.eventId))
    return { registrationCount: registrations.length, ticketRevenueMinor: 0, currency: 'GHS',
      attendedCount: registrations.filter((item) => item.attended).length,
      attendanceRate: registrations.length ? registrations.filter((item) => item.attended).length * 100 / registrations.length : 0,
      registrationsOverTime: [] }
  },

  async getCoupons(): Promise<Coupon[]> { return [] },
  async createCoupon(input: CouponInput): Promise<Coupon> {
    return { id: makeId('coupon'), ...input, used: 0, eventTitle: null }
  },
  async updateCoupon(id: string, input: CouponInput): Promise<Coupon> {
    return { id, ...input, used: 0, eventTitle: null }
  },

  // TODO: replace with GET /api/admin/users.
  async getUsers(page = 1, pageSize = 20, search = '', role?: Role): Promise<Page<User>> {
    await pause()
    const query = search.trim().toLowerCase()
    return paginate(getDatabase().users.map(publicUser).filter((user) =>
      (!query || user.name.toLowerCase().includes(query) || user.email.toLowerCase().includes(query)) &&
      (!role || user.role === role)), page, pageSize)
  },

  async searchOrganizers(search = '', pageSize = 20): Promise<Page<User>> {
    await pause()
    const query = search.trim().toLowerCase()
    return paginate(getDatabase().users.map(publicUser).filter((user) =>
      user.role !== 'admin' &&
      user.active &&
      (!query || user.name.toLowerCase().includes(query) || user.email.toLowerCase().includes(query))),
    1, pageSize)
  },

  // TODO: replace with PATCH /api/admin/users/{id}/role.
  async updateUserRole(id: string, role: Exclude<Role, 'admin'>) {
    await pause()
    const database = getDatabase()
    const user = database.users.find((item) => item.id === id)
    if (!user) throw new Error('User account could not be found.')
    if (user.role === 'admin') throw new Error('Admin roles cannot be changed here.')
    const admin = getCurrentUser(database)
    const previousRole = user.role
    user.role = role
    if (role === 'organizer') {
      database.organizerApplications
        .filter((application) => application.userId === id && application.status === 'pending')
        .forEach((application) => {
          application.status = 'approved'
          application.reviewedAt = new Date().toISOString()
          application.reviewedByAdminId = admin.id
          application.reviewedByAdminName = admin.name
          application.rejectionReason = null
        })
    }
    appendAdminAudit(
      database,
      admin,
      'UserRoleChanged',
      'User',
      user.id,
      { previousRole, newRole: role },
    )
    saveDatabase(database)
  },

  // TODO: replace with PATCH /api/admin/users/{id}/status.
  async updateUserStatus(id: string, active: boolean) {
    await pause()
    const database = getDatabase()
    const user = database.users.find((item) => item.id === id)
    if (!user) throw new Error('User account could not be found.')
    if (user.role === 'admin') throw new Error('Admin accounts cannot be deactivated here.')
    const admin = getCurrentUser(database)
    user.active = active
    if (!active) {
      appendAdminAudit(
        database,
        admin,
        'UserDeactivated',
        'User',
        user.id,
        { role: user.role },
      )
    }
    saveDatabase(database)
  },

  async updateProfile(id: string, imageUrl: string | null): Promise<User> {
    await pause()
    const database = getDatabase()
    const currentUser = getCurrentUser(database)
    if (currentUser.id !== id) throw new Error('You may only update your own profile.')
    currentUser.imageUrl = imageUrl
    saveDatabase(database)
    return publicUser(currentUser)
  },

  // TODO: replace with GET /api/admin/events.
  async getAllEvents(page = 1, pageSize = 20, filters: EventFilters = {}): Promise<Page<EventItem>> {
    await pause()
    const database = getDatabase()
    const query = filters.search?.trim().toLowerCase()
    return paginate(database.events
      .filter((event) =>
        (!query || event.title.toLowerCase().includes(query) || event.organizerName.toLowerCase().includes(query)) &&
        (!filters.category || event.category === filters.category))
      .sort((left, right) => left.date.localeCompare(right.date))
      .map((event) => eventWithCount(database, event)), page, pageSize)
  },

  // TODO: replace with GET /api/admin/reports/summary.
  async getReports(page = 1, pageSize = 20): Promise<ReportsData> {
    await pause()
    const database = getDatabase()
    const events = database.events.map((event) => {
      const registrations = database.registrations.filter(
        (registration) => registration.eventId === event.id,
      )
      const attended = registrations.filter((registration) => registration.attended).length
      return {
        eventId: event.id,
        title: event.title,
        organizerName: event.organizerName,
        registrations: registrations.length,
        attended,
        attendanceRate: registrations.length
          ? (attended / registrations.length) * 100
          : 0,
      }
    })
    const organizers = database.users
      .filter((user) => user.role !== 'admin' && database.events.some(event => event.organizerId === user.id))
      .map((organizer) => {
        const organizerEvents = database.events.filter(
          (event) => event.organizerId === organizer.id,
        )
        const eventIds = new Set(organizerEvents.map((event) => event.id))
        return {
          organizerId: organizer.id,
          organizerName: organizer.name,
          events: organizerEvents.length,
          registrations: database.registrations.filter((registration) =>
            eventIds.has(registration.eventId),
          ).length,
        }
      })
      .sort((left, right) => right.registrations - left.registrations)
    const attended = database.registrations.filter(
      (registration) => registration.attended,
    ).length
    return {
      totalEvents: database.events.length,
      totalRegistrations: database.registrations.length,
      totalUsers: database.users.length,
      attendanceRate: database.registrations.length
        ? (attended / database.registrations.length) * 100
        : 0,
      events: events.slice((page - 1) * pageSize, page * pageSize),
      organizers,
      eventPage: page,
      eventPageSize: pageSize,
      eventTotalCount: events.length,
      eventTotalPages: Math.ceil(events.length / pageSize),
    }
  },

  async getFailedEmails(page = 1, pageSize = 20) {
    await pause()
    const database = getDatabase()
    const actor = getCurrentUser(database)
    if (actor.role !== 'admin') throw new Error('Admin access is required.')
    return paginate(
      [...database.emailDeadLetters].sort((left, right) =>
        right.createdAt.localeCompare(left.createdAt)),
      page,
      pageSize,
    )
  },

  async retryFailedEmail(id: string) {
    await pause()
    const database = getDatabase()
    const admin = getCurrentUser(database)
    if (admin.role !== 'admin') throw new Error('Admin access is required.')
    const message = database.emailDeadLetters.find((item) => item.id === id)
    if (!message) throw new Error('Failed email message not found.')
    if (!message.canRetry) {
      throw new Error('This message cannot be retried safely. Generate a new domain action instead.')
    }
    message.manualRetryCount += 1
    message.lastRetriedAt = new Date().toISOString()
    database.emailDeadLetters = database.emailDeadLetters.filter((item) => item.id !== id)
    appendAdminAudit(
      database,
      admin,
      'EmailDeadLetterRetried',
      'EmailOutboxMessage',
      id,
      { kind: message.kind, previousAttemptCount: message.attemptCount },
    )
    saveDatabase(database)
  },

  async getFailedImageCleanups(page = 1, pageSize = 20) {
    await pause()
    const database = getDatabase()
    if (getCurrentUser(database).role !== 'admin') throw new Error('Admin access is required.')
    return paginate(database.failedImageCleanups ?? [], page, pageSize)
  },

  async retryFailedImageCleanup(id: string) {
    await pause()
    const database = getDatabase()
    const admin = getCurrentUser(database)
    if (admin.role !== 'admin') throw new Error('Admin access is required.')
    const item = (database.failedImageCleanups ?? []).find((candidate) => candidate.id === id)
    if (!item) throw new Error('Failed image cleanup item not found.')
    database.failedImageCleanups = (database.failedImageCleanups ?? [])
      .filter((candidate) => candidate.id !== id)
    appendAdminAudit(database, admin, 'ImageCleanupRetried', 'ImageUpload', id, {
      bucket: item.bucket, objectKey: item.objectKey, previousAttemptCount: item.deleteAttemptCount,
    })
    saveDatabase(database)
  },

  async getAdminAuditLogs(search = '', page = 1, pageSize = 20) {
    await pause()
    const database = getDatabase()
    const actor = getCurrentUser(database)
    if (actor.role !== 'admin') throw new Error('Admin access is required.')
    const query = search.trim().toLowerCase()
    return paginate(database.adminAuditLogs
      .filter((log) => !query ||
        log.actorName.toLowerCase().includes(query) ||
        log.action.toLowerCase().includes(query) ||
        log.targetType.toLowerCase().includes(query) ||
        log.targetId.toLowerCase().includes(query))
      .sort((left, right) => right.createdAt.localeCompare(left.createdAt)), page, pageSize)
  },

  async exportAdminAuditLogs(from, to) {
    await pause()
    const database = getDatabase()
    if (getCurrentUser(database).role !== 'admin') throw new Error('Admin access is required.')
    const rows = database.adminAuditLogs.filter((log) =>
      (!from || log.createdAt >= from) && (!to || log.createdAt <= to))
    const quote = (value: string) => `"${value.replace(/"/g, '""')}"`
    return new Blob([
      'createdAt,actorUserId,actorName,action,targetType,targetId,detailsJson\r\n',
      ...rows.map((log) => [log.createdAt, log.actorUserId, log.actorName, log.action,
        log.targetType, log.targetId, log.detailsJson].map(quote).join(',') + '\r\n'),
    ], { type: 'text/csv' })
  },

  async submitBookingRequest(input: BookingRequestInput): Promise<string> {
    await pause()
    if (input.website.trim()) return 'Your organizer request has been received.'
    if (new Date(input.proposedDate).getTime() <= Date.now()) {
      throw new Error('The proposed date must be in the future.')
    }
    const database = getDatabase()
    const now = new Date().toISOString()
    database.bookingRequests.push({
      id: makeId('booking-request'),
      organizationName: input.organizationName.trim(),
      contactName: input.contactName.trim(),
      email: input.email.trim().toLowerCase(),
      phone: input.phone.trim(),
      eventType: input.eventType.trim(),
      proposedDate: new Date(input.proposedDate).toISOString(),
      alternativeDates: input.alternativeDates?.trim() || undefined,
      flexibilityNote: input.flexibilityNote?.trim() || undefined,
      estimatedAttendance: input.estimatedAttendance,
      preferredOrganizer: input.preferredOrganizer?.trim() || undefined,
      requestedOrganizerId: input.requestedOrganizerId || null,
      requestedOrganizerName: database.users.find(user => user.id === input.requestedOrganizerId)?.name ?? null,
      description: input.description.trim(),
      status: 'submitted',
      assignedOrganizerId: null,
      assignedOrganizerName: null,
      organizerResponseNote: null,
      draftEventId: null,
      submittedAt: now,
      updatedAt: now,
    })
    saveDatabase(database)
    return 'Your organizer request has been received.'
  },

  async getOrganizers(search = '', category = '', page = 1, pageSize = 12): Promise<Page<OrganizerSummary>> {
    await pause()
    const query = search.trim().toLowerCase()
    const database = getDatabase()
    const items = database.users.filter(user => user.role !== 'admin' && user.active && user.directory?.isVisible && database.events.some(event => event.organizerId === user.id))
      .filter(user => (!query || user.name.toLowerCase().includes(query)) && (!category || user.directory?.specialties.includes(category as never)))
      .map(user => ({ id: user.id, name: user.name, imageUrl: user.imageUrl ?? null, bannerUrl: user.directory?.bannerUrl ?? null, bio: user.directory?.bio ?? null, specialties: user.directory?.specialties ?? [] }))
    return paginate(items, page, pageSize)
  },

  async getOrganizer(id: string): Promise<OrganizerDetail> {
    const page = await this.getOrganizers('', '', 1, 100)
    const organizer = page.items.find(item => item.id === id)
    if (!organizer) throw new Error('Organizer not found.')
    const user = getDatabase().users.find(item => item.id === id)!
    const database = getDatabase()
    return { ...organizer, instagramUrl: user.directory?.instagramUrl ?? null, twitterUrl: user.directory?.twitterUrl ?? null, facebookUrl: user.directory?.facebookUrl ?? null, websiteUrl: user.directory?.websiteUrl ?? null, events: database.events.filter(event => event.organizerId === id && (event.isPublished ?? true)).map(event => eventWithCount(database, event)) }
  },

  async getOrganizerDirectorySettings(): Promise<OrganizerDirectorySettings> {
    const user = getCurrentUser(getDatabase()) as DirectoryStoredUser
    return user.directory ?? { isVisible: false, bio: null, bannerUrl: null, instagramUrl: null, twitterUrl: null, facebookUrl: null, websiteUrl: null, specialties: [] }
  },

  async updateOrganizerDirectorySettings(settings: OrganizerDirectorySettings): Promise<OrganizerDirectorySettings> {
    const database = getDatabase(); const user = getCurrentUser(database) as DirectoryStoredUser
    user.directory = settings; saveDatabase(database); return settings
  },

  async getBookingRequests(page = 1, pageSize = 20): Promise<Page<BookingRequest>> {
    await pause()
    const database = getDatabase()
    const actor = getCurrentUser(database)
    if (actor.role !== 'admin') throw new Error('Admin access is required.')
    return paginate([...database.bookingRequests].sort((left, right) =>
      right.submittedAt.localeCompare(left.submittedAt)), page, pageSize)
  },

  async getAssignedBookingRequests(page = 1, pageSize = 20): Promise<Page<BookingRequest>> {
    await pause()
    const database = getDatabase()
    const organizer = getCurrentUser(database)
    if (organizer.role !== 'organizer') throw new Error('Organizer access is required.')
    return paginate(database.bookingRequests
      .filter((request) => request.assignedOrganizerId === organizer.id)
      .sort((left, right) => right.submittedAt.localeCompare(left.submittedAt)), page, pageSize)
  },

  async assignBookingRequest(id: string, organizerId: string): Promise<BookingRequest> {
    await pause()
    const database = getDatabase()
    const admin = getCurrentUser(database)
    if (admin.role !== 'admin') throw new Error('Admin access is required.')
    const request = database.bookingRequests.find((item) => item.id === id)
    if (!request) throw new Error('Booking request not found.')
    const organizer = database.users.find((user) =>
      user.id === organizerId && user.role !== 'admin' && user.active && database.events.some(event => event.organizerId === user.id))
    if (!organizer) throw new Error('Choose an active Organizer.')
    if (request.status !== 'sentToOrganizer') {
      ensureBookingTransition(request.status, 'sentToOrganizer')
    }
    const previousOrganizerId = request.assignedOrganizerId
    request.assignedOrganizerId = organizer.id
    request.assignedOrganizerName = organizer.name
    request.status = 'sentToOrganizer'
    request.updatedAt = new Date().toISOString()
    appendAdminAudit(
      database,
      admin,
      previousOrganizerId ? 'BookingRequestReassigned' : 'BookingRequestAssigned',
      'BookingRequest',
      request.id,
      { previousOrganizerId, newOrganizerId: organizer.id },
    )
    saveDatabase(database)
    return request
  },

  async updateBookingRequestStatus(
    id: string,
    status: Extract<BookingRequestStatus, 'underReview' | 'converted' | 'closed'>,
  ): Promise<BookingRequest> {
    await pause()
    const database = getDatabase()
    const admin = getCurrentUser(database)
    if (admin.role !== 'admin') throw new Error('Admin access is required.')
    const request = database.bookingRequests.find((item) => item.id === id)
    if (!request) throw new Error('Booking request not found.')
    const previousStatus = request.status
    ensureBookingTransition(previousStatus, status)
    request.status = status
    request.updatedAt = new Date().toISOString()
    appendAdminAudit(
      database,
      admin,
      'BookingRequestStatusChanged',
      'BookingRequest',
      request.id,
      { previousStatus, newStatus: status },
    )
    saveDatabase(database)
    return request
  },

  async respondToBookingRequest(
    id: string,
    accept: boolean,
    note?: string,
  ): Promise<BookingRequest> {
    await pause()
    const database = getDatabase()
    const organizer = getCurrentUser(database)
    if (organizer.role !== 'organizer') throw new Error('Organizer access is required.')
    const request = database.bookingRequests.find((item) => item.id === id)
    if (!request) throw new Error('Booking request not found.')
    if (request.assignedOrganizerId !== organizer.id) {
      throw new Error('Only the assigned Organizer can respond.')
    }
    const status = accept ? 'accepted' : 'declined'
    ensureBookingTransition(request.status, status)
    request.status = status
    request.organizerResponseNote = note?.trim() || null
    request.updatedAt = new Date().toISOString()
    if (accept) {
      const draft: StoredEvent = {
        id: makeId('event'),
        title: `${request.organizationName}: ${request.eventType}`.slice(0, 200),
        description: request.description,
        date: request.proposedDate,
        location: 'To be confirmed',
        capacity: request.estimatedAttendance,
        category: 'Cultural Events',
        organizerId: organizer.id,
        organizerName: organizer.name,
        createdAt: new Date().toISOString(),
        isPublished: false,
        version: 1,
      }
      database.events.push(draft)
      request.draftEventId = draft.id
    }
    saveDatabase(database)
    return request
  },
}
