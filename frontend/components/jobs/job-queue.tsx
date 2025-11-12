"use client"

import { useState } from "react"
import { Plus, Filter, Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { JobCard } from "./job-card"
import { CreateJobDialog } from "./create-job-dialog"
import { RecommendationsSheet } from "./recommendations-sheet"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

const mockJobs = [
  {
    id: "1",
    type: "Hardwood Installation",
    address: "123 Oak Street, Downtown",
    scheduledDate: "2025-11-12",
    scheduledTime: "09:00 AM",
    duration: "3 hours",
    assignmentStatus: "Unassigned" as const,
    status: "Pending" as const,
    priority: "Normal",
    requiredSkills: ["Hardwood Installation", "Finishing"],
  },
  {
    id: "2",
    type: "Tile Repair",
    address: "456 Maple Ave, North Side",
    scheduledDate: "2025-11-12",
    scheduledTime: "02:00 PM",
    duration: "2 hours",
    assignmentStatus: "Assigned" as const,
    status: "In Progress" as const,
    priority: "Rush",
    requiredSkills: ["Tile", "Repair"],
    assignedTo: "Mike Johnson",
  },
  {
    id: "3",
    type: "Carpet Installation",
    address: "789 Pine Road, West End",
    scheduledDate: "2025-11-13",
    scheduledTime: "10:00 AM",
    duration: "4 hours",
    assignmentStatus: "Unassigned" as const,
    status: "Pending" as const,
    priority: "Normal",
    requiredSkills: ["Carpet Installation"],
  },
]

export function JobQueue() {
  const [searchQuery, setSearchQuery] = useState("")
  const [assignmentStatusFilter, setAssignmentStatusFilter] = useState("Unassigned")
  const [statusFilter, setStatusFilter] = useState("all")
  const [priorityFilter, setPriorityFilter] = useState("all")
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null)
  const [isRecommendationsOpen, setIsRecommendationsOpen] = useState(false)

  const handleRequestRecommendations = (jobId: string) => {
    setSelectedJobId(jobId)
    setIsRecommendationsOpen(true)
  }

  const filteredJobs = mockJobs.filter((job) => {
    const matchesSearch =
      job.type.toLowerCase().includes(searchQuery.toLowerCase()) ||
      job.address.toLowerCase().includes(searchQuery.toLowerCase()) ||
      job.requiredSkills.some((skill) => skill.toLowerCase().includes(searchQuery.toLowerCase()))
    const matchesAssignmentStatus = assignmentStatusFilter === "all" || job.assignmentStatus === assignmentStatusFilter
    const matchesStatus = statusFilter === "all" || job.status === statusFilter
    const matchesPriority = priorityFilter === "all" || job.priority === priorityFilter
    return matchesSearch && matchesAssignmentStatus && matchesStatus && matchesPriority
  })

  const selectedJob = mockJobs.find((job) => job.id === selectedJobId)

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

      <div className="grid gap-4">
        {filteredJobs.map((job) => (
          <JobCard key={job.id} job={job} onRequestRecommendations={handleRequestRecommendations} />
        ))}
      </div>

      <CreateJobDialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen} />

      {selectedJob && (
        <RecommendationsSheet open={isRecommendationsOpen} onOpenChange={setIsRecommendationsOpen} job={selectedJob} />
      )}
    </div>
  )
}
