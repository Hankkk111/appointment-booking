import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import Dashboard from '../pages/Dashboard.jsx'
import { AuthProvider } from '../context/AuthContext.jsx'
import api from '../api/axios'

vi.mock('../api/axios')

const appointment = {
  id: 7,
  customerUsername: 'alice',
  serviceName: 'Haircut',
  providerName: 'Jane Stylist',
  startTime: '2030-01-15T10:00:00Z',
  status: 'Pending',
}

// Answers GET requests by path; `appointments` can be a function so a test
// can make a later refresh fail.
function mockGets({ appointments }) {
  api.get.mockImplementation((url) => {
    if (url === '/services') return Promise.resolve({ data: [] })
    if (url === '/providers') return Promise.resolve({ data: [] })
    if (url === '/appointments') {
      return typeof appointments === 'function' ? appointments() : Promise.resolve({ data: appointments })
    }
    return Promise.reject(new Error(`unexpected GET ${url}`))
  })
}

function renderDashboard() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <Dashboard />
      </AuthProvider>
    </MemoryRouter>
  )
}

describe('Dashboard', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    localStorage.setItem('token', 'jwt-123')
    localStorage.setItem('username', 'alice')
    localStorage.setItem('role', 'Customer')
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  afterEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  it('cancels an appointment and reloads the list', async () => {
    let calls = 0
    mockGets({
      appointments: () => {
        calls += 1
        const status = calls === 1 ? 'Pending' : 'Cancelled'
        return Promise.resolve({ data: [{ ...appointment, status }] })
      },
    })
    api.delete.mockResolvedValueOnce({})

    renderDashboard()
    fireEvent.click(await screen.findByRole('button', { name: 'Cancel' }))

    expect(api.delete).toHaveBeenCalledWith('/appointments/7')
    expect(await screen.findByText('Cancelled')).toBeInTheDocument()
  })

  it('shows an error when the list cannot be refreshed after cancelling', async () => {
    let calls = 0
    mockGets({
      appointments: () => {
        calls += 1
        return calls === 1
          ? Promise.resolve({ data: [appointment] })
          : Promise.reject({ response: { data: { message: 'Database unavailable' } } })
      },
    })
    api.delete.mockResolvedValueOnce({})

    renderDashboard()
    fireEvent.click(await screen.findByRole('button', { name: 'Cancel' }))

    expect(await screen.findByText('Database unavailable')).toBeInTheDocument()
  })

  it('does nothing when the user dismisses the confirmation', async () => {
    window.confirm.mockReturnValue(false)
    mockGets({ appointments: [appointment] })

    renderDashboard()
    fireEvent.click(await screen.findByRole('button', { name: 'Cancel' }))

    await waitFor(() => expect(window.confirm).toHaveBeenCalled())
    expect(api.delete).not.toHaveBeenCalled()
  })
})
