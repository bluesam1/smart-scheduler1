"use client"

import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from "@/components/ui/select"
import { useState } from "react"
import { Plus, Search, ArrowUpDown } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { CreateJobDialog } from "./create-job-dialog"
import Link from "next/link"

const mockJobs = [
  {
    id: "1",
    type: "Hardwood Installation",
    address: "123 Oak Street",
    city: "New York",
    state: "NY",
    scheduledDate: "2025-11-12",
    scheduledTime: "09:00 AM",
    duration: 3,
    assignmentStatus: "Unassigned",
    status: "Pending",
    priority: "Normal",
    assignedTo: null,
  },
  {
    id: "2",
    type: "Tile Repair",
    address: "456 Maple Ave",
    city: "Brooklyn",
    state: "NY",
    scheduledDate: "2025-11-12",
    scheduledTime: "02:00 PM",
    duration: 2,
    assignmentStatus: "Assigned",
    status: "In Progress",
    priority: "Rush",
    assignedTo: "Mike Johnson",
  },
  {
    id: "3",
    type: "Carpet Installation",
    address: "789 Pine Road",
    city: "Queens",
    state: "NY",
    scheduledDate: "2025-11-13",
    scheduledTime: "10:00 AM",
    duration: 4,
    assignmentStatus: "Partially Assigned",
    status: "Pending",
    priority: "Normal",
    assignedTo: "Sarah Smith",
  },
]

export function JobsTable() {
  const [searchQuery, setSearchQuery] = useState("")
  const [assignmentStatusFilter, setAssignmentStatusFilter] = useState("all")
  const [statusFilter, setStatusFilter] = useState("all")
  const [priorityFilter, setPriorityFilter] = useState("all")
  const [sortField, setSortField] = useState<string | null>(null)
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc")
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)

  const handleSort = (field: string) => {
    if (sortField === field) {
      setSortDirection(sortDirection === "asc" ? "desc" : "asc")
    } else {
      setSortField(field)
      setSortDirection("asc")
    }
  }

  let filteredJobs = mockJobs.filter((job) => {
    const matchesSearch =
      job.type.toLowerCase().includes(searchQuery.toLowerCase()) ||
      job.address.toLowerCase().includes(searchQuery.toLowerCase())
    const matchesAssignmentStatus = assignmentStatusFilter === "all" || job.assignmentStatus === assignmentStatusFilter
    const matchesStatus = statusFilter === "all" || job.status === statusFilter
    const matchesPriority = priorityFilter === "all" || job.priority === priorityFilter
    return matchesSearch && matchesAssignmentStatus && matchesStatus && matchesPriority
  })

  if (sortField) {
    filteredJobs = [...filteredJobs].sort((a, b) => {
      const aVal = a[sortField as keyof typeof a]
      const bVal = b[sortField as keyof typeof b]
      if (aVal < bVal) return sortDirection === "asc" ? -1 : 1
      if (aVal > bVal) return sortDirection === "asc" ? 1 : -1
      return 0
    })
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
              <SelectItem value="Partially Assigned">Partially Assigned</SelectItem>
              <SelectItem value="Assigned">Assigned</SelectItem>
            </SelectContent>
          </Select>
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-[140px]">
              <SelectValue />
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
            {filteredJobs.map((job) => (
              <TableRow key={job.id} className="cursor-pointer hover:bg-muted/50">
                <TableCell>
                  <Link href={`/jobs/${job.id}`} className="font-medium hover:underline">
                    {job.type}
                  </Link>
                </TableCell>
                <TableCell className="text-sm text-muted-foreground">{job.city}</TableCell>
                <TableCell className="text-sm text-muted-foreground">{job.state}</TableCell>
                <TableCell>{job.scheduledDate}</TableCell>
                <TableCell>{job.scheduledTime}</TableCell>
                <TableCell>
                  <Badge variant={job.priority === "Rush" ? "destructive" : "outline"}>{job.priority}</Badge>
                </TableCell>
                <TableCell>
                  <Badge
                    variant={
                      job.assignmentStatus === "Unassigned"
                        ? "outline"
                        : job.assignmentStatus === "Partially Assigned"
                          ? "secondary"
                          : "default"
                    }
                  >
                    {job.assignmentStatus}
                  </Badge>
                </TableCell>
                <TableCell>
                  <Badge variant="outline">{job.status}</Badge>
                </TableCell>
                <TableCell>{job.assignedTo || "-"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <CreateJobDialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen} />
    </div>
  )
}
