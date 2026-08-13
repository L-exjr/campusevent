import Button, { type ButtonProps } from 'react-bootstrap/Button'
import { useNavigate, type To } from 'react-router-dom'

interface LinkButtonProps extends Omit<ButtonProps, 'href'> {
  to: To
  replace?: boolean
  state?: unknown
}

export default function LinkButton({ to, replace = false, state, onClick, ...props }: LinkButtonProps) {
  const navigate = useNavigate()
  return <Button {...props} onClick={(event) => { onClick?.(event); if (!event.defaultPrevented) navigate(to, { replace, state }) }} />
}
