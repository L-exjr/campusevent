import { useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Form from 'react-bootstrap/Form'
import Table from 'react-bootstrap/Table'
import { api } from '../../api'
import type { EventRegistrant } from '../../types'

interface AttendanceChecklistProps {
  eventId: string
  registrants: EventRegistrant[]
  onSaved: () => Promise<void>
}

export default function AttendanceChecklist({
  eventId,
  registrants,
  onSaved,
}: AttendanceChecklistProps) {
  const [attendance, setAttendance] = useState<Record<string, boolean>>(() =>
    Object.fromEntries(registrants.map((item) => [item.registrationId, item.attended])),
  )
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  const markAll = (value: boolean) => {
    setAttendance(Object.fromEntries(registrants.map((item) => [item.registrationId, value])))
    setSaved(false)
  }

  const save = async () => {
    setBusy(true)
    setError(null)
    setSaved(false)
    try {
      await api.updateAttendance(eventId, attendance)
      setSaved(true)
      await onSaved()
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Attendance could not be saved.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      {saved && <Alert variant="success">Attendance saved successfully.</Alert>}
      {error && <Alert variant="danger">{error}</Alert>}
      <div className="d-flex flex-column flex-sm-row justify-content-between align-items-sm-center gap-3 mb-3">
        <p className="text-secondary mb-0">
          {Object.values(attendance).filter(Boolean).length} of {registrants.length} marked present
        </p>
        <div className="d-flex gap-2">
          <Button size="sm" variant="light" onClick={() => markAll(true)}>Mark all</Button>
          <Button size="sm" variant="light" onClick={() => markAll(false)}>Clear all</Button>
        </div>
      </div>
      <div className="table-shell">
        <Table responsive hover className="align-middle mb-0 attendance-table">
          <thead>
            <tr>
              <th>Present</th>
              <th>Student</th>
              <th>Email</th>
            </tr>
          </thead>
          <tbody>
            {registrants.map((registrant) => (
              <tr key={registrant.registrationId}>
                <td>
                  <Form.Check
                    aria-label={`Mark ${registrant.name} present`}
                    checked={Boolean(attendance[registrant.registrationId])}
                    onChange={(event) => {
                      setSaved(false)
                      setAttendance({ ...attendance, [registrant.registrationId]: event.target.checked })
                    }}
                  />
                </td>
                <td className="fw-semibold">{registrant.name}</td>
                <td>{registrant.email}</td>
              </tr>
            ))}
          </tbody>
        </Table>
      </div>
      <div className="d-flex justify-content-end mt-3">
        <Button size="lg" onClick={() => void save()} disabled={busy}>
          {busy ? 'Saving…' : 'Save attendance'}
        </Button>
      </div>
    </>
  )
}
