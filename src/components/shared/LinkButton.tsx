import Button, { type ButtonProps } from 'react-bootstrap/Button'
import { useNavigate, type To } from 'react-router-dom'

interface LinkButtonProps extends Omit<ButtonProps, 'href' | 'onClick'> {
  to: To
  replace?: boolean
  state?: unknown
}

export default function LinkButton({ to, replace = false, state, ...props }: LinkButtonProps) {
  const navigate = useNavigate()
  return <Button {...props} onClick={() => navigate(to, { replace, state })} />
}
