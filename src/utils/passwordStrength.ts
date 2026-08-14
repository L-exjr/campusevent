export type PasswordStrength = 'weak' | 'medium' | 'strong'

export function evaluatePasswordStrength(password: string) {
  let score = 0
  if (password.length >= 8) score += 1
  if (password.length >= 12) score += 1
  if (/[a-z]/.test(password) && /[A-Z]/.test(password)) score += 1
  if (/\d/.test(password)) score += 1
  if (/[^A-Za-z0-9]/.test(password)) score += 1

  const strength: PasswordStrength = score >= 4 ? 'strong' : score >= 2 ? 'medium' : 'weak'
  return { score, strength }
}
