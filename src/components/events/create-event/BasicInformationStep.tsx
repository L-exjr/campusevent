import type { ChangeEventHandler } from 'react'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import type { EventInput } from '../../../types'
import { EVENT_CATEGORIES } from '../../../types'
import { IMAGE_ACCEPT } from '../../../api/imageStorage'

export interface BasicInformationErrors {
  title?: string
  description?: string
  eventDate?: string
  eventTime?: string
  image?: string
}

interface BasicInformationStepProps {
  values: EventInput
  eventDate: string
  eventTime: string
  imagePreview: string
  errors?: BasicInformationErrors
  disabled?: boolean
  minimumDate?: string
  onValuesChange: (changes: Partial<EventInput>) => void
  onEventDateChange: (date: string) => void
  onEventTimeChange: (time: string) => void
  onImageChange: ChangeEventHandler<HTMLInputElement>
}

export default function BasicInformationStep({
  values,
  eventDate,
  eventTime,
  imagePreview,
  errors = {},
  disabled = false,
  minimumDate,
  onValuesChange,
  onEventDateChange,
  onEventTimeChange,
  onImageChange,
}: BasicInformationStepProps) {
  return (
    <section className="event-wizard-step" aria-labelledby="basic-information-heading">
      <div className="form-section-heading mb-4">
        <span>01</span>
        <div>
          <h3 id="basic-information-heading">Basic information</h3>
          <p>Tell attendees what the event is about and when it begins.</p>
        </div>
      </div>

      <Row className="g-3">
        <Col xs={12}>
          <Form.Group controlId="create-event-title">
            <Form.Label>Event title</Form.Label>
            <Form.Control
              required
              minLength={3}
              maxLength={200}
              value={values.title}
              isInvalid={Boolean(errors.title)}
              disabled={disabled}
              placeholder="e.g. Future of AI Symposium"
              onChange={(event) => onValuesChange({ title: event.target.value })}
            />
            <Form.Control.Feedback type="invalid">
              {errors.title ?? 'Enter an event title.'}
            </Form.Control.Feedback>
          </Form.Group>
        </Col>

        <Col xs={12}>
          <Form.Group controlId="create-event-description">
            <Form.Label>Description</Form.Label>
            <Form.Control
              as="textarea"
              required
              rows={5}
              minLength={10}
              maxLength={5000}
              value={values.description}
              isInvalid={Boolean(errors.description)}
              disabled={disabled}
              placeholder="What should attendees expect?"
              onChange={(event) => onValuesChange({ description: event.target.value })}
            />
            <div className="d-flex justify-content-between gap-3">
              <Form.Control.Feedback type="invalid">
                {errors.description ?? 'Add a short description.'}
              </Form.Control.Feedback>
              <Form.Text className="ms-auto text-nowrap">
                {values.description.length.toLocaleString()} / 5,000
              </Form.Text>
            </div>
          </Form.Group>
        </Col>

        <Col md={4}>
          <Form.Group controlId="create-event-category">
            <Form.Label>Category</Form.Label>
            <Form.Select
              value={values.category}
              disabled={disabled}
              onChange={(event) =>
                onValuesChange({ category: event.target.value as EventInput['category'] })
              }
            >
              {EVENT_CATEGORIES.map((category) => (
                <option key={category}>{category}</option>
              ))}
            </Form.Select>
          </Form.Group>
        </Col>

        <Col md={4}>
          <Form.Group controlId="create-event-date">
            <Form.Label>Event date</Form.Label>
            <Form.Control
              type="date"
              required
              min={minimumDate}
              value={eventDate}
              isInvalid={Boolean(errors.eventDate)}
              disabled={disabled}
              onChange={(event) => onEventDateChange(event.target.value)}
            />
            <Form.Control.Feedback type="invalid">
              {errors.eventDate ?? 'Choose a future event date.'}
            </Form.Control.Feedback>
          </Form.Group>
        </Col>

        <Col md={4}>
          <Form.Group controlId="create-event-time">
            <Form.Label>Start time</Form.Label>
            <Form.Control
              type="time"
              required
              value={eventTime}
              isInvalid={Boolean(errors.eventTime)}
              disabled={disabled}
              onChange={(event) => onEventTimeChange(event.target.value)}
            />
            <Form.Control.Feedback type="invalid">
              {errors.eventTime ?? 'Choose the start time.'}
            </Form.Control.Feedback>
          </Form.Group>
        </Col>

        <Col xs={12}>
          <Form.Group controlId="create-event-image">
            <Form.Label>Cover image</Form.Label>
            <div className={`event-cover-upload${errors.image ? ' is-invalid' : ''}`}>
              <img
                src={imagePreview}
                alt="Event cover preview"
                className="event-cover-upload__preview"
              />
              <div className="event-cover-upload__copy">
                <strong>Choose a clear, landscape image</strong>
                <span>Optional. JPG, PNG, or WebP; 16:9 recommended; maximum 5 MB.</span>
                <Form.Control
                  type="file"
                  accept={IMAGE_ACCEPT}
                  disabled={disabled}
                  isInvalid={Boolean(errors.image)}
                  onChange={onImageChange}
                />
                {errors.image && (
                  <div className="invalid-feedback d-block">{errors.image}</div>
                )}
              </div>
            </div>
          </Form.Group>
        </Col>
      </Row>
    </section>
  )
}
