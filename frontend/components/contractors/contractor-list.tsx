"use client"

import { useState } from "react"
import { Plus, Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ContractorCard } from "./contractor-card"
import { AddContractorDialog } from "./add-contractor-dialog"

const mockContractors = [
  {
    id: "1",
    name: "John Martinez",
    skills: ["Hardwood Installation", "Laminate", "Tile"],
    rating: 92,
    availability: "Available",
    baseLocation: "Downtown District",
    jobsToday: 2,
    maxJobs: 4,
  },
  {
    id: "2",
    name: "Sarah Chen",
    skills: ["Carpet Installation", "Vinyl", "Hardwood"],
    rating: 88,
    availability: "Busy",
    baseLocation: "North Side",
    jobsToday: 3,
    maxJobs: 4,
  },
  {
    id: "3",
    name: "Mike Johnson",
    skills: ["Tile", "Stone", "Marble"],
    rating: 95,
    availability: "Available",
    baseLocation: "West End",
    jobsToday: 1,
    maxJobs: 4,
  },
]

export function ContractorList() {
  const [searchQuery, setSearchQuery] = useState("")
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false)

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search contractors by name or skills..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10"
          />
        </div>
        <Button onClick={() => setIsAddDialogOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Add Contractor
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {mockContractors.map((contractor) => (
          <ContractorCard key={contractor.id} contractor={contractor} />
        ))}
      </div>

      <AddContractorDialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen} />
    </div>
  )
}
