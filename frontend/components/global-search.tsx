"use client"

import { useState, useEffect, useRef, useCallback } from "react"
import { Search, Briefcase, User } from "lucide-react"
import { Input } from "@/components/ui/input"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth/auth-context"
import { createApiClients } from "@/lib/api/api-client-config"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import type { JobDto, ContractorDto } from "@/lib/api/generated/api-client"

interface SearchJob {
  id: string
  type: string
  address: string
}

interface SearchContractor {
  id: string
  name: string
  skills: string
}

export function GlobalSearch() {
  const { getTokenProvider, isAuthenticated } = useAuth()
  const [query, setQuery] = useState("")
  const [isOpen, setIsOpen] = useState(false)
  const [allJobs, setAllJobs] = useState<JobDto[]>([])
  const [allContractors, setAllContractors] = useState<ContractorDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [results, setResults] = useState<{ jobs: SearchJob[]; contractors: SearchContractor[] }>({
    jobs: [],
    contractors: [],
  })
  const ref = useRef<HTMLDivElement>(null)
  const router = useRouter()

  const fetchData = useCallback(async () => {
    if (!isAuthenticated) return

    setIsLoading(true)
    try {
      const tokenProvider = getTokenProvider()
      if (!tokenProvider) {
        setIsLoading(false)
        return
      }

      const { client } = createApiClients(tokenProvider)
      
      // Fetch jobs and contractors in parallel
      const [jobs, contractors] = await Promise.all([
        client.getJobs(null, null, 100), // Limit to 100 for search
        client.getContractors(null, 100), // Limit to 100 for search
      ])

      setAllJobs(jobs || [])
      setAllContractors(contractors || [])
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      if (isAuthenticationError(err)) {
        toast.error("Please log in to search")
      } else {
        console.error("Failed to load search data:", errorMessage)
      }
    } finally {
      setIsLoading(false)
    }
  }, [isAuthenticated, getTokenProvider])

  // Fetch data on mount
  useEffect(() => {
    fetchData()
  }, [fetchData])

  useEffect(() => {
    if (query.length > 0 && !isLoading) {
      // Filter jobs
      const jobResults: SearchJob[] = allJobs
        .filter(
          (job) =>
            job.type?.toLowerCase().includes(query.toLowerCase()) ||
            job.location?.address?.toLowerCase().includes(query.toLowerCase()) ||
            job.location?.formattedAddress?.toLowerCase().includes(query.toLowerCase()) ||
            job.location?.city?.toLowerCase().includes(query.toLowerCase()),
        )
        .map((job) => ({
          id: job.id || "",
          type: job.type || "",
          address: job.location?.formattedAddress || job.location?.address || `${job.location?.city || ""}, ${job.location?.state || ""}`.trim() || "",
        }))
        .slice(0, 5) // Limit to 5 results

      // Filter contractors
      const contractorResults: SearchContractor[] = allContractors
        .filter(
          (contractor) =>
            contractor.name?.toLowerCase().includes(query.toLowerCase()) ||
            contractor.skills?.some((skill) => skill.toLowerCase().includes(query.toLowerCase())),
        )
        .map((contractor) => ({
          id: contractor.id || "",
          name: contractor.name || "",
          skills: contractor.skills?.join(", ") || "",
        }))
        .slice(0, 5) // Limit to 5 results

      setResults({ jobs: jobResults, contractors: contractorResults })
      setIsOpen(true)
    } else {
      setIsOpen(false)
    }
  }, [query, allJobs, allContractors, isLoading])

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
