"use client"

import { useState, useEffect, useCallback } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Clock, CheckCircle2, UserCheck, XCircle, Briefcase, UserPlus } from "lucide-react"
import { useAuth } from "@/lib/auth/auth-context"
import { createAuthenticatedFetch, API_BASE_URL } from "@/lib/api/api-client-config"
import { Skeleton } from "@/components/ui/skeleton"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { AlertCircle } from "lucide-react"
import { formatDistanceToNow } from "date-fns"

interface Activity {
  id: string
  type: "assignment" | "completion" | "cancellation" | "contractor_added" | "job_created" | "unknown"
  title: string
  description: string
  timestamp: string
  metadata?: Record<string, any>
}

export function ActivityFeed() {
  const { getTokenProvider } = useAuth()
  const [activities, setActivities] = useState<Activity[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchActivities = useCallback(async () => {
    const tokenProvider = getTokenProvider()
    if (!tokenProvider) {
      setError("Authentication required")
      setIsLoading(false)
      return
    }

    const authenticatedFetch = createAuthenticatedFetch(tokenProvider)

    try {
      const response = await authenticatedFetch(`${API_BASE_URL}/api/activity?limit=20`, {
        method: "GET",
      })

      if (!response.ok) {
        if (response.status === 401) {
          throw new Error("Authentication required. Please log in.")
        }
        throw new Error(`Failed to fetch activities: ${response.statusText}`)
      }

      const data: Activity[] = await response.json()
      setActivities(data)
      setError(null)
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "Failed to fetch activities"
      setError(errorMessage)
      console.error("Error fetching activities:", err)
    } finally {
      setIsLoading(false)
    }
  }, [getTokenProvider])

  // Initial fetch
  useEffect(() => {
    fetchActivities()
  }, [fetchActivities])

  // Auto-refresh every 30 seconds
  useEffect(() => {
    const interval = setInterval(() => {
      fetchActivities()
    }, 30 * 1000) // 30 seconds

    return () => clearInterval(interval)
  }, [fetchActivities])

  // TODO: Add SignalR real-time updates
  // This would listen for domain events and add new activities to the feed
  // For MVP, auto-refresh is sufficient

  const getActivityIcon = (type: Activity["type"]) => {
    switch (type) {
      case "assignment":
        return <UserCheck className="h-4 w-4 text-accent" />
      case "completion":
        return <CheckCircle2 className="h-4 w-4 text-chart-3" />
      case "cancellation":
        return <XCircle className="h-4 w-4 text-destructive" />
      case "job_created":
        return <Briefcase className="h-4 w-4 text-primary" />
      case "contractor_added":
        return <UserPlus className="h-4 w-4 text-chart-1" />
      default:
        return <Clock className="h-4 w-4 text-muted-foreground" />
    }
  }

  const formatTimestamp = (timestamp: string): string => {
    try {
      const date = new Date(timestamp)
      return formatDistanceToNow(date, { addSuffix: true })
    } catch {
      return timestamp
    }
  }

  if (isLoading) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg flex items-center gap-2">Recent Activity</CardTitle>
        </CardHeader>
        <CardContent>
          <ScrollArea className="h-[300px] pr-4">
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="flex gap-3">
                  <Skeleton className="h-8 w-8 rounded-full" />
                  <div className="flex-1 space-y-2">
                    <Skeleton className="h-4 w-32" />
                    <Skeleton className="h-3 w-full" />
                  </div>
                </div>
              ))}
            </div>
          </ScrollArea>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg flex items-center gap-2">Recent Activity</CardTitle>
        </CardHeader>
        <CardContent>
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-lg flex items-center gap-2">Recent Activity</CardTitle>
      </CardHeader>
      <CardContent>
        <ScrollArea className="h-[300px] pr-4">
          {activities.length === 0 ? (
            <div className="text-center text-sm text-muted-foreground py-8">
              No recent activity
            </div>
          ) : (
            <div className="space-y-4">
              {activities.map((activity) => (
                <div key={activity.id} className="flex gap-3">
                  <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted">
                    {getActivityIcon(activity.type)}
                  </div>
                  <div className="flex-1 space-y-1">
                    <div className="flex items-start justify-between gap-2">
                      <p className="text-sm font-medium leading-tight text-balance">{activity.title}</p>
                      <Badge variant="outline" className="shrink-0 text-xs">
                        <Clock className="mr-1 h-3 w-3" />
                        {formatTimestamp(activity.timestamp)}
                      </Badge>
                    </div>
                    <p className="text-sm text-muted-foreground text-pretty">{activity.description}</p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </ScrollArea>
      </CardContent>
    </Card>
  )
}
