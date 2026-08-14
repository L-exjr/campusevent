import { evaluatePasswordStrength } from '../../utils/passwordStrength'

export default function PasswordStrengthMeter({ password }: { password: string }) {
  if (!password) return null
  const { strength } = evaluatePasswordStrength(password)
  const value = strength === 'strong' ? 3 : strength === 'medium' ? 2 : 1
  return (
    <div className={`password-strength password-strength--${strength}`} aria-live="polite">
      <div className="password-strength__header">
        <span>Password strength</span><strong>{strength[0].toUpperCase() + strength.slice(1)}</strong>
      </div>
      <div className="password-strength__meter" role="progressbar" aria-label="Password strength"
        aria-valuemin={1} aria-valuemax={3} aria-valuenow={value} aria-valuetext={strength}>
        {[1, 2, 3].map((segment) => <span key={segment} className={segment <= value ? 'is-active' : ''} />)}
      </div>
      <small>Use 12+ characters with uppercase, lowercase, a number, and a symbol.</small>
    </div>
  )
}
