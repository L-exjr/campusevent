export type Role = 'student' | 'organizer' | 'admin'
export type OrganizerApplicationStatus = 'pending' | 'approved' | 'rejected'

export type EventCategory =
  | 'Academic'
  | 'Career'
  | 'Culture'
  | 'Sports'
  | 'Technology'
  | 'Wellness'

export interface User {
  id: string
  name: string
  email: string
  role: Role
  active: boolean
  joinedAt: string
}

export interface EventItem {
  id: string
  title: string
  description: string
  date: string
  capacity: number
  category: EventCategory
  location: string
  organizerId: string
  organizerName: string
  createdAt: string
  registeredCount: number
}

export interface EventInput {
  title: string
  description: string
  date: string
  capacity: number
  category: EventCategory
  location: string
}

export interface EventFilters {
  search?: string
  category?: string
  date?: string
}

export interface Registration {
  id: string
  eventId: string
  studentId: string
  registeredAt: string
  attended: boolean
}

export interface StudentRegistration {
  registration: Registration
  event: EventItem
}

export interface EventRegistrant {
  registrationId: string
  userId: string
  name: string
  email: string
  registeredAt: string
  attended: boolean
}

export interface AuthSession {
  token: string
  expiresAt: string
  user: User
}

export interface OrganizerApplication {
  id: string
  userId: string
  userName: string
  userEmail: string
  reason: string
  status: OrganizerApplicationStatus
  rejectionReason: string | null
  submittedAt: string
  reviewedAt: string | null
  reviewedByAdminId: string | null
  reviewedByAdminName: string | null
}

export interface EventReport {
  eventId: string
  title: string
  organizerName: string
  registrations: number
  attended: number
  attendanceRate: number
}

export interface OrganizerReport {
  organizerId: string
  organizerName: string
  events: number
  registrations: number
}

export interface ReportsData {
  totalEvents: number
  totalRegistrations: number
  totalUsers: number
  attendanceRate: number
  events: EventReport[]
  organizers: OrganizerReport[]
}

export const EVENT_CATEGORIES: EventCategory[] = [
  'Academic',
  'Career',
  'Culture',
  'Sports',
  'Technology',
  'Wellness',
]
