import Container from 'react-bootstrap/Container'
import LinkButton from '../../components/shared/LinkButton'
import { useAuth } from '../../hooks/useAuth'
import { getHomeForRole } from '../../utils/permissions'

export default function NotFoundPage() {
  const { user } = useAuth()
  const destination = user ? getHomeForRole(user.role) : '/login'
  return (
    <main className="status-page">
      <Container className="text-center">
        <div className="status-code">404</div>
        <p className="eyebrow">Page not found</p>
        <h1>Looks like this event moved on.</h1>
        <p className="text-secondary mx-auto">
          The page may have been removed, renamed, or never existed in the first place.
        </p>
        <LinkButton to={destination} size="lg">
          Go to dashboard
        </LinkButton>
      </Container>
    </main>
  )
}
