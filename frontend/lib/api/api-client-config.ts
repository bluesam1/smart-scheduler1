/**
 * API Client Configuration
 * 
 * Factory function to create API clients with authentication token injection.
 * This will be updated once the NSwag-generated client is available.
 */

// Get API base URL from environment variable
export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5004';

/**
 * Configuration for API client requests
 */
export interface ApiClientConfig {
  baseUrl: string;
  headers: Record<string, string>;
}

/**
 * Token provider interface for dynamic token access and refresh
 */
export interface TokenProvider {
  getToken: () => string | null;
  refreshToken: () => Promise<string | null>;
}

/**
 * Creates API client configuration with authentication token
 * 
 * @param accessToken - JWT access token from Cognito
 * @returns API client configuration with Authorization header
 */
export function createApiClientConfig(accessToken: string | null): ApiClientConfig {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };

  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  return {
    baseUrl: API_BASE_URL,
    headers,
  };
}

/**
 * Creates a custom fetch function that includes authentication headers and handles 401 responses
 * by automatically refreshing the token and retrying the request.
 * 
 * @param tokenProvider - Provider for getting current token and refreshing it
 * @returns Custom fetch function with Authorization header and automatic token refresh
 */
export function createAuthenticatedFetch(tokenProvider: TokenProvider | null) {
  return async (url: RequestInfo, init?: RequestInit): Promise<Response> => {
    const getToken = () => tokenProvider?.getToken() || null;
    
    // First attempt with current token
    let accessToken = getToken();
    const headers = new Headers(init?.headers)
    
    // Add Content-Type if not present
    if (!headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }
    
    // Add Authorization header if token is available
    if (accessToken) {
      headers.set('Authorization', `Bearer ${accessToken}`)
    } else {
      // Log warning if no token available but tokenProvider exists
      if (tokenProvider) {
        console.warn('[API Client] Token provider exists but getToken() returned null. User may need to log in again.')
      } else {
        console.warn('[API Client] No token provider available. Request will be sent without authentication.')
      }
    }
    
    let response = await fetch(url, {
      ...init,
      headers,
    })
    
    // If we get a 401 and have a token provider, try to refresh and retry once
    if (response.status === 401 && tokenProvider && accessToken) {
      console.log('[API Client] Received 401, attempting token refresh...')
      try {
        // Attempt to refresh the token
        const newToken = await tokenProvider.refreshToken()
        
        if (newToken) {
          console.log('[API Client] Token refreshed successfully, retrying request...')
          // Retry the request with the new token
          headers.set('Authorization', `Bearer ${newToken}`)
          response = await fetch(url, {
            ...init,
            headers,
          })
        } else {
          console.warn('[API Client] Token refresh returned null. User may need to log in again.')
        }
      } catch (error) {
        // If refresh fails, return the original 401 response
        // The caller should handle this by redirecting to login
        console.warn('[API Client] Token refresh failed:', error)
      }
    } else if (response.status === 401 && !accessToken) {
      console.error('[API Client] 401 Unauthorized: No access token available. Please log in.')
    }
    
    return response
  }
}

/**
 * Factory function to create API clients with authentication
 * 
 * @param tokenProvider - Provider for getting current token and refreshing it (or null for unauthenticated requests)
 * @returns Object containing API client instances
 */
export function createApiClients(tokenProvider: TokenProvider | null) {
  const accessToken = tokenProvider?.getToken() || null
  const config = createApiClientConfig(accessToken)
  const authenticatedFetch = createAuthenticatedFetch(tokenProvider)
  
  // Import the generated Client class
  // Note: Using dynamic import to avoid issues with server-side rendering
  const { Client } = require('./generated/api-client')
  
  // Create API client instance with authenticated fetch
  const apiClient = new Client(config.baseUrl, {
    fetch: authenticatedFetch,
  })

  return {
    client: apiClient,
    config,
  }
}

/**
 * Gets the API base URL from environment variables
 * 
 * @returns API base URL (defaults to http://localhost:5004)
 */
export function getApiBaseUrl(): string {
  return API_BASE_URL;
}

