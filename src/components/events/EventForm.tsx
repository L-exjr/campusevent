import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import type { EventInput, EventItem } from '../../types'
import { EVENT_CATEGORIES } from '../../types'
import { toDateTimeLocal } from '../../utils/formatters'
import {
  DEFAULT_EVENT_IMAGE,
  IMAGE_ACCEPT,
  uploadImage,
  validateImageFile,
} from '../../api/imageStorage'

interface EventFormProps {
  event?: EventItem | null
  busy?: boolean
  error?: string | null
  submitLabel: string
  onSubmit: (input: EventInput) => Promise<void>
  onCancel: () => void
}

function initialValues(event?: EventItem | null): EventInput {
  return event
    ? {
        title: event.title,
        description: event.description,
        date: toDateTimeLocal(event.date),
        capacity: event.capacity,
        category: event.category,
        location: event.location,
        imageUrl: event.imageUrl,
        isPublished: event.isPublished,
        version: event.version,
      }
    : {
        title: '',
        description: '',
        date: '',
        capacity: 50,
        category: 'Academic',
        location: '',
        imageUrl: null,
        isPublished: true,
      }
}

export default function EventForm({
  event,
  busy = false,
  error,
  submitLabel,
  onSubmit,
  onCancel,
}: EventFormProps) {
  const [values, setValues] = useState<EventInput>(() => initialValues(event))
  const [validated, setValidated] = useState(false)
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imagePreview, setImagePreview] = useState(event?.imageUrl ?? DEFAULT_EVENT_IMAGE)
  const [imageError, setImageError] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [minimumDate] = useState(() =>
    toDateTimeLocal(new Date(Date.now() + 5 * 60_000).toISOString()),
  )

  useEffect(() => () => {
    if (imagePreview.startsWith('blob:')) URL.revokeObjectURL(imagePreview)
  }, [imagePreview])

  const handleImageChange = (change: ChangeEvent<HTMLInputElement>) => {
    const file = change.target.files?.[0]
    setImageError(null)
    if (!file) {
      setImageFile(null)
      setImagePreview(values.imageUrl ?? DEFAULT_EVENT_IMAGE)
      return
    }
    try {
      validateImageFile(file)
      setImageFile(file)
      setImagePreview(URL.createObjectURL(file))
    } catch (caught) {
      change.target.value = ''
      setImageFile(null)
      setImageError(caught instanceof Error ? caught.message : 'Choose a valid image.')
    }
  }

  const handleSubmit = async (submission: FormEvent<HTMLFormElement>) => {
    submission.preventDefault()
    const form = submission.currentTarget
    const normalizedValues = {
      ...values,
      title: values.title.trim(),
      description: values.description.trim(),
      location: values.location.trim(),
    }
    const hasValidTrimmedText =
      normalizedValues.title.length >= 3 &&
      normalizedValues.description.length >= 10 &&
      normalizedValues.location.length > 0
    if (!form.checkValidity() || !hasValidTrimmedText) {
      submission.stopPropagation()
      setValidated(true)
      return
    }
    setUploading(true)
    setImageError(null)
    try {
      const imageUrl = imageFile
        ? await uploadImage(imageFile, 'event-images')
        : normalizedValues.imageUrl
      await onSubmit({ ...normalizedValues, imageUrl })
    } catch (caught) {
      setImageError(caught instanceof Error ? caught.message : 'The image could not be uploaded.')
    } finally {
      setUploading(false)
    }
  }

  return (
    <Form noValidate validated={validated} onSubmit={(submission) => void handleSubmit(submission)}>
      {(error || imageError) && <Alert variant="danger">{error ?? imageError}</Alert>}
      <Row className="g-3">
        <Col xs={12}>
          <Form.Group controlId="event-image">
            <Form.Label>Cover image</Form.Label>
            <div className="d-flex flex-column flex-md-row gap-3 align-items-md-center">
              <img
                src={imagePreview}
                alt="Event cover preview"
                className="rounded border object-fit-cover"
                style={{ width: 220, aspectRatio: '16 / 9' }}
              />
              <div className="flex-grow-1">
                <Form.Control
                  type="file"
                  accept={IMAGE_ACCEPT}
                  onChange={handleImageChange}
                  disabled={busy || uploading}
                />
                <Form.Text>Optional. JPG, PNG, or WebP; maximum 5 MB.</Form.Text>
              </div>
            </div>
          </Form.Group>
        </Col>
        <Col xs={12}>
          <Form.Group controlId="event-title">
            <Form.Label>Event title</Form.Label>
            <Form.Control
              required
              minLength={3}
              maxLength={200}
              isInvalid={validated && values.title.trim().length < 3}
              value={values.title}
              onChange={(eventValue) => setValues({ ...values, title: eventValue.target.value })}
              placeholder="e.g. Future of AI Symposium"
            />
            <Form.Control.Feedback type="invalid">Enter an event title.</Form.Control.Feedback>
          </Form.Group>
        </Col>
        <Col xs={12}>
          <Form.Group controlId="event-description">
            <Form.Label>Description</Form.Label>
            <Form.Control
              as="textarea"
              rows={4}
              required
              minLength={10}
              maxLength={5000}
              isInvalid={validated && values.description.trim().length < 10}
              value={values.description}
              onChange={(eventValue) =>
                setValues({ ...values, description: eventValue.target.value })
              }
              placeholder="What should attendees expect?"
            />
            <Form.Control.Feedback type="invalid">Add a short description.</Form.Control.Feedback>
          </Form.Group>
        </Col>
        <Col md={6}>
          <Form.Group controlId="event-date">
            <Form.Label>Date and time</Form.Label>
            <Form.Control
              type="datetime-local"
              required
              min={event ? undefined : minimumDate}
              value={values.date}
              onChange={(eventValue) => setValues({ ...values, date: eventValue.target.value })}
            />
            <Form.Control.Feedback type="invalid">
              {event ? 'Choose a date and time.' : 'Choose a future date and time.'}
            </Form.Control.Feedback>
          </Form.Group>
        </Col>
        <Col md={6}>
          <Form.Group controlId="event-category">
            <Form.Label>Category</Form.Label>
            <Form.Select
              value={values.category}
              onChange={(eventValue) =>
                setValues({
                  ...values,
                  category: eventValue.target.value as EventInput['category'],
                })
              }
            >
              {EVENT_CATEGORIES.map((category) => (
                <option key={category}>{category}</option>
              ))}
            </Form.Select>
          </Form.Group>
        </Col>
        <Col md={8}>
          <Form.Group controlId="event-location">
            <Form.Label>Location</Form.Label>
            <Form.Control
              required
              maxLength={300}
              isInvalid={validated && values.location.trim().length === 0}
              value={values.location}
              onChange={(eventValue) => setValues({ ...values, location: eventValue.target.value })}
              placeholder="Building and room"
            />
            <Form.Control.Feedback type="invalid">Enter a location.</Form.Control.Feedback>
          </Form.Group>
        </Col>
        <Col md={4}>
          <Form.Group controlId="event-capacity">
            <Form.Label>Capacity</Form.Label>
            <Form.Control
              type="number"
              required
              min={1}
              max={100000}
              value={values.capacity}
              onChange={(eventValue) =>
                setValues({ ...values, capacity: Number(eventValue.target.value) })
              }
            />
            <Form.Control.Feedback type="invalid">Use at least 1.</Form.Control.Feedback>
          </Form.Group>
        </Col>
        <Col xs={12}>
          <Form.Check
            id="event-published"
            type="switch"
            label="Publish this event so Students can find and register for it"
            checked={values.isPublished ?? true}
            onChange={(eventValue) => setValues({ ...values, isPublished: eventValue.target.checked })}
          />
          {event && !event.isPublished && <Form.Text>This event is currently a private draft.</Form.Text>}
        </Col>
      </Row>
      <div className="d-flex justify-content-end gap-2 mt-4">
        <Button variant="light" onClick={onCancel} disabled={busy || uploading}>
          Cancel
        </Button>
        <Button type="submit" disabled={busy || uploading}>
          {uploading ? 'Uploading image…' : busy ? 'Saving…' : submitLabel}
        </Button>
      </div>
    </Form>
  )
}
