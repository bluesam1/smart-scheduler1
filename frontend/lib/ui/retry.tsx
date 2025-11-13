/**
 * Retry Logic Utilities
 * 
 * Utilities for retrying failed API requests with exponential backoff.
 */

import { useState, useCallback } from "react"

/**
 * Options for retry logic
 */
export interface RetryOptions {
  /** Maximum number of retry attempts */
  maxAttempts?: number
  /** Initial delay in milliseconds */
  initialDelay?: number
  /** Maximum delay in milliseconds */
  maxDelay?: number
  /** Multiplier for exponential backoff */
  backoffMultiplier?: number
  /** Function to determine if error should be retried */
  shouldRetry?: (error: unknown) => boolean
}

const DEFAULT_OPTIONS: Required<Omit<RetryOptions, "shouldRetry">> = {
  maxAttempts: 3,
  initialDelay: 1000,
  maxDelay: 10000,
  backoffMultiplier: 2,
}

/**
 * Calculates delay for retry attempt using exponential backoff
 */
function calculateDelay(attempt: number, options: Required<Omit<RetryOptions, "shouldRetry">>): number {
  const delay = options.initialDelay * Math.pow(options.backoffMultiplier, attempt - 1)
  return Math.min(delay, options.maxDelay)
}

/**
 * Default function to determine if error should be retried
 * Retries on network errors and 5xx server errors
 */
function defaultShouldRetry(error: unknown): boolean {
  // Network errors (no response)
  if (error instanceof TypeError && error.message.includes("fetch")) {
    return true
  }

  // Server errors (5xx)
  if (error && typeof error === "object" && "statusCode" in error) {
    const statusCode = (error as { statusCode?: number }).statusCode
    if (statusCode && statusCode >= 500 && statusCode < 600) {
      return true
    }
  }

  return false
}

/**
 * Retries an async operation with exponential backoff
 * 
 * @param operation - Function to retry
 * @param options - Retry options
 * @returns Promise that resolves with the operation result
 * 
 * @example
 * ```tsx
 * const data = await retryWithBackoff(
 *   () => api.fetchData(),
 *   { maxAttempts: 3 }
 * )
 * ```
 */
export async function retryWithBackoff<T>(
  operation: () => Promise<T>,
  options: RetryOptions = {}
): Promise<T> {
  const opts: Required<Omit<RetryOptions, "shouldRetry">> & { shouldRetry: (error: unknown) => boolean } = {
    ...DEFAULT_OPTIONS,
    shouldRetry: options.shouldRetry || defaultShouldRetry,
    ...options,
  }

  let lastError: unknown

  for (let attempt = 1; attempt <= opts.maxAttempts; attempt++) {
    try {
      return await operation()
    } catch (error) {
      lastError = error

      // Don't retry if we've reached max attempts
      if (attempt >= opts.maxAttempts) {
        break
      }

      // Don't retry if shouldRetry returns false
      if (!opts.shouldRetry(error)) {
        break
      }

      // Calculate delay and wait before retrying
      const delay = calculateDelay(attempt, opts)
      await new Promise((resolve) => setTimeout(resolve, delay))
    }
  }

  throw lastError
}

/**
 * Hook for managing retry logic
 * 
 * @param operation - Function to retry
 * @param options - Retry options
 * @returns Object with execute function and retry state
 * 
 * @example
 * ```tsx
 * const { execute, isRetrying, retryCount } = useRetry(
 *   () => api.fetchData(),
 *   { maxAttempts: 3 }
 * )
 * 
 * const handleFetch = async () => {
 *   try {
 *     const data = await execute()
 *   } catch (error) {
 *     // Handle final error after all retries
 *   }
 * }
 * ```
 */
export function useRetry<T extends (...args: any[]) => Promise<any>>(
  operation: T,
  options: RetryOptions = {}
) {
  const [isRetrying, setIsRetrying] = useState(false)
  const [retryCount, setRetryCount] = useState(0)

  const execute = useCallback(
    async (...args: Parameters<T>): Promise<ReturnType<T> extends Promise<infer U> ? U : never> => {
      setIsRetrying(true)
      setRetryCount(0)

      try {
        return await retryWithBackoff(
          async () => {
            setRetryCount((prev) => prev + 1)
            return await operation(...args)
          },
          {
            ...options,
            shouldRetry: (error) => {
              const shouldRetry = options.shouldRetry || defaultShouldRetry
              if (shouldRetry(error)) {
                setRetryCount((prev) => prev + 1)
                return true
              }
              return false
            },
          }
        )
      } finally {
        setIsRetrying(false)
      }
    },
    [operation, options]
  )

  return { execute, isRetrying, retryCount }
}

