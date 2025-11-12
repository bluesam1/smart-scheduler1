"use client"

import { useState } from "react"
import { Plus, Search, ArrowUpDown } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { AddContractorDialog } from "./add-contractor-dialog"
import { ContractorDetailsDialog } from "./contractor-details-dialog"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

const mockContractors = [
  {
    id: "1",
    name: "John Martinez",
    skills: ["Hardwood Installation", "Laminate", "Tile"],
    rating: 92,
    availability: "Available",
    baseLocation: "Downtown District",
    city: "New York",
    state: "NY",
    jobsToday: 2,
    maxJobs: 4,
    timezone: "America/New_York",
  },
  {
    id: "2",
    name: "Sarah Chen",
    skills: ["Carpet Installation", "Vinyl", "Hardwood"],
    rating: 88,
    availability: "Busy",
    baseLocation: "North Side",
    city: "New York",
    state: "NY",
    jobsToday: 3,
    maxJobs: 4,
    timezone: "America/New_York",
  },
  {
    id: "3",
    name: "Mike Johnson",
    skills: ["Tile", "Stone", "Marble"],
    rating: 95,
    availability: "Available",
    baseLocation: "West End",
    city: "Brooklyn",
    state: "NY",
    jobsToday: 1,
    maxJobs: 4,
    timezone: "America/New_York",
  },
]

export function ContractorsTable() {
  const [searchQuery, setSearchQuery] = useState("")
  const [availabilityFilter, setAvailabilityFilter] = useState("all")
  const [sortField, setSortField] = useState<string | null>(null)
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc")
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false)
  const [selectedContractor, setSelectedContractor] = useState<(typeof mockContractors)[0] | null>(null)

  const handleSort = (field: string) => {
    if (sortField === field) {
      setSortDirection(sortDirection === "asc" ? "desc" : "asc")
    } else {
      setSortField(field)
      setSortDirection("asc")
    }
  }

  let filteredContractors = mockContractors.filter((contractor) => {
    const matchesSearch =
      contractor.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      contractor.skills.some((skill) => skill.toLowerCase().includes(searchQuery.toLowerCase()))
    const matchesAvailability = availabilityFilter === "all" || contractor.availability === availabilityFilter
    return matchesSearch && matchesAvailability
  })

  if (sortField) {
    filteredContractors = [...filteredContractors].sort((a, b) => {
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
              placeholder="Search contractors..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10"
            />
          </div>
          <Select value={availabilityFilter} onValueChange={setAvailabilityFilter}>
            <SelectTrigger className="w-[140px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="Available">Available</SelectItem>
              <SelectItem value="Busy">Busy</SelectItem>
              <SelectItem value="Off Duty">Off Duty</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <Button onClick={() => setIsAddDialogOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Add Contractor
        </Button>
      </div>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>
                <Button variant="ghost" size="sm" onClick={() => handleSort("name")} className="h-8 px-2">
                  Name
                  <ArrowUpDown className="ml-2 h-3 w-3" />
                </Button>
              </TableHead>
              <TableHead>Skills</TableHead>
              <TableHead>
                <Button variant="ghost" size="sm" onClick={() => handleSort("rating")} className="h-8 px-2">
                  Rating
                  <ArrowUpDown className="ml-2 h-3 w-3" />
                </Button>
              </TableHead>
              <TableHead>
                <Button variant="ghost" size="sm" onClick={() => handleSort("availability")} className="h-8 px-2">
                  Status
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
              <TableHead>Utilization</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredContractors.map((contractor) => (
              <TableRow
                key={contractor.id}
                className="cursor-pointer hover:bg-muted/50"
                onClick={() => setSelectedContractor(contractor)}
              >
                <TableCell className="font-medium">{contractor.name}</TableCell>
                <TableCell>
                  <div className="flex flex-wrap gap-1">
                    {contractor.skills.slice(0, 2).map((skill) => (
                      <Badge key={skill} variant="secondary" className="text-xs">
                        {skill}
                      </Badge>
                    ))}
                    {contractor.skills.length > 2 && (
                      <Badge variant="secondary" className="text-xs">
                        +{contractor.skills.length - 2}
                      </Badge>
                    )}
                  </div>
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <div className="text-sm font-medium">{contractor.rating}/100</div>
                  </div>
                </TableCell>
                <TableCell>
                  <Badge variant={contractor.availability === "Available" ? "default" : "secondary"}>
                    {contractor.availability}
                  </Badge>
                </TableCell>
                <TableCell className="text-sm text-muted-foreground">{contractor.city}</TableCell>
                <TableCell className="text-sm text-muted-foreground">{contractor.state}</TableCell>
                <TableCell>
                  {contractor.jobsToday}/{contractor.maxJobs}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <AddContractorDialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen} />

      {selectedContractor && (
        <ContractorDetailsDialog
          contractor={selectedContractor}
          open={!!selectedContractor}
          onOpenChange={(open) => !open && setSelectedContractor(null)}
        />
      )}
    </div>
  )
}
