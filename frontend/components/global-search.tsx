"use client"

import { useState, useEffect, useRef } from "react"
import { Search, Briefcase, User } from "lucide-react"
import { Input } from "@/components/ui/input"
import { useRouter } from "next/navigation"

// Mock data - in production this would come from your API
const mockJobs = [
  { id: "1", type: "Hardwood Installation", address: "123 Oak Street" },
  { id: "2", type: "Tile Repair", address: "456 Maple Ave" },
  { id: "3", type: "Carpet Installation", address: "789 Pine Road" },
]

const mockContractors = [
  { id: "1", name: "John Martinez", skills: "Hardwood, Laminate" },
  { id: "2", name: "Sarah Chen", skills: "Carpet, Vinyl" },
  { id: "3", name: "Mike Johnson", skills: "Tile, Stone" },
]

export function GlobalSearch() {
  const [query, setQuery] = useState("")
  const [isOpen, setIsOpen] = useState(false)
  const [results, setResults] = useState<{ jobs: typeof mockJobs; contractors: typeof mockContractors }>({
    jobs: [],
    contractors: [],
  })
  const ref = useRef<HTMLDivElement>(null)
  const router = useRouter()

  useEffect(() => {
    if (query.length > 0) {
      const jobResults = mockJobs.filter(
        (job) =>
          job.type.toLowerCase().includes(query.toLowerCase()) ||
          job.address.toLowerCase().includes(query.toLowerCase()),
      )
      const contractorResults = mockContractors.filter(
        (contractor) =>
          contractor.name.toLowerCase().includes(query.toLowerCase()) ||
          contractor.skills.toLowerCase().includes(query.toLowerCase()),
      )
      setResults({ jobs: jobResults, contractors: contractorResults })
      setIsOpen(true)
    } else {
      setIsOpen(false)
    }
  }, [query])

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (ref.current && !ref.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [])

  const handleJobClick = (jobId: string) => {
    router.push(`/jobs/${jobId}`)
    setIsOpen(false)
    setQuery("")
  }

  const handleContractorClick = (contractorId: string) => {
    router.push(`/contractors?id=${contractorId}`)
    setIsOpen(false)
    setQuery("")
  }

  const hasResults = results.jobs.length > 0 || results.contractors.length > 0

  return (
    <div ref={ref} className="relative w-full max-w-md">
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder="Search jobs and contractors..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onFocus={() => query.length > 0 && setIsOpen(true)}
          className="pl-10"
        />
      </div>

      {isOpen && (
        <div className="absolute top-full mt-2 w-full rounded-lg border border-border bg-card shadow-lg z-50 max-h-[400px] overflow-auto">
          {!hasResults && query.length > 0 && (
            <div className="p-4 text-sm text-muted-foreground text-center">No results found</div>
          )}

          {results.jobs.length > 0 && (
            <div className="p-2">
              <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">Jobs</div>
              {results.jobs.map((job) => (
                <button
                  key={job.id}
                  onClick={() => handleJobClick(job.id)}
                  className="flex items-start gap-3 w-full rounded-md px-3 py-2 text-left hover:bg-accent transition-colors"
                >
                  <Briefcase className="h-4 w-4 mt-0.5 text-muted-foreground flex-shrink-0" />
                  <div className="flex-1 min-w-0">
                    <div className="font-medium text-sm">{job.type}</div>
                    <div className="text-xs text-muted-foreground truncate">{job.address}</div>
                  </div>
                </button>
              ))}
            </div>
          )}

          {results.contractors.length > 0 && (
            <div className="p-2 border-t border-border">
              <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">Contractors</div>
              {results.contractors.map((contractor) => (
                <button
                  key={contractor.id}
                  onClick={() => handleContractorClick(contractor.id)}
                  className="flex items-start gap-3 w-full rounded-md px-3 py-2 text-left hover:bg-accent transition-colors"
                >
                  <User className="h-4 w-4 mt-0.5 text-muted-foreground flex-shrink-0" />
                  <div className="flex-1 min-w-0">
                    <div className="font-medium text-sm">{contractor.name}</div>
                    <div className="text-xs text-muted-foreground truncate">{contractor.skills}</div>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
