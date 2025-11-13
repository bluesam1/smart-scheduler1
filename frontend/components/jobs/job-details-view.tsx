"use client"

import { useState, useEffect } from "react"
import { ArrowLeft, MapPin, Calendar, Clock, Sparkles, User, Edit2, Map, ChevronDown, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { RecommendationsSheet } from "./recommendations-sheet"
import Link from "next/link"
import { MapViewDialog } from "@/components/map-view-dialog"
import { MapInline } from "@/components/map-inline"
import { createApiClients } from "@/lib/api/api-client-config"
import { useAuth } from "@/lib/auth/auth-context"
import { useSignalR } from "@/hooks/use-signalr"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import { GooglePlacesAutocomplete, type PlaceResult } from "@/components/ui/google-places-autocomplete"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import { estimateTimezoneFromCoordinates } from "@/lib/timezone-utils"
import type { JobDto, UpdateJobRequest, GeoLocationDto } from "@/lib/api/generated/api-client"
import type { JobAssigned, JobRescheduled, JobCancelled } from "@/lib/realtime/signalr-types"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"

export function JobDetailsView({ jobId }: { jobId: string }) {
  const { getTokenProvider } = useAuth()
  const { client: signalRClient } = useSignalR()
  const [job, setJob] = useState<JobDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [recommendationsOpen, setRecommendationsOpen] = useState(false)
  const [mapOpen, setMapOpen] = useState(false)
  
  // Address editing state
  const [isEditingAddress, setIsEditingAddress] = useState(false)
  const [isAddressDetailsOpen, setIsAddressDetailsOpen] = useState(false)
  const [editedAddress, setEditedAddress] = useState("") // Street address
  const [editedFormattedAddress, setEditedFormattedAddress] = useState("") // Full formatted address
  const [editedCity, setEditedCity] = useState("")
  const [editedState, setEditedState] = useState("")
  const [editedPostalCode, setEditedPostalCode] = useState("")
  const [editedLatitude, setEditedLatitude] = useState("")
  const [editedLongitude, setEditedLongitude] = useState("")
  const [selectedPlace, setSelectedPlace] = useState<PlaceResult | null>(null)
  const [isSavingAddress, setIsSavingAddress] = useState(false)
  
  // Reschedule state
  const [rescheduleOpen, setRescheduleOpen] = useState(false)
  const [rescheduleDate, setRescheduleDate] = useState("")
  const [rescheduleStartTime, setRescheduleStartTime] = useState("")
  const [isRescheduling, setIsRescheduling] = useState(false)
  
  // Cancel state
  const [cancelOpen, setCancelOpen] = useState(false)
  const [cancelReason, setCancelReason] = useState("")
  const [isCancelling, setIsCancelling] = useState(false)

  useEffect(() => {
    const fetchJob = async () => {
      if (!jobId || jobId.trim() === "") {
        setError("Job ID is required")
        setIsLoading(false)
        return
      }

      // Validate GUID format
      const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
      if (!guidRegex.test(jobId)) {
        setError("Invalid job ID format")
        setIsLoading(false)
        return
      }

      setIsLoading(true)
      setError(null)
      
      try {
        const tokenProvider = getTokenProvider()
        if (!tokenProvider) {
          setError("Authentication required")
          setIsLoading(false)
          toast.error("Please log in to view job details")
          return
        }

        const { client } = createApiClients(tokenProvider)
        console.log("[JobDetailsView] Fetching job with ID:", jobId)
        const data = await client.getJobById(jobId)
        
        if (!data) {
          setError("Job not found")
          setIsLoading(false)
          return
        }
        
        console.log("[JobDetailsView] Job loaded successfully:", data)
        setJob(data)
        
        // Initialize address editing state
        if (data.location) {
          setEditedFormattedAddress(data.location.formattedAddress || "")
          setEditedAddress(data.location.address || "")
          setEditedCity(data.location.city || "")
          setEditedState(data.location.state || "")
          setEditedPostalCode(data.location.postalCode || "")
          setEditedLatitude(data.location.latitude?.toString() || "")
          setEditedLongitude(data.location.longitude?.toString() || "")
        }
      } catch (err) {
        console.error("[JobDetailsView] Error loading job:", err)
        const errorMessage = formatErrorForDisplay(err)
        setError(errorMessage)
        
        if (isAuthenticationError(err)) {
          toast.error("Please log in to view job details")
        } else {
          toast.error(`Failed to load job: ${errorMessage}`)
        }
      } finally {
        setIsLoading(false)
      }
    }

    fetchJob()
  }, [jobId, getTokenProvider])

  // Subscribe to JobAssigned events via SignalR
  useEffect(() => {
    if (!signalRClient || !jobId) return

    const handleJobAssigned = async (event: JobAssigned) => {
      // Only update if this event is for the current job
      if (event.jobId === jobId) {
        console.log("JobAssigned event received for current job:", event)
        
        // Refresh job details from API
        try {
          const tokenProvider = getTokenProvider()
          if (tokenProvider) {
            const { client } = createApiClients(tokenProvider)
            const updatedJob = await client.getJobById(jobId)
            if (updatedJob) {
              setJob(updatedJob)
              toast.success("Job assignment updated")
            }
          }
        } catch (err) {
          console.error("Failed to refresh job details after assignment:", err)
          toast.error("Failed to refresh job details")
        }
      }
    }

    const unsubscribe = signalRClient.onJobAssigned(handleJobAssigned)

    return () => {
      unsubscribe()
    }
  }, [signalRClient, jobId, getTokenProvider])

  // Subscribe to JobRescheduled events via SignalR
  useEffect(() => {
    if (!signalRClient || !jobId) return

    const handleJobRescheduled = async (event: JobRescheduled) => {
      // Only update if this event is for the current job
      if (event.jobId === jobId) {
        console.log("JobRescheduled event received for current job:", event)
        
        // Refresh job details from API
        try {
          const tokenProvider = getTokenProvider()
          if (tokenProvider) {
            const { client } = createApiClients(tokenProvider)
            const updatedJob = await client.getJobById(jobId)
            if (updatedJob) {
              setJob(updatedJob)
              toast.success("Job rescheduled - time updated")
            }
          }
        } catch (err) {
          console.error("Failed to refresh job details after reschedule:", err)
          toast.error("Failed to refresh job details")
        }
      }
    }

    const unsubscribe = signalRClient.onJobRescheduled(handleJobRescheduled)

    return () => {
      unsubscribe()
    }
  }, [signalRClient, jobId, getTokenProvider])

  // Subscribe to JobCancelled events via SignalR
  useEffect(() => {
    if (!signalRClient || !jobId) return

    const handleJobCancelled = async (event: JobCancelled) => {
      // Only update if this event is for the current job
      if (event.jobId === jobId) {
        console.log("JobCancelled event received for current job:", event)
        
        // Refresh job details from API
        try {
          const tokenProvider = getTokenProvider()
          if (tokenProvider) {
            const { client } = createApiClients(tokenProvider)
            const updatedJob = await client.getJobById(jobId)
            if (updatedJob) {
              setJob(updatedJob)
              toast.info(`Job cancelled: ${event.reason || "No reason provided"}`)
            }
          }
        } catch (err) {
          console.error("Failed to refresh job details after cancellation:", err)
          toast.error("Failed to refresh job details")
        }
      }
    }

    const unsubscribe = signalRClient.onJobCancelled(handleJobCancelled)

    return () => {
      unsubscribe()
    }
  }, [signalRClient, jobId, getTokenProvider])

  // Handler for reschedule
  const handleReschedule = async () => {
    if (!job || !rescheduleDate || !rescheduleStartTime) {
      toast.error("Please select a date and time")
      return
    }

    setIsRescheduling(true)
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

      // Calculate start and end times
      const startDateTime = new Date(`${rescheduleDate}T${rescheduleStartTime}:00`)
      const durationMinutes = job.duration || 120
      const endDateTime = new Date(startDateTime.getTime() + durationMinutes * 60000)

      const response = await fetch(`${apiUrl}/api/jobs/${job.id}/reschedule`, {
        method: "PUT",
        headers,
        body: JSON.stringify({
          startUtc: startDateTime.toISOString(),
          endUtc: endDateTime.toISOString(),
        }),
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: "Failed to reschedule job" }))
        
        if (response.status === 409) {
          toast.error(`Reschedule failed: ${errorData.message || "Time slot conflicts with existing assignments."}`)
        } else if (response.status === 404) {
          toast.error(`Reschedule failed: ${errorData.message || "Job not found."}`)
        } else if (response.status === 400) {
          toast.error(`Reschedule failed: ${errorData.message || "Invalid request."}`)
        } else {
          toast.error(`Reschedule failed: ${errorData.message || `HTTP ${response.status}`}`)
        }
        return
      }

      const updatedJob = await response.json()
      setJob(updatedJob)
      setRescheduleOpen(false)
      toast.success("Job rescheduled successfully!")
      
      // Refresh job details
      const { client } = createApiClients(tokenProvider!)
      const refreshedJob = await client.getJobById(jobId)
      if (refreshedJob) {
        setJob(refreshedJob)
      }
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      toast.error(`Failed to reschedule job: ${errorMessage}`)
    } finally {
      setIsRescheduling(false)
    }
  }

  // Handler for cancel
  const handleCancel = async () => {
    if (!job) return

    setIsCancelling(true)
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

      const response = await fetch(`${apiUrl}/api/jobs/${job.id}/cancel`, {
        method: "POST",
        headers,
        body: JSON.stringify({
          reason: cancelReason || "No reason provided",
        }),
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: "Failed to cancel job" }))
        
        if (response.status === 400) {
          toast.error(`Cancel failed: ${errorData.message || "Cannot cancel this job."}`)
        } else if (response.status === 404) {
          toast.error(`Cancel failed: ${errorData.message || "Job not found."}`)
        } else {
          toast.error(`Cancel failed: ${errorData.message || `HTTP ${response.status}`}`)
        }
        return
      }

      const updatedJob = await response.json()
      setJob(updatedJob)
      setCancelOpen(false)
      setCancelReason("")
      toast.success("Job cancelled successfully!")
      
      // Refresh job details
      const { client } = createApiClients(tokenProvider!)
      const refreshedJob = await client.getJobById(jobId)
      if (refreshedJob) {
        setJob(refreshedJob)
      }
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      toast.error(`Failed to cancel job: ${errorMessage}`)
    } finally {
      setIsCancelling(false)
    }
  }

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-12">
        <Spinner className="h-8 w-8" />
        <p className="text-sm text-muted-foreground">Loading job details...</p>
      </div>
    )
  }

  if (error || !job) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-12">
        <p className="text-sm text-destructive">{error || "Job not found"}</p>
        <Button variant="outline" asChild>
          <Link href="/jobs">Back to Jobs</Link>
        </Button>
      </div>
    )
  }

  const formatDate = (dateString?: string) => {
    if (!dateString) return "-"
    try {
      const date = new Date(dateString)
      return date.toLocaleDateString()
    } catch {
      return dateString
    }
  }

  const formatTime = (dateString?: string) => {
    if (!dateString) return "-"
    try {
      const date = new Date(dateString)
      return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    } catch {
      return dateString
    }
  }

  const handleSaveAddress = async () => {
    if (!job?.id) {
      toast.error("Job ID is missing")
      return
    }

    setIsSavingAddress(true)

    try {
      const tokenProvider = getTokenProvider()
      if (!tokenProvider) {
        toast.error("Please log in to update job address")
        setIsSavingAddress(false)
        return
      }
      const { client } = createApiClients(tokenProvider)

      // Prepare location update
      let location: GeoLocationDto | undefined
      const originalAddress = job.location?.formattedAddress || job.location?.address || ""
      
      if (editedFormattedAddress !== originalAddress && editedFormattedAddress.trim()) {
        // If a place was selected, use its coordinates; otherwise preserve existing coordinates
        if (selectedPlace) {
          location = {
            latitude: selectedPlace.latitude,
            longitude: selectedPlace.longitude,
            address: selectedPlace.address || undefined,
            city: selectedPlace.city || undefined,
            state: selectedPlace.state || undefined,
            postalCode: selectedPlace.postalCode || undefined,
            country: selectedPlace.country || undefined,
            formattedAddress: selectedPlace.formattedAddress || undefined,
            placeId: selectedPlace.placeId || undefined,
          } as GeoLocationDto
        } else if (editedLatitude && editedLongitude) {
          // Use manually entered coordinates
          location = {
            latitude: parseFloat(editedLatitude),
            longitude: parseFloat(editedLongitude),
            address: editedAddress || undefined,
            city: editedCity || undefined,
            state: editedState || undefined,
            postalCode: editedPostalCode || undefined,
            country: "US",
            formattedAddress: editedFormattedAddress || undefined,
          } as GeoLocationDto
        } else {
          // Preserve existing coordinates and update address fields only
          location = {
            ...(job.location || {}),
            address: editedAddress || undefined,
            city: editedCity || undefined,
            state: editedState || undefined,
            postalCode: editedPostalCode || undefined,
            formattedAddress: editedFormattedAddress || undefined,
          } as GeoLocationDto
        }
      }

      if (!location) {
        toast.info("No changes to save")
        setIsSavingAddress(false)
        setIsEditingAddress(false)
        return
      }

      const updateRequest: UpdateJobRequest = {
        location,
      }

      const updatedJob = await client.updateJob(job.id, updateRequest)
      setJob(updatedJob)
      setSelectedPlace(null)
      setIsEditingAddress(false)
      toast.success("Job address updated successfully")
    } catch (err) {
      console.error("[JobDetailsView] Error updating address:", err)
      const errorMessage = formatErrorForDisplay(err)
      
      if (isAuthenticationError(err)) {
        toast.error("Please log in to update job address")
      } else {
        toast.error(`Failed to update address: ${errorMessage}`)
      }
    } finally {
      setIsSavingAddress(false)
    }
  }

  const address = job.location?.formattedAddress || 
    `${job.location?.address || ""}, ${job.location?.city || ""}, ${job.location?.state || ""} ${job.location?.postalCode || ""}`.trim()

  const mapLocations = [
    {
      type: "job" as const,
      name: job.type || "Job",
      address: address,
      coordinates: job.location ? {
        lat: job.location.latitude || 0,
        lng: job.location.longitude || 0,
      } : undefined,
    },
    ...(job.assignedContractors && job.assignedContractors.length > 0
      ? job.assignedContractors.map((assignment) => ({
          type: "contractor" as const,
          name: `Contractor ${assignment.contractorId?.substring(0, 8)}`,
          address: "Contractor location", // In production, fetch contractor's actual address
        }))
      : []),
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link href="/jobs">
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div className="flex-1">
          <h1 className="text-3xl font-bold">{job.type || "Unknown Job"}</h1>
          <p className="text-muted-foreground">Job ID: {job.id}</p>
        </div>
        <div className="flex gap-2">
          <Badge
            variant={
              job.assignmentStatus === "Unassigned"
                ? "outline"
                : job.assignmentStatus === "PartiallyAssigned"
                  ? "secondary"
                  : "default"
            }
          >
            {job.assignmentStatus || "Unassigned"}
          </Badge>
          <Badge variant="outline">{job.status || "Created"}</Badge>
          {job.priority === "Rush" && <Badge variant="destructive">Rush</Badge>}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2 space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Job Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <div className="text-sm font-medium text-muted-foreground mb-1">Description</div>
                <p>{job.description || "No description provided"}</p>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <div className="text-sm font-medium text-muted-foreground mb-1">Desired Start Date</div>
                  <div className="flex items-center gap-2">
                    <Calendar className="h-4 w-4 text-muted-foreground" />
                    {formatDate(job.serviceWindow?.start)}
                  </div>
                </div>
                <div>
                  <div className="text-sm font-medium text-muted-foreground mb-1">Desired Start Time</div>
                  <div className="flex items-center gap-2">
                    <Clock className="h-4 w-4 text-muted-foreground" />
                    {formatTime(job.serviceWindow?.start)}
                  </div>
                </div>
              </div>

              <div>
                <div className="flex items-center justify-between mb-2">
                  <div className="text-sm font-medium text-muted-foreground">Location</div>
                  {!isEditingAddress && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setIsEditingAddress(true)}
                      className="h-7 text-xs"
                    >
                      <Edit2 className="mr-1 h-3 w-3" />
                      Edit
                    </Button>
                  )}
                </div>
                
                {isEditingAddress ? (
                  <div className="space-y-4">
                    <GooglePlacesAutocomplete
                      label="Job Address"
                      value={editedFormattedAddress}
                      onChange={setEditedFormattedAddress}
                      onPlaceSelect={(place: PlaceResult) => {
                        setEditedAddress(place.address)
                        setEditedFormattedAddress(place.formattedAddress)
                        setEditedCity(place.city)
                        setEditedState(place.state)
                        setEditedPostalCode(place.postalCode)
                        setEditedLatitude(place.latitude.toString())
                        setEditedLongitude(place.longitude.toString())
                        setSelectedPlace(place)
                      }}
                      placeholder="Start typing address..."
                      disabled={isSavingAddress}
                      id="job-address-edit"
                    />
                    
                    <Collapsible open={isAddressDetailsOpen} onOpenChange={setIsAddressDetailsOpen}>
                      <CollapsibleTrigger asChild>
                        <Button
                          type="button"
                          variant="ghost"
                          className="w-full justify-between p-0 h-auto font-normal text-sm text-muted-foreground hover:text-foreground"
                        >
                          <span className="truncate text-left">
                            {editedFormattedAddress || [editedAddress, editedCity, editedState, editedPostalCode].filter(Boolean).join(", ") || "Address Details (Auto-filled from address)"}
                          </span>
                          <ChevronDown className={`h-4 w-4 transition-transform flex-shrink-0 ml-2 ${isAddressDetailsOpen ? 'rotate-180' : ''}`} />
                        </Button>
                      </CollapsibleTrigger>
                      <CollapsibleContent className="space-y-4 pt-2">
                        <div className="grid grid-cols-2 gap-4">
                          <div className="grid gap-2">
                            <Label htmlFor="edit-city">City</Label>
                            <Input 
                              id="edit-city" 
                              placeholder="New York" 
                              value={editedCity}
                              onChange={(e) => setEditedCity(e.target.value)}
                              disabled={isSavingAddress}
                            />
                          </div>
                          <div className="grid gap-2">
                            <Label htmlFor="edit-state">State</Label>
                            <Input 
                              id="edit-state" 
                              placeholder="NY" 
                              value={editedState}
                              onChange={(e) => setEditedState(e.target.value.toUpperCase())}
                              disabled={isSavingAddress}
                              maxLength={2}
                              className="uppercase"
                            />
                          </div>
                        </div>
                        
                        <div className="grid gap-2">
                          <Label htmlFor="edit-postalCode">Postal Code</Label>
                          <Input 
                            id="edit-postalCode" 
                            placeholder="10001" 
                            value={editedPostalCode}
                            onChange={(e) => setEditedPostalCode(e.target.value)}
                            disabled={isSavingAddress}
                          />
                        </div>
                        
                        <div className="grid grid-cols-2 gap-4">
                          <div className="grid gap-2">
                            <Label>Latitude</Label>
                            <div className="px-3 py-2 text-sm border rounded-md bg-muted/50 text-muted-foreground">
                              {editedLatitude ? parseFloat(editedLatitude).toFixed(6) : job.location?.latitude?.toFixed(6) || "Not set"}
                            </div>
                          </div>
                          <div className="grid gap-2">
                            <Label>Longitude</Label>
                            <div className="px-3 py-2 text-sm border rounded-md bg-muted/50 text-muted-foreground">
                              {editedLongitude ? parseFloat(editedLongitude).toFixed(6) : job.location?.longitude?.toFixed(6) || "Not set"}
                            </div>
                          </div>
                        </div>
                      </CollapsibleContent>
                    </Collapsible>
                    
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          setIsEditingAddress(false)
                          setSelectedPlace(null)
                          // Reset to original values
                          if (job.location) {
                            setEditedFormattedAddress(job.location.formattedAddress || "")
                            setEditedAddress(job.location.address || "")
                            setEditedCity(job.location.city || "")
                            setEditedState(job.location.state || "")
                            setEditedPostalCode(job.location.postalCode || "")
                            setEditedLatitude(job.location.latitude?.toString() || "")
                            setEditedLongitude(job.location.longitude?.toString() || "")
                          }
                        }}
                        disabled={isSavingAddress}
                      >
                        Cancel
                      </Button>
                      <Button
                        size="sm"
                        onClick={handleSaveAddress}
                        disabled={isSavingAddress}
                      >
                        {isSavingAddress ? (
                          <>
                            <Spinner className="mr-2 h-4 w-4" />
                            Saving...
                          </>
                        ) : (
                          "Save Address"
                        )}
                      </Button>
                    </div>
                  </div>
                ) : (
                  <>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div className="flex items-start gap-2">
                        <MapPin className="h-4 w-4 text-muted-foreground mt-0.5 flex-shrink-0" />
                        <span>{address}</span>
                      </div>
                      {job.location && (job.location.latitude !== 0 || job.location.longitude !== 0) && (
                        <div className="md:mt-0 mt-3">
                          <MapInline locations={mapLocations} height="250px" />
                        </div>
                      )}
                    </div>
                  </>
                )}
              </div>

              <div>
                <div className="text-sm font-medium text-muted-foreground mb-1">Estimated Duration</div>
                <p>{job.duration ? `${Math.round(job.duration / 60)} hours` : "-"}</p>
              </div>

              <div>
                <div className="text-sm font-medium text-muted-foreground mb-1">Timezone</div>
                <Badge variant="outline">{job.timezone || "Unknown"}</Badge>
              </div>

              <div>
                <div className="text-sm font-medium text-muted-foreground mb-1">Required Skills</div>
                <div className="flex flex-wrap gap-2">
                  {job.requiredSkills && job.requiredSkills.length > 0 ? (
                    job.requiredSkills.map((skill) => (
                      <Badge key={skill} variant="secondary">
                        {skill}
                      </Badge>
                    ))
                  ) : (
                    <span className="text-sm text-muted-foreground">No skills specified</span>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Activity History</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex gap-3 pb-4 border-b border-border">
                  <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
                    <User className="h-4 w-4 text-primary" />
                  </div>
                  <div className="flex-1">
                    <div className="font-medium text-sm">Job Created</div>
                    <div className="text-xs text-muted-foreground">{formatDate(job.createdAt)}</div>
                  </div>
                </div>
                <div className="flex gap-3">
                  <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
                    <Edit2 className="h-4 w-4 text-primary" />
                  </div>
                  <div className="flex-1">
                    <div className="font-medium text-sm">Job Updated</div>
                    <div className="text-xs text-muted-foreground">{formatDate(job.updatedAt)}</div>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Assignment</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {job.assignedContractors && job.assignedContractors.length > 0 ? (
                <>
                  <div>
                    <div className="text-sm font-medium text-muted-foreground mb-1">Assigned To</div>
                    <div className="font-medium">
                      {job.assignedContractors.length} contractor(s) assigned
                    </div>
                  </div>
                  <Button
                    variant="outline"
                    className="w-full bg-transparent"
                    onClick={() => setRecommendationsOpen(true)}
                  >
                    Reassign
                  </Button>
                </>
              ) : (
                <Button className="w-full" onClick={() => setRecommendationsOpen(true)}>
                  <Sparkles className="mr-2 h-4 w-4" />
                  Get Recommendations
                </Button>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button variant="outline" className="w-full justify-start bg-transparent">
                <Edit2 className="mr-2 h-4 w-4" />
                Edit Job
              </Button>
              <Button 
                variant="outline" 
                className="w-full justify-start bg-transparent"
                onClick={() => {
                  if (job?.serviceWindow) {
                    const startDate = new Date(job.serviceWindow.start)
                    setRescheduleDate(startDate.toISOString().split("T")[0])
                    setRescheduleStartTime(startDate.toTimeString().slice(0, 5))
                  }
                  setRescheduleOpen(true)
                }}
                disabled={!job || job.status === "Completed" || job.status === "Cancelled"}
              >
                <Calendar className="mr-2 h-4 w-4" />
                Reschedule
              </Button>
              <Button 
                variant="outline" 
                className="w-full justify-start bg-transparent text-destructive hover:text-destructive"
                onClick={() => setCancelOpen(true)}
                disabled={!job || job.status === "Completed" || job.status === "Cancelled"}
              >
                <X className="mr-2 h-4 w-4" />
                Cancel Job
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>

      <RecommendationsSheet open={recommendationsOpen} onOpenChange={setRecommendationsOpen} job={job} />
      <MapViewDialog open={mapOpen} onOpenChange={setMapOpen} locations={mapLocations} title={`Map: ${job.type || "Job"}`} />
      
      {/* Reschedule Dialog */}
      <Dialog open={rescheduleOpen} onOpenChange={setRescheduleOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reschedule Job</DialogTitle>
            <DialogDescription>
              Select a new date and time for this job. All assigned contractors will be validated for availability.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="reschedule-date">Date</Label>
              <Input
                id="reschedule-date"
                type="date"
                value={rescheduleDate}
                onChange={(e) => setRescheduleDate(e.target.value)}
                min={new Date().toISOString().split("T")[0]}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="reschedule-time">Start Time</Label>
              <Input
                id="reschedule-time"
                type="time"
                value={rescheduleStartTime}
                onChange={(e) => setRescheduleStartTime(e.target.value)}
              />
            </div>
            {job && (
              <div className="text-sm text-muted-foreground">
                Duration: {job.duration || 120} minutes
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setRescheduleOpen(false)} disabled={isRescheduling}>
              Cancel
            </Button>
            <Button onClick={handleReschedule} disabled={isRescheduling || !rescheduleDate || !rescheduleStartTime}>
              {isRescheduling ? (
                <>
                  <Spinner className="mr-2 h-4 w-4" />
                  Rescheduling...
                </>
              ) : (
                "Reschedule"
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Cancel Confirmation Dialog */}
      <AlertDialog open={cancelOpen} onOpenChange={setCancelOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Cancel Job</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to cancel this job? This action cannot be undone. All active assignments will be cancelled.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <div className="space-y-2 py-4">
            <Label htmlFor="cancel-reason">Reason (optional)</Label>
            <Input
              id="cancel-reason"
              placeholder="Enter cancellation reason..."
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
            />
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isCancelling}>Keep Job</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleCancel}
              disabled={isCancelling}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isCancelling ? (
                <>
                  <Spinner className="mr-2 h-4 w-4" />
                  Cancelling...
                </>
              ) : (
                "Cancel Job"
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
