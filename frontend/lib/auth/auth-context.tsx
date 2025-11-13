"use client"

import type React from "react"
import { createContext, useContext, useState, useEffect, useCallback } from "react"
import {
  CognitoUserPool,
  CognitoUser,
  AuthenticationDetails,
  CognitoUserAttribute,
  CognitoRefreshToken,
} from "amazon-cognito-identity-js"

// Token storage keys
const ACCESS_TOKEN_KEY = "smartscheduler_access_token"
const REFRESH_TOKEN_KEY = "smartscheduler_refresh_token"
const ID_TOKEN_KEY = "smartscheduler_id_token"

// Cognito configuration from environment variables
const getUserPool = () => {
  const userPoolId = process.env.NEXT_PUBLIC_COGNITO_USER_POOL_ID
  const appClientId = process.env.NEXT_PUBLIC_COGNITO_APP_CLIENT_ID
  const region = process.env.NEXT_PUBLIC_COGNITO_REGION || "us-east-1"

  if (!userPoolId || !appClientId) {
    throw new Error(
      "Cognito configuration missing. Please set NEXT_PUBLIC_COGNITO_USER_POOL_ID and NEXT_PUBLIC_COGNITO_APP_CLIENT_ID environment variables."
    )
  }

  return new CognitoUserPool({
    UserPoolId: userPoolId,
    ClientId: appClientId,
  })
}

export interface AuthTokens {
  accessToken: string
  idToken: string
  refreshToken: string
}

export interface AuthContextType {
  isAuthenticated: boolean
  isLoading: boolean
  user: CognitoUser | null
  accessToken: string | null
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  changePassword: (oldPassword: string, newPassword: string) => Promise<void>
  handleNewPasswordRequired: (cognitoUser: CognitoUser, newPassword: string, userAttributes?: Record<string, string>) => Promise<void>
  refreshAccessToken: () => Promise<string | null>
  getTokenProvider: () => { getToken: () => string | null; refreshToken: () => Promise<string | null> } | null
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [user, setUser] = useState<CognitoUser | null>(null)
  const [accessToken, setAccessToken] = useState<string | null>(null)

  // Load tokens from sessionStorage on mount
  useEffect(() => {
    const storedAccessToken = sessionStorage.getItem(ACCESS_TOKEN_KEY)
    if (storedAccessToken) {
      setAccessToken(storedAccessToken)
      setIsAuthenticated(true)
      
      // Restore user object from stored tokens
      const userPool = getUserPool()
      const storedIdToken = sessionStorage.getItem(ID_TOKEN_KEY)
      if (storedIdToken) {
        try {
          // Extract username from ID token
          const payload = JSON.parse(atob(storedIdToken.split('.')[1]))
          const username = payload.email || payload['cognito:username']
          if (username) {
            const cognitoUser = new CognitoUser({
              Username: username,
              Pool: userPool,
            })
            setUser(cognitoUser)
          }
        } catch (error) {
          console.error("Failed to restore user from token:", error)
          // Clear invalid tokens
          clearTokens()
        }
      }
    }
    setIsLoading(false)
  }, [])

  const clearTokens = useCallback(() => {
    sessionStorage.removeItem(ACCESS_TOKEN_KEY)
    sessionStorage.removeItem(REFRESH_TOKEN_KEY)
    sessionStorage.removeItem(ID_TOKEN_KEY)
    setAccessToken(null)
    setIsAuthenticated(false)
    setUser(null)
  }, [])

  const storeTokens = useCallback((tokens: AuthTokens) => {
    sessionStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken)
    sessionStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken)
    sessionStorage.setItem(ID_TOKEN_KEY, tokens.idToken)
    setAccessToken(tokens.accessToken)
    setIsAuthenticated(true)
  }, [])

  const login = useCallback(
    async (email: string, password: string): Promise<void> => {
      return new Promise((resolve, reject) => {
        const userPool = getUserPool()
        const authenticationDetails = new AuthenticationDetails({
          Username: email,
          Password: password,
        })

        const cognitoUser = new CognitoUser({
          Username: email,
          Pool: userPool,
        })

        cognitoUser.authenticateUser(authenticationDetails, {
          onSuccess: (result) => {
            const tokens: AuthTokens = {
              accessToken: result.getAccessToken().getJwtToken(),
              idToken: result.getIdToken().getJwtToken(),
              refreshToken: result.getRefreshToken().getToken(),
            }
            storeTokens(tokens)
            setUser(cognitoUser)
            resolve()
          },
          onFailure: (err) => {
            clearTokens()
            reject(err)
          },
          newPasswordRequired: (userAttributes, requiredAttributes) => {
            // Store user temporarily for password change
            // The user object is already in the challenge state, ready for completeNewPasswordChallenge
            setUser(cognitoUser)
            // Reject with special error to indicate password change required
            // Include the user object in the error so we can use it directly
            const error = new Error("NEW_PASSWORD_REQUIRED") as any
            error.cognitoUser = cognitoUser
            error.userAttributes = userAttributes
            error.requiredAttributes = requiredAttributes
            reject(error)
          },
        })
      })
    },
    [storeTokens, clearTokens]
  )

  const handleNewPasswordRequired = useCallback(
    async (cognitoUser: CognitoUser, newPassword: string, userAttributes?: Record<string, string>): Promise<void> => {
      return new Promise((resolve, reject) => {
        // Use the existing CognitoUser object that's already in the challenge state
        // This is more efficient than re-authenticating
        cognitoUser.completeNewPasswordChallenge(
          newPassword,
          userAttributes || {}, // User attributes if required
          {
            onSuccess: (result) => {
              const tokens: AuthTokens = {
                accessToken: result.getAccessToken().getJwtToken(),
                idToken: result.getIdToken().getJwtToken(),
                refreshToken: result.getRefreshToken().getToken(),
              }
              storeTokens(tokens)
              setUser(cognitoUser)
              resolve()
            },
            onFailure: (err) => {
              clearTokens()
              reject(err)
            },
          }
        )
      })
    },
    [storeTokens, clearTokens]
  )

  const changePassword = useCallback(
    async (oldPassword: string, newPassword: string): Promise<void> => {
      return new Promise((resolve, reject) => {
        if (!user) {
          reject(new Error("User not authenticated"))
          return
        }

        user.changePassword(oldPassword, newPassword, (err, result) => {
          if (err) {
            reject(err)
            return
          }
          resolve()
        })
      })
    },
    [user]
  )

  const refreshAccessToken = useCallback(async (): Promise<string | null> => {
    return new Promise((resolve, reject) => {
      const storedRefreshToken = sessionStorage.getItem(REFRESH_TOKEN_KEY)
      if (!storedRefreshToken || !user) {
        resolve(null)
        return
      }

      // Create CognitoRefreshToken object from stored token string
      const refreshToken = new CognitoRefreshToken({ RefreshToken: storedRefreshToken })

      user.refreshSession(refreshToken, (err, session) => {
        if (err || !session) {
          clearTokens()
          resolve(null)
          return
        }

        const tokens: AuthTokens = {
          accessToken: session.getAccessToken().getJwtToken(),
          idToken: session.getIdToken().getJwtToken(),
          refreshToken: session.getRefreshToken().getToken(),
        }
        storeTokens(tokens)
        resolve(tokens.accessToken)
      })
    })
  }, [user, storeTokens, clearTokens])

  const logout = useCallback(async (): Promise<void> => {
    return new Promise((resolve) => {
      if (user) {
        user.signOut(() => {
          clearTokens()
          resolve()
        })
      } else {
        clearTokens()
        resolve()
      }
    })
  }, [user, clearTokens])

  // Token provider for API client integration
  const getTokenProvider = useCallback(() => {
    if (!isAuthenticated || !accessToken) {
      return null
    }
    return {
      getToken: () => accessToken,
      refreshToken: refreshAccessToken,
    }
  }, [isAuthenticated, accessToken, refreshAccessToken])

  // Proactive token refresh: refresh tokens before they expire (at 80% of lifetime)
  useEffect(() => {
    if (!accessToken || !isAuthenticated) {
      return
    }

    try {
      // Parse JWT to get expiration time
      const payload = JSON.parse(atob(accessToken.split('.')[1]))
      const exp = payload.exp
      if (!exp) {
        return
      }

      // Calculate expiration time (in milliseconds)
      const expirationTime = exp * 1000
      const now = Date.now()
      const timeUntilExpiry = expirationTime - now

      // Refresh at 80% of token lifetime (20% before expiration)
      const refreshTime = timeUntilExpiry * 0.8

      // Only set up refresh if token hasn't expired and refresh time is positive
      if (refreshTime > 0 && timeUntilExpiry > 0) {
        const timeoutId = setTimeout(async () => {
          try {
            await refreshAccessToken()
          } catch (error) {
            console.warn('Proactive token refresh failed:', error)
            // If refresh fails, clear tokens (user will need to log in again)
            clearTokens()
          }
        }, refreshTime)

        return () => clearTimeout(timeoutId)
      }
    } catch (error) {
      // If we can't parse the token, don't set up proactive refresh
      console.warn('Failed to parse token for proactive refresh:', error)
    }
  }, [accessToken, isAuthenticated, refreshAccessToken, clearTokens])

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        isLoading,
        user,
        accessToken,
        login,
        logout,
        changePassword,
        handleNewPasswordRequired,
        refreshAccessToken,
        getTokenProvider,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider")
  }
  return context
}

