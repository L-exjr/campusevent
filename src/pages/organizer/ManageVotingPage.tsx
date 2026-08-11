import { useCallback, useEffect, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import { useParams } from 'react-router-dom'
import { api } from '../../api'
import LoadingState from '../../components/shared/LoadingState'
import LinkButton from '../../components/shared/LinkButton'
import PageHeader from '../../components/shared/PageHeader'
import { useAuth } from '../../hooks/useAuth'
import type { VotingCampaignInput } from '../../types'
import { toDateTimeLocal } from '../../utils/formatters'

type EditableCategory = VotingCampaignInput['categories'][number]

function blankCategory(): EditableCategory {
  return {
    name: '',
    description: '',
    mode: 'free',
    pricePerVoteMinor: 0,
    nominees: [
      { name: '', description: '' },
      { name: '', description: '' },
    ],
  }
}

export default function ManageVotingPage() {
  const { id = '' } = useParams()
  const { user } = useAuth()
  const [eventTitle, setEventTitle] = useState('Event voting')
  const [form, setForm] = useState<VotingCampaignInput | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const event = await api.getManagementEvent(id)
      setEventTitle(event.title)
      try {
        const campaign = await api.getVotingCampaign(id)
        setForm({
          opensAt: toDateTimeLocal(campaign.opensAt),
          closesAt: toDateTimeLocal(campaign.closesAt),
          isPublished: campaign.isPublished,
          categories: campaign.categories.map((category) => ({
            name: category.name,
            description: category.description ?? '',
            mode: category.mode,
            pricePerVoteMinor: category.pricePerVoteMinor,
            nominees: category.nominees.map((nominee) => ({
              name: nominee.name,
              description: nominee.description ?? '',
            })),
          })),
        })
      } catch {
        const opens = new Date()
        const closes = new Date(event.date)
        setForm({
          opensAt: toDateTimeLocal(opens.toISOString()),
          closesAt: toDateTimeLocal(closes.toISOString()),
          isPublished: false,
          categories: [blankCategory()],
        })
      }
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Voting settings could not be loaded.')
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    let active = true
    void Promise.resolve().then(() => {
      if (active) return load()
    })
    return () => { active = false }
  }, [load])

  const updateCategory = (index: number, update: Partial<EditableCategory>) => {
    if (!form) return
    setForm({
      ...form,
      categories: form.categories.map((category, itemIndex) =>
        itemIndex === index ? { ...category, ...update } : category),
    })
  }

  const save = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!form) return
    setSaving(true)
    setSaved(false)
    setError(null)
    try {
      await api.saveVotingCampaign(id, {
        ...form,
        opensAt: new Date(form.opensAt).toISOString(),
        closesAt: new Date(form.closesAt).toISOString(),
      })
      setSaved(true)
      await load()
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Voting settings could not be saved.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <LoadingState label="Loading voting settings" />
  const back = user?.role === 'admin' ? '/admin/events' : '/organizer/events'
  if (!form) return <Alert variant="danger">{error ?? 'Voting settings are unavailable.'}</Alert>

  return (
    <>
      <PageHeader
        eyebrow="Voting management"
        title={eventTitle}
        description="Create categories and nominees. Public totals stay hidden until the campaign closes."
        action={<LinkButton to={back}>Back to events</LinkButton>}
      />
      {error && <Alert variant="danger">{error}</Alert>}
      {saved && <Alert variant="success">Voting campaign saved.</Alert>}
      <Alert variant="light">
        Category and nominee structure locks as soon as anyone votes or opens a paid checkout.
      </Alert>
      <Form onSubmit={(event) => void save(event)}>
        <Card className="detail-card border-0 mb-4">
          <Card.Body className="p-4">
            <Row className="g-3">
              <Col md={5}>
                <Form.Group>
                  <Form.Label>Voting opens</Form.Label>
                  <Form.Control required type="datetime-local" value={form.opensAt}
                    onChange={(event) => setForm({ ...form, opensAt: event.target.value })} />
                </Form.Group>
              </Col>
              <Col md={5}>
                <Form.Group>
                  <Form.Label>Voting closes</Form.Label>
                  <Form.Control required type="datetime-local" value={form.closesAt}
                    onChange={(event) => setForm({ ...form, closesAt: event.target.value })} />
                </Form.Group>
              </Col>
              <Col md={2} className="d-flex align-items-end">
                <Form.Check type="switch" label="Published" checked={form.isPublished}
                  onChange={(event) => setForm({ ...form, isPublished: event.target.checked })} />
              </Col>
            </Row>
          </Card.Body>
        </Card>
        {form.categories.map((category, categoryIndex) => (
          <Card className="detail-card border-0 mb-4" key={categoryIndex}>
            <Card.Body className="p-4">
              <div className="d-flex justify-content-between gap-3 mb-3">
                <h2 className="h4">Category {categoryIndex + 1}</h2>
                {form.categories.length > 1 && (
                  <Button type="button" variant="outline-danger" size="sm" onClick={() => setForm({
                    ...form,
                    categories: form.categories.filter((_, index) => index !== categoryIndex),
                  })}>Remove category</Button>
                )}
              </div>
              <Row className="g-3 mb-4">
                <Col md={6}><Form.Group><Form.Label>Name</Form.Label><Form.Control required minLength={2}
                  value={category.name} onChange={(event) => updateCategory(categoryIndex, { name: event.target.value })} /></Form.Group></Col>
                <Col md={3}><Form.Group><Form.Label>Voting type</Form.Label><Form.Select value={category.mode}
                  onChange={(event) => updateCategory(categoryIndex, { mode: event.target.value as EditableCategory['mode'] })}>
                    <option value="free">Free — one per user</option><option value="paid">Paid — quantities</option>
                  </Form.Select></Form.Group></Col>
                <Col md={3}><Form.Group><Form.Label>Price per vote (GHS)</Form.Label><Form.Control type="number" min="0.01" step="0.01"
                  disabled={category.mode === 'free'} value={(category.pricePerVoteMinor / 100).toFixed(2)}
                  onChange={(event) => updateCategory(categoryIndex, { pricePerVoteMinor: Math.round(Number(event.target.value) * 100) })} /></Form.Group></Col>
                <Col xs={12}><Form.Group><Form.Label>Description</Form.Label><Form.Control as="textarea" rows={2}
                  value={category.description} onChange={(event) => updateCategory(categoryIndex, { description: event.target.value })} /></Form.Group></Col>
              </Row>
              <h3 className="h5">Nominees</h3>
              {category.nominees.map((nominee, nomineeIndex) => (
                <Row className="g-2 mb-2" key={nomineeIndex}>
                  <Col md={4}><Form.Control required minLength={2} placeholder="Nominee name" value={nominee.name}
                    onChange={(event) => updateCategory(categoryIndex, { nominees: category.nominees.map((item, index) => index === nomineeIndex ? { ...item, name: event.target.value } : item) })} /></Col>
                  <Col><Form.Control placeholder="Short description (optional)" value={nominee.description}
                    onChange={(event) => updateCategory(categoryIndex, { nominees: category.nominees.map((item, index) => index === nomineeIndex ? { ...item, description: event.target.value } : item) })} /></Col>
                  <Col xs="auto"><Button type="button" variant="outline-danger" disabled={category.nominees.length <= 2}
                    onClick={() => updateCategory(categoryIndex, { nominees: category.nominees.filter((_, index) => index !== nomineeIndex) })}>Remove</Button></Col>
                </Row>
              ))}
              <Button type="button" variant="outline-secondary" size="sm" onClick={() => updateCategory(categoryIndex, {
                nominees: [...category.nominees, { name: '', description: '' }],
              })}>Add nominee</Button>
            </Card.Body>
          </Card>
        ))}
        <div className="d-flex flex-wrap gap-2">
          <Button type="button" variant="outline-primary" onClick={() => setForm({
            ...form,
            categories: [...form.categories, blankCategory()],
          })}>Add category</Button>
          <Button type="submit" disabled={saving}>{saving ? 'Saving…' : 'Save voting campaign'}</Button>
        </div>
      </Form>
    </>
  )
}
