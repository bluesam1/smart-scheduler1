"use client"

import { useEffect, useState, useCallback } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Users, Briefcase, Clock, TrendingUp } from "lucide-react"
import { useAuth } from "@/lib/auth/auth-context"
import { createAuthenticatedFetch, API_BASE_URL } from "@/lib/api/api-client-config"
import { Skeleton } from "@/components/ui/skeleton"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { AlertCircle } from "lucide-react"

interface DashboardStatistics {
  activeContractors: StatMetric
  pendingJobs: JobStatMetric
  averageAssignmentTime: TimeMetric
  utilizationRate: PercentMetric
}

interface StatMetric {
  value: number
  changeIndicator?: string | null
}

interface JobStatMetric {
  value: number
  unassigned: number
  changeIndicator?: string | null
}

interface TimeMetric {
  valueMinutes: number
  changeIndicator?: string | null
}

interface PercentMetric {
  value: number
  changeIndicator?: string | null
}

export function StatsOverview() {
  const { getTokenProvider } = useAuth()
  const [statistics, setStatistics] = useState<DashboardStatistics | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchStatistics = useCallback(async () => {
    const tokenProvider = getTokenProvider()
    if (!tokenProvider) {
      setError("Authentication required")
      setIsLoading(false)
      return
    }

    const authenticatedFetch = createAuthenticatedFetch(tokenProvider)

    try {
      const response = await authenticatedFetch(`${API_BASE_URL}/api/dashboard/stats`, {
        method: "GET",
      })

      if (!response.ok) {
        if (response.status === 401) {
          throw new Error("Authentication required. Please log in.")
        }
        throw new Error(`Failed to fetch statistics: ${response.statusText}`)
      }

      const data: DashboardStatistics = await response.json()
      setStatistics(data)
      setError(null)
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "Failed to fetch statistics"
      setError(errorMessage)
      console.error("Error fetching dashboard statistics:", err)
    } finally {
      setIsLoading(false)
    }
  }, [getTokenProvider])

  // Initial fetch
  useEffect(() => {
    fetchStatistics()
  }, [fetchStatistics])

  // Auto-refresh every 5 minutes
  useEffect(() => {
    const interval = setInterval(() => {
      fetchStatistics()
    }, 5 * 60 * 1000) // 5 minutes

    return () => clearInterval(interval)
  }, [fetchStatistics])

  const formatTime = (minutes: number): string => {
    if (minutes < 60) {
      return `${minutes}m`
    }
    const hours = Math.floor(minutes / 60)
    const mins = minutes % 60
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`
  }

  const formatPercent = (value: number): string => {
    return `${value.toFixed(1)}%`
  }

  if (isLoading) {
    return (
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {[1, 2, 3, 4].map((i) => (
          <Card key={i}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-4" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-8 w-16 mb-2" />
              <Skeleton className="h-3 w-20" />
            </CardContent>
          </Card>
        ))}
      </div>
    )
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertDescription>{error}</AlertDescription>
      </Alert>
    )
  }

  if (!statistics) {
    return null
  }

  // Add null checks for nested properties to prevent runtime errors
  const stats = [
    {
      title: "Active Contractors",
      value: statistics.activeContractors?.value?.toString() ?? "0",
      change: statistics.activeContractors?.changeIndicator || "",
      icon: Users,
    },
    {
      title: "Pending Jobs",
      value: statistics.pendingJobs?.value?.toString() ?? "0",
      change: (statistics.pendingJobs?.unassigned ?? 0) > 0
        ? `${statistics.pendingJobs.unassigned} unassigned${statistics.pendingJobs?.changeIndicator ? ` • ${statistics.pendingJobs.changeIndicator}` : ""}`
        : statistics.pendingJobs?.changeIndicator || "",
      icon: Briefcase,
    },
    {
      title: "Avg Assignment Time",
      value: formatTime(statistics.averageAssignmentTime?.valueMinutes ?? 0),
      change: statistics.averageAssignmentTime?.changeIndicator || "",
      icon: Clock,
    },
    {
      title: "Utilization Rate",
      value: formatPercent(statistics.utilizationRate?.value ?? 0),
      change: statistics.utilizationRate?.changeIndicator || "",
      icon: TrendingUp,
    },
  ]

  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
      {stats.map((stat) => (
        <Card key={stat.title}>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{stat.title}</CardTitle>
            <stat.icon className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stat.value}</div>
            {stat.change && (
              <p className="text-xs text-muted-foreground">{stat.change}</p>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
