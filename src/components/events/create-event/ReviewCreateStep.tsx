import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Form from 'react-bootstrap/Form'
import type { EventInput } from '../../../types'
import { formatDateTime } from '../../../utils/formatters'
import type { RegistrationMode } from './EventToolsStep'

export type EditableEventStep = 1 | 2 | 3

interface ReviewCreateStepProps {
  values: EventInput
  registrationMode: RegistrationMode
  imagePreview: string
  busy?: boolean
  uploading?: boolean
  confirmationDisabled?: boolean
  onEdit: (step: EditableEventStep) => void
  onPublishedChange: (isPublished: boolean) => void
  onBack: () => void
}

function formatMoney(amountMinor: number, currency: EventInput['currency']) {
  return `${currency} ${(amountMinor / 100).toFixed(2)}`
}

function venueLabel(values: EventInput) {
  if (values.format === 'physical') return 'In person'
  if (values.format === 'virtual') return 'Virtual'
  return 'Hybrid'
}

export default function ReviewCreateStep({
  values,
  registrationMode,
  imagePreview,
  busy = false,
  uploading = false,
  confirmationDisabled = false,
  onEdit,
  onPublishedChange,
  onBack,
}: ReviewCreateStepProps) {
  const isPublished = values.isPublished ?? true
  const submitLabel = isPublished ? 'Publish event' : 'Create draft'

  return (
    <section className="event-wizard-step" aria-labelledby="review-create-heading">
      <div className="form-section-heading mb-4">
        <span>04</span>
        <div>
          <h3 id="review-create-heading">Review and create</h3>
          <p>Confirm every detail, choose visibility, and create the event.</p>
        </div>
      </div>

      <div className="event-review-sections">
        <section className="event-review-section" aria-labelledby="review-basic-heading">
          <header>
            <div>
              <span>Step 1</span>
              <h4 id="review-basic-heading">Basic information</h4>
            </div>
            <Button variant="link" size="sm" onClick={() => onEdit(1)} disabled={busy || uploading}>
              Edit
            </Button>
          </header>
          <div className="event-review-cover-row">
            <img src={imagePreview} alt="Event cover preview" className="event-review-cover" />
            <dl className="review-list mb-0 flex-grow-1">
              <dt>Title</dt>
              <dd>{values.title}</dd>
              <dt>Description</dt>
              <dd className="review-list__long-text">{values.description}</dd>
              <dt>Category</dt>
              <dd>{values.category}</dd>
              <dt>Starts</dt>
              <dd>{formatDateTime(values.date)}</dd>
              <dt>Ends</dt><dd>{formatDateTime(values.endDate ?? '')}</dd>
              {(values.instagramUrl || values.twitterUrl || values.facebookUrl || values.websiteUrl) && <><dt>Social links</dt><dd>{[values.instagramUrl, values.twitterUrl, values.facebookUrl, values.websiteUrl].filter(Boolean).join(' · ')}</dd></>}
            </dl>
          </div>
        </section>

        <section className="event-review-section" aria-labelledby="review-venue-heading">
          <header>
            <div>
              <span>Step 2</span>
              <h4 id="review-venue-heading">Venue</h4>
            </div>
            <Button variant="link" size="sm" onClick={() => onEdit(2)} disabled={busy || uploading}>
              Edit
            </Button>
          </header>
          <dl className="review-list mb-0">
            <dt>Type</dt>
            <dd>{venueLabel(values)}</dd>
            {values.format !== 'virtual' && (
              <>
                <dt>Address</dt>
                <dd>{values.location}</dd>
                {values.latitude != null && values.longitude != null && <><dt>Map pin</dt><dd>{values.latitude.toFixed(5)}, {values.longitude.toFixed(5)}</dd></>}
              </>
            )}
            {values.format !== 'physical' && (
              <>
                <dt>Meeting link</dt>
                <dd className="text-break">{values.meetingUrl}</dd>
                <dt>Platform</dt><dd>{values.virtualPlatform}</dd>
              </>
            )}
          </dl>
        </section>

        <section className="event-review-section" aria-labelledby="review-tools-heading">
          <header>
            <div>
              <span>Step 3</span>
              <h4 id="review-tools-heading">Event tools</h4>
            </div>
            <Button variant="link" size="sm" onClick={() => onEdit(3)} disabled={busy || uploading}>
              Edit
            </Button>
          </header>
          <dl className="review-list mb-0">
            <dt>Ticketing</dt><dd>{values.ticketingEnabled ? (registrationMode === 'paid' ? 'Paid tickets' : 'Free tickets') : 'Disabled'}</dd>
            <dt>Registrations</dt><dd>{values.registrationsEnabled ? 'Enabled' : 'Disabled'}</dd>
            <dt>Capacity</dt>
            <dd>{values.capacity.toLocaleString()}</dd>
            {values.ticketingEnabled && registrationMode === 'paid' && (
              <>
                <dt>Ticket price</dt>
                <dd>{formatMoney(values.priceMinor, values.currency)}</dd>
                <dt>Sales open</dt>
                <dd>{formatDateTime(values.salesStartsAt ?? '')}</dd>
                <dt>Sales close</dt>
                <dd>{formatDateTime(values.salesEndsAt ?? '')}</dd>
              </>
            )}
            <dt>Voting</dt>
            <dd>{values.votingEnabled ? 'Enabled; configure the campaign after creation.' : 'Disabled'}</dd>
          </dl>
        </section>

        <section className="event-review-section" aria-labelledby="review-publishing-heading">
          <header>
            <div>
              <span>Step 4</span>
              <h4 id="review-publishing-heading">Publishing</h4>
            </div>
          </header>
          <fieldset>
            <legend className="form-label">What should happen after creation?</legend>
            <div className="event-choice-grid">
              <label className={`event-choice-card${!isPublished ? ' is-selected' : ''}`}>
                <Form.Check.Input
                  type="radio"
                  name="create-event-publishing"
                  value="draft"
                  checked={!isPublished}
                  disabled={busy || uploading}
                  onChange={() => onPublishedChange(false)}
                />
                <span>
                  <strong>Save as draft</strong>
                  <small>Keep the event private until it is ready.</small>
                </span>
              </label>
              <label className={`event-choice-card${isPublished ? ' is-selected' : ''}`}>
                <Form.Check.Input
                  type="radio"
                  name="create-event-publishing"
                  value="publish"
                  checked={isPublished}
                  disabled={busy || uploading}
                  onChange={() => onPublishedChange(true)}
                />
                <span>
                  <strong>Publish now</strong>
                  <small>Make the event visible and available for registration.</small>
                </span>
              </label>
            </div>
          </fieldset>
        </section>
      </div>

      {confirmationDisabled && (
        <Alert variant="warning" className="mt-4 mb-0">
          This event cannot be created until the outstanding paid-ticketing requirement is
          resolved.
        </Alert>
      )}

      <div className="form-actions mt-4">
        <Button variant="light" onClick={onBack} disabled={busy || uploading}>
          Back
        </Button>
        <Button type="submit" disabled={busy || uploading || confirmationDisabled}>
          {uploading ? 'Uploading image…' : busy ? 'Creating…' : submitLabel}
        </Button>
      </div>
    </section>
  )
}
