import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Form from 'react-bootstrap/Form'
import type { EventInput } from '../../../types'
import { toDateTimeLocal } from '../../../utils/formatters'
import {
  DEFAULT_EVENT_IMAGE,
  validateImageFile,
} from '../../../api/imageStorage'
import BasicInformationStep from './BasicInformationStep'
import EventToolsStep, { type RegistrationMode } from './EventToolsStep'
import EventWizardProgress, { type EventWizardStep } from './EventWizardProgress'
import ReviewCreateStep from './ReviewCreateStep'
import VenueStep from './VenueStep'
import {
  buildCreateEventPayload,
  combineDateTime,
  firstInvalidStep,
  hasErrors,
  validateBasicInformation,
  validateEventTools,
  validateEventWizard,
  validateVenue,
  type EventWizardErrors,
  type EventWizardSnapshot,
} from './eventWizardValidation'
import './EventCreationWizard.css'

interface EventCreationWizardProps {
  busy?: boolean
  error?: string | null
  onSubmit: (input: EventInput, imageFile: File | null) => Promise<void>
  onCancel: () => void
}

const initialValues: EventInput = {
  title: '',
  description: '',
  date: '',
  capacity: 50,
  category: 'Art & Exhibition',
  location: '',
  format: 'physical',
  meetingUrl: null,
  endDate: null,
  virtualPlatform: null,
  latitude: null,
  longitude: null,
  instagramUrl: null,
  twitterUrl: null,
  facebookUrl: null,
  websiteUrl: null,
  ticketingEnabled: false,
  registrationsEnabled: true,
  votingEnabled: false,
  salesStartsAt: null,
  salesEndsAt: null,
  imageUrl: null,
  isPublished: true,
  priceMinor: 0,
  currency: 'GHS',
}

const emptyErrors: EventWizardErrors = {
  basic: {},
  venue: {},
  tools: {},
}

export default function EventCreationWizard({
  busy = false,
  error,
  onSubmit,
  onCancel,
}: EventCreationWizardProps) {
  const [currentStep, setCurrentStep] = useState<EventWizardStep>(1)
  const [values, setValues] = useState<EventInput>(initialValues)
  const [eventDate, setEventDate] = useState('')
  const [eventTime, setEventTime] = useState('')
  const [eventEndDate, setEventEndDate] = useState('')
  const [eventEndTime, setEventEndTime] = useState('')
  const [registrationMode, setRegistrationMode] = useState<RegistrationMode>('free')
  const [errors, setErrors] = useState<EventWizardErrors>(emptyErrors)
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imagePreview, setImagePreview] = useState(DEFAULT_EVENT_IMAGE)
  const [imageError, setImageError] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [minimumDateTime] = useState(() =>
    toDateTimeLocal(new Date(Date.now() + 5 * 60_000).toISOString()),
  )

  useEffect(() => () => {
    if (imagePreview.startsWith('blob:')) URL.revokeObjectURL(imagePreview)
  }, [imagePreview])

  const snapshot: EventWizardSnapshot = {
    values,
    eventDate,
    eventTime,
    eventEndDate,
    eventEndTime,
    registrationMode,
  }

  const updateValues = (changes: Partial<EventInput>) => {
    setValues((current) => ({ ...current, ...changes }))
  }

  const handleDateChange = (date: string) => {
    setEventDate(date)
    updateValues({ date: combineDateTime(date, eventTime) })
  }

  const handleTimeChange = (time: string) => {
    setEventTime(time)
    updateValues({ date: combineDateTime(eventDate, time) })
    if (eventDate && time && !eventEndDate && !eventEndTime) {
      const end = new Date(`${eventDate}T${time}`)
      end.setHours(end.getHours() + 1)
      const endDate = toDateTimeLocal(end.toISOString())
      setEventEndDate(endDate.slice(0, 10))
      setEventEndTime(endDate.slice(11, 16))
      updateValues({ date: combineDateTime(eventDate, time), endDate })
    }
  }

  const handleImageChange = (change: ChangeEvent<HTMLInputElement>) => {
    const file = change.target.files?.[0]
    setImageError(null)
    setErrors((current) => ({ ...current, basic: { ...current.basic, image: undefined } }))

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
      const message = caught instanceof Error ? caught.message : 'Choose a valid image.'
      change.target.value = ''
      setImageFile(null)
      setImageError(message)
      setErrors((current) => ({
        ...current,
        basic: { ...current.basic, image: message },
      }))
    }
  }

  const goNext = () => {
    if (currentStep === 4) return

    if (currentStep === 1) {
      const basic = validateBasicInformation(snapshot, minimumDateTime, imageError)
      setErrors((current) => ({ ...current, basic }))
      if (hasErrors(basic)) return
    }
    if (currentStep === 2) {
      const venue = validateVenue(snapshot)
      setErrors((current) => ({ ...current, venue }))
      if (hasErrors(venue)) return
    }
    if (currentStep === 3) {
      const tools = validateEventTools(snapshot)
      setErrors((current) => ({ ...current, tools }))
      if (hasErrors(tools)) return
    }

    setCurrentStep((currentStep + 1) as EventWizardStep)
  }

  const goBack = () => {
    if (currentStep === 1) {
      onCancel()
      return
    }
    setCurrentStep((currentStep - 1) as EventWizardStep)
  }

  const handleSubmit = async (submission: FormEvent<HTMLFormElement>) => {
    submission.preventDefault()
    if (currentStep !== 4) return

    const allErrors = validateEventWizard(snapshot, minimumDateTime, imageError)
    setErrors(allErrors)
    const invalidStep = firstInvalidStep(allErrors)
    if (invalidStep) {
      setCurrentStep(invalidStep)
      return
    }

    setUploading(true)
    setImageError(null)
    try {
      const payload = buildCreateEventPayload(snapshot, values.imageUrl)
      await onSubmit(payload, imageFile)
    } catch (caught) {
      const message = caught instanceof Error
        ? caught.message
        : 'The event could not be created.'
      setImageError(message)
    } finally {
      setUploading(false)
    }
  }

  const disabled = busy || uploading

  return (
    <Form
      className="event-creation-wizard"
      noValidate
      aria-busy={disabled}
      onSubmit={(submission) => void handleSubmit(submission)}
    >
      <EventWizardProgress currentStep={currentStep} />
      {(error || imageError) && <Alert variant="danger" role="alert">{error ?? imageError}</Alert>}

      {currentStep === 1 && (
        <BasicInformationStep
          values={values}
          eventDate={eventDate}
          eventTime={eventTime}
          eventEndDate={eventEndDate}
          eventEndTime={eventEndTime}
          imagePreview={imagePreview}
          errors={errors.basic}
          disabled={disabled}
          minimumDate={minimumDateTime.slice(0, 10)}
          onValuesChange={updateValues}
          onEventDateChange={handleDateChange}
          onEventTimeChange={handleTimeChange}
          onEventEndDateChange={(date) => { setEventEndDate(date); updateValues({ endDate: combineDateTime(date, eventEndTime) }) }}
          onEventEndTimeChange={(time) => { setEventEndTime(time); updateValues({ endDate: combineDateTime(eventEndDate, time) }) }}
          onImageChange={handleImageChange}
        />
      )}

      {currentStep === 2 && (
        <VenueStep
          values={values}
          errors={errors.venue}
          disabled={disabled}
          onValuesChange={updateValues}
        />
      )}

      {currentStep === 3 && (
        <EventToolsStep
          values={values}
          registrationMode={registrationMode}
          errors={errors.tools}
          disabled={disabled}
          onRegistrationModeChange={setRegistrationMode}
          onValuesChange={updateValues}
        />
      )}

      {currentStep === 4 && (
        <ReviewCreateStep
          values={values}
          registrationMode={registrationMode}
          imagePreview={imagePreview}
          busy={busy}
          uploading={uploading}
          confirmationDisabled={registrationMode === 'paid'}
          onEdit={setCurrentStep}
          onPublishedChange={(isPublished) => updateValues({ isPublished })}
          onBack={goBack}
        />
      )}

      {currentStep < 4 && (
        <div className="form-actions mt-4">
          <Button variant="light" onClick={goBack} disabled={disabled}>
            {currentStep === 1 ? 'Cancel' : 'Back'}
          </Button>
          <Button type="button" onClick={goNext} disabled={disabled}>
            Continue
          </Button>
        </div>
      )}
    </Form>
  )
}
