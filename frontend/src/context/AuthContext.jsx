import { createContext, useContext, useState, useCallback } from 'react'
import api from '../api/axios'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [token, setToken] = useState(localStorage.getItem('token'))
  const [username, setUsername] = useState(localStorage.getItem('username'))
  const [role, setRole] = useState(localStorage.getItem('role'))

  const login = useCallback(async (credentials) => {
    const { data } = await api.post('/auth/login', credentials)
    localStorage.setItem('token', data.token)
    localStorage.setItem('username', data.username)
    localStorage.setItem('role', data.role)
    setToken(data.token)
    setUsername(data.username)
    setRole(data.role)
    return data
  }, [])

  const register = useCallback(async (payload) => {
    const { data } = await api.post('/auth/register', payload)
    localStorage.setItem('token', data.token)
    localStorage.setItem('username', data.username)
    localStorage.setItem('role', data.role)
    setToken(data.token)
    setUsername(data.username)
    setRole(data.role)
    return data
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('token')
    localStorage.removeItem('username')
    localStorage.removeItem('role')
    setToken(null)
    setUsername(null)
    setRole(null)
  }, [])

  const value = {
    token,
    username,
    role,
    isAdmin: role === 'Admin',
    isAuthenticated: !!token,
    login,
    register,
    logout,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
