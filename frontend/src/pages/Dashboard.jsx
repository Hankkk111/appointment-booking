import { useCallback, useEffect, useState } from 'react'
import api from '../api/axios'
import Navbar from '../components/Navbar.jsx'
import BookingForm from '../components/BookingForm.jsx'
import AppointmentList from '../components/AppointmentList.jsx'
import { useAuth } from '../context/AuthContext.jsx'

export default function Dashboard() {
  const { isAdmin } = useAuth()
  const [services, setServices] = useState([])
  const [providers, setProviders] = useState([])
  const [appointments, setAppointments] = useState([])
  const [loading, setLoading] = useState(true)
  const [errorMsg, setErrorMsg] = useState('')

  const fetchReferenceData = useCallback(async () => {
    const [servicesRes, providersRes] = await Promise.all([
      api.get('/services'),
      api.get('/providers'),
    ])
    setServices(servicesRes.data)
    setProviders(providersRes.data)
  }, [])

  const fetchAppointments = useCallback(async () => {
    const { data } = await api.get('/appointments')
    setAppointments(data)
  }, [])

  const loadAll = useCallback(async () => {
    setLoading(true)
    setErrorMsg('')
    try {
      await Promise.all([fetchReferenceData(), fetchAppointments()])
    } catch (err) {
      setErrorMsg(err.response?.data?.message || 'Failed to load data')
    } finally {
      setLoading(false)
    }
  }, [fetchReferenceData, fetchAppointments])

  useEffect(() => {
    loadAll()
  }, [loadAll])

  const handleCancel = async (appointment) => {
    if (!window.confirm(`Cancel the ${appointment.serviceName} appointment with ${appointment.providerName}?`)) {
      return
    }
    setErrorMsg('')
    try {
      await api.delete(`/appointments/${appointment.id}`)
      // Awaited so a failed refresh lands in the catch below instead of
      // becoming an unhandled rejection the user never sees.
      await fetchAppointments()
    } catch (err) {
      setErrorMsg(err.response?.data?.message || 'Failed to cancel appointment')
    }
  }

  return (
    <div className="dashboard">
      <Navbar />

      <main className="dashboard-content">
        {errorMsg && <div className="alert-error">{errorMsg}</div>}

        {loading ? (
          <div className="loading-state">Loading...</div>
        ) : (
          <>
            <BookingForm services={services} providers={providers} onBooked={fetchAppointments} />

            <div className="section-header">
              <h1>{isAdmin ? 'All appointments' : 'My appointments'}</h1>
            </div>

            <AppointmentList
              appointments={appointments}
              onCancel={handleCancel}
              showCustomer={isAdmin}
            />
          </>
        )}
      </main>
    </div>
  )
}
