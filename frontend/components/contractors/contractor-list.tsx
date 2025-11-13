"use client"

import { useState, useEffect } from "react"
import { Plus, Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ContractorCard } from "./contractor-card"
import { AddContractorDialog } from "./add-contractor-dialog"
import { createApiClients } from "@/lib/api/api-client-config"
import { useAuth } from "@/lib/auth/auth-context"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import type { ContractorDto } from "@/lib/api/generated/api-client"

export function ContractorList() {
  const { getTokenProvider } = useAuth()
  const [contractors, setContractors] = useState<ContractorDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState("")
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false)

  const fetchContractors = async () => {
    setIsLoading(true)
    setError(null)
    
    try {
      const tokenProvider = getTokenProvider()
      const { client } = createApiClients(tokenProvider)
      const data = await client.getContractors(null, null)
      setContractors(data)
    } catch (err) {
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
  }

  useEffect(() => {
    fetchContractors()
  }, [])

  const handleContractorAdded = () => {
    // Refresh the list after adding a contractor
    fetchContractors()
  }

  // Filter contractors based on search query
  const filteredContractors = contractors.filter((contractor) => {
    if (!searchQuery.trim()) return true
    
    const query = searchQuery.toLowerCase()
    return (
      contractor.name.toLowerCase().includes(query) ||
      contractor.skills.some((skill) => skill.toLowerCase().includes(query))
    )
  })

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

      {filteredContractors.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-4 py-12 text-center">
          <p className="text-sm text-muted-foreground">
            {searchQuery ? "No contractors match your search." : "No contractors found."}
          </p>
          {!searchQuery && (
            <Button onClick={() => setIsAddDialogOpen(true)} variant="outline">
              <Plus className="mr-2 h-4 w-4" />
              Add Your First Contractor
            </Button>
          )}
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {filteredContractors.map((contractor) => (
            <ContractorCard 
              key={contractor.id} 
              contractor={contractor}
              onContractorUpdated={fetchContractors}
            />
          ))}
        </div>
      )}

      <AddContractorDialog 
        open={isAddDialogOpen} 
        onOpenChange={setIsAddDialogOpen}
        onContractorAdded={handleContractorAdded}
      />
    </div>
  )
}
