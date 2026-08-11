import { useLocation } from 'react-router-dom'
import LinkButton from '../shared/LinkButton'

export default function MobileStickyCta() {
  const { pathname } = useLocation()
  if (pathname.startsWith('/request-organizer') ||
      /^\/(admin|organizer|student|profile)/.test(pathname)) return null
  return (
    <div className="mobile-sticky-cta d-md-none">
      <LinkButton to="/request-organizer" className="w-100" size="lg">
        Request an Organizer
      </LinkButton>
    </div>
  )
}
