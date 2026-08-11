import { useCallback, useEffect, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Card from 'react-bootstrap/Card'
import { useSearchParams } from 'react-router-dom'
import { api } from '../../api'
import LinkButton from '../../components/shared/LinkButton'
import LoadingState from '../../components/shared/LoadingState'
import type { EventPaymentStatus } from '../../types'

const terminalStatuses = new Set<EventPaymentStatus['status']>([
  'verified',
  'failed',
  'expired',
  'refundPending',
  'refunded',
  'refundFailed',
])

export default function PaymentCallbackPage() {
  const [searchParams] = useSearchParams()
  const reference = searchParams.get('reference') ?? searchParams.get('trxref') ?? ''
  const [payment, setPayment] = useState<EventPaymentStatus | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [attempts, setAttempts] = useState(0)

  const refresh = useCallback(async () => {
    if (!reference) {
      setError('The payment reference is missing.')
      return
    }
    try {
      setPayment(await api.getPaymentStatus(reference))
      setError(null)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Payment status could not be loaded.')
    } finally {
      setAttempts((value) => value + 1)
    }
  }, [reference])

  useEffect(() => {
    if (payment && terminalStatuses.has(payment.status)) return
    if (attempts >= 30) return
    const timeout = window.setTimeout(() => void refresh(), payment ? 2_000 : 0)
    return () => window.clearTimeout(timeout)
  }, [attempts, payment, refresh])

  const isWaiting = !error && (!payment || payment.status === 'pending') && attempts < 30

  return (
    <Card className="detail-card border-0 mx-auto" style={{ maxWidth: 680 }}>
      <Card.Body className="p-4 p-lg-5">
        <p className="eyebrow">Payment status</p>
        {isWaiting && <LoadingState label="Waiting for Paystack to verify your payment…" />}
        {payment?.status === 'verified' && (
          <Alert variant="success">
            <Alert.Heading>Payment verified</Alert.Heading>
            Your registration is confirmed. A confirmation email is on its way.
          </Alert>
        )}
        {payment?.status === 'expired' && (
          <Alert variant="warning">This checkout reservation expired. Return to the event to try again.</Alert>
        )}
        {payment?.status === 'failed' && (
          <Alert variant="danger">The payment could not be verified. You have not been registered.</Alert>
        )}
        {payment && ['refundPending', 'refunded', 'refundFailed'].includes(payment.status) && (
          <Alert variant={payment.status === 'refundFailed' ? 'danger' : 'warning'}>
            The payment arrived after the reserved place was no longer available. A refund has been requested.
          </Alert>
        )}
        {attempts >= 30 && payment?.status === 'pending' && (
          <Alert variant="info">
            Verification is taking longer than expected. You can safely revisit this page or check your registrations later.
          </Alert>
        )}
        {error && <Alert variant="danger">{error}</Alert>}
        <div className="d-flex flex-wrap gap-2 mt-4">
          <LinkButton to="/student/registrations">My registrations</LinkButton>
          <LinkButton to="/events" variant="outline-primary">Browse events</LinkButton>
        </div>
      </Card.Body>
    </Card>
  )
}
