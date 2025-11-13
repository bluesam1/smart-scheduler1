/**
 * Loading State Utilities
 * 
 * Reusable hooks and components for managing loading states in components.
 */

import { useState, useCallback, useEffect } from "react"
import { Spinner } from "@/components/ui/spinner"
import { Skeleton } from "@/components/ui/skeleton"

/**
 * Hook for managing async operation loading state
 * 
 * @returns Object with isLoading state and async wrapper function
 * 
 * @example
 * ```tsx
 * const { isLoading, execute } = useAsyncOperation()
 * 
 * const handleSave = execute(async () => {
 *   await saveData()
 * })
 * ```
 */
export function useAsyncOperation<T extends (...args: any[]) => Promise<any>>() {
  const [isLoading, setIsLoading] = useState(false)

  const execute = useCallback(
    async (operation: T, ...args: Parameters<T>): Promise<ReturnType<T> extends Promise<infer U> ? U : never> => {
      setIsLoading(true)
      try {
        return await operation(...args)
      } finally {
        setIsLoading(false)
      }
    },
    []
  )

  return { isLoading, execute }
}

/**
 * Hook for managing data fetching with loading state
 * 
 * @param fetchFn - Function to fetch data
 * @returns Object with data, isLoading, error, and refetch function
 * 
 * @example
 * ```tsx
 * const { data, isLoading, error, refetch } = useDataFetch(
 *   async () => await api.getData()
 * )
 * ```
 */
export function useDataFetch<T>(
  fetchFn: () => Promise<T>,
  options?: {
    immediate?: boolean
    onError?: (error: unknown) => void
  }
) {
  const [data, setData] = useState<T | null>(null)
  const [isLoading, setIsLoading] = useState(options?.immediate !== false)
  const [error, setError] = useState<Error | null>(null)

  const fetch = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const result = await fetchFn()
      setData(result)
      return result
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err))
      setError(error)
      options?.onError?.(err)
      throw err
    } finally {
      setIsLoading(false)
    }
  }, [fetchFn, options?.onError])

  useEffect(() => {
    if (options?.immediate !== false) {
      // Initial fetch on mount
      fetch()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []) // Only run on mount

  return { data, isLoading, error, refetch: fetch }
}

/**
 * Loading indicator component
 */
export function LoadingIndicator({ size = "default", className }: { size?: "sm" | "default" | "lg"; className?: string }) {
  const sizeClasses = {
    sm: "h-4 w-4",
    default: "h-8 w-8",
    lg: "h-12 w-12",
  }

  return (
    <div className={`flex items-center justify-center ${className || ""}`}>
      <Spinner className={sizeClasses[size]} />
    </div>
  )
}

/**
 * Loading overlay component
 */
export function LoadingOverlay({ message }: { message?: string }) {
  return (
    <div className="absolute inset-0 flex items-center justify-center bg-background/80 backdrop-blur-sm z-50">
      <div className="flex flex-col items-center gap-2">
        <Spinner className="h-8 w-8" />
        {message && <p className="text-sm text-muted-foreground">{message}</p>}
      </div>
    </div>
  )
}

/**
 * Skeleton loader for list items
 */
export function ListSkeleton({ count = 3 }: { count?: number }) {
  return (
    <div className="space-y-4">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="flex items-center gap-4">
          <Skeleton className="h-12 w-12 rounded-full" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-4 w-1/2" />
          </div>
        </div>
      ))}
    </div>
  )
}

/**
 * Skeleton loader for table rows
 */
export function TableSkeleton({ rows = 5, columns = 4 }: { rows?: number; columns?: number }) {
  return (
    <>
      {Array.from({ length: rows }).map((_, i) => (
        <tr key={i}>
          {Array.from({ length: columns }).map((_, j) => (
            <td key={j} className="px-4 py-3">
              <Skeleton className="h-4 w-full" />
            </td>
          ))}
        </tr>
      ))}
    </>
  )
}

/**
 * Skeleton loader for cards
 */
export function CardSkeleton({ count = 3 }: { count?: number }) {
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="rounded-lg border p-4 space-y-3">
          <Skeleton className="h-6 w-3/4" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-2/3" />
        </div>
      ))}
    </div>
  )
}

