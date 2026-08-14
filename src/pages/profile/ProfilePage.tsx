import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import Alert from 'react-bootstrap/Alert'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import Col from 'react-bootstrap/Col'
import Form from 'react-bootstrap/Form'
import Row from 'react-bootstrap/Row'
import {
  DEFAULT_PROFILE_IMAGE,
  IMAGE_ACCEPT,
  uploadImage,
  validateImageFile,
} from '../../api/imageStorage'
import PageHeader from '../../components/shared/PageHeader'
import NotificationToast from '../../components/shared/NotificationToast'
import { useAuth } from '../../hooks/useAuth'
import { api } from '../../api'
import { EVENT_CATEGORIES, type OrganizerDirectorySettings } from '../../types'

export default function ProfilePage() {
  const { user, updateProfileImage } = useAuth()
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imageUrl, setImageUrl] = useState<string | null>(user?.imageUrl ?? null)
  const [preview, setPreview] = useState(user?.imageUrl ?? DEFAULT_PROFILE_IMAGE)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [directory, setDirectory] = useState<OrganizerDirectorySettings | null>(null)
  const [bannerFile, setBannerFile] = useState<File | null>(null)

  useEffect(() => { if (user && user.role !== 'admin') void api.getOrganizerDirectorySettings().then(setDirectory).catch(caught => setError(caught instanceof Error ? caught.message : 'Directory settings could not be loaded.')) }, [user])

  useEffect(() => () => {
    if (preview.startsWith('blob:')) URL.revokeObjectURL(preview)
  }, [preview])

  const selectImage = (change: ChangeEvent<HTMLInputElement>) => {
    const file = change.target.files?.[0]
    setError(null)
    setSaved(false)
    if (!file) return
    try {
      validateImageFile(file)
      setImageFile(file)
      setPreview(URL.createObjectURL(file))
    } catch (caught) {
      change.target.value = ''
      setImageFile(null)
      setError(caught instanceof Error ? caught.message : 'Choose a valid image.')
    }
  }

  const removeImage = () => {
    setImageFile(null)
    setImageUrl(null)
    setPreview(DEFAULT_PROFILE_IMAGE)
    setError(null)
    setSaved(false)
  }

  const saveProfile = async (submission: FormEvent<HTMLFormElement>) => {
    submission.preventDefault()
    setBusy(true)
    setError(null)
    setSaved(false)
    try {
      const uploadedUrl = imageFile
        ? await uploadImage(imageFile, 'profile-images')
        : imageUrl
      await updateProfileImage(uploadedUrl)
      setImageUrl(uploadedUrl)
      setImageFile(null)
      setPreview(uploadedUrl ?? DEFAULT_PROFILE_IMAGE)
      setSaved(true)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Your profile picture could not be saved.')
    } finally {
      setBusy(false)
    }
  }

  const saveDirectory = async (submission: FormEvent<HTMLFormElement>) => {
    submission.preventDefault(); if (!directory) return
    setBusy(true); setError(null); setSaved(false)
    try {
      const bannerUrl = bannerFile ? await uploadImage(bannerFile, 'organizer-banners') : directory.bannerUrl
      const updated = await api.updateOrganizerDirectorySettings({ ...directory, bannerUrl })
      setDirectory(updated); setBannerFile(null); setSaved(true)
    } catch (caught) { setError(caught instanceof Error ? caught.message : 'Directory settings could not be saved.') } finally { setBusy(false) }
  }

  return (
    <>
      <PageHeader
        eyebrow="Your account"
        title="Profile"
        description="Choose the picture shown with your Campus Events account."
      />
      <Row className="justify-content-center">
        <Col lg={8}>
          <Card className="border-0">
            <Card.Body className="p-4 p-md-5">
              <NotificationToast message={saved ? 'Profile picture updated.' : null} onClose={() => setSaved(false)} />
              {error && <Alert variant="danger">{error}</Alert>}
              <Form onSubmit={(submission) => void saveProfile(submission)}>
                <div className="d-flex flex-column flex-md-row gap-4 align-items-md-center">
                  <img
                    src={preview}
                    alt="Profile preview"
                    className="rounded-circle border object-fit-cover"
                    style={{ width: 180, height: 180 }}
                  />
                  <div className="flex-grow-1">
                    <h2 className="h4">{user?.name}</h2>
                    <p className="text-secondary">{user?.email}</p>
                    <Form.Group controlId="profile-image">
                      <Form.Label>Profile picture</Form.Label>
                      <Form.Control
                        type="file"
                        accept={IMAGE_ACCEPT}
                        onChange={selectImage}
                        disabled={busy}
                      />
                      <Form.Text>Optional. JPG, PNG, or WebP; maximum 5 MB.</Form.Text>
                    </Form.Group>
                    <div className="d-flex flex-wrap gap-2 mt-3">
                      <Button type="submit" disabled={busy}>
                        {busy ? 'Uploading…' : 'Save profile picture'}
                      </Button>
                      {(imageUrl || imageFile) && (
                        <Button type="button" variant="light" onClick={removeImage} disabled={busy}>
                          Remove picture
                        </Button>
                      )}
                    </div>
                  </div>
                </div>
              </Form>
            </Card.Body>
          </Card>
        </Col>
      </Row>
      {user?.role !== 'admin' && directory && <Row className="justify-content-center mt-4"><Col lg={8}><Card className="border-0"><Card.Body className="p-4 p-md-5"><h2 className="h3">Public Organizer directory</h2><p className="text-secondary">Opt in after creating an event. Email and phone are never shown.</p><Form onSubmit={event => void saveDirectory(event)}>
        <Form.Check type="switch" id="directory-visible" className="mb-3" label="Show my profile in the public directory" checked={directory.isVisible} onChange={event => setDirectory({ ...directory, isVisible: event.target.checked })} />
        <Form.Group className="mb-3" controlId="directory-bio"><Form.Label>Public bio</Form.Label><Form.Control as="textarea" rows={5} maxLength={3000} value={directory.bio ?? ''} onChange={event => setDirectory({ ...directory, bio: event.target.value })} /></Form.Group>
        <Form.Group className="mb-3" controlId="directory-banner"><Form.Label>Banner image</Form.Label><Form.Control type="file" accept={IMAGE_ACCEPT} onChange={event => setBannerFile((event.target as HTMLInputElement).files?.[0] ?? null)} /></Form.Group>
        <Form.Label>Specialties</Form.Label><Row className="g-2 mb-3">{EVENT_CATEGORIES.map(category => <Col sm={6} key={category}><Form.Check id={`specialty-${category}`} label={category} checked={directory.specialties.includes(category)} onChange={event => setDirectory({ ...directory, specialties: event.target.checked ? [...directory.specialties, category] : directory.specialties.filter(item => item !== category) })} /></Col>)}</Row>
        <Row className="g-3">{(['instagramUrl', 'twitterUrl', 'facebookUrl', 'websiteUrl'] as const).map(field => <Col md={6} key={field}><Form.Group controlId={`directory-${field}`}><Form.Label>{field.replace('Url', '').replace(/^./, char => char.toUpperCase())}</Form.Label><Form.Control type="url" maxLength={2048} value={directory[field] ?? ''} onChange={event => setDirectory({ ...directory, [field]: event.target.value })} /></Form.Group></Col>)}</Row>
        <Button className="mt-4" type="submit" disabled={busy}>{busy ? 'Saving…' : 'Save directory settings'}</Button>
      </Form></Card.Body></Card></Col></Row>}
    </>
  )
}
