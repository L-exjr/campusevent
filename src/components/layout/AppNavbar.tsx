import { useState } from 'react'
import Badge from 'react-bootstrap/Badge'
import Container from 'react-bootstrap/Container'
import Dropdown from 'react-bootstrap/Dropdown'
import Nav from 'react-bootstrap/Nav'
import Navbar from 'react-bootstrap/Navbar'
import { NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { getInitials } from '../../utils/formatters'
import { getNavigationGroupsForRole, ROLE_LABELS } from '../../utils/permissions'
import LinkButton from '../shared/LinkButton'
import AccessibleTooltip from '../shared/AccessibleTooltip'

export default function AppNavbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [expanded, setExpanded] = useState(false)
  const [failedImageUrl, setFailedImageUrl] = useState<string | null>(null)
  const navigation = user ? getNavigationGroupsForRole(user.role) : {
    primary: [{ label: 'Explore events', to: '/events' }, { label: 'Request an Organizer', to: '/organizers' }],
    secondary: [],
  }

  const handleLogout = async () => {
    setExpanded(false)
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <Navbar
      expand="lg"
      className="app-navbar"
      sticky="top"
      expanded={expanded}
      onToggle={setExpanded}
    >
      <Container>
        <Navbar.Brand as={NavLink} to="/" className="d-flex align-items-center gap-2">
          <span className="brand-mark" aria-hidden="true">
            C
          </span>
          <span>Campus Events</span>
        </Navbar.Brand>
        <AccessibleTooltip label={expanded ? 'Close navigation' : 'Open navigation'}>
          <Navbar.Toggle aria-controls="main-navigation" aria-expanded={expanded} label={expanded ? 'Close navigation' : 'Open navigation'} />
        </AccessibleTooltip>
        <Navbar.Collapse id="main-navigation">
          <Nav className="mx-auto gap-lg-2" onSelect={() => setExpanded(false)}>
            {navigation.primary.map((item) => (
              <Nav.Link
                key={item.to}
                as={NavLink}
                to={item.to}
                end={item.to.split('/').length === 2}
                onClick={() => setExpanded(false)}
              >
                {item.label}
              </Nav.Link>
            ))}
          </Nav>
          {user && (
            <div className="navbar-user d-flex align-items-center gap-3 mt-3 mt-lg-0">
              <Dropdown align="end" className="navbar-account-menu">
                <Dropdown.Toggle variant="link" className="navbar-account-toggle d-flex align-items-center gap-2">
              {user.imageUrl && failedImageUrl !== user.imageUrl ? (
                <img
                  src={user.imageUrl}
                  alt={`${user.name} profile`}
                  className="avatar object-fit-cover"
                  onError={() => setFailedImageUrl(user.imageUrl)}
                />
              ) : (
                <div
                  className="avatar"
                  aria-label={`${user.name} profile placeholder`}
                >
                  <span aria-hidden="true">{getInitials(user.name)}</span>
                </div>
              )}
              <div className="lh-sm me-auto">
                <div className="fw-semibold small">{user.name}</div>
                <Badge bg="light" text="dark" className="role-badge mt-1">
                  {ROLE_LABELS[user.role]}
                </Badge>
              </div>
                </Dropdown.Toggle>
                <Dropdown.Menu>
                  {navigation.secondary.map((item) => (
                    <Dropdown.Item key={item.to} as={NavLink} to={item.to} onClick={() => setExpanded(false)}>
                      {item.label}
                    </Dropdown.Item>
                  ))}
                  <Dropdown.Divider />
                  <Dropdown.Item onClick={() => { setExpanded(false); void handleLogout() }}>Log out</Dropdown.Item>
                </Dropdown.Menu>
              </Dropdown>
            </div>
          )}
          {!user && (
            <div className="d-flex align-items-center gap-2 mt-3 mt-lg-0">
              <LinkButton to="/login" variant="light" onNavigate={() => setExpanded(false)}>
                Sign in
              </LinkButton>
              <LinkButton to="/register" onNavigate={() => setExpanded(false)}>
                Create account
              </LinkButton>
            </div>
          )}
        </Navbar.Collapse>
      </Container>
    </Navbar>
  )
}
