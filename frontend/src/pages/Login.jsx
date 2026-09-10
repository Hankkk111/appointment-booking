import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext.jsx'

export default function Login() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ username: '', password: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const handleChange = (e) => setForm({ ...form, [e.target.name]: e.target.value })

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login(form)
      navigate('/')
    } catch (err) {
      setError(err.response?.data?.message || 'Login failed, please check your username and password')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <h1>Appointment Booking</h1>
        <p className="subtitle">Sign in to your account</p>

        {error && <div className="alert-error">{error}</div>}

        <label>
          Username
          <input name="username" value={form.username} onChange={handleChange} required />
        </label>

        <label>
          Password
          <input type="password" name="password" value={form.password} onChange={handleChange} required />
        </label>

        <button type="submit" disabled={loading}>
          {loading ? 'Signing in...' : 'Sign In'}
        </button>

        <p className="switch-link">
          Don't have an account? <Link to="/register">Register</Link>
        </p>
      </form>
    </div>
  )
}
