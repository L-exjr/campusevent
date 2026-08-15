import Container from 'react-bootstrap/Container'
import { Outlet } from 'react-router-dom'
import AppNavbar from './AppNavbar'
import Breadcrumbs from './Breadcrumbs'
import MobileStickyCta from './MobileStickyCta'
import LinkButton from '../shared/LinkButton'
import { useAuth } from '../../hooks/useAuth'

export default function AppLayout() {
  const { user } = useAuth()
  return (
    <div className="app-shell">
      <AppNavbar />
      <main className="app-main">
        <Container>
          <Breadcrumbs />
          <Outlet />
        </Container>
      </main>
      <footer className="app-footer">
        <Container className="d-flex flex-column flex-lg-row justify-content-between gap-3">
          <div><strong>Campus Events</strong><span className="text-secondary ms-2">Plan well. Show up. Belong.</span></div>
          <nav className="d-flex flex-wrap gap-3" aria-label="Footer navigation">
            <LinkButton to="/events" variant="link" className="p-0">Events</LinkButton>
            {user?.role !== 'admin' && (
              <LinkButton to="/request-organizer" variant="link" className="p-0">Request an Organizer</LinkButton>
            )}
            <LinkButton to="/about" variant="link" className="p-0">About</LinkButton>
            <LinkButton to="/privacy" variant="link" className="p-0">Privacy</LinkButton>
          </nav>
        </Container>
      </footer>
      <MobileStickyCta />
    </div>
  )
}
