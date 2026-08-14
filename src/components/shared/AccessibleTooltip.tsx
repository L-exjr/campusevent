import { cloneElement, useEffect, useId, useState, type ReactElement } from 'react'
import Overlay from 'react-bootstrap/Overlay'
import Tooltip from 'react-bootstrap/Tooltip'

interface AccessibleTooltipProps {
  label: string
  children: ReactElement<Record<string, unknown>>
  placement?: 'top' | 'right' | 'bottom' | 'left'
}

export default function AccessibleTooltip({
  label,
  children,
  placement = 'top',
}: AccessibleTooltipProps) {
  const id = useId()
  const [target, setTarget] = useState<HTMLElement | null>(null)
  const [show, setShow] = useState(false)

  useEffect(() => {
    if (!show) return
    const dismiss = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setShow(false)
    }
    document.addEventListener('keydown', dismiss)
    return () => document.removeEventListener('keydown', dismiss)
  }, [show])

  const child = cloneElement(children, {
    ref: setTarget,
    'aria-describedby': show ? id : undefined,
    onMouseEnter: () => setShow(true),
    onMouseLeave: () => setShow(false),
    onFocus: () => setShow(true),
    onBlur: () => setShow(false),
  })

  return (
    <>
      {child}
      <Overlay target={target} show={show} placement={placement}>
        {(props) => <Tooltip id={id} {...props}>{label}</Tooltip>}
      </Overlay>
    </>
  )
}
