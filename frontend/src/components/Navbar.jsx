import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext.jsx'

export default function Navbar() {
  const { username, role, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <nav className="navbar">
      <div className="navbar-brand">📅 Appointment Booking</div>
      <div className="navbar-user">
        <span>Hi, {username} {role === 'Admin' && <span className="badge">Admin</span>}</span>
        <button onClick={handleLogout}>Log out</button>
      </div>
    </nav>
  )
}
