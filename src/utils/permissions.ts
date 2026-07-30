import type { EventItem, Role, User } from '../types'

export type Permission =
  | 'events:browse'
  | 'registrations:own'
  | 'events:own:manage'
  | 'attendance:manage'
  | 'users:manage'
  | 'organizer-applications:view'
  | 'events:all:manage'
  | 'reports:view'

const ROLE_PERMISSIONS: Record<Role, Permission[]> = {
  student: ['events:browse', 'registrations:own'],
  organizer: ['events:own:manage', 'attendance:manage'],
  admin: ['users:manage', 'organizer-applications:view', 'events:all:manage', 'reports:view'],
}

export const ROLE_HOME: Record<Role, string> = {
  student: '/student',
  organizer: '/organizer',
  admin: '/admin',
}

export const ROLE_LABELS: Record<Role, string> = {
  student: 'Student',
  organizer: 'Organizer',
  admin: 'Admin',
}

export const ROUTE_ROLES: Record<Role | 'authenticated', Role[]> = {
  student: ['student'],
  organizer: ['organizer'],
  admin: ['admin'],
  authenticated: ['student', 'organizer', 'admin'],
}

export interface NavigationItem {
  label: string
  to: string
}

const ROLE_NAVIGATION: Record<Role, NavigationItem[]> = {
  student: [
    { label: 'Overview', to: '/student' },
    { label: 'My registrations', to: '/student/registrations' },
    { label: 'Apply to organize', to: '/student/organizer-application' },
  ],
  organizer: [
    { label: 'Overview', to: '/organizer' },
    { label: 'Manage events', to: '/organizer/events' },
  ],
  admin: [
    { label: 'Reports', to: '/admin' },
    { label: 'Users', to: '/admin/users' },
    { label: 'Applications', to: '/admin/organizer-applications' },
    { label: 'All events', to: '/admin/events' },
    { label: 'Booking requests', to: '/admin/booking-requests' },
  ],
}

export function hasPermission(role: Role, permission: Permission) {
  return ROLE_PERMISSIONS[role].includes(permission)
}

export function canAccessRole(role: Role, allowedRoles: Role[]) {
  return allowedRoles.includes(role)
}

export function getHomeForRole(role: Role) {
  return ROLE_HOME[role]
}

export function getNavigationForRole(role: Role) {
  return [
    { label: 'Explore events', to: '/events' },
    { label: 'Request an Organizer', to: '/request-organizer' },
    ...ROLE_NAVIGATION[role],
    { label: 'Profile', to: '/profile' },
  ]
}

export function canManageUserAccount(role: Role) {
  return role !== 'admin'
}

export function canManageEvent(user: User, event: EventItem) {
  return user.role === 'admin' || (user.role === 'organizer' && event.organizerId === user.id)
}
