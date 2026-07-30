import { useCallback, useEffect, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Badge from 'react-bootstrap/Badge'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Form from 'react-bootstrap/Form'
import { api } from '../../api'
import type { BookingRequest, User } from '../../types'
import ErrorState from '../../components/shared/ErrorState'
import LoadingState from '../../components/shared/LoadingState'
import PageHeader from '../../components/shared/PageHeader'
import { useApiResource } from '../../hooks/useApiResource'

export default function AdminBookingQueue() {
  const load = useCallback(() => api.getBookingRequests(), [])
  const {data,loading,error,reload,setData}=useApiResource(load)
  const [organizers,setOrganizers]=useState<User[]>([]); const [selected,setSelected]=useState<Record<string,string>>({}); const [notice,setNotice]=useState<string|null>(null); const [actionError,setActionError]=useState<string|null>(null)
  useEffect(()=>{ void api.getUsers().then(users=>setOrganizers(users.filter(user=>user.role==='organizer'&&user.active))).catch(()=>setActionError('Organizers could not be loaded.')) },[])
  const assign=async(request:BookingRequest)=>{const organizerId=selected[request.id];if(!organizerId)return
    try{const updated=await api.assignBookingRequest(request.id,organizerId);setData(current=>(current??[]).map(item=>item.id===updated.id?updated:item));setNotice('Request sent to the Organizer.');setActionError(null)}catch(caught){setActionError(caught instanceof Error?caught.message:'Unable to assign the request.')}}
  return <><PageHeader eyebrow="Public requests" title="Booking request queue" description="Review incoming organization requests and route them to an active Organizer." />
    {notice&&<Alert variant="success">{notice}</Alert>}{actionError&&<Alert variant="danger">{actionError}</Alert>}
    {loading?<LoadingState label="Loading booking requests"/>:error?<ErrorState message={error} onRetry={()=>void reload()}/>:<div className="d-grid gap-3">{(data??[]).map(request=><Card key={request.id} className="border-0"><Card.Body className="p-4"><div className="d-flex justify-content-between gap-3"><div><h2 className="h5">{request.organizationName}</h2><p className="text-secondary mb-2">{request.eventType} · {new Date(request.proposedDate).toLocaleString()} · {request.estimatedAttendance} guests</p></div><Badge bg="secondary" className="align-self-start">{request.status}</Badge></div><p>{request.description}</p><p className="small mb-3">Contact: <a href={`mailto:${request.email}`}>{request.contactName}</a> · {request.phone}</p>{request.assignedOrganizerName?<strong>Assigned to {request.assignedOrganizerName}</strong>:<div className="d-flex gap-2"><Form.Select aria-label={`Organizer for ${request.organizationName}`} value={selected[request.id]??''} onChange={e=>setSelected(current=>({...current,[request.id]:e.target.value}))}><option value="">Choose an Organizer</option>{organizers.map(user=><option key={user.id} value={user.id}>{user.name}</option>)}</Form.Select><Button disabled={!selected[request.id]} onClick={()=>void assign(request)}>Send</Button></div>}</Card.Body></Card>)}</div>}
  </>
}
