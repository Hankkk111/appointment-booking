import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import BookingForm from '../components/BookingForm.jsx'
import api from '../api/axios'

vi.mock('../api/axios')

const services = [{ id: 1, name: 'Haircut', durationMinutes: 60, price: 45 }]
const providers = [{ id: 2, name: 'Jane Stylist' }]

// Formats a Date as the local "YYYY-MM-DDTHH:mm" value a datetime-local input holds.
function toDateTimeLocal(date) {
  const pad = (n) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

const tomorrowAtTen = () => {
  const d = new Date()
  d.setDate(d.getDate() + 1)
  d.setHours(10, 0, 0, 0)
  return toDateTimeLocal(d)
}

describe('BookingForm', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  it('shows a validation message when submitted with no fields filled in', async () => {
    render(<BookingForm services={services} providers={providers} onBooked={vi.fn()} />)

    fireEvent.click(screen.getByText('Book Appointment'))

    expect(await screen.findByText(/please choose a service/i)).toBeInTheDocument()
    expect(api.post).not.toHaveBeenCalled()
  })

  it('submits the booking with the selected values and calls onBooked on success', async () => {
    api.post.mockResolvedValueOnce({ data: { id: 1 } })
    const onBooked = vi.fn()

    render(<BookingForm services={services} providers={providers} onBooked={onBooked} />)

    fireEvent.change(screen.getByLabelText(/service/i), { target: { value: '1' } })
    fireEvent.change(screen.getByLabelText(/provider/i), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText(/date & time/i), {
      target: { value: tomorrowAtTen() },
    })

    fireEvent.click(screen.getByText('Book Appointment'))

    await waitFor(() => expect(api.post).toHaveBeenCalledTimes(1))

    const [, payload] = api.post.mock.calls[0]
    expect(payload.serviceId).toBe(1)
    expect(payload.providerId).toBe(2)
    expect(onBooked).toHaveBeenCalled()
  })

  it('surfaces the API conflict message when the slot is already booked', async () => {
    api.post.mockRejectedValueOnce({
      response: { status: 409, data: { message: 'Jane Stylist is already booked for that time slot.' } },
    })

    render(<BookingForm services={services} providers={providers} onBooked={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/service/i), { target: { value: '1' } })
    fireEvent.change(screen.getByLabelText(/provider/i), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText(/date & time/i), {
      target: { value: tomorrowAtTen() },
    })

    fireEvent.click(screen.getByText('Book Appointment'))

    expect(await screen.findByText(/already booked for that time slot/i)).toBeInTheDocument()
  })

  it('rejects a start time in the past without calling the API', async () => {
    const yesterday = new Date()
    yesterday.setDate(yesterday.getDate() - 1)

    render(<BookingForm services={services} providers={providers} onBooked={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/service/i), { target: { value: '1' } })
    fireEvent.change(screen.getByLabelText(/provider/i), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText(/date & time/i), {
      target: { value: toDateTimeLocal(yesterday) },
    })

    fireEvent.click(screen.getByText('Book Appointment'))

    expect(await screen.findByText(/start time in the future/i)).toBeInTheDocument()
    expect(api.post).not.toHaveBeenCalled()
  })
})
