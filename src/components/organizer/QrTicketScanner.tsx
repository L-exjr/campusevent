import { useEffect, useRef, useState } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Form from 'react-bootstrap/Form'
import type { IScannerControls } from '@zxing/browser'

interface QrTicketScannerProps {
  busy: boolean
  onToken: (token: string) => Promise<void>
}

export default function QrTicketScanner({ busy, onToken }: QrTicketScannerProps) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const controlsRef = useRef<IScannerControls | null>(null)
  const [active, setActive] = useState(false)
  const [manualToken, setManualToken] = useState('')
  const [cameraError, setCameraError] = useState<string | null>(null)

  const stop = () => {
    controlsRef.current?.stop()
    controlsRef.current = null
    setActive(false)
  }

  useEffect(() => () => controlsRef.current?.stop(), [])

  const start = async () => {
    setCameraError(null)
    setActive(true)
    try {
      const { BrowserQRCodeReader } = await import('@zxing/browser')
      const reader = new BrowserQRCodeReader()
      controlsRef.current = await reader.decodeFromConstraints(
        { video: { facingMode: { ideal: 'environment' } } },
        videoRef.current!,
        (result) => {
          if (!result) return
          const token = result.getText()
          stop()
          void onToken(token)
        },
      )
    } catch {
      setActive(false)
      setCameraError('Camera access failed. Allow camera permission or use the manual token field.')
    }
  }

  return (
    <div>
      {cameraError && <Alert variant="warning">{cameraError}</Alert>}
      <div className="ratio ratio-4x3 bg-dark rounded overflow-hidden mb-3">
        <video
          ref={videoRef}
          className="w-100 h-100 object-fit-cover"
          muted
          playsInline
          aria-label="QR ticket camera preview"
        />
        {!active && (
          <div className="d-flex align-items-center justify-content-center text-white text-center p-4">
            Camera preview appears here after you start scanning.
          </div>
        )}
      </div>
      <div className="d-grid gap-2 d-sm-flex mb-4">
        {active ? (
          <Button variant="outline-danger" onClick={stop} disabled={busy}>Stop camera</Button>
        ) : (
          <Button onClick={() => void start()} disabled={busy}>Start camera scanner</Button>
        )}
      </div>
      <Form.Group controlId="manual-ticket-token">
        <Form.Label>Manual ticket token</Form.Label>
        <Form.Control
          as="textarea"
          rows={3}
          value={manualToken}
          onChange={(event) => setManualToken(event.target.value)}
          placeholder="Paste the signed token if the camera cannot scan"
          disabled={busy}
        />
      </Form.Group>
      <Button
        className="mt-2"
        variant="outline-primary"
        disabled={busy || !manualToken.trim()}
        onClick={() => void onToken(manualToken.trim()).then(() => setManualToken(''))}
      >
        {busy ? 'Checking in…' : 'Check in token'}
      </Button>
    </div>
  )
}
