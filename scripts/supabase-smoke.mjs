import { createSupabaseClient } from '../src/api/supabaseClient.js'

const client = createSupabaseClient(
  process.env.VITE_SUPABASE_URL,
  process.env.VITE_SUPABASE_ANON_KEY,
)

for (const bucket of ['event-images', 'profile-images']) {
  const { error } = await client.storage.from(bucket).list('', { limit: 1 })
  if (error) {
    throw new Error(`${bucket} smoke check failed: ${error.message}`)
  }
  console.log(`✓ Connected to ${bucket}`)
}

console.log('Supabase Storage client initialized and both buckets are reachable.')
