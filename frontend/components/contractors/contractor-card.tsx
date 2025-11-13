"use client"

import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import { MapPin, Star, Clock } from "lucide-react"
import { useState } from "react"
import { ContractorDetailsDialog } from "./contractor-details-dialog"
import type { ContractorDto } from "@/lib/api/generated/api-client"

interface ContractorCardProps {
  contractor: ContractorDto
  onContractorUpdated?: () => void
}

export function ContractorCard({ contractor, onContractorUpdated }: ContractorCardProps) {
  const utilizationPercent = contractor.currentUtilization ?? 0
  const jobsToday = contractor.jobsToday ?? 0
  const maxJobs = contractor.maxJobsPerDay ?? 4
  const isAvailable = contractor.availability === "Available"
  const [detailsOpen, setDetailsOpen] = useState(false)

  // Format location display
  const locationDisplay = contractor.baseLocation?.formattedAddress 
    || contractor.baseLocation?.address 
    || `${contractor.baseLocation?.city || ""}, ${contractor.baseLocation?.state || ""}`.trim()
    || "Location not set"

  return (
    <>
      <Card className="hover:border-primary/50 transition-colors cursor-pointer" onClick={() => setDetailsOpen(true)}>
        <CardHeader className="pb-3">
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <h3 className="font-semibold text-balance">{contractor.name || "Unnamed Contractor"}</h3>
              <div className="mt-1 flex items-center gap-1 text-sm text-muted-foreground">
                <MapPin className="h-3 w-3" />
                <span className="truncate">{locationDisplay}</span>
              </div>
            </div>
            <Badge variant={isAvailable ? "default" : "secondary"}>
              {contractor.availability || "Unknown"}
            </Badge>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex items-center justify-between text-sm">
            <div className="flex items-center gap-1">
              <Star className="h-4 w-4 fill-accent text-accent" />
              <span className="font-medium">{contractor.rating ?? 50}/100</span>
            </div>
            <div className="flex items-center gap-1 text-muted-foreground">
              <Clock className="h-4 w-4" />
              <span>
                {jobsToday}/{maxJobs} jobs
              </span>
            </div>
          </div>

          <div className="space-y-1.5">
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>Current utilization</span>
              <span>{utilizationPercent.toFixed(0)}%</span>
            </div>
            <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
              <div className="h-full bg-primary transition-all" style={{ width: `${utilizationPercent}%` }} />
            </div>
          </div>

          <div className="flex flex-wrap gap-1">
            {(contractor.skills || []).slice(0, 3).map((skill) => (
              <Badge key={skill} variant="outline" className="text-xs">
                {skill}
              </Badge>
            ))}
            {(contractor.skills?.length ?? 0) > 3 && (
              <Badge variant="outline" className="text-xs">
                +{(contractor.skills?.length ?? 0) - 3} more
              </Badge>
            )}
          </div>
        </CardContent>
      </Card>

      <ContractorDetailsDialog 
        open={detailsOpen} 
        onOpenChange={(open) => {
          setDetailsOpen(open)
          if (!open) {
            // Refresh when dialog closes
            onContractorUpdated?.()
          }
        }}
        contractor={contractor}
        onContractorUpdated={onContractorUpdated}
      />
    </>
  )
}
