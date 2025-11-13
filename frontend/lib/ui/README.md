# UI Utilities

Reusable utilities for loading states, error handling, and retry logic.

## Loading States

### `useAsyncOperation`

Hook for managing async operation loading state.

```tsx
import { useAsyncOperation } from "@/lib/ui/loading"

function MyComponent() {
  const { isLoading, execute } = useAsyncOperation()
  
  const handleSave = execute(async () => {
    await saveData()
  })
  
  return (
    <Button onClick={handleSave} disabled={isLoading}>
      {isLoading ? "Saving..." : "Save"}
    </Button>
  )
}
```

### `useDataFetch`

Hook for managing data fetching with loading state.

```tsx
import { useDataFetch } from "@/lib/ui/loading"

function MyComponent() {
  const { data, isLoading, error, refetch } = useDataFetch(
    async () => await api.getData(),
    { immediate: true }
  )
  
  if (isLoading) return <LoadingIndicator />
  if (error) return <ErrorDisplay error={error.message} onRetry={refetch} />
  
  return <div>{data}</div>
}
```

### Loading Components

- `LoadingIndicator` - Simple spinner component
- `LoadingOverlay` - Full-screen loading overlay
- `ListSkeleton` - Skeleton loader for lists
- `TableSkeleton` - Skeleton loader for tables
- `CardSkeleton` - Skeleton loader for cards

## Error Handling

### `useErrorState`

Hook for managing error state.

```tsx
import { useErrorState } from "@/lib/ui/error"

function MyComponent() {
  const { error, setError, clearError } = useErrorState()
  
  const handleAction = async () => {
    try {
      await doSomething()
    } catch (err) {
      setError(err)
    }
  }
  
  return (
    <>
      {error && <ErrorDisplay error={error} onDismiss={clearError} />}
      <Button onClick={handleAction}>Do Something</Button>
    </>
  )
}
```

### Error Components

- `ErrorDisplay` - Full error alert with retry/dismiss actions
- `ErrorMessage` - Inline error message
- `ErrorFallback` - Error boundary fallback component
- `NetworkError` - Network-specific error display
- `ValidationError` - Form validation error display

## Retry Logic

### `retryWithBackoff`

Function for retrying async operations with exponential backoff.

```tsx
import { retryWithBackoff } from "@/lib/ui/retry"

const data = await retryWithBackoff(
  () => api.fetchData(),
  { 
    maxAttempts: 3,
    initialDelay: 1000,
    shouldRetry: (error) => error.statusCode >= 500
  }
)
```

### `useRetry`

Hook for managing retry logic.

```tsx
import { useRetry } from "@/lib/ui/retry"

function MyComponent() {
  const { execute, isRetrying, retryCount } = useRetry(
    () => api.fetchData(),
    { maxAttempts: 3 }
  )
  
  const handleFetch = async () => {
    try {
      const data = await execute()
    } catch (error) {
      // Handle final error after all retries
    }
  }
  
  return (
    <Button onClick={handleFetch} disabled={isRetrying}>
      {isRetrying ? `Retrying... (${retryCount})` : "Fetch"}
    </Button>
  )
}
```

## Optimistic Updates

### `useOptimisticUpdate`

Hook for managing optimistic updates with rollback support.

```tsx
import { useOptimisticUpdate } from "@/lib/ui/optimistic"

function MyComponent() {
  const { data, updateOptimistically, rollback } = useOptimisticUpdate(initialJobs)
  
  const handleAssign = async (jobId, contractorId) => {
    const result = updateOptimistically(jobs, {
      onOptimisticUpdate: (jobs) => 
        jobs.map(j => j.id === jobId ? { ...j, assignedTo: contractorId } : j)
    })
    
    try {
      await api.assignJob(jobId, contractorId)
      result.commit()
    } catch (error) {
      result.rollback()
    }
  }
}
```

### `useOptimisticOperation`

Hook for optimistic async operations.

```tsx
import { useOptimisticOperation } from "@/lib/ui/optimistic"

const optimisticAssign = useOptimisticOperation(
  async (jobId, contractorId) => await api.assignJob(jobId, contractorId),
  {
    onOptimisticUpdate: (jobs) => 
      jobs.map(j => j.id === jobId ? { ...j, assignedTo: contractorId } : j),
    onSync: (serverData, optimisticData) => serverData // Use server data
  }
)

await optimisticAssign(jobs, setJobs, jobId, contractorId)
```

### Optimistic Update Utilities

- `optimisticListUpdate` - Update item in list
- `optimisticListAdd` - Add item to list
- `optimisticListRemove` - Remove item from list

### Best Practices

1. **Always provide rollback logic** - Store previous state before optimistic update
2. **Sync with server response** - Update with actual server data when available
3. **Handle conflicts** - If server data differs, prefer server data
4. **Show loading states** - Indicate that operation is in progress
5. **Handle errors gracefully** - Rollback and show error message

## Patterns

### Standard API Call Pattern

```tsx
function MyComponent() {
  const [data, setData] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  
  const fetchData = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const result = await api.getData()
      setData(result)
    } catch (err) {
      setError(formatErrorForDisplay(err))
    } finally {
      setIsLoading(false)
    }
  }, [])
  
  useEffect(() => {
    fetchData()
  }, [fetchData])
  
  if (isLoading) return <LoadingIndicator />
  if (error) return <ErrorDisplay error={error} onRetry={fetchData} />
  
  return <div>{data}</div>
}
```

### With Retry Logic

```tsx
import { retryWithBackoff } from "@/lib/ui/retry"

const fetchData = useCallback(async () => {
  setIsLoading(true)
  setError(null)
  try {
    const result = await retryWithBackoff(
      () => api.getData(),
      { maxAttempts: 3 }
    )
    setData(result)
  } catch (err) {
    setError(formatErrorForDisplay(err))
  } finally {
    setIsLoading(false)
  }
}, [])
```

### Error Handling Best Practices

1. **Always use `formatErrorForDisplay`** for user-facing error messages
2. **Check error types** using `isAuthenticationError`, `isServerError`, etc.
3. **Provide retry actions** for transient errors
4. **Show loading states** during retries
5. **Log errors** to console for debugging

### Loading State Best Practices

1. **Show loading immediately** for user actions
2. **Use skeleton UI** for list/table loading
3. **Use spinners** for button actions
4. **Disable interactions** during loading
5. **Show progress** for long operations when possible

