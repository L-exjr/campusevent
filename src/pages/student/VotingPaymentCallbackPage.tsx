import { useCallback, useEffect, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Card from 'react-bootstrap/Card'
import { useSearchParams } from 'react-router-dom'
import { api } from '../../api'
import LinkButton from '../../components/shared/LinkButton'
import LoadingState from '../../components/shared/LoadingState'
import type { VotingPaymentStatus } from '../../types'

export default function VotingPaymentCallbackPage() {
  const [searchParams] = useSearchParams()
  const reference = searchParams.get('reference') ?? searchParams.get('trxref') ?? ''
  const [payment, setPayment] = useState<VotingPaymentStatus | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [attempts, setAttempts] = useState(0)

  const refresh = useCallback(async () => {
    if (!reference) {
      setError('The voting payment reference is missing.')
      return
    }
    try {
      setPayment(await api.getVotingPaymentStatus(reference))
      setError(null)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Voting payment status could not be loaded.')
    } finally {
      setAttempts((value) => value + 1)
    }
  }, [reference])

  useEffect(() => {
    if (payment?.status === 'verified' || payment?.status === 'failed' || attempts >= 30) return
    const timeout = window.setTimeout(() => void refresh(), payment ? 2_000 : 0)
    return () => window.clearTimeout(timeout)
  }, [attempts, payment, refresh])

  return (
    <Card className="detail-card border-0 mx-auto" style={{ maxWidth: 680 }}>
      <Card.Body className="p-4 p-lg-5">
        <p className="eyebrow">Voting payment</p>
        {!error && (!payment || payment.status === 'pending') && attempts < 30 && (
          <LoadingState label="Waiting for Paystack to verify and record your votes…" />
        )}
        {payment?.status === 'verified' && payment.voteRecorded && (
          <Alert variant="success">
            <Alert.Heading>Votes recorded</Alert.Heading>
            Your payment was verified and {payment.quantity} {payment.quantity === 1 ? 'vote was' : 'votes were'} added.
          </Alert>
        )}
        {payment?.status === 'failed' && (
          <Alert variant="danger">The payment could not be verified. No votes were added.</Alert>
        )}
        {attempts >= 30 && payment?.status === 'pending' && (
          <Alert variant="info">Verification is taking longer than expected. It is safe to revisit this page.</Alert>
        )}
        {error && <Alert variant="danger">{error}</Alert>}
        <LinkButton to="/events">Return to events</LinkButton>
      </Card.Body>
    </Card>
  )
}
