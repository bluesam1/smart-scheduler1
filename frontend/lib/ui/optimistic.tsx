/**
 * Optimistic UI Update Utilities
 * 
 * Utilities for implementing optimistic UI updates with rollback support.
 */

import { useState, useCallback, useRef } from "react"

/**
 * Options for optimistic updates
 */
export interface OptimisticOptions<T> {
  /** Function to apply optimistic update */
  onOptimisticUpdate: (data: T) => T
  /** Function to rollback on error */
  onRollback?: (previousData: T) => void
  /** Function to sync with server response */
  onSync?: (serverData: T, optimisticData: T) => T
}

/**
 * Result of optimistic update operation
 */
export interface OptimisticResult<T> {
  /** Optimistically updated data */
  optimisticData: T
  /** Function to commit the update (call on success) */
  commit: (serverData?: T) => void
  /** Function to rollback the update (call on error) */
  rollback: () => void
}

/**
 * Hook for managing optimistic updates
 * 
 * @param initialData - Initial data state
 * @returns Object with data, update function, and rollback function
 * 
 * @example
 * ```tsx
 * const { data, updateOptimistically, rollback } = useOptimisticUpdate(initialJobs)
 * 
 * const handleAssign = async (jobId, contractorId) => {
 *   const result = updateOptimistically(jobs, {
 *     onOptimisticUpdate: (jobs) => 
 *       jobs.map(j => j.id === jobId ? { ...j, assignedTo: contractorId } : j)
 *   })
 *   
 *   try {
 *     await api.assignJob(jobId, contractorId)
 *     result.commit()
 *   } catch (error) {
 *     result.rollback()
 *   }
 * }
 * ```
 */
export function useOptimisticUpdate<T>(initialData: T) {
  const [data, setData] = useState<T>(initialData)
  const previousDataRef = useRef<T>(initialData)

  const updateOptimistically = useCallback(
    (options: OptimisticOptions<T>): OptimisticResult<T> => {
      // Store previous state for rollback
      previousDataRef.current = data

      // Apply optimistic update
      const optimisticData = options.onOptimisticUpdate(data)
      setData(optimisticData)

      const commit = (serverData?: T) => {
        if (serverData !== undefined && options.onSync) {
          // Sync with server data
          const syncedData = options.onSync(serverData, optimisticData)
          setData(syncedData)
          previousDataRef.current = syncedData
        } else if (serverData !== undefined) {
          // Use server data directly
          setData(serverData)
          previousDataRef.current = serverData
        } else {
          // Keep optimistic data
          previousDataRef.current = optimisticData
        }
      }

      const rollback = () => {
        setData(previousDataRef.current)
        options.onRollback?.(previousDataRef.current)
      }

      return { optimisticData, commit, rollback }
    },
    [data]
  )

  const rollback = useCallback(() => {
    setData(previousDataRef.current)
  }, [])

  const reset = useCallback((newData: T) => {
    setData(newData)
    previousDataRef.current = newData
  }, [])

  return { data, updateOptimistically, rollback, reset }
}

/**
 * Hook for optimistic async operations
 * 
 * @param operation - Async operation to perform
 * @param options - Optimistic update options
 * @returns Function to execute the operation with optimistic updates
 * 
 * @example
 * ```tsx
 * const optimisticAssign = useOptimisticOperation(
 *   async (jobId, contractorId) => await api.assignJob(jobId, contractorId),
 *   {
 *     onOptimisticUpdate: (jobs) => 
 *       jobs.map(j => j.id === jobId ? { ...j, assignedTo: contractorId } : j)
 *   }
 * )
 * 
 * await optimisticAssign(jobId, contractorId)
 * ```
 */
export function useOptimisticOperation<TData, TArgs extends any[]>(
  operation: (...args: TArgs) => Promise<TData>,
  options: OptimisticOptions<TData>
) {
  const previousDataRef = useRef<TData | null>(null)

  return useCallback(
    async (
      currentData: TData,
      setData: (data: TData) => void,
      ...args: TArgs
    ): Promise<TData> => {
      // Store previous state
      previousDataRef.current = currentData

      // Apply optimistic update
      const optimisticData = options.onOptimisticUpdate(currentData)
      setData(optimisticData)

      try {
        // Execute operation
        const serverData = await operation(...args)

        // Commit with server data
        if (options.onSync) {
          const syncedData = options.onSync(serverData, optimisticData)
          setData(syncedData)
          return syncedData
        } else {
          setData(serverData)
          return serverData
        }
      } catch (error) {
        // Rollback on error
        setData(previousDataRef.current!)
        options.onRollback?.(previousDataRef.current!)
        throw error
      }
    },
    [operation, options]
  )
}

/**
 * Utility function for optimistic list updates
 */
export function optimisticListUpdate<T extends { id: string }>(
  list: T[],
  itemId: string,
  update: Partial<T> | ((item: T) => T)
): T[] {
  return list.map((item) => {
    if (item.id === itemId) {
      return typeof update === "function" ? update(item) : { ...item, ...update }
    }
    return item
  })
}

/**
 * Utility function for optimistic list additions
 */
export function optimisticListAdd<T>(list: T[], newItem: T, position: "start" | "end" = "end"): T[] {
  return position === "start" ? [newItem, ...list] : [...list, newItem]
}

/**
 * Utility function for optimistic list removals
 */
export function optimisticListRemove<T extends { id: string }>(list: T[], itemId: string): T[] {
  return list.filter((item) => item.id !== itemId)
}


