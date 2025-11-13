"use client"

import type React from "react"
import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from "@/components/ui/sheet"
import { Badge } from "@/components/ui/badge"
import { RecommendationCard } from "./recommendation-card"
import { Sparkles, Clock, MapPin, ChevronLeft, ChevronRight, CalendarIcon, Map } from "lucide-react"
import { MapViewDialog } from "@/components/map-view-dialog"
import { useAuth } from "@/lib/auth/auth-context"
import { formatErrorForDisplay } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import { useSignalR } from "@/hooks/use-signalr"

interface Job {
  id: string
  type: string
  address?: string
  scheduledDate?: string
  scheduledTime?: string
  duration?: number
  priority?: string
  requiredSkills?: string[]
  desiredDate?: string
  serviceWindow?: {
    start?: string
    end?: string
  }
  location?: {
    formattedAddress?: string
    address?: string
    [key: string]: any
  }
}

interface RecommendationsSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  job: Job
}

// API response types
interface ApiRecommendation {
  contractorId: string
  contractorName: string
  score: number
  scoreBreakdown: {
    availability: number
    rating: number
    distance: number
    rotation?: number
  }
  rationale: string
  suggestedSlots: Array<{
    startUtc: string
    endUtc: string
    type: string
    confidence: number
  }>
  distance: number
  eta: number
}

interface ApiRecommendationResponse {
  requestId: string
  jobId: string
  recommendations: ApiRecommendation[]
  configVersion: number
  generatedAt: string
}

// Transform API response to component format
const transformRecommendation = (apiRec: ApiRecommendation, index: number) => {
  // Format time slots
  const slots = apiRec.suggestedSlots.map((slot) => {
    const startDate = new Date(slot.startUtc)
    const timeStr = startDate.toLocaleTimeString("en-US", {
      hour: "numeric",
      minute: "2-digit",
      hour12: true,
    })
    
    let label = slot.type
    if (slot.type === "earliest") label = "Earliest"
    else if (slot.type === "lowest-travel") label = "Lowest Travel"
    else if (slot.type === "highest-confidence") label = "Best Fit"
    
    let confidence = "Medium"
    if (slot.confidence >= 80) confidence = "High"
    else if (slot.confidence < 60) confidence = "Low"
    
    return {
      time: timeStr,
      label,
      confidence,
    }
  })

  // Format distance
  const distanceMiles = (apiRec.distance / 1609.34).toFixed(1) // meters to miles
  const travelTime = `${apiRec.eta} min`

  return {
    contractorId: apiRec.contractorId,
    contractorName: apiRec.contractorName,
    baseLocation: "Base Location", // Not in API response, using placeholder
    rating: Math.round(apiRec.scoreBreakdown.rating),
    totalScore: Math.round(apiRec.score),
    scores: {
      availability: Math.round(apiRec.scoreBreakdown.availability),
      rating: Math.round(apiRec.scoreBreakdown.rating),
      distance: Math.round(apiRec.scoreBreakdown.distance),
      rotation: apiRec.scoreBreakdown.rotation ? Math.round(apiRec.scoreBreakdown.rotation) : 0,
    },
    travelTime,
    travelDistance: `${distanceMiles} miles`,
    rationale: apiRec.rationale,
    suggestedSlots: slots,
    currentUtilization: 0, // Not in API response
    jobsToday: 0, // Not in API response
    skills: [], // Not in API response
  }
}

export function RecommendationsSheet({ open, onOpenChange, job }: RecommendationsSheetProps) {
  const { getTokenProvider } = useAuth()
  const { client } = useSignalR()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [selectedDate, setSelectedDate] = useState(() => {
    // Try to get date from scheduledDate, desiredDate, or serviceWindow.start
    const dateString = job.scheduledDate || job.desiredDate || job.serviceWindow?.start
    
    if (!dateString) {
      // Default to today if no date available
      return new Date().toISOString().split("T")[0]
    }
    
    try {
      const date = new Date(dateString)
      // Check if date is valid
      if (isNaN(date.getTime())) {
        console.warn("[RecommendationsSheet] Invalid date:", dateString)
        return new Date().toISOString().split("T")[0]
      }
      return date.toISOString().split("T")[0]
    } catch (err) {
      console.error("[RecommendationsSheet] Error parsing date:", dateString, err)
      return new Date().toISOString().split("T")[0]
    }
  })

  const [recommendations, setRecommendations] = useState<any[]>([])
  const [mapOpen, setMapOpen] = useState(false)
  const [assigningContractorId, setAssigningContractorId] = useState<string | null>(null)
  const [assigningSlotTime, setAssigningSlotTime] = useState<string | null>(null)

  // Subscribe to RecommendationReady events
  useEffect(() => {
    if (!client || !open) return

    const unsubscribe = client.onRecommendationReady((event) => {
      // Only refresh if this event is for the current job
      if (event.jobId === job.id) {
        console.log("RecommendationReady event received for job", job.id)
        fetchRecommendations(selectedDate)
      }
    })

    return unsubscribe
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [client, open, job.id])

  const fetchRecommendations = async (dateString: string) => {
    setLoading(true)
    setError(null)
    
    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5004"
      const tokenProvider = getTokenProvider()
      const token = tokenProvider?.getToken() || null
      
      const headers: Record<string, string> = {
        "Content-Type": "application/json",
      }
      
      if (token) {
        headers["Authorization"] = `Bearer ${token}`
      }

      const response = await fetch(`${apiUrl}/api/recommendations`, {
        method: "POST",
        headers,
        body: JSON.stringify({
          jobId: job.id,
          desiredDate: dateString,
          maxResults: 10,
        }),
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: "Failed to fetch recommendations" }))
        throw new Error(errorData.message || `HTTP ${response.status}`)
      }

      const data: ApiRecommendationResponse = await response.json()
      const transformed = data.recommendations.map((rec, index) => transformRecommendation(rec, index))
      setRecommendations(transformed)
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      setError(errorMessage)
      toast.error(`Failed to load recommendations: ${errorMessage}`)
      setRecommendations([])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (open && job.id) {
      fetchRecommendations(selectedDate)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedDate, open, job.id])

  const navigateDate = (direction: "prev" | "next") => {
    const currentDate = new Date(selectedDate)
    currentDate.setDate(currentDate.getDate() + (direction === "next" ? 1 : -1))
    setSelectedDate(currentDate.toISOString().split("T")[0])
  }

  const handleDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSelectedDate(e.target.value)
  }

  const formatDisplayDate = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleDateString("en-US", {
      weekday: "short",
      month: "short",
      day: "numeric",
      year: "numeric",
    })
  }

  const handleAssign = async (contractorId: string, slotTime: string) => {
    setAssigningContractorId(contractorId)
    setAssigningSlotTime(slotTime)
    
    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5004"
      const tokenProvider = getTokenProvider()
      const token = tokenProvider?.getToken() || null
      
      const headers: Record<string, string> = {
        "Content-Type": "application/json",
      }
      
      if (token) {
        headers["Authorization"] = `Bearer ${token}`
      }

      // Parse slotTime - it might be a JSON string from calendar scheduler or a time string
      let startUtc: string
      let endUtc: string
      
      try {
        // Try parsing as JSON first (from calendar scheduler)
        const sessions = JSON.parse(slotTime)
        if (Array.isArray(sessions) && sessions.length > 0) {
          // Calendar scheduler sends ScheduleSession[] with date, startTime, endTime
          const session = sessions[0]
          const sessionDate = session.date || selectedDate
          
          // Parse startTime and endTime (format: "HH:mm")
          const [startHours, startMinutes] = (session.startTime || "08:00").split(":").map(Number)
          const [endHours, endMinutes] = (session.endTime || "10:00").split(":").map(Number)
          
          // Create date objects in local timezone, then convert to UTC
          const startDate = new Date(`${sessionDate}T${String(startHours).padStart(2, '0')}:${String(startMinutes).padStart(2, '0')}:00`)
          const endDate = new Date(`${sessionDate}T${String(endHours).padStart(2, '0')}:${String(endMinutes).padStart(2, '0')}:00`)
          
          // If we have durationHours, use that to calculate end time
          if (session.durationHours) {
            endDate.setTime(startDate.getTime() + session.durationHours * 60 * 60 * 1000)
          }
          
          startUtc = startDate.toISOString()
          endUtc = endDate.toISOString()
        } else {
          throw new Error("Invalid session format")
        }
      } catch {
        // If not JSON, try to parse as time string and construct UTC dates
        // slotTime might be like "08:00" or "08:00 - 10:00"
        const selectedDateTime = new Date(selectedDate)
        const [timePart] = slotTime.split(" - ")
        const [hours, minutes] = timePart.split(":").map(Number)
        selectedDateTime.setHours(hours, minutes || 0, 0, 0)
        
        // Assume job duration if available, otherwise default to 2 hours
        const durationMinutes = job.duration || 120
        startUtc = selectedDateTime.toISOString()
        endUtc = new Date(selectedDateTime.getTime() + durationMinutes * 60000).toISOString()
      }

      // Optimistic update: mark the contractor as assigned in UI
      setRecommendations(prev => prev.map(rec => 
        rec.contractorId === contractorId 
          ? { ...rec, isAssigning: true }
          : rec
      ))

      const response = await fetch(`${apiUrl}/api/jobs/${job.id}/assign`, {
        method: "POST",
        headers,
        body: JSON.stringify({
          contractorId,
          startUtc,
          endUtc,
          source: "Auto",
        }),
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: "Failed to assign job" }))
        
        // Rollback optimistic update
        setRecommendations(prev => prev.map(rec => 
          rec.contractorId === contractorId 
            ? { ...rec, isAssigning: false }
            : rec
        ))

        if (response.status === 409) {
          // Conflict - contractor unavailable
          toast.error(`Assignment failed: ${errorData.message || "Contractor is not available for this time slot."}`)
        } else if (response.status === 404) {
          toast.error(`Assignment failed: ${errorData.message || "Job or contractor not found."}`)
        } else if (response.status === 400) {
          toast.error(`Assignment failed: ${errorData.message || "Invalid request."}`)
        } else {
          toast.error(`Assignment failed: ${errorData.message || `HTTP ${response.status}`}`)
        }
        return
      }

      const assignment = await response.json()
      
      // Success - update UI
      toast.success(`Job assigned to ${recommendations.find(r => r.contractorId === contractorId)?.contractorName || "contractor"} successfully!`)
      
      // Update recommendations to mark assigned contractor
      setRecommendations(prev => prev.map(rec => 
        rec.contractorId === contractorId 
          ? { ...rec, isAssigned: true, isAssigning: false }
          : rec
      ))

      // Close the sheet after a short delay to show success message
      setTimeout(() => {
        onOpenChange(false)
        // Trigger a refresh of job data (parent component should handle this)
        window.dispatchEvent(new CustomEvent('jobAssigned', { detail: { jobId: job.id, assignment } }))
      }, 1000)
    } catch (err) {
      // Rollback optimistic update
      setRecommendations(prev => prev.map(rec => 
        rec.contractorId === contractorId 
          ? { ...rec, isAssigning: false }
          : rec
      ))

      const errorMessage = formatErrorForDisplay(err)
      toast.error(`Failed to assign job: ${errorMessage}`)
    } finally {
      setAssigningContractorId(null)
      setAssigningSlotTime(null)
    }
  }

  const mapLocations = [
    {
      type: "job" as const,
      name: job.type,
      address: job.address || job.location?.formattedAddress || job.location?.address || "Address not available",
    },
    ...recommendations.map((rec) => ({
      type: "contractor" as const,
      name: rec.contractorName,
      address: rec.baseLocation,
      skills: rec.skills,
    })),
  ]

  return (
    <>
      <Sheet open={open} onOpenChange={onOpenChange}>
        <SheetContent className="w-full sm:max-w-2xl overflow-y-auto">
          <SheetHeader>
            <SheetTitle className="flex items-center gap-2">
              <Sparkles className="h-5 w-5 text-primary" />
              Contractor Recommendations
            </SheetTitle>
            <SheetDescription>
              Find the best contractors for this job based on skills, availability, and location.
            </SheetDescription>
            <div className="space-y-2 pt-2 text-sm text-muted-foreground">
              <div className="font-semibold text-foreground text-balance">{job.type}</div>
              <div className="flex flex-col gap-1">
                <div className="flex items-center gap-1.5">
                  <MapPin className="h-3 w-3" />
                  <span className="text-pretty">{job.address || job.location?.formattedAddress || job.location?.address || "Address not available"}</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <Clock className="h-3 w-3" />
                  <span>
                    {job.scheduledDate || job.desiredDate || job.serviceWindow?.start 
                      ? new Date(job.scheduledDate || job.desiredDate || job.serviceWindow?.start || "").toLocaleDateString()
                      : "Date not set"
                    } 
                    {job.serviceWindow?.start && (
                      <> at {new Date(job.serviceWindow.start).toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit", hour12: true })}</>
                    )}
                    {job.duration && ` (${Math.round(job.duration / 60)}h)`}
                  </span>
                </div>
              </div>
              <div className="flex flex-wrap gap-1 pt-1">
                {(job.requiredSkills || []).map((skill) => (
                  <Badge key={skill} variant="secondary" className="text-xs">
                    {skill}
                  </Badge>
                ))}
              </div>
              <Button
                variant="outline"
                size="sm"
                className="mt-2 w-full bg-transparent"
                onClick={() => setMapOpen(true)}
              >
                <Map className="mr-2 h-4 w-4" />
                View Map
              </Button>
            </div>
          </SheetHeader>

          <div className="mt-6 flex items-center justify-between gap-2 p-3 bg-muted/50 rounded-lg border border-border">
            <Button variant="ghost" size="icon" onClick={() => navigateDate("prev")} className="h-8 w-8">
              <ChevronLeft className="h-4 w-4" />
            </Button>

            <div className="flex items-center gap-2 flex-1 justify-center">
              <CalendarIcon className="h-4 w-4 text-muted-foreground" />
              <div className="relative">
                <input
                  type="date"
                  value={selectedDate}
                  onChange={handleDateChange}
                  className="px-3 py-1 text-sm font-medium bg-background border border-input rounded-md cursor-pointer hover:bg-accent transition-colors"
                />
              </div>
              <span className="text-sm font-medium text-foreground">{formatDisplayDate(selectedDate)}</span>
            </div>

            <Button variant="ghost" size="icon" onClick={() => navigateDate("next")} className="h-8 w-8">
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>

          <div className="mt-6 space-y-4">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {loading ? "Refreshing..." : `Showing ${recommendations.length} qualified contractors`}
              </p>
              <Button
                variant="outline"
                size="sm"
                onClick={() => fetchRecommendations(selectedDate)}
                disabled={loading}
              >
                {loading ? "Refreshing..." : "Refresh"}
              </Button>
            </div>

            {error && (
              <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4">
                <p className="text-sm text-destructive">{error}</p>
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-2"
                  onClick={() => fetchRecommendations(selectedDate)}
                >
                  Retry
                </Button>
              </div>
            )}
            
            {loading && recommendations.length === 0 ? (
              <div className="flex flex-col items-center justify-center gap-4 py-12">
                <Spinner className="h-8 w-8" />
                <p className="text-sm text-muted-foreground">Loading recommendations...</p>
              </div>
            ) : (
              <div className="space-y-4">
                {recommendations.length === 0 && !loading ? (
                  <div className="rounded-lg border border-border bg-muted/50 p-8 text-center">
                    <p className="text-sm text-muted-foreground">No recommendations available for this date.</p>
                  </div>
                ) : (
                  recommendations.map((recommendation, index) => (
                    <RecommendationCard
                      key={recommendation.contractorId}
                      recommendation={recommendation}
                      rank={index + 1}
                      onAssign={handleAssign}
                      jobDuration={job.duration ? Math.round(job.duration / 60) : 2}
                    />
                  ))
                )}
              </div>
            )}
          </div>
        </SheetContent>
      </Sheet>

      <MapViewDialog
        open={mapOpen}
        onOpenChange={setMapOpen}
        locations={mapLocations}
        title={`Contractor Locations: ${job.type}`}
      />
    </>
  )
}
