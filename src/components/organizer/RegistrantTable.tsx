import Badge from 'react-bootstrap/Badge'
import Table from 'react-bootstrap/Table'
import type { EventRegistrant } from '../../types'
import { formatDateTime } from '../../utils/formatters'

interface RegistrantTableProps {
  registrants: EventRegistrant[]
}

export default function RegistrantTable({ registrants }: RegistrantTableProps) {
  return (
    <div className="table-shell">
      <Table responsive hover className="align-middle mb-0">
        <thead>
          <tr>
            <th>Registrant</th>
            <th>Email</th>
            <th>Registered</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {registrants.map((registrant) => (
            <tr key={registrant.registrationId}>
              <td className="fw-semibold">{registrant.name}</td>
              <td>{registrant.email}</td>
              <td>{formatDateTime(registrant.registeredAt)}</td>
              <td>
                <Badge bg={registrant.attended ? 'success' : 'light'} text={registrant.attended ? undefined : 'dark'}>
                  {registrant.attended ? 'Attended' : 'Not marked'}
                </Badge>
              </td>
            </tr>
          ))}
        </tbody>
      </Table>
    </div>
  )
}
