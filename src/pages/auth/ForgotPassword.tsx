import { useState, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Container from 'react-bootstrap/Container'
import Form from 'react-bootstrap/Form'
import { Link } from 'react-router-dom'
import { api } from '../../api'

export default function ForgotPassword() {
  const [email, setEmail] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [validated, setValidated] = useState(false)
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const form = event.currentTarget as HTMLFormElement
    if (!form.checkValidity()) { event.stopPropagation(); setValidated(true); return }
    setValidated(true); setBusy(true); setError(null)
    try { setMessage(await api.forgotPassword(email)) }
    catch (caught) { setError(caught instanceof Error ? caught.message : 'Unable to request a reset link.') }
    finally { setBusy(false) }
  }
  return <main className="auth-page"><Container><Card className="auth-card border-0 mx-auto" style={{maxWidth: 620}}><Card.Body className="p-4 p-md-5">
    <h1 className="h2">Forgot your password?</h1>
    <p className="text-secondary">Enter your email. If it belongs to an account, we’ll send a 30-minute reset link.</p>
    {message && <Alert variant="success">{message}</Alert>}{error && <Alert variant="danger">{error}</Alert>}
    {!message && <Form noValidate validated={validated} onSubmit={(event) => void submit(event)}><Form.Group className="mb-3" controlId="forgot-email"><Form.Label>Email address</Form.Label><Form.Control type="email" autoComplete="email" required value={email} onChange={(event) => setEmail(event.target.value)} /><Form.Control.Feedback type="invalid">Enter a valid email address.</Form.Control.Feedback></Form.Group><Button type="submit" className="w-100" disabled={busy}>{busy ? 'Sending…' : 'Send reset link'}</Button></Form>}
    <p className="text-center mt-4 mb-0"><Link to="/login">Back to sign in</Link></p>
  </Card.Body></Card></Container></main>
}
