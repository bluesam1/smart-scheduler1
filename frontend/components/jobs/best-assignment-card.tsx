"use client"

import { useState, useEffect, useCallback, useRef } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { RefreshCw, Calendar, Clock, Sparkles, CheckCircle2 } from "lucide-react"
import { useAuth } from "@/lib/auth/auth-context"
import { createApiClients } from "@/lib/api/api-client-config"
import { formatErrorForDisplay } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import { useSignalR } from "@/lib/realtime/signalr-context"

interface BestAssignmentCardProps {
  jobId: string
  onAssigned?: () => void
}

interface TimeSlot {
  startUtc: string
  endUtc: string
  type: string
  confidence: number
}

interface Recommendation {
  contractorId: string
  contractorName: string
  score: number
  suggestedSlots: TimeSlot[]
  distance: number
  eta: number
}

export function BestAssignmentCard({ jobId, onAssigned }: BestAssignmentCardProps) {
  const { getTokenProvider, isAuthenticated } = useAuth()
  const { client: signalRClient, isConnected } = useSignalR()
  const [loading, setLoading] = useState(true)
  const [recalculating, setRecalculating] = useState(false)
  const [assigning, setAssigning] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [bestRecommendation, setBestRecommendation] = useState<Recommendation | null>(null)

  // Store getTokenProvider in a ref to avoid dependency loops
  const getTokenProviderRef = useRef(getTokenProvider)
  useEffect(() => {
    getTokenProviderRef.current = getTokenProvider
  }, [getTokenProvider])

  const fetchBestAssignment = useCallback(async () => {
    if (!isAuthenticated) return

    setLoading(true)
    setError(null)

    try {
      // Use ref to get the latest getTokenProvider without adding it to dependencies
      const tokenProvider = getTokenProviderRef.current()
      if (!tokenProvider) {
        setError("Authentication required")
        setLoading(false)
        return
      }

      const { client } = createApiClients(tokenProvider)
      
      // Fetch latest recommendations for this job
      const response = await client.getLatestRecommendations(jobId)

      // Get the first (best) recommendation
      if (response.recommendations && response.recommendations.length > 0) {
        const best = response.recommendations[0]
        setBestRecommendation({
          contractorId: best.contractorId,
          contractorName: best.contractorName,
          score: best.score,
          suggestedSlots: best.suggestedSlots || [],
          distance: best.distance,
          eta: best.eta
        })
      } else {
        setBestRecommendation(null)
      }
    } catch (err: any) {
      // 404 is expected if no recommendations calculated yet
      if (err.status === 404) {
        setBestRecommendation(null)
      } else {
        const errorMessage = formatErrorForDisplay(err)
        setError(errorMessage)
        console.error("[BestAssignmentCard] Error fetching recommendations:", errorMessage)
      }
    } finally {
      setLoading(false)
    }
  }, [jobId, isAuthenticated]) // Removed getTokenProvider from deps - use ref instead

  const handleRecalculate = async () => {
    setRecalculating(true)
    setError(null)

    try {
      const tokenProvider = getTokenProvider()
      if (!tokenProvider) {
        toast.error("Authentication required")
        setRecalculating(false)
        return
      }

      const { client } = createApiClients(tokenProvider)
      await client.recalculateRecommendations({ jobId })
      
      toast.info("Recalculating recommendations...")
      // Don't fetch immediately - wait for SignalR event
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      setError(errorMessage)
      toast.error(`Failed to recalculate: ${errorMessage}`)
      setRecalculating(false)
    }
  }

  const handleScheduleNow = async () => {
    if (!bestRecommendation || bestRecommendation.suggestedSlots.length === 0) {
      toast.error("No suggested time slots available")
      return
    }

    setAssigning(true)

    try {
      const tokenProvider = getTokenProvider()
      if (!tokenProvider) {
        toast.error("Authentication required")
        setAssigning(false)
        return
      }

      const { client } = createApiClients(tokenProvider)
      
      // Use the first suggested slot
      const slot = bestRecommendation.suggestedSlots[0]
      
      await client.assignJob(jobId, {
        contractorId: bestRecommendation.contractorId,
        startUtc: slot.startUtc,
        endUtc: slot.endUtc
      })

      toast.success(`Job assigned to ${bestRecommendation.contractorName}!`)
      
      if (onAssigned) {
        onAssigned()
      }
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      toast.error(`Failed to assign: ${errorMessage}`)
    } finally {
      setAssigning(false)
    }
  }

  // Subscribe to SignalR RecommendationReady events
  useEffect(() => {
    if (!signalRClient || !isConnected) return

    const handleRecommendationReady = (payload: any) => {
      console.log("[BestAssignmentCard] RecommendationReady event received:", payload)
      
      // Check if this event is for our job
      if (payload.jobId === jobId) {
        console.log("[BestAssignmentCard] Refetching recommendations for job", jobId)
        fetchBestAssignment()
        setRecalculating(false)
        toast.success("Recommendations updated!")
      }
    }

    // Use the SignalRClient's onRecommendationReady method which returns an unsubscribe function
    const unsubscribe = signalRClient.onRecommendationReady(handleRecommendationReady)

    return () => {
      unsubscribe()
    }
  }, [signalRClient, isConnected, jobId, fetchBestAssignment])

  // Initial fetch
  useEffect(() => {
    fetchBestAssignment()
  }, [fetchBestAssignment])

  const formatTimeSlot = (slot: TimeSlot) => {
    const start = new Date(slot.startUtc)
    const end = new Date(slot.endUtc)
    
    const dateFormat: Intl.DateTimeFormatOptions = { 
      weekday: 'short', 
      month: 'short', 
      day: 'numeric' 
    }
    const timeFormat: Intl.DateTimeFormatOptions = { 
      hour: 'numeric', 
      minute: '2-digit',
      hour12: true 
    }

    const startDate = start.toLocaleDateString('en-US', dateFormat)
    const startTime = start.toLocaleTimeString('en-US', timeFormat)
    const endTime = end.toLocaleTimeString('en-US', timeFormat)

    // Check if it spans multiple days
    const daysDiff = Math.floor((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24))
    
    if (daysDiff >= 1) {
      const endDate = end.toLocaleDateString('en-US', dateFormat)
      return (
        <div>
          <div className="font-medium">{startDate} - {endDate} ({daysDiff + 1} days)</div>
          <div className="text-xs text-muted-foreground mt-1">
            Multi-day assignment
          </div>
        </div>
      )
    }

    return (
      <div>
        <div className="font-medium">{startDate}</div>
        <div className="text-sm text-muted-foreground">{startTime} - {endTime}</div>
      </div>
    )
  }

  if (loading) {
    return (
      <Card>
        <CardContent className="pt-6">
          <div className="flex items-center justify-center gap-2 py-4">
            <Spinner className="h-4 w-4" />
            <span className="text-sm text-muted-foreground">Loading best assignment...</span>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card className="border-destructive/50">
        <CardContent className="pt-6">
          <div className="text-sm text-destructive mb-2">Failed to load recommendations</div>
          <Button onClick={fetchBestAssignment} variant="outline" size="sm">
            Try Again
          </Button>
        </CardContent>
      </Card>
    )
  }

  if (!bestRecommendation) {
    return (
      <Card className="border-muted">
        <CardContent className="pt-6">
          <div className="text-sm text-muted-foreground mb-3">No recommendations calculated yet</div>
          <Button 
            onClick={handleRecalculate} 
            disabled={recalculating}
            size="sm"
            variant="outline"
            className="gap-2"
          >
            <RefreshCw className={`h-4 w-4 ${recalculating ? "animate-spin" : ""}`} />
            {recalculating ? "Calculating..." : "Calculate Now"}
          </Button>
        </CardContent>
      </Card>
    )
  }

  const bestSlot = bestRecommendation.suggestedSlots.length > 0 
    ? bestRecommendation.suggestedSlots[0] 
    : null

  return (
    <Card className="border-primary/20 bg-primary/5">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <Sparkles className="h-4 w-4 text-primary" />
          Best Assignment
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Contractor Info */}
        <div className="flex items-start justify-between">
          <div>
            <div className="font-semibold">{bestRecommendation.contractorName}</div>
            <div className="text-xs text-muted-foreground mt-1">
              {bestRecommendation.distance && (
                <span>{Math.round(bestRecommendation.distance / 1000)} km away</span>
              )}
              {bestRecommendation.eta && (
                <span> • {bestRecommendation.eta} min travel</span>
              )}
            </div>
          </div>
          <Badge variant="default" className="gap-1">
            {Math.round(bestRecommendation.score)}
            <span className="text-xs opacity-80">/100</span>
          </Badge>
        </div>

        {/* Time Slot */}
        {bestSlot && (
          <div className="rounded-lg border bg-background p-3 space-y-2">
            <div className="flex items-center gap-2 text-xs text-muted-foreground uppercase tracking-wide">
              <Calendar className="h-3 w-3" />
              Recommended Time
            </div>
            {formatTimeSlot(bestSlot)}
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <Clock className="h-3 w-3" />
              Confidence: {bestSlot.confidence}%
            </div>
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex gap-2">
          <Button
            onClick={handleScheduleNow}
            disabled={assigning || !bestSlot}
            variant="outline"
            className="flex-1 gap-2"
          >
            {assigning ? (
              <>
                <Spinner className="h-4 w-4" />
                Assigning...
              </>
            ) : (
              <>
                <CheckCircle2 className="h-4 w-4" />
                Schedule Now
              </>
            )}
          </Button>
          <Button
            onClick={handleRecalculate}
            disabled={recalculating}
            variant="outline"
            size="icon"
            title="Recalculate recommendations"
          >
            <RefreshCw className={`h-4 w-4 ${recalculating ? "animate-spin" : ""}`} />
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}

