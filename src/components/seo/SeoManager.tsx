import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'

interface SeoDefinition {
  title: string
  description: string
  noIndex?: boolean
}

const defaultSeo: SeoDefinition = {
  title: 'Campus Events | Discover what is happening',
  description: 'Discover, book, and manage memorable campus events in one connected place.',
}

const routes: Array<[RegExp, SeoDefinition]> = [
  [/^\/$/, defaultSeo],
  [/^\/events$/, { title: 'Explore Events | Campus Events', description: 'Browse upcoming campus events, compare details, and reserve your place.' }],
  [/^\/events\/[^/]+\/voting$/, { title: 'Event Voting | Campus Events', description: 'Vote for event nominees and view verified results when voting closes.' }],
  [/^\/events\/[^/]+$/, { title: 'Event Details | Campus Events', description: 'Review event details, availability, directions, pricing, and registration options.' }],
  [/^\/about$/, { title: 'About Campus Events', description: 'Learn how Campus Events connects students, organizers, and administrators.' }],
  [/^\/request-organizer\/thank-you$/, { title: 'Request Received | Campus Events', description: 'Your event organizer request has been received for review.', noIndex: true }],
  [/^\/request-organizer$/, { title: 'Request an Event Organizer | Campus Events', description: 'Tell us about your event and request support from a campus organizer.' }],
  [/^\/privacy$/, { title: 'Privacy Policy | Campus Events', description: 'Learn how Campus Events handles account, booking, attendance, voting, and payment data.' }],
  [/^\/login$/, { title: 'Sign In | Campus Events', description: 'Sign in securely to manage your Campus Events account.', noIndex: true }],
  [/^\/register$/, { title: 'Create an Account | Campus Events', description: 'Create a Student account to register and vote at campus events.', noIndex: true }],
  [/^\/forgot-password$/, { title: 'Forgot Password | Campus Events', description: 'Request a secure Campus Events password reset link.', noIndex: true }],
  [/^\/reset-password$/, { title: 'Reset Password | Campus Events', description: 'Choose a new password for your Campus Events account.', noIndex: true }],
  [/^\/student$/, { title: 'Student Dashboard | Campus Events', description: 'See upcoming events and your Student account activity.', noIndex: true }],
  [/^\/student\/registrations$/, { title: 'My Registrations | Campus Events', description: 'Access your event registrations, QR tickets, and attendance certificates.', noIndex: true }],
  [/^\/student\/organizer-application$/, { title: 'Organizer Application | Campus Events', description: 'Apply for organizer access and review your application status.', noIndex: true }],
  [/^\/organizer$/, { title: 'Organizer Dashboard | Campus Events', description: 'Review your event portfolio and organizer activity.', noIndex: true }],
  [/^\/organizer\/events$/, { title: 'Manage Events | Campus Events', description: 'Create and maintain your event listings.', noIndex: true }],
  [/^\/organizer\/booking-requests$/, { title: 'Assigned Booking Requests | Campus Events', description: 'Review and respond to assigned organizer requests.', noIndex: true }],
  [/^\/organizer\/events\/[^/]+\/registrants$/, { title: 'Event Registrants | Campus Events', description: 'Review registered attendees for this event.', noIndex: true }],
  [/^\/organizer\/events\/[^/]+\/attendance$/, { title: 'Event Attendance | Campus Events', description: 'Scan signed tickets and manage confirmed attendance.', noIndex: true }],
  [/^\/organizer\/events\/[^/]+\/voting$/, { title: 'Manage Event Voting | Campus Events', description: 'Configure categories, nominees, pricing, dates, and voting visibility.', noIndex: true }],
  [/^\/admin$/, { title: 'Reports Dashboard | Campus Events', description: 'Review platform activity, registrations, and attendance reporting.', noIndex: true }],
  [/^\/admin\/users$/, { title: 'User Administration | Campus Events', description: 'Manage user access and account status.', noIndex: true }],
  [/^\/admin\/organizer-applications$/, { title: 'Organizer Applications | Campus Events', description: 'Review pending organizer access requests.', noIndex: true }],
  [/^\/admin\/events$/, { title: 'Event Administration | Campus Events', description: 'Oversee all event records and ownership.', noIndex: true }],
  [/^\/admin\/booking-requests$/, { title: 'Booking Request Queue | Campus Events', description: 'Review, assign, and track public organizer requests.', noIndex: true }],
  [/^\/admin\/email-outbox$/, { title: 'Failed Email Delivery | Campus Events', description: 'Review and retry failed email deliveries.', noIndex: true }],
  [/^\/admin\/image-cleanup$/, { title: 'Failed Image Cleanup | Campus Events', description: 'Review and retry failed storage cleanup operations.', noIndex: true }],
  [/^\/admin\/audit-logs$/, { title: 'Administrative Audit Log | Campus Events', description: 'Review immutable administrative activity records.', noIndex: true }],
  [/^\/admin\/events\/[^/]+\/registrants$/, { title: 'Event Registrants Administration | Campus Events', description: 'Review event attendees as an administrator.', noIndex: true }],
  [/^\/admin\/events\/[^/]+\/voting$/, { title: 'Voting Administration | Campus Events', description: 'Review or configure an event voting campaign.', noIndex: true }],
  [/^\/student/, { title: 'Student Workspace | Campus Events', description: 'Manage your registrations, tickets, and certificates.', noIndex: true }],
  [/^\/organizer/, { title: 'Organizer Workspace | Campus Events', description: 'Manage events, voting, registrations, and attendance.', noIndex: true }],
  [/^\/admin/, { title: 'Administration | Campus Events', description: 'Campus Events administrative operations and reporting.', noIndex: true }],
  [/^\/profile$/, { title: 'Your Profile | Campus Events', description: 'Manage your Campus Events profile.', noIndex: true }],
  [/^\/payment\//, { title: 'Payment Status | Campus Events', description: 'Check the server-verified status of your event payment.', noIndex: true }],
  [/^\/voting\/payment\//, { title: 'Voting Payment Status | Campus Events', description: 'Check the server-verified status of your voting payment.', noIndex: true }],
]

function setMeta(selector: string, attribute: 'name' | 'property', key: string, content: string) {
  let element = document.head.querySelector<HTMLMetaElement>(selector)
  if (!element) {
    element = document.createElement('meta')
    element.setAttribute(attribute, key)
    document.head.append(element)
  }
  element.content = content
}

export default function SeoManager() {
  const location = useLocation()

  useEffect(() => {
    const seo = routes.find(([pattern]) => pattern.test(location.pathname))?.[1] ?? {
      title: 'Page Not Found | Campus Events',
      description: 'The requested Campus Events page could not be found.',
      noIndex: true,
    }
    const configuredOrigin = import.meta.env.VITE_PUBLIC_SITE_URL?.trim().replace(/\/$/, '')
    const origin = configuredOrigin || window.location.origin
    const canonicalUrl = `${origin}${location.pathname}`
    const socialImage = `${origin}/og-campus-events.png`

    document.title = seo.title
    setMeta('meta[name="description"]', 'name', 'description', seo.description)
    setMeta('meta[name="robots"]', 'name', 'robots', seo.noIndex ? 'noindex,nofollow' : 'index,follow')
    setMeta('meta[property="og:title"]', 'property', 'og:title', seo.title)
    setMeta('meta[property="og:description"]', 'property', 'og:description', seo.description)
    setMeta('meta[property="og:url"]', 'property', 'og:url', canonicalUrl)
    setMeta('meta[property="og:image"]', 'property', 'og:image', socialImage)
    setMeta('meta[name="twitter:title"]', 'name', 'twitter:title', seo.title)
    setMeta('meta[name="twitter:description"]', 'name', 'twitter:description', seo.description)
    setMeta('meta[name="twitter:image"]', 'name', 'twitter:image', socialImage)

    let canonical = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]')
    if (!canonical) {
      canonical = document.createElement('link')
      canonical.rel = 'canonical'
      document.head.append(canonical)
    }
    canonical.href = canonicalUrl
  }, [location.pathname])

  return null
}
