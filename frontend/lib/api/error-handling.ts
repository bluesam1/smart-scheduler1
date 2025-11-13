/**
 * API Error Handling Utilities
 * 
 * Standardized error handling for API requests.
 */

export interface ApiError {
  message: string;
  statusCode?: number;
  statusText?: string;
  details?: any;
}

/**
 * Custom error class for API errors
 */
export class ApiException extends Error {
  statusCode?: number;
  statusText?: string;
  details?: any;

  constructor(message: string, statusCode?: number, statusText?: string, details?: any) {
    super(message);
    this.name = 'ApiException';
    this.statusCode = statusCode;
    this.statusText = statusText;
    this.details = details;
  }
}

/**
 * Parses an API error response
 * 
 * @param response - Fetch Response object
 * @returns Promise resolving to ApiError
 */
export async function parseApiError(response: Response): Promise<ApiError> {
  let details: any = null;
  let message = response.statusText || 'An error occurred';

  try {
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      details = await response.json();
      // Try to extract error message from common error response formats
      if (details.message) {
        message = details.message;
      } else if (details.title) {
        message = details.title;
      } else if (details.error) {
        message = typeof details.error === 'string' ? details.error : details.error.message || message;
      }
    } else {
      const text = await response.text();
      if (text) {
        message = text;
        details = { raw: text };
      }
    }
  } catch (error) {
    // If we can't parse the error response, use the status text
    console.warn('Failed to parse error response:', error);
  }

  return {
    message,
    statusCode: response.status,
    statusText: response.statusText,
    details,
  };
}

/**
 * Handles API errors and throws appropriate exceptions
 * 
 * @param response - Fetch Response object
 * @throws ApiException if response is not ok
 */
export async function handleApiError(response: Response): Promise<void> {
  if (response.ok) {
    return;
  }

  const error = await parseApiError(response);
  throw new ApiException(
    error.message,
    error.statusCode,
    error.statusText,
    error.details
  );
}

/**
 * Checks if an error is an authentication error (401)
 * 
 * @param error - Error to check
 * @returns True if error is a 401 Unauthorized
 */
export function isAuthenticationError(error: unknown): boolean {
  if (error instanceof ApiException) {
    return error.statusCode === 401;
  }
  return false;
}

/**
 * Checks if an error is an authorization error (403)
 * 
 * @param error - Error to check
 * @returns True if error is a 403 Forbidden
 */
export function isAuthorizationError(error: unknown): boolean {
  if (error instanceof ApiException) {
    return error.statusCode === 403;
  }
  return false;
}

/**
 * Checks if an error is a client error (4xx)
 * 
 * @param error - Error to check
 * @returns True if error is a 4xx status code
 */
export function isClientError(error: unknown): boolean {
  if (error instanceof ApiException) {
    return error.statusCode !== undefined && error.statusCode >= 400 && error.statusCode < 500;
  }
  return false;
}

/**
 * Checks if an error is a server error (5xx)
 * 
 * @param error - Error to check
 * @returns True if error is a 5xx status code
 */
export function isServerError(error: unknown): boolean {
  if (error instanceof ApiException) {
    return error.statusCode !== undefined && error.statusCode >= 500;
  }
  return false;
}

/**
 * Gets a user-friendly error message from an error
 * 
 * @param error - Error to extract message from
 * @returns User-friendly error message
 */
export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiException) {
    return error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'An unexpected error occurred';
}

/**
 * Formats an error for display to the user
 * 
 * @param error - Error to format
 * @returns Formatted error message suitable for user display
 */
export function formatErrorForDisplay(error: unknown): string {
  const message = getErrorMessage(error);

  // Add context based on error type
  if (isAuthenticationError(error)) {
    return 'You are not authenticated. Please log in.';
  }
  if (isAuthorizationError(error)) {
    return 'You do not have permission to perform this action.';
  }
  if (isServerError(error)) {
    return 'The server encountered an error. Please try again later.';
  }

  return message;
}

