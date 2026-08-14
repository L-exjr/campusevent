import { apiRequest } from './httpClient'

export const IMAGE_ACCEPT = 'image/jpeg,image/png,image/webp'
export const MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024
export const DEFAULT_EVENT_IMAGE =
  'data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%221200%22 height=%22675%22 viewBox=%220 0 1200 675%22%3E%3Crect width=%221200%22 height=%22675%22 fill=%22%23e9ecef%22/%3E%3Cpath d=%22M0 520L310 260l210 190 160-130 520 355H0z%22 fill=%22%23ced4da%22/%3E%3Ccircle cx=%22940%22 cy=%22200%22 r=%2280%22 fill=%22%23adb5bd%22/%3E%3Ctext x=%22600%22 y=%22620%22 text-anchor=%22middle%22 font-family=%22sans-serif%22 font-size=%2240%22 fill=%22%23495057%22%3ECampus event%3C/text%3E%3C/svg%3E'
export const DEFAULT_PROFILE_IMAGE =
  'data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%22400%22 height=%22400%22 viewBox=%220 0 400 400%22%3E%3Crect width=%22400%22 height=%22400%22 rx=%22200%22 fill=%22%23e9ecef%22/%3E%3Ccircle cx=%22200%22 cy=%22145%22 r=%2275%22 fill=%22%23adb5bd%22/%3E%3Cpath d=%22M65 370c18-92 68-138 135-138s117 46 135 138%22 fill=%22%23adb5bd%22/%3E%3C/svg%3E'

const UPLOAD_ENDPOINTS = {
  'event-images': '/uploads/event-image',
  'profile-images': '/uploads/profile-image',
  'organizer-banners': '/uploads/organizer-banner',
}

const ALLOWED_TYPES = {
  'image/jpeg': 'jpg',
  'image/png': 'png',
  'image/webp': 'webp',
}

export function validateImageFile(file) {
  if (!(file instanceof File)) throw new Error('Choose an image file.')
  if (!Object.hasOwn(ALLOWED_TYPES, file.type)) {
    throw new Error('Choose a JPG, PNG, or WebP image.')
  }
  if (file.size > MAX_IMAGE_SIZE_BYTES) {
    throw new Error('Images must be 5 MB or smaller.')
  }
}

/**
 * @param {File} file
 * @param {keyof typeof UPLOAD_ENDPOINTS} bucket
 * @param {string | null} [eventId]
 */
export async function uploadImage(file, bucket, eventId = null) {
  // This is UX-only validation. The authenticated backend repeats validation
  // and performs the Supabase write with its server-only service-role key.
  validateImageFile(file)
  const endpoint = UPLOAD_ENDPOINTS[bucket]
  if (!endpoint) throw new Error('Unsupported image bucket.')

  const body = new FormData()
  body.append('file', file)
  const requestEndpoint = bucket === 'event-images' && eventId
    ? `${endpoint}?eventId=${encodeURIComponent(eventId)}`
    : endpoint
  const response = await apiRequest(requestEndpoint, {
    method: 'POST',
    body,
  })
  if (!response?.url) throw new Error('The API did not return an image URL.')
  return response.url
}
