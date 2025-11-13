"use client"

import { useState, useEffect, useCallback } from "react"
import { Plus, Filter, Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { JobCard } from "./job-card"
import { CreateJobDialog } from "./create-job-dialog"
import { RecommendationsSheet } from "./recommendations-sheet"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useAuth } from "@/lib/auth/auth-context"
import { createApiClients } from "@/lib/api/api-client-config"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import type { JobDto } from "@/lib/api/generated/api-client"

interface JobCardJob {
  id: string
  type: string
  address: string
  scheduledDate: string
  scheduledTime: string
  duration: string
  assignmentStatus: "Unassigned" | "Partially Assigned" | "Assigned"
  status: "Pending" | "In Progress" | "Completed" | "Canceled"
  priority: string
  requiredSkills: string[]
  assignedTo?: string
  timezone?: string
}

// Helper function to map JobDto to JobCardJob format
function mapJobDtoToJobCard(job: JobDto): JobCardJob | null {
  if (!job.id || !job.type) return null

  // Format date and time from serviceWindow
  let scheduledDate = ""
  let scheduledTime = ""
  if (job.serviceWindow?.start) {
    try {
      const date = new Date(job.serviceWindow.start)
      scheduledDate = date.toISOString().split("T")[0]
      scheduledTime = date.toLocaleTimeString("en-US", {
        hour: "numeric",
        minute: "2-digit",
        hour12: true,
      })
    } catch {
      // Fallback if date parsing fails
      scheduledDate = job.desiredDate || ""
    }
  } else if (job.desiredDate) {
    scheduledDate = job.desiredDate
  }

  // Format duration
  let duration = ""
  if (job.duration) {
    const hours = Math.floor(job.duration / 60)
    const minutes = job.duration % 60
    if (hours > 0 && minutes > 0) {
      duration = `${hours} hour${hours > 1 ? "s" : ""} ${minutes} minute${minutes > 1 ? "s" : ""}`
    } else if (hours > 0) {
      duration = `${hours} hour${hours > 1 ? "s" : ""}`
    } else {
      duration = `${minutes} minute${minutes > 1 ? "s" : ""}`
    }
  } else {
    duration = "Not specified"
  }

  // Format address
  const address =
    job.location?.formattedAddress ||
    job.location?.address ||
    `${job.location?.city || ""}, ${job.location?.state || ""}`.trim() ||
    "Address not specified"

  // Map assignment status
  let assignmentStatus: "Unassigned" | "Partially Assigned" | "Assigned" = "Unassigned"
  if (job.assignmentStatus === "Assigned") {
    assignmentStatus = "Assigned"
  } else if (job.assignmentStatus === "PartiallyAssigned") {
    assignmentStatus = "Partially Assigned"
  }

  // Map status
  let status: "Pending" | "In Progress" | "Completed" | "Canceled" = "Pending"
  if (job.status === "InProgress") {
    status = "In Progress"
  } else if (job.status === "Completed") {
    status = "Completed"
  } else if (job.status === "Cancelled") {
    status = "Canceled"
  }

  // Get assigned contractor name if available
  const assignedTo =
    job.assignedContractors && job.assignedContractors.length > 0
      ? `${job.assignedContractors.length} contractor(s)`
      : undefined

  return {
    id: job.id,
    type: job.type,
    address,
    scheduledDate,
    scheduledTime,
    duration,
    assignmentStatus,
    status,
    priority: job.priority || "Normal",
    requiredSkills: job.requiredSkills || [],
    assignedTo,
    timezone: job.timezone,
  }
}

export function JobQueue() {
  const { getTokenProvider, isAuthenticated, isLoading: authLoading } = useAuth()
  const [jobs, setJobs] = useState<JobCardJob[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState("")
  const [assignmentStatusFilter, setAssignmentStatusFilter] = useState("Unassigned")
  const [statusFilter, setStatusFilter] = useState("all")
  const [priorityFilter, setPriorityFilter] = useState("all")
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null)
  const [isRecommendationsOpen, setIsRecommendationsOpen] = useState(false)

  const fetchJobs = useCallback(async () => {
    if (authLoading) return

    if (!isAuthenticated) {
      setIsLoading(false)
      setError("Authentication required. Please log in.")
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      const tokenProvider = getTokenProvider()
      if (!tokenProvider) {
        setError("Authentication required. Please log in.")
        setIsLoading(false)
        return
      }

      const { client } = createApiClients(tokenProvider)
      const data = await client.getJobs(null, null, null)

      // Map JobDto[] to JobCardJob[]
      const mappedJobs = data
        .map(mapJobDtoToJobCard)
        .filter((job): job is JobCardJob => job !== null)

      setJobs(mappedJobs)
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      setError(errorMessage)

      if (isAuthenticationError(err)) {
        toast.error("Please log in to view jobs")
      } else {
        toast.error(`Failed to load jobs: ${errorMessage}`)
      }
    } finally {
      setIsLoading(false)
    }
  }, [isAuthenticated, authLoading, getTokenProvider])

  useEffect(() => {
    fetchJobs()
  }, [fetchJobs])

  const handleRequestRecommendations = (jobId: string) => {
    setSelectedJobId(jobId)
    setIsRecommendationsOpen(true)
  }

  const handleJobCreated = () => {
    fetchJobs()
  }

  const filteredJobs = jobs.filter((job) => {
    const matchesSearch =
      job.type.toLowerCase().includes(searchQuery.toLowerCase()) ||
      job.address.toLowerCase().includes(searchQuery.toLowerCase()) ||
      job.requiredSkills.some((skill) => skill.toLowerCase().includes(searchQuery.toLowerCase()))
    const matchesAssignmentStatus = assignmentStatusFilter === "all" || job.assignmentStatus === assignmentStatusFilter
    const matchesStatus = statusFilter === "all" || job.status === statusFilter
    const matchesPriority = priorityFilter === "all" || job.priority === priorityFilter
    return matchesSearch && matchesAssignmentStatus && matchesStatus && matchesPriority
  })

  const selectedJob = jobs.find((job) => job.id === selectedJobId)

  if (authLoading || isLoading) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-12">
        <Spinner className="h-8 w-8" />
        <p className="text-sm text-muted-foreground">
          {authLoading ? "Initializing..." : "Loading jobs..."}
        </p>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-12">
        <p className="text-sm text-destructive">{error}</p>
        <Button onClick={fetchJobs} variant="outline">
          Retry
        </Button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search jobs by type, address, or skills..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10"
          />
        </div>

        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-2 flex-wrap">
            <Filter className="h-4 w-4 text-muted-foreground" />
            <Select value={assignmentStatusFilter} onValueChange={setAssignmentStatusFilter}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Assignment status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Assignments</SelectItem>
                <SelectItem value="Unassigned">Unassigned</SelectItem>
                <SelectItem value="Partially Assigned">Partially Assigned</SelectItem>
                <SelectItem value="Assigned">Assigned</SelectItem>
              </SelectContent>
            </Select>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Job status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="Pending">Pending</SelectItem>
                <SelectItem value="In Progress">In Progress</SelectItem>
                <SelectItem value="Completed">Completed</SelectItem>
                <SelectItem value="Canceled">Canceled</SelectItem>
              </SelectContent>
            </Select>
            <Select value={priorityFilter} onValueChange={setPriorityFilter}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Filter by priority" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Priorities</SelectItem>
                <SelectItem value="Rush">Rush</SelectItem>
                <SelectItem value="Normal">Normal</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <Button onClick={() => setIsCreateDialogOpen(true)}>
            <Plus className="mr-2 h-4 w-4" />
            Create Job
          </Button>
        </div>
      </div>

      {filteredJobs.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-4 py-12 text-center">
          <p className="text-sm text-muted-foreground">
            {searchQuery || assignmentStatusFilter !== "Unassigned" || statusFilter !== "all" || priorityFilter !== "all"
              ? "No jobs match your filters."
              : "No jobs found."}
          </p>
        </div>
      ) : (
        <div className="grid gap-4">
          {filteredJobs.map((job) => (
            <JobCard key={job.id} job={job} onRequestRecommendations={handleRequestRecommendations} />
          ))}
        </div>
      )}

      <CreateJobDialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen} onJobCreated={handleJobCreated} />

      {selectedJob && (
        <RecommendationsSheet open={isRecommendationsOpen} onOpenChange={setIsRecommendationsOpen} job={selectedJob} />
      )}
    </div>
  )
}
