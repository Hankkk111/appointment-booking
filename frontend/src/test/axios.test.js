import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import api from '../api/axios'

// A stub adapter runs the real interceptor chain without any network I/O:
// it either echoes the outgoing request config or fails with a given status.
const echoAdapter = (config) =>
  Promise.resolve({ data: null, status: 200, statusText: 'OK', headers: {}, config })

const failingAdapter = (status) => (config) =>
  Promise.reject(Object.assign(new Error(`Request failed with status ${status}`), {
    config,
    response: { status, data: {} },
  }))

function seedSession() {
  localStorage.setItem('token', 'jwt-123')
  localStorage.setItem('username', 'alice')
  localStorage.setItem('role', 'Customer')
}

describe('api client', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.stubGlobal('location', { pathname: '/', href: '/' })
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('attaches the stored JWT as a Bearer token', async () => {
    seedSession()

    const res = await api.get('/appointments', { adapter: echoAdapter })

    expect(res.config.headers.Authorization).toBe('Bearer jwt-123')
  })

  it('sends no Authorization header when nobody is logged in', async () => {
    const res = await api.get('/services', { adapter: echoAdapter })

    expect(res.config.headers.Authorization).toBeUndefined()
  })

  it('clears the session and redirects to /login on a 401', async () => {
    seedSession()

    await expect(api.get('/appointments', { adapter: failingAdapter(401) })).rejects.toMatchObject({
      response: { status: 401 },
    })

    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('username')).toBeNull()
    expect(localStorage.getItem('role')).toBeNull()
    expect(window.location.href).toBe('/login')
  })

  it('does not redirect again when the 401 happens on the login page', async () => {
    window.location.pathname = '/login'
    window.location.href = '/login?next=dashboard'

    await expect(api.post('/auth/login', {}, { adapter: failingAdapter(401) })).rejects.toBeTruthy()

    expect(window.location.href).toBe('/login?next=dashboard')
  })

  it('keeps the session for errors other than 401', async () => {
    seedSession()

    await expect(api.post('/appointments', {}, { adapter: failingAdapter(409) })).rejects.toMatchObject({
      response: { status: 409 },
    })

    expect(localStorage.getItem('token')).toBe('jwt-123')
    expect(window.location.href).toBe('/')
  })
})
