import { useCallback, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Form from 'react-bootstrap/Form'
import Table from 'react-bootstrap/Table'
import { useParams } from 'react-router-dom'
import { api } from '../../api'
import ErrorState from '../../components/shared/ErrorState'
import LinkButton from '../../components/shared/LinkButton'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'
import type { EventTeamRole } from '../../types'

const labels: Record<EventTeamRole, string> = { admin: 'Admin', member: 'Member', checkInStaff: 'Check-in Staff' }

export default function EventTeamPage() {
  const { id = '' } = useParams()
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<EventTeamRole>('member')
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const load = useCallback(async () => ({ event: await api.getManagementEvent(id), members: await api.getEventTeam(id) }), [id])
  const { data, loading, error, reload } = useApiResource(load)

  const invite = async (event: React.FormEvent) => {
    event.preventDefault(); setBusy(true); setActionError(null)
    try { await api.inviteEventTeamMember(id, email, role); setEmail(''); await reload() }
    catch (caught) { setActionError(caught instanceof Error ? caught.message : 'The invitation could not be sent.') }
    finally { setBusy(false) }
  }
  const changeRole = async (userId: string, nextRole: EventTeamRole) => {
    setBusy(true); setActionError(null)
    try { await api.updateEventTeamMember(id, userId, nextRole); await reload() }
    catch (caught) { setActionError(caught instanceof Error ? caught.message : 'The role could not be changed.') }
    finally { setBusy(false) }
  }
  const remove = async (userId: string) => {
    setBusy(true); setActionError(null)
    try { await api.removeEventTeamMember(id, userId); await reload() }
    catch (caught) { setActionError(caught instanceof Error ? caught.message : 'The member could not be removed.') }
    finally { setBusy(false) }
  }

  if (loading) return <LoadingState label="Loading event team" />
  if (error || !data) return <ErrorState message={error ?? 'No data returned.'} onRetry={() => void reload()} />
  return <>
    <LinkButton to="/organizer/events" variant="link" className="px-0 mb-2">← Back to events</LinkButton>
    <PageHeader eyebrow="Event management" title={`${data.event.title} team`} description="Invite existing users and assign only the access they need." />
    {actionError && <Alert variant="danger">{actionError}</Alert>}
    <Card className="border-0 mb-4"><Card.Body>
      <Form onSubmit={(event) => void invite(event)}><div className="d-flex flex-column flex-md-row gap-3 align-items-md-end">
        <Form.Group className="flex-grow-1" controlId="team-email"><Form.Label>Email</Form.Label><Form.Control type="email" required value={email} onChange={event => setEmail(event.target.value)} placeholder="Existing account email" /></Form.Group>
        <Form.Group controlId="team-role"><Form.Label>Role</Form.Label><Form.Select value={role} onChange={event => setRole(event.target.value as EventTeamRole)}><option value="admin">Admin</option><option value="member">Member</option><option value="checkInStaff">Check-in Staff</option></Form.Select></Form.Group>
        <Button type="submit" disabled={busy || !email.trim()}>Invite team member</Button>
      </div></Form>
      <small className="text-secondary">The person must already have an active Campus Events account.</small>
    </Card.Body></Card>
    <div className="table-shell"><Table responsive hover className="align-middle mb-0"><thead><tr><th>Person</th><th>Role</th><th className="text-end">Action</th></tr></thead><tbody>
      {data.members.map(member => <tr key={member.userId}><td><strong>{member.name}</strong><div className="text-secondary small">{member.email}</div></td><td>{member.isOwner ? 'Owner' : <Form.Select aria-label={`Role for ${member.name}`} disabled={busy} value={member.role ?? 'member'} onChange={event => void changeRole(member.userId, event.target.value as EventTeamRole)}>{Object.entries(labels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</Form.Select>}</td><td className="text-end">{!member.isOwner && <Button variant="outline-danger" size="sm" disabled={busy} onClick={() => void remove(member.userId)}>Remove</Button>}</td></tr>)}
    </tbody></Table></div>
  </>
}
