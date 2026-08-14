import Breadcrumb from 'react-bootstrap/Breadcrumb'
import { Link, useLocation } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { getHomeForRole } from '../../utils/permissions'

const labels: Record<string, string> = {
  events: 'Events',
  voting: 'Voting',
  about: 'About',
  privacy: 'Privacy',
  'request-organizer': 'Request an Organizer',
  'thank-you': 'Thank you',
  student: 'Student',
  organizer: 'Organizer',
  admin: 'Admin',
  profile: 'Profile',
  registrations: 'Registrations',
  attendance: 'Attendance',
  registrants: 'Registrants',
  'booking-requests': 'Booking requests',
  applications: 'Applications',
  users: 'Users',
}

export default function Breadcrumbs() {
  const { pathname } = useLocation()
  const { user } = useAuth()
  const homePath = user ? getHomeForRole(user.role) : '/'
  if (pathname === '/') return null
  const segments = pathname.split('/').filter(Boolean)
  const crumbs = segments.map((segment, index) => {
    const isIdentifier = /^[0-9a-f-]{20,}$/i.test(segment) || segment.startsWith('event-')
    const label = isIdentifier ? 'Event' : labels[segment] ?? segment.replaceAll('-', ' ')
    return { label, to: `/${segments.slice(0, index + 1).join('/')}` }
  })

  return (
    <Breadcrumb className="app-breadcrumbs" aria-label="Breadcrumb">
      <Breadcrumb.Item linkAs={Link} linkProps={{ to: homePath }}>Home</Breadcrumb.Item>
      {crumbs.map((crumb, index) => (
        <Breadcrumb.Item
          key={crumb.to}
          active={index === crumbs.length - 1}
          linkAs={Link}
          linkProps={{ to: crumb.to }}
        >
          {crumb.label}
        </Breadcrumb.Item>
      ))}
    </Breadcrumb>
  )
}
