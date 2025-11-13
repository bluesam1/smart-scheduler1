/**
 * Utility functions for checking user roles from JWT token.
 */

/**
 * Decodes a JWT token and returns the payload.
 */
function decodeJwtToken(token: string): any {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) {
      return null
    }
    const payload = parts[1]
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(decoded)
  } catch (error) {
    console.error('Error decoding JWT token:', error)
    return null
  }
}

/**
 * Gets the user's roles from the JWT token.
 * Roles are typically in the 'groups' or 'cognito:groups' claim.
 */
export function getUserRoles(token: string | null): string[] {
  if (!token) {
    return []
  }

  const payload = decodeJwtToken(token)
  if (!payload) {
    return []
  }

  // Check for 'groups' claim (standard OIDC)
  if (Array.isArray(payload.groups)) {
    return payload.groups
  }

  // Check for 'cognito:groups' claim (AWS Cognito specific)
  if (Array.isArray(payload['cognito:groups'])) {
    return payload['cognito:groups']
  }

  return []
}

/**
 * Checks if the user has the Admin role.
 */
export function isAdmin(token: string | null): boolean {
  const roles = getUserRoles(token)
  return roles.includes('Admin')
}

/**
 * Checks if the user has the Dispatcher role.
 */
export function isDispatcher(token: string | null): boolean {
  const roles = getUserRoles(token)
  return roles.includes('Dispatcher')
}

/**
 * Checks if the user has any of the specified roles.
 */
export function hasRole(token: string | null, roles: string[]): boolean {
  const userRoles = getUserRoles(token)
  return roles.some(role => userRoles.includes(role))
}

