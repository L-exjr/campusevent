import { useState, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Container from 'react-bootstrap/Container'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { getHomeForRole } from '../../utils/permissions'
import { usingMockApi } from '../../api'
import PasswordInput from '../../components/auth/PasswordInput'
import GoogleSignInButton from '../../components/auth/GoogleSignInButton'

const DEMO_ACCOUNTS = [
  { role: 'Student', email: 'student@cevents.com' },
  { role: 'Organizer', email: 'organizer@cevents.com' },
  { role: 'Admin', email: 'admin@cevents.com' },
]

export default function LoginPage() {
  const { login, googleLogin } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const returnPath = (location.state as { from?: unknown } | null)?.from
  const [email, setEmail] = useState(usingMockApi ? 'student@cevents.com' : '')
  const [password, setPassword] = useState(usingMockApi ? 'demo123' : '')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const session = await login(email, password)
      navigate(
        session.user.role === 'student' &&
          typeof returnPath === 'string' &&
          returnPath.startsWith('/events/')
          ? returnPath
          : getHomeForRole(session.user.role),
        { replace: true },
      )
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Unable to sign in.')
    } finally {
      setBusy(false)
    }
  }

  const selectDemoAccount = (accountEmail: string) => {
    setEmail(accountEmail)
    setPassword('demo123')
    setError(null)
  }

  const handleGoogleCredential = async (credential: string) => {
    setBusy(true); setError(null)
    try {
      const session = await googleLogin(credential)
      navigate(getHomeForRole(session.user.role), { replace: true })
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Google sign-in failed.')
    } finally { setBusy(false) }
  }

  return (
    <main className="auth-page">
      <Container>
        <Row className="g-4 align-items-stretch auth-layout">
          <Col lg={6}>
            <section className="auth-hero h-100">
              <Link to="/" className="auth-brand">
                <span className="brand-mark">C</span>
                Campus Events
              </Link>
              <div className="my-auto py-5">
                <Badge bg="light" text="dark" className="auth-kicker mb-4">
                  Your campus, in one place
                </Badge>
                <h1>Discover what’s happening. Make your place in it.</h1>
                <p>
                  Browse events, manage attendance, and keep your community moving—from one clear workspace.
                </p>
              </div>
              <div className="auth-quote">
                <div>
                  <strong>Everything you need to show up prepared.</strong>
                  <p className="mb-0">One calendar. Clear roles. Better events.</p>
                </div>
              </div>
            </section>
          </Col>
          <Col lg={6}>
            <Card className="auth-card border-0 h-100">
              <Card.Body className="p-4 p-md-5 d-flex flex-column justify-content-center">
                <p className="eyebrow mb-2">Welcome back</p>
                <h2 className="h1 mb-2">Sign in to continue</h2>
                <p className="text-secondary mb-4">
                  {usingMockApi ? 'Use a demo account or your registered student account.' : 'Use your Campus Events account.'}
                </p>
                {error && <Alert variant="danger">{error}</Alert>}
                <Form onSubmit={(event) => void handleSubmit(event)}>
                  <Form.Group className="mb-3" controlId="login-email">
                    <Form.Label>Email address</Form.Label>
                    <Form.Control
                      type="email"
                      required
                      autoComplete="email"
                      value={email}
                      onChange={(event) => setEmail(event.target.value)}
                    />
                  </Form.Group>
                  <Form.Group className="mb-4">
                    <div className="d-flex justify-content-between"><Form.Label htmlFor="login-password">Password</Form.Label></div>
                    <PasswordInput id="login-password" autoComplete="current-password" value={password} onChange={setPassword} />
                    <Link to="/forgot-password">Forgot password?</Link>
                  </Form.Group>
                  <Button type="submit" size="lg" className="w-100" disabled={busy}>
                    {busy ? 'Signing in…' : 'Sign in'}
                  </Button>
                </Form>
                {!usingMockApi && <><div className="auth-divider"><span>or</span></div><GoogleSignInButton onCredential={(credential) => void handleGoogleCredential(credential)} onUnavailable={setError} /></>}
                {usingMockApi && (
                  <>
                    <div className="auth-divider"><span>Demo access</span></div>
                    <div className="d-grid gap-2">
                      {DEMO_ACCOUNTS.map((account) => (
                        <Button
                          key={account.role}
                          variant="light"
                          className="demo-account d-flex justify-content-between align-items-center"
                          onClick={() => selectDemoAccount(account.email)}
                        >
                          <span>{account.role}</span>
                          <small>{account.email}</small>
                        </Button>
                      ))}
                    </div>
                  </>
                )}
                <p className="text-center text-secondary mt-4 mb-0">
                  New to Campus Events? <Link to="/register" state={location.state}>Create an account</Link>
                </p>
              </Card.Body>
            </Card>
          </Col>
        </Row>
      </Container>
    </main>
  )
}
