"use client"

import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from "@/components/ui/select"
import { useState, useEffect, useCallback } from "react"
import { Plus, Search, ArrowUpDown } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { CreateJobDialog } from "./create-job-dialog"
import { ContractorDetailsDialog } from "@/components/contractors/contractor-details-dialog"
import Link from "next/link"
import { createApiClients } from "@/lib/api/api-client-config"
import { useAuth } from "@/lib/auth/auth-context"
import { useSignalR } from "@/hooks/use-signalr"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import type { JobDto, ContractorDto } from "@/lib/api/generated/api-client"
import type { JobAssigned } from "@/lib/realtime/signalr-types"

export function JobsTable() {
  const { getTokenProvider, isAuthenticated, isLoading: authLoading } = useAuth()
  const { client: signalRClient } = useSignalR()
  const [jobs, setJobs] = useState<JobDto[]>([])
  const [contractors, setContractors] = useState<Map<string, ContractorDto>>(new Map())
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState("")
  const [assignmentStatusFilter, setAssignmentStatusFilter] = useState("all")
  const [statusFilter, setStatusFilter] = useState("all")
  const [priorityFilter, setPriorityFilter] = useState("all")
  const [sortField, setSortField] = useState<string | null>(null)
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc")
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [selectedContractor, setSelectedContractor] = useState<ContractorDto | null>(null)
  const [isContractorDialogOpen, setIsContractorDialogOpen] = useState(false)

  const fetchJobs = useCallback(async () => {
    // Don't fetch if auth is still loading
    if (authLoading) {
      return
    }

    // If user is not authenticated, set error and stop loading
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
      
      // Fetch jobs and contractors in parallel
      const statusParam = statusFilter !== "all" ? statusFilter : null
      const priorityParam = priorityFilter !== "all" ? priorityFilter : null
      const [jobsData, contractorsData] = await Promise.all([
        client.getJobs(statusParam, priorityParam, null),
        client.getContractors(null, null)
      ])
      
      setJobs(jobsData)
      
      // Build contractor lookup map
      const contractorMap = new Map<string, ContractorDto>()
      contractorsData.forEach(contractor => {
        if (contractor.id) {
          contractorMap.set(contractor.id, contractor)
        }
      })
      setContractors(contractorMap)
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
  }, [statusFilter, priorityFilter, getTokenProvider, isAuthenticated, authLoading])

  useEffect(() => {
    fetchJobs()
  }, [fetchJobs])

  // Subscribe to JobAssigned events via SignalR
  useEffect(() => {
    if (!signalRClient) return

    const handleJobAssigned = async (event: JobAssigned) => {
      console.log("JobAssigned event received:", event)
      
      // Update the job in the list if it exists
      setJobs((prevJobs) => {
        const jobIndex = prevJobs.findIndex((j) => j.id === event.jobId)
        if (jobIndex >= 0) {
          // Job exists in list, refresh it from API to get updated data
          const tokenProvider = getTokenProvider()
          if (tokenProvider) {
            const { client } = createApiClients(tokenProvider)
            client.getJobById(event.jobId).then((updatedJob) => {
              if (updatedJob) {
                setJobs((currentJobs) => {
                  const newJobs = [...currentJobs]
                  newJobs[jobIndex] = updatedJob
                  return newJobs
                })
              }
            }).catch((err) => {
              console.error("Failed to refresh job after assignment:", err)
              // Fallback: refresh entire list
              fetchJobs()
            })
          }
        } else {
          // Job not in current list, refresh entire list
          fetchJobs()
        }
        return prevJobs
      })

      toast.success(`Job ${event.jobId.substring(0, 8)}... assigned to contractor`)
    }

    const unsubscribe = signalRClient.onJobAssigned(handleJobAssigned)

    return () => {
      unsubscribe()
    }
  }, [signalRClient, getTokenProvider, fetchJobs])

  const handleJobCreated = () => {
    fetchJobs()
  }

  const handleSort = (field: string) => {
    if (sortField === field) {
      setSortDirection(sortDirection === "asc" ? "desc" : "asc")
    } else {
      setSortField(field)
      setSortDirection("asc")
    }
  }

  let filteredJobs = jobs.filter((job) => {
    const jobType = job.type?.toLowerCase() || ""
    const address = job.location?.address?.toLowerCase() || job.location?.formattedAddress?.toLowerCase() || ""
    const city = job.location?.city?.toLowerCase() || ""
    const matchesSearch = !searchQuery.trim() || 
      jobType.includes(searchQuery.toLowerCase()) ||
      address.includes(searchQuery.toLowerCase()) ||
      city.includes(searchQuery.toLowerCase())
    const matchesAssignmentStatus = assignmentStatusFilter === "all" || job.assignmentStatus === assignmentStatusFilter
    const matchesStatus = statusFilter === "all" || job.status === statusFilter
    const matchesPriority = priorityFilter === "all" || job.priority === priorityFilter
    return matchesSearch && matchesAssignmentStatus && matchesStatus && matchesPriority
  })

  if (sortField) {
    filteredJobs = [...filteredJobs].sort((a, b) => {
      let aVal: any
      let bVal: any
      
      if (sortField === "type") {
        aVal = a.type || ""
        bVal = b.type || ""
      } else if (sortField === "city") {
        aVal = a.location?.city || ""
        bVal = b.location?.city || ""
      } else if (sortField === "state") {
        aVal = a.location?.state || ""
        bVal = b.location?.state || ""
      } else if (sortField === "scheduledDate") {
        aVal = a.serviceWindow?.start || ""
        bVal = b.serviceWindow?.start || ""
      } else if (sortField === "priority") {
        aVal = a.priority || ""
        bVal = b.priority || ""
      } else if (sortField === "assignmentStatus") {
        aVal = a.assignmentStatus || ""
        bVal = b.assignmentStatus || ""
      } else if (sortField === "status") {
        aVal = a.status || ""
        bVal = b.status || ""
      } else {
        aVal = a[sortField as keyof JobDto]
        bVal = b[sortField as keyof JobDto]
      }
      
      if (aVal < bVal) return sortDirection === "asc" ? -1 : 1
      if (aVal > bVal) return sortDirection === "asc" ? 1 : -1
      return 0
    })
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
    <div className="space-y-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex-1 flex items-center gap-2">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Search jobs..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10"
            />
          </div>
          <Select value={assignmentStatusFilter} onValueChange={setAssignmentStatusFilter}>
            <SelectTrigger className="w-[160px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Assignments</SelectItem>
              <SelectItem value="Unassigned">Unassigned</SelectItem>
              <SelectItem value="PartiallyAssigned">Partially Assigned</SelectItem>
              <SelectItem value="Assigned">Assigned</SelectItem>
            </SelectContent>
          </Select>
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-[140px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="Created">Created</SelectItem>
              <SelectItem value="Assigned">Assigned</SelectItem>
              <SelectItem value="InProgress">In Progress</SelectItem>
              <SelectItem value="Completed">Completed</SelectItem>
              <SelectItem value="Cancelled">Cancelled</SelectItem>
            </SelectContent>
          </Select>
          <Select value={priorityFilter} onValueChange={setPriorityFilter}>
            <SelectTrigger className="w-[140px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Priority</SelectItem>
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

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>
                <Button variant="ghost" size="sm" onClick={() => handleSort("type")} className="h-8 px-2">
                  Job Type
                  <ArrowUpDown className="ml-2 h-3 w-3" />
                </Button>
              </TableHead>
              <TableHead>
                <Button variant="ghost" size="sm" onClick={() => handleSort("city")} className="h-8 px-2">
                  City
                  <ArrowUpDown className="ml-2 h-3 w-3" />
                </Button>
              </TableHead>
              <TableHead>
                <Button variant="ghost" size="sm" onClick={() => handleSort("state")} className="h-8 px-2">
                  State
                  <ArrowUpDown className="ml-2 h-3 w-3" />
                </Button>
              </TableHead>
              <TableHead>
                <Button variant="ghost" size="sm" onClick={() => handleSort("scheduledDate")} className="h-8 px-2">
                  Date
                  <ArrowUpDown className="ml-2 h-3 w-3" />
                </Button>
              </TableHead>
              <TableHead>Time</TableHead>
              <TableHead>
                <Button variant="ghost" size="sm" onClick={() => handleSort("priority")} className="h-8 px-2">
                  Priority
                  <ArrowUpDown className="ml-2 h-3 w-3" />
                </Button>
              </TableHead>
              <TableHead>
                <Button variant="ghost" size="sm" onClick={() => handleSort("assignmentStatus")} className="h-8 px-2">
                  Assignment
                  <ArrowUpDown className="ml-2 h-3 w-3" />
                </Button>
              </TableHead>
              <TableHead>
                <Button variant="ghost" size="sm" onClick={() => handleSort("status")} className="h-8 px-2">
                  Status
                  <ArrowUpDown className="ml-2 h-3 w-3" />
                </Button>
              </TableHead>
              <TableHead>Assigned To</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredJobs.length === 0 ? (
              <TableRow>
                <TableCell colSpan={9} className="text-center py-8 text-muted-foreground">
                  {searchQuery || assignmentStatusFilter !== "all" || statusFilter !== "all" || priorityFilter !== "all"
                    ? "No jobs match your filters."
                    : "No jobs found."}
                </TableCell>
              </TableRow>
            ) : (
              filteredJobs.map((job) => (
                <TableRow key={job.id} className="cursor-pointer hover:bg-muted/50">
                  <TableCell>
                    <Link href={`/jobs?id=${job.id}`} className="font-medium hover:underline">
                      {job.type || "Unknown"}
                    </Link>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">{job.location?.city || "-"}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{job.location?.state || "-"}</TableCell>
                  <TableCell>{formatDate(job.serviceWindow?.start)}</TableCell>
                  <TableCell>{formatTime(job.serviceWindow?.start)}</TableCell>
                  <TableCell>
                    <Badge variant={job.priority === "Rush" ? "destructive" : "outline"}>{job.priority || "Normal"}</Badge>
                  </TableCell>
                  <TableCell>
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
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline">{job.status || "Created"}</Badge>
                  </TableCell>
                  <TableCell>
                    {job.assignedContractors && job.assignedContractors.length > 0 ? (
                      <div className="flex flex-wrap gap-1">
                        {job.assignedContractors.map((assignment, idx) => {
                          const contractor = assignment.contractorId ? contractors.get(assignment.contractorId) : null
                          return (
                            <Button
                              key={idx}
                              variant="link"
                              size="sm"
                              className="h-auto p-0 text-blue-600 hover:text-blue-800 hover:underline"
                              onClick={(e) => {
                                e.stopPropagation()
                                if (contractor) {
                                  setSelectedContractor(contractor)
                                  setIsContractorDialogOpen(true)
                                }
                              }}
                            >
                              {contractor?.name || "Unknown"}
                            </Button>
                          )
                        })}
                      </div>
                    ) : (
                      <span className="text-muted-foreground">-</span>
                    )}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <CreateJobDialog 
        open={isCreateDialogOpen} 
        onOpenChange={setIsCreateDialogOpen}
        onJobCreated={handleJobCreated}
      />

      {selectedContractor && (
        <ContractorDetailsDialog
          open={isContractorDialogOpen}
          onOpenChange={setIsContractorDialogOpen}
          contractor={selectedContractor}
          onContractorUpdated={() => {
            fetchJobs() // Refresh to get updated contractor data
          }}
        />
      )}
    </div>
  )
}
