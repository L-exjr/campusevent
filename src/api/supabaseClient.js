import { createClient } from '@supabase/supabase-js'

let client

export function createSupabaseClient(
  projectUrl = import.meta.env?.VITE_SUPABASE_URL,
  anonKey = import.meta.env?.VITE_SUPABASE_ANON_KEY,
) {
  if (!projectUrl || !anonKey) {
    throw new Error('Supabase is not configured. Set VITE_SUPABASE_URL and VITE_SUPABASE_ANON_KEY.')
  }
  return createClient(projectUrl, anonKey, {
    auth: {
      persistSession: false,
      autoRefreshToken: false,
      detectSessionInUrl: false,
    },
  })
}

export function getSupabaseClient() {
  client ??= createSupabaseClient()
  return client
}
