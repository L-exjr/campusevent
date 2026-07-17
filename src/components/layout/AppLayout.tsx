import Container from 'react-bootstrap/Container'
import { Outlet } from 'react-router-dom'
import AppNavbar from './AppNavbar'

export default function AppLayout() {
  return (
    <div className="app-shell">
      <AppNavbar />
      <main className="app-main">
        <Container>
          <Outlet />
        </Container>
      </main>
      <footer className="app-footer">
        <Container className="d-flex flex-column flex-sm-row justify-content-between gap-2">
          <span>Campus Events</span>
          <span className="text-secondary">Plan well. Show up. Belong.</span>
        </Container>
      </footer>
    </div>
  )
}
