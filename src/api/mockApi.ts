import type {
  AuthSession,
  EventFilters,
  EventInput,
  EventItem,
  EventRegistrant,
  OrganizerApplication,
  Registration,
  ReportsData,
  Role,
  StudentRegistration,
  User,
} from '../types'
import { EVENT_CATEGORIES } from '../types'
import type { EventManagementApi } from './EventManagementApi'

type StoredEvent = Omit<EventItem, 'registeredCount' | 'imageUrl'> & { imageUrl?: string | null }
type StoredUser = Omit<User, 'imageUrl'> & { imageUrl?: string | null; password: string }

interface MockDatabase {
  users: StoredUser[]
  events: StoredEvent[]
  registrations: Registration[]
  organizerApplications: OrganizerApplication[]
}

const DB_KEY = 'campus_events_mock_db'
const SESSION_KEY = 'campus_events_session'
const LEGACY_EMAIL_SUFFIX = '@campus.edu'
const EMAIL_SUFFIX = '@cevents.com'

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
  const users: StoredUser[] = [
    {
      id: 'user-student-1',
      name: 'Maya Johnson',
      email: 'student@cevents.com',
      password: 'demo123',
      role: 'student',
      active: true,
      joinedAt,
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
      category: 'Technology',
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
      category: 'Career',
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
      category: 'Culture',
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
      category: 'Academic',
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
      category: 'Wellness',
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
      category: 'Sports',
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

  return { users, events, registrations, organizerApplications }
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
    database.organizerApplications ??= []
    database.users.forEach((user) => { user.imageUrl ??= null })
    database.events.forEach((event) => { event.imageUrl ??= null })

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

function normalizeEventInput(input: EventInput, requireFutureDate: boolean): EventInput {
  const title = input.title.trim()
  const description = input.description.trim()
  const location = input.location.trim()
  const date = new Date(input.date)
  const category = EVENT_CATEGORIES.find(
    (item) => item.toLowerCase() === input.category.trim().toLowerCase(),
  )

  if (title.length < 3) throw new Error('Event titles must contain at least 3 characters.')
  if (description.length < 10) throw new Error('Event descriptions must contain at least 10 characters.')
  if (!location) throw new Error('An event location is required.')
  if (!Number.isFinite(date.getTime())) throw new Error('Enter a valid event date and time.')
  if (requireFutureDate && date.getTime() <= Date.now()) {
    throw new Error('New events must be scheduled in the future.')
  }
  if (!Number.isInteger(input.capacity) || input.capacity < 1 || input.capacity > 100000) {
    throw new Error('Event capacity must be between 1 and 100000.')
  }
  if (!category) throw new Error('Choose a supported event category.')

  return {
    title,
    description,
    date: date.toISOString(),
    capacity: input.capacity,
    category,
    location,
    imageUrl: input.imageUrl ?? null,
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
  async getEvents(filters: EventFilters = {}): Promise<EventItem[]> {
    await pause()
    const database = getDatabase()
    const query = filters.search?.trim().toLowerCase()
    const selectedDay = filters.date
      ? {
          start: new Date(`${filters.date}T00:00:00`).getTime(),
          end: new Date(`${filters.date}T23:59:59.999`).getTime(),
        }
      : null
    return database.events
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
      .map((event) => eventWithCount(database, event))
  },

  // TODO: replace with GET /api/events/{id}.
  async getEvent(id: string): Promise<EventItem> {
    await pause()
    const database = getDatabase()
    const event = database.events.find((item) => item.id === id)
    if (!event) throw new Error('This event could not be found.')
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

  // TODO: replace with GET /api/registrations/me.
  async getStudentRegistrations(studentId: string): Promise<StudentRegistration[]> {
    await pause()
    const database = getDatabase()
    return database.registrations
      .filter((item) => item.studentId === studentId)
      .map((registration) => {
        const event = database.events.find((item) => item.id === registration.eventId)
        if (!event) return null
        return { registration, event: eventWithCount(database, event) }
      })
      .filter((item): item is StudentRegistration => item !== null)
      .sort((left, right) => left.event.date.localeCompare(right.event.date))
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

  async getPendingOrganizerApplications(): Promise<OrganizerApplication[]> {
    await pause()
    const database = getDatabase()
    const user = getCurrentUser(database)
    if (user.role !== 'admin') throw new Error('Admin access is required.')
    return database.organizerApplications
      .filter((application) => application.status === 'pending')
      .sort((left, right) => right.submittedAt.localeCompare(left.submittedAt))
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
    saveDatabase(database)
    return application
  },

  // TODO: replace with organizer-scoped GET /api/events/mine.
  async getOrganizerEvents(organizerId: string): Promise<EventItem[]> {
    await pause()
    const database = getDatabase()
    return database.events
      .filter((event) => event.organizerId === organizerId)
      .sort((left, right) => left.date.localeCompare(right.date))
      .map((event) => eventWithCount(database, event))
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
    }
    database.events.push(event)
    saveDatabase(database)
    return eventWithCount(database, event)
  },

  // TODO: replace with PUT /api/events/{id}.
  async updateEvent(id: string, input: EventInput): Promise<EventItem> {
    await pause()
    const database = getDatabase()
    const event = database.events.find((item) => item.id === id)
    if (!event) throw new Error('This event could not be found.')
    Object.assign(event, normalizeEventInput(input, false))
    saveDatabase(database)
    return eventWithCount(database, event)
  },

  // TODO: replace with DELETE /api/events/{id}.
  async deleteEvent(id: string) {
    await pause()
    const database = getDatabase()
    database.events = database.events.filter((event) => event.id !== id)
    database.registrations = database.registrations.filter(
      (registration) => registration.eventId !== id,
    )
    saveDatabase(database)
  },

  // TODO: replace with GET /api/events/{id}/registrants.
  async getEventRegistrants(eventId: string): Promise<EventRegistrant[]> {
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
    return database.registrations
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
      .sort((left, right) => left.name.localeCompare(right.name))
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

  // TODO: replace with GET /api/admin/users.
  async getUsers(): Promise<User[]> {
    await pause()
    return getDatabase().users.map(publicUser)
  },

  // TODO: replace with PATCH /api/admin/users/{id}/role.
  async updateUserRole(id: string, role: Exclude<Role, 'admin'>) {
    await pause()
    const database = getDatabase()
    const user = database.users.find((item) => item.id === id)
    if (!user) throw new Error('User account could not be found.')
    if (user.role === 'admin') throw new Error('Admin roles cannot be changed here.')
    user.role = role
    if (role === 'organizer') {
      const admin = getCurrentUser(database)
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
    saveDatabase(database)
  },

  // TODO: replace with PATCH /api/admin/users/{id}/status.
  async updateUserStatus(id: string, active: boolean) {
    await pause()
    const database = getDatabase()
    const user = database.users.find((item) => item.id === id)
    if (!user) throw new Error('User account could not be found.')
    if (user.role === 'admin') throw new Error('Admin accounts cannot be deactivated here.')
    user.active = active
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
  async getAllEvents(): Promise<EventItem[]> {
    await pause()
    const database = getDatabase()
    return database.events
      .sort((left, right) => left.date.localeCompare(right.date))
      .map((event) => eventWithCount(database, event))
  },

  // TODO: replace with GET /api/admin/reports/summary.
  async getReports(): Promise<ReportsData> {
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
      .filter((user) => user.role === 'organizer')
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
      events,
      organizers,
    }
  },
}
