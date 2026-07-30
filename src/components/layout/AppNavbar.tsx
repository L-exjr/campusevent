import Badge from 'react-bootstrap/Badge'
import Container from 'react-bootstrap/Container'
import Nav from 'react-bootstrap/Nav'
import Navbar from 'react-bootstrap/Navbar'
import { NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { getInitials } from '../../utils/formatters'
import { getNavigationForRole, ROLE_LABELS } from '../../utils/permissions'
import LinkButton from '../shared/LinkButton'
import { DEFAULT_PROFILE_IMAGE } from '../../api/supabaseStorage'

export default function AppNavbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const navigation = user
    ? getNavigationForRole(user.role)
    : [{ label: 'Explore events', to: '/events' }]

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <Navbar expand="lg" className="app-navbar" sticky="top">
      <Container>
        <Navbar.Brand as={NavLink} to="/events" className="d-flex align-items-center gap-2">
          <span className="brand-mark" aria-hidden="true">
            C
          </span>
          <span>Campus Events</span>
        </Navbar.Brand>
        <Navbar.Toggle aria-controls="main-navigation" />
        <Navbar.Collapse id="main-navigation">
          <Nav className="mx-auto gap-lg-2">
            {navigation.map((item) => (
              <Nav.Link key={item.to} as={NavLink} to={item.to} end={item.to.split('/').length === 2}>
                {item.label}
              </Nav.Link>
            ))}
          </Nav>
          {user && (
            <div className="navbar-user d-flex align-items-center gap-3 mt-3 mt-lg-0">
              {user.imageUrl ? (
                <img
                  src={user.imageUrl}
                  alt={`${user.name} profile`}
                  className="avatar object-fit-cover"
                />
              ) : (
                <div
                  className="avatar"
                  aria-label={`${user.name} profile placeholder`}
                  style={{ backgroundImage: `url(${DEFAULT_PROFILE_IMAGE})`, backgroundSize: 'cover' }}
                >
                  <span className="visually-hidden">{getInitials(user.name)}</span>
                </div>
              )}
              <div className="lh-sm me-auto">
                <div className="fw-semibold small">{user.name}</div>
                <Badge bg="light" text="dark" className="role-badge mt-1">
                  {ROLE_LABELS[user.role]}
                </Badge>
              </div>
              <Nav.Link onClick={() => void handleLogout()} className="logout-link">
                Log out
              </Nav.Link>
            </div>
          )}
          {!user && (
            <div className="d-flex align-items-center gap-2 mt-3 mt-lg-0">
              <LinkButton to="/login" variant="light">
                Sign in
              </LinkButton>
              <LinkButton to="/register">
                Create account
              </LinkButton>
            </div>
          )}
        </Navbar.Collapse>
      </Container>
    </Navbar>
  )
}
