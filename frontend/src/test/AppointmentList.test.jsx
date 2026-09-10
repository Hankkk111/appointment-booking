import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import AppointmentList from '../components/AppointmentList.jsx'

const baseAppointments = [
  {
    id: 1,
    customerUsername: 'alice',
    serviceName: 'Haircut',
    providerName: 'Jane Stylist',
    startTime: '2026-10-01T10:00:00Z',
    status: 'Pending',
  },
  {
    id: 2,
    customerUsername: 'bob',
    serviceName: 'Consultation',
    providerName: 'Bob Stylist',
    startTime: '2026-10-02T14:00:00Z',
    status: 'Completed',
  },
  {
    id: 3,
    customerUsername: 'carol',
    serviceName: 'Check-up',
    providerName: 'Jane Stylist',
    startTime: '2026-10-03T09:00:00Z',
    status: 'Cancelled',
  },
]

describe('AppointmentList', () => {
  it('shows an empty-state message when there are no appointments', () => {
    render(<AppointmentList appointments={[]} onCancel={vi.fn()} />)
    expect(screen.getByText(/no appointments yet/i)).toBeInTheDocument()
  })

  it('renders a row for every appointment with its status badge', () => {
    render(<AppointmentList appointments={baseAppointments} onCancel={vi.fn()} />)

    expect(screen.getByText('Haircut')).toBeInTheDocument();
    expect(screen.getByText('Pending')).toBeInTheDocument();
    expect(screen.getByText('Completed')).toBeInTheDocument();
    expect(screen.getByText('Cancelled')).toBeInTheDocument();
  })

  it('only shows a Cancel button for appointments that are still active', () => {
    render(<AppointmentList appointments={baseAppointments} onCancel={vi.fn()} />)

    // Only the "Pending" appointment (id 1) should have a Cancel button --
    // Completed and Cancelled appointments should not.
    const cancelButtons = screen.getAllByText('Cancel')
    expect(cancelButtons).toHaveLength(1)
  })

  it('calls onCancel with the clicked appointment', () => {
    const onCancel = vi.fn()
    render(<AppointmentList appointments={baseAppointments} onCancel={onCancel} />)

    fireEvent.click(screen.getByText('Cancel'))

    expect(onCancel).toHaveBeenCalledWith(baseAppointments[0])
  })

  it('shows the Customer column only when showCustomer is true', () => {
    const { rerender } = render(
      <AppointmentList appointments={baseAppointments} onCancel={vi.fn()} showCustomer={false} />
    )
    expect(screen.queryByText('alice')).not.toBeInTheDocument()

    rerender(<AppointmentList appointments={baseAppointments} onCancel={vi.fn()} showCustomer={true} />)
    expect(screen.getByText('alice')).toBeInTheDocument()
  })
})
