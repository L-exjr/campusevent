import type { EventItem, User } from '../../types'

export const users: Record<User['role'], User> = {
  student: {
    id: 'student-1',
    name: 'Sam Student',
    email: 'sam@example.test',
    role: 'student',
    active: true,
    joinedAt: '2026-01-01T12:00:00Z',
    imageUrl: null,
  },
  organizer: {
    id: 'organizer-1',
    name: 'Olivia Organizer',
    email: 'olivia@example.test',
    role: 'organizer',
    active: true,
    joinedAt: '2026-01-01T12:00:00Z',
    imageUrl: null,
  },
  admin: {
    id: 'admin-1',
    name: 'Ada Admin',
    email: 'ada@example.test',
    role: 'admin',
    active: true,
    joinedAt: '2026-01-01T12:00:00Z',
    imageUrl: null,
  },
}

export const event: EventItem = {
  id: 'event-1',
  title: 'Campus Testing Workshop',
  description: 'A practical workshop about reliable software testing.',
  date: '2030-08-15T14:00:00Z',
  capacity: 30,
  category: 'Technology',
  location: 'Engineering Hall',
  format: 'physical',
  meetingUrl: null,
  salesStartsAt: null,
  salesEndsAt: null,
  organizerId: users.organizer.id,
  organizerName: users.organizer.name,
  createdAt: '2026-07-01T12:00:00Z',
  registeredCount: 1,
  imageUrl: null,
  isPublished: true,
  version: 1,
  priceMinor: 0,
  currency: 'GHS',
}

export function apiEvent(overrides: Partial<EventItem> = {}) {
  const item = { ...event, ...overrides }
  return {
    id: item.id,
    title: item.title,
    description: item.description,
    date: item.date,
    capacity: item.capacity,
    category: item.category,
    location: item.location,
    format: item.format === 'physical'
      ? 'Physical'
      : item.format === 'virtual'
        ? 'Virtual'
        : 'Hybrid',
    meetingUrl: item.meetingUrl,
    salesStartsAt: item.salesStartsAt,
    salesEndsAt: item.salesEndsAt,
    organizerId: item.organizerId,
    organizerName: item.organizerName,
    createdAt: item.createdAt,
    registrationCount: item.registeredCount,
    imageUrl: item.imageUrl,
    isPublished: item.isPublished,
    version: item.version,
  }
}

export function paginated<T>(items: T[]) {
  return { items, page: 1, pageSize: 100, totalCount: items.length, totalPages: 1 }
}
