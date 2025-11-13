/**
 * Error Handling Utilities
 * 
 * Reusable components and hooks for displaying and handling errors.
 */

import { useState, useCallback } from "react"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { AlertCircle, X, RefreshCw } from "lucide-react"
import { formatErrorForDisplay, isAuthenticationError, isServerError } from "@/lib/api/error-handling"

/**
 * Hook for managing error state
 * 
 * @returns Object with error state and handlers
 * 
 * @example
 * ```tsx
 * const { error, setError, clearError } = useErrorState()
 * ```
 */
export function useErrorState() {
  const [error, setError] = useState<string | null>(null)

  const clearError = useCallback(() => {
    setError(null)
  }, [])

  const handleError = useCallback((err: unknown) => {
    setError(formatErrorForDisplay(err))
  }, [])

  return { error, setError: handleError, clearError }
}

/**
 * Error display component
 */
export function ErrorDisplay({
  error,
  onRetry,
  onDismiss,
  title = "An error occurred",
  className,
}: {
  error: string | null
  onRetry?: () => void
  onDismiss?: () => void
  title?: string
  className?: string
}) {
  if (!error) return null

  return (
    <Alert variant="destructive" className={className}>
      <AlertCircle className="h-4 w-4" />
      <AlertTitle>{title}</AlertTitle>
      <AlertDescription className="flex items-center justify-between gap-2">
        <span>{error}</span>
        <div className="flex items-center gap-2">
          {onRetry && (
            <Button variant="ghost" size="sm" onClick={onRetry}>
              <RefreshCw className="h-3 w-3 mr-1" />
              Retry
            </Button>
          )}
          {onDismiss && (
            <Button variant="ghost" size="sm" onClick={onDismiss}>
              <X className="h-3 w-3" />
            </Button>
          )}
        </div>
      </AlertDescription>
    </Alert>
  )
}

/**
 * Error message component for inline display
 */
export function ErrorMessage({
  error,
  className,
}: {
  error: string | null
  className?: string
}) {
  if (!error) return null

  return (
    <div className={`text-sm text-destructive ${className || ""}`} role="alert">
      {error}
    </div>
  )
}

/**
 * Error boundary fallback component
 */
export function ErrorFallback({
  error,
  resetErrorBoundary,
}: {
  error: Error
  resetErrorBoundary: () => void
}) {
  const isAuthError = isAuthenticationError(error)
  const isServerErr = isServerError(error)

  return (
    <div className="flex flex-col items-center justify-center gap-4 py-12">
      <AlertCircle className="h-12 w-12 text-destructive" />
      <div className="text-center space-y-2">
        <h2 className="text-lg font-semibold">Something went wrong</h2>
        <p className="text-sm text-muted-foreground max-w-md">
          {isAuthError
            ? "You need to log in to continue."
            : isServerErr
              ? "The server encountered an error. Please try again later."
              : formatErrorForDisplay(error)}
        </p>
      </div>
      <div className="flex gap-2">
        {isAuthError ? (
          <Button onClick={() => (window.location.href = "/login")}>Go to Login</Button>
        ) : (
          <Button onClick={resetErrorBoundary} variant="outline">
            <RefreshCw className="h-4 w-4 mr-2" />
            Try Again
          </Button>
        )}
      </div>
    </div>
  )
}

/**
 * Network error component
 */
export function NetworkError({ onRetry }: { onRetry?: () => void }) {
  return (
    <Alert variant="destructive">
      <AlertCircle className="h-4 w-4" />
      <AlertTitle>Network Error</AlertTitle>
      <AlertDescription>
        Unable to connect to the server. Please check your internet connection and try again.
        {onRetry && (
          <Button variant="outline" size="sm" onClick={onRetry} className="mt-2">
            <RefreshCw className="h-3 w-3 mr-1" />
            Retry
          </Button>
        )}
      </AlertDescription>
    </Alert>
  )
}

/**
 * Validation error component for form fields
 */
export function ValidationError({
  errors,
  className,
}: {
  errors?: Array<{ message?: string } | undefined>
  className?: string
}) {
  if (!errors || errors.length === 0) return null

  const errorMessages = errors.filter((e) => e?.message).map((e) => e!.message!)

  if (errorMessages.length === 0) return null

  return (
    <div className={`text-sm text-destructive ${className || ""}`} role="alert">
      {errorMessages.length === 1 ? (
        errorMessages[0]
      ) : (
        <ul className="ml-4 list-disc space-y-1">
          {errorMessages.map((msg, i) => (
            <li key={i}>{msg}</li>
          ))}
        </ul>
      )}
    </div>
  )
}


