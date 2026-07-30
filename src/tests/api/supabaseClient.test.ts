import { createSupabaseClient } from '../../api/supabaseClient'

describe('Supabase client configuration', () => {
  it('initializes the storage client from supplied browser configuration', () => {
    const client = createSupabaseClient(
      'https://example-project.supabase.co',
      'example-anon-key',
    )

    expect(client.storage.from('event-images')).toBeDefined()
  })

  it('fails clearly when browser configuration is missing', () => {
    expect(() => createSupabaseClient('', '')).toThrow(
      'Set VITE_SUPABASE_URL and VITE_SUPABASE_ANON_KEY',
    )
  })
})
