import { useState } from 'react'
import Button from 'react-bootstrap/Button'
import Form from 'react-bootstrap/Form'
import InputGroup from 'react-bootstrap/InputGroup'

interface PasswordInputProps {
  id: string
  value: string
  onChange: (value: string) => void
  autoComplete: string
  minLength?: number
  required?: boolean
}

export default function PasswordInput({
  id, value, onChange, autoComplete, minLength, required = true,
}: PasswordInputProps) {
  const [visible, setVisible] = useState(false)
  const label = visible ? 'Hide password' : 'Show password'
  return (
    <InputGroup>
      <Form.Control
        id={id}
        type={visible ? 'text' : 'password'}
        required={required}
        minLength={minLength}
        autoComplete={autoComplete}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
      <Button
        variant="outline-secondary"
        type="button"
        aria-label={label}
        aria-pressed={visible}
        onClick={() => setVisible((current) => !current)}
      >
        {visible ? (
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="m3 3 18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.9 4.2A10.8 10.8 0 0 1 12 4c5.5 0 9 8 9 8a18 18 0 0 1-2 3.2M6.6 6.6C4.3 8.1 3 12 3 12s3.5 8 9 8a9 9 0 0 0 4-.9"/></svg>
        ) : (
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M3 12s3.5-8 9-8 9 8 9 8-3.5 8-9 8-9-8-9-8Z"/><circle cx="12" cy="12" r="3"/></svg>
        )}
      </Button>
    </InputGroup>
  )
}
