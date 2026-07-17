import Container from 'react-bootstrap/Container'
import LinkButton from '../../components/shared/LinkButton'
import { useAuth } from '../../hooks/useAuth'
import { getHomeForRole } from '../../utils/permissions'

export default function UnauthorizedPage() {
  const { user } = useAuth()
  const destination = user ? getHomeForRole(user.role) : '/login'
  return (
    <main className="status-page">
      <Container className="text-center">
        <div className="status-code">403</div>
        <p className="eyebrow">Access restricted</p>
        <h1>This area belongs to another role.</h1>
        <p className="text-secondary mx-auto">
          Your account is active, but it doesn’t have permission to open this page.
        </p>
        <LinkButton to={destination} size="lg">
          Return to dashboard
        </LinkButton>
      </Container>
    </main>
  )
}
