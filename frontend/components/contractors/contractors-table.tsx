"use client"

import { useState, useEffect, useCallback } from "react"
import { Plus, Search, ArrowUpDown } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { AddContractorDialog } from "./add-contractor-dialog"
import { ContractorDetailsDialog } from "./contractor-details-dialog"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { createApiClients } from "@/lib/api/api-client-config"
import { useAuth } from "@/lib/auth/auth-context"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import type { ContractorDto } from "@/lib/api/generated/api-client"

export function ContractorsTable() {
  const { getTokenProvider } = useAuth()
  const [contractors, setContractors] = useState<ContractorDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState("")
  const [availabilityFilter, setAvailabilityFilter] = useState("all")
  const [sortField, setSortField] = useState<string | null>(null)
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc")
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false)
  const [selectedContractor, setSelectedContractor] = useState<ContractorDto | null>(null)

  const fetchContractors = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    
    try {
      const tokenProvider = getTokenProvider()
      if (!tokenProvider) {
        toast.error("Please log in to view contractors")
        setIsLoading(false)
        return
      }
      const { client } = createApiClients(tokenProvider)
      const data = await client.getContractors(null, null)
      console.log("[ContractorsTable] Fetched contractors:", data)
      console.log("[ContractorsTable] Number of contractors:", data?.length || 0)
      setContractors(data || [])
    } catch (err) {
      console.error("[ContractorsTable] Error fetching contractors:", err)
      const errorMessage = formatErrorForDisplay(err)
      setError(errorMessage)
      
      if (isAuthenticationError(err)) {
        toast.error("Please log in to view contractors")
      } else {
        toast.error(`Failed to load contractors: ${errorMessage}`)
      }
    } finally {
      setIsLoading(false)
    }
  }, [getTokenProvider])

  useEffect(() => {
    fetchContractors()
  }, [fetchContractors])

  const handleContractorAdded = () => {
    // Refresh the list after adding a contractor
    fetchContractors()
  }

  const handleSort = (field: string) => {
    if (sortField === field) {
      setSortDirection(sortDirection === "asc" ? "desc" : "asc")
    } else {
      setSortField(field)
      setSortDirection("asc")
    }
  }

  // Filter contractors based on search query and availability
  let filteredContractors = contractors.filter((contractor) => {
    const matchesSearch =
      !searchQuery.trim() ||
      contractor.name?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      contractor.skills?.some((skill) => skill.toLowerCase().includes(searchQuery.toLowerCase()))
    
    // Note: Availability filtering would need to be calculated based on working hours and calendar
    // For now, we'll skip availability filtering as it requires more complex logic
    const matchesAvailability = availabilityFilter === "all" // TODO: Implement availability calculation
    
    return matchesSearch && matchesAvailability
  })

  // Debug logging
  useEffect(() => {
    console.log("[ContractorsTable] contractors state:", contractors)
    console.log("[ContractorsTable] filteredContractors:", filteredContractors)
  }, [contractors, filteredContractors])

  // Sort contractors
  if (sortField) {
    filteredContractors = [...filteredContractors].sort((a, b) => {
      let aVal: any
      let bVal: any
      
      switch (sortField) {
        case "name":
          aVal = a.name || ""
          bVal = b.name || ""
          break
        case "rating":
          aVal = a.rating || 0
          bVal = b.rating || 0
          break
        case "city":
          aVal = a.baseLocation?.city || ""
          bVal = b.baseLocation?.city || ""
          break
        case "state":
          aVal = a.baseLocation?.state || ""
          bVal = b.baseLocation?.state || ""
          break
        default:
          return 0
      }
      
      if (aVal < bVal) return sortDirection === "asc" ? -1 : 1
      if (aVal > bVal) return sortDirection === "asc" ? 1 : -1
      return 0
    })
  }

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-12">
        <Spinner className="h-8 w-8" />
        <p className="text-sm text-muted-foreground">Loading contractors...</p>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-12">
        <p className="text-sm text-destructive">{error}</p>
        <Button onClick={fetchContractors} variant="outline">
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
            {filteredContractors.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-8 text-muted-foreground">
                  {searchQuery ? "No contractors match your search." : "No contractors found."}
                </TableCell>
              </TableRow>
            ) : (
              filteredContractors.map((contractor) => {
                console.log("[ContractorsTable] Rendering contractor:", contractor)
                return (
                  <TableRow
                    key={contractor.id || `contractor-${Math.random()}`}
                    className="cursor-pointer hover:bg-muted/50"
                    onClick={() => setSelectedContractor(contractor)}
                  >
                    <TableCell className="font-medium">{contractor.name || "Unknown"}</TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {contractor.skills && contractor.skills.length > 0 ? (
                          <>
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
                          </>
                        ) : (
                          <span className="text-xs text-muted-foreground">No skills</span>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <div className="text-sm font-medium">{contractor.rating || 0}/100</div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant="secondary">
                        {/* TODO: Calculate availability based on working hours and calendar */}
                        N/A
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {contractor.baseLocation?.city || "-"}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {contractor.baseLocation?.state || "-"}
                    </TableCell>
                    <TableCell>
                      {/* TODO: Calculate utilization based on jobs */}
                      -
                    </TableCell>
                  </TableRow>
                )
              })
            )}
          </TableBody>
        </Table>
      </div>

      <AddContractorDialog 
        open={isAddDialogOpen} 
        onOpenChange={setIsAddDialogOpen}
        onContractorAdded={handleContractorAdded}
      />

      {selectedContractor && (
        <ContractorDetailsDialog
          contractor={selectedContractor}
          open={!!selectedContractor}
          onOpenChange={(open) => {
            if (!open) {
              setSelectedContractor(null)
              // Refresh the table when dialog closes to show any updates
              fetchContractors()
            }
          }}
          onContractorUpdated={fetchContractors}
        />
      )}
    </div>
  )
}
