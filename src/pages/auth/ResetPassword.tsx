import { useState, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Container from 'react-bootstrap/Container'
import Form from 'react-bootstrap/Form'
import { Link, useSearchParams } from 'react-router-dom'
import { api } from '../../api'
import PasswordInput from '../../components/auth/PasswordInput'

export default function ResetPassword() {
  const [params] = useSearchParams(); const token = params.get('token') ?? ''
  const [password, setPassword] = useState(''); const [confirm, setConfirm] = useState('')
  const [message, setMessage] = useState<string | null>(null); const [error, setError] = useState<string | null>(null); const [busy, setBusy] = useState(false)
  const [validated, setValidated] = useState(false)
  const submit = async (event: FormEvent) => { event.preventDefault(); setError(null)
    const form = event.currentTarget as HTMLFormElement
    setValidated(true)
    if (!token) return setError('This reset link is missing its token.')
    if (!form.checkValidity() || password.length < 8 || password !== confirm) { event.stopPropagation(); return }
    setBusy(true); try { setMessage(await api.resetPassword(token, password)) } catch (caught) { setError(caught instanceof Error ? caught.message : 'Unable to reset the password.') } finally { setBusy(false) }
  }
  return <main className="auth-page"><Container><Card className="auth-card border-0 mx-auto" style={{maxWidth: 620}}><Card.Body className="p-4 p-md-5"><h1 className="h2">Choose a new password</h1>
    {message && <Alert variant="success">{message} <Link to="/login">Sign in</Link></Alert>}{error && <Alert variant="danger">{error}</Alert>}
    {!message && <Form noValidate validated={validated} onSubmit={(event) => void submit(event)}><Form.Group className="mb-3"><Form.Label htmlFor="reset-password">New password</Form.Label><PasswordInput id="reset-password" value={password} onChange={setPassword} autoComplete="new-password" minLength={8} isInvalid={validated && password.length < 8} invalidFeedback="Use at least 8 characters." /></Form.Group><Form.Group className="mb-3"><Form.Label htmlFor="reset-confirm">Confirm password</Form.Label><PasswordInput id="reset-confirm" value={confirm} onChange={setConfirm} autoComplete="new-password" minLength={8} isInvalid={validated && (confirm.length < 8 || password !== confirm)} invalidFeedback="Enter the same password again." /></Form.Group><Button type="submit" className="w-100" disabled={busy}>{busy ? 'Resetting…' : 'Reset password'}</Button></Form>}
  </Card.Body></Card></Container></main>
}
