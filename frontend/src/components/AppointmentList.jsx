const STATUS_CLASS = {
  Pending: 'status-pending',
  Confirmed: 'status-confirmed',
  Completed: 'status-completed',
  Cancelled: 'status-cancelled',
}

function formatDateTime(iso) {
  return new Date(iso).toLocaleString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export default function AppointmentList({ appointments, onCancel, showCustomer }) {
  if (appointments.length === 0) {
    return <div className="empty-state">No appointments yet. Book one above to get started.</div>
  }

  return (
    <table className="appointment-table">
      <thead>
        <tr>
          {showCustomer && <th>Customer</th>}
          <th>Service</th>
          <th>Provider</th>
          <th>When</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        {appointments.map((a) => (
          <tr key={a.id}>
            {showCustomer && <td>{a.customerUsername}</td>}
            <td>{a.serviceName}</td>
            <td>{a.providerName}</td>
            <td>{formatDateTime(a.startTime)}</td>
            <td>
              <span className={`status-badge ${STATUS_CLASS[a.status] || ''}`}>{a.status}</span>
            </td>
            <td>
              {a.status !== 'Cancelled' && a.status !== 'Completed' && (
                <button className="btn-link danger" onClick={() => onCancel(a)}>
                  Cancel
                </button>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
