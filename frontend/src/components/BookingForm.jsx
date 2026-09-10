import { useState } from 'react'
import api from '../api/axios'

export default function BookingForm({ services, providers, onBooked }) {
  const [form, setForm] = useState({ serviceId: '', providerId: '', startTime: '', notes: '' })
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value })
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setSuccess('')

    if (!form.serviceId || !form.providerId || !form.startTime) {
      setError('Please choose a service, a provider, and a start time.')
      return
    }

    setSubmitting(true)
    try {
      await api.post('/appointments', {
        serviceId: Number(form.serviceId),
        providerId: Number(form.providerId),
        startTime: new Date(form.startTime).toISOString(),
        notes: form.notes || null,
      })
      setSuccess('Appointment booked!')
      setForm({ serviceId: '', providerId: '', startTime: '', notes: '' })
      onBooked()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to book appointment')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="booking-form" onSubmit={handleSubmit}>
      <h2>Book an appointment</h2>

      {error && <div className="alert-error">{error}</div>}
      {success && <div className="alert-success">{success}</div>}

      <div className="form-row">
        <label>
          Service
          <select name="serviceId" value={form.serviceId} onChange={handleChange}>
            <option value="">Select a service...</option>
            {services.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name} ({s.durationMinutes} min, ${Number(s.price).toFixed(2)})
              </option>
            ))}
          </select>
        </label>

        <label>
          Provider
          <select name="providerId" value={form.providerId} onChange={handleChange}>
            <option value="">Select a provider...</option>
            {providers.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      <label>
        Date &amp; time
        <input
          type="datetime-local"
          name="startTime"
          value={form.startTime}
          onChange={handleChange}
        />
      </label>

      <label>
        Notes (optional)
        <textarea name="notes" value={form.notes} onChange={handleChange} rows={2} />
      </label>

      <button type="submit" disabled={submitting}>
        {submitting ? 'Booking...' : 'Book Appointment'}
      </button>
    </form>
  )
}
