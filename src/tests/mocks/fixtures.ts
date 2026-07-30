import type { EventItem, User } from '../../types'

export const users: Record<User['role'], User> = {
  student: {
    id: 'student-1',
    name: 'Sam Student',
    email: 'sam@example.test',
    role: 'student',
    active: true,
    joinedAt: '2026-01-01T12:00:00Z',
  },
  organizer: {
    id: 'organizer-1',
    name: 'Olivia Organizer',
    email: 'olivia@example.test',
    role: 'organizer',
    active: true,
    joinedAt: '2026-01-01T12:00:00Z',
  },
  admin: {
    id: 'admin-1',
    name: 'Ada Admin',
    email: 'ada@example.test',
    role: 'admin',
    active: true,
    joinedAt: '2026-01-01T12:00:00Z',
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
  organizerId: users.organizer.id,
  organizerName: users.organizer.name,
  createdAt: '2026-07-01T12:00:00Z',
  registeredCount: 1,
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
    organizerId: item.organizerId,
    organizerName: item.organizerName,
    createdAt: item.createdAt,
    registrationCount: item.registeredCount,
  }
}

export function paginated<T>(items: T[]) {
  return { items, page: 1, pageSize: 100, totalCount: items.length, totalPages: 1 }
}
