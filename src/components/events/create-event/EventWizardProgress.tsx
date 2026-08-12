export type EventWizardStep = 1 | 2 | 3 | 4

interface EventWizardProgressProps {
  currentStep: EventWizardStep
}

const steps: Array<{ number: EventWizardStep; label: string }> = [
  { number: 1, label: 'Basic information' },
  { number: 2, label: 'Venue' },
  { number: 3, label: 'Event tools' },
  { number: 4, label: 'Review' },
]

export default function EventWizardProgress({ currentStep }: EventWizardProgressProps) {
  return (
    <ol className="form-progress" aria-label="Create event progress">
      {steps.map((step) => (
        <li
          key={step.number}
          className={
            step.number === currentStep
              ? 'is-active'
              : step.number < currentStep
                ? 'is-complete'
                : ''
          }
          aria-current={step.number === currentStep ? 'step' : undefined}
        >
          <span>{step.number}</span>
          {step.label}
        </li>
      ))}
    </ol>
  )
}
