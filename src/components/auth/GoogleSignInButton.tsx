import { useEffect, useRef } from 'react'

declare global {
  interface Window {
    google?: {
      accounts: { id: {
        initialize: (options: { client_id: string; callback: (response: { credential: string }) => void }) => void
        renderButton: (parent: HTMLElement, options: Record<string, unknown>) => void
      } }
    }
  }
}

interface Props {
  onCredential: (credential: string) => void
  onUnavailable: (message: string) => void
}

export default function GoogleSignInButton({ onCredential, onUnavailable }: Props) {
  const container = useRef<HTMLDivElement>(null)
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined

  useEffect(() => {
    if (!clientId) return
    const render = () => {
      if (!window.google || !container.current) return
      window.google.accounts.id.initialize({
        client_id: clientId,
        callback: ({ credential }) => onCredential(credential),
      })
      container.current.replaceChildren()
      window.google.accounts.id.renderButton(container.current, {
        theme: 'outline', size: 'large', width: container.current.clientWidth,
      })
    }
    const existing = document.querySelector<HTMLScriptElement>('script[data-google-identity]')
    if (existing) {
      if (window.google) render()
      else existing.addEventListener('load', render, { once: true })
      return () => existing.removeEventListener('load', render)
    }
    const script = document.createElement('script')
    script.src = 'https://accounts.google.com/gsi/client'
    script.async = true
    script.defer = true
    script.dataset.googleIdentity = 'true'
    script.onload = render
    script.onerror = () => onUnavailable('Google sign-in could not be loaded.')
    document.head.append(script)
  }, [clientId, onCredential, onUnavailable])

  if (!clientId) return null
  return <div ref={container} className="w-100 d-flex justify-content-center" aria-label="Sign in with Google" />
}
