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
import PasswordInput from '../../components/auth/PasswordInput'
import GoogleSignInButton from '../../components/auth/GoogleSignInButton'
import { usingMockApi } from '../../api'

export default function RegisterPage() {
  const { register, googleLogin } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const returnPath = (location.state as { from?: unknown } | null)?.from
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [validated, setValidated] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    const form = event.currentTarget
    const fieldsValid = form.checkValidity() && name.trim().length >= 2 && password.length >= 8 && password === confirmPassword
    setValidated(true)
    if (!fieldsValid) {
      event.stopPropagation()
      return
    }
    setBusy(true)
    try {
      const session = await register(name, email, password)
      navigate(
        typeof returnPath === 'string' && returnPath.startsWith('/events/')
          ? returnPath
          : getHomeForRole(session.user.role),
        { replace: true },
      )
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Unable to create your account.')
    } finally {
      setBusy(false)
    }
  }

  const handleGoogleCredential = async (credential: string) => {
    setBusy(true); setError(null)
    try {
      const session = await googleLogin(credential)
      navigate(getHomeForRole(session.user.role), { replace: true })
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Google sign-up failed.')
    } finally { setBusy(false) }
  }

  return (
    <main className="auth-page">
      <Container>
        <Row className="g-0 align-items-stretch auth-layout auth-layout--register">
          <Col lg={{ span: 6, order: 2 }}>
            <section className="auth-hero h-100">
              <Link to="/" className="auth-brand"><span className="brand-mark">C</span>Campus Events</Link>
              <div className="my-auto py-5">
                <Badge bg="light" text="dark" className="auth-kicker mb-4">Join your campus community</Badge>
                <h1>Find your next experience. Start here.</h1>
                <p>Create one account for event discovery, registration, tickets, and everything that comes next.</p>
              </div>
              <div className="auth-quote"><div>
                <strong>Already have an account?</strong>
                <p>Welcome back—your events and registrations are waiting.</p>
                <Link to="/login" state={location.state} className="btn btn-outline-light auth-switch-link">Sign in</Link>
              </div></div>
            </section>
          </Col>
          <Col lg={{ span: 6, order: 1 }}>
            <Card className="auth-card border-0 h-100">
              <Card.Body className="p-4 p-md-5 d-flex flex-column justify-content-center">
                <p className="eyebrow mb-2">Student registration</p>
                <h2 className="h1 mb-2">Create your account</h2>
                <p className="text-secondary mb-4">
                  New accounts start as Students. An Admin can promote your account to Organizer later.
                </p>
                {error && <Alert variant="danger">{error}</Alert>}
                <Form noValidate validated={validated} onSubmit={(event) => void handleSubmit(event)}>
                  <Form.Group className="mb-3" controlId="register-name">
                    <Form.Label>Full name</Form.Label>
                    <Form.Control
                      required
                      minLength={2}
                      isInvalid={validated && name.trim().length < 2}
                      autoComplete="name"
                      value={name}
                      onChange={(event) => setName(event.target.value)}
                    />
                    <Form.Control.Feedback type="invalid">Enter your full name.</Form.Control.Feedback>
                  </Form.Group>
                  <Form.Group className="mb-3" controlId="register-email">
                    <Form.Label>Email address</Form.Label>
                    <Form.Control
                      type="email"
                      required
                      autoComplete="email"
                      value={email}
                      onChange={(event) => setEmail(event.target.value)}
                    />
                    <Form.Control.Feedback type="invalid">Enter a valid email address.</Form.Control.Feedback>
                  </Form.Group>
                  <Row>
                    <Col md={6}>
                      <Form.Group className="mb-3">
                        <Form.Label htmlFor="register-password">Password</Form.Label>
                        <PasswordInput
                          id="register-password"
                          minLength={8}
                          autoComplete="new-password"
                          value={password}
                          onChange={setPassword}
                          isInvalid={validated && password.length < 8}
                          invalidFeedback="Use at least 8 characters."
                        />
                      </Form.Group>
                    </Col>
                    <Col md={6}>
                      <Form.Group className="mb-3">
                        <Form.Label htmlFor="register-confirm-password">Confirm password</Form.Label>
                        <PasswordInput
                          id="register-confirm-password"
                          autoComplete="new-password"
                          value={confirmPassword}
                          onChange={setConfirmPassword}
                          isInvalid={validated && (confirmPassword.length === 0 || password !== confirmPassword)}
                          invalidFeedback="Enter the same password again."
                        />
                      </Form.Group>
                    </Col>
                  </Row>
                  <Button type="submit" size="lg" className="w-100 mt-2" disabled={busy}>
                    {busy ? 'Creating account…' : 'Create student account'}
                  </Button>
                </Form>
                {!usingMockApi && <><div className="auth-divider"><span>or continue with</span></div><GoogleSignInButton onCredential={(credential) => void handleGoogleCredential(credential)} onUnavailable={setError} /></>}
                <p className="text-center text-secondary mt-4 mb-0">
                  Already have an account? <Link to="/login" state={location.state}>Sign in</Link>
                </p>
              </Card.Body>
            </Card>
          </Col>
        </Row>
      </Container>
    </main>
  )
}
