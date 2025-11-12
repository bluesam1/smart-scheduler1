"use client"

import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import { MapPin, Star, Clock } from "lucide-react"
import { useState } from "react"
import { ContractorDetailsDialog } from "./contractor-details-dialog"

interface ContractorCardProps {
  contractor: {
    id: string
    name: string
    skills: string[]
    rating: number
    availability: string
    baseLocation: string
    jobsToday: number
    maxJobs: number
  }
}

export function ContractorCard({ contractor }: ContractorCardProps) {
  const utilizationPercent = (contractor.jobsToday / contractor.maxJobs) * 100
  const isAvailable = contractor.availability === "Available"
  const [detailsOpen, setDetailsOpen] = useState(false)

  return (
    <>
      <Card className="hover:border-primary/50 transition-colors cursor-pointer" onClick={() => setDetailsOpen(true)}>
        <CardHeader className="pb-3">
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <h3 className="font-semibold text-balance">{contractor.name}</h3>
              <div className="mt-1 flex items-center gap-1 text-sm text-muted-foreground">
                <MapPin className="h-3 w-3" />
                <span>{contractor.baseLocation}</span>
              </div>
            </div>
            <Badge variant={isAvailable ? "default" : "secondary"}>{contractor.availability}</Badge>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex items-center justify-between text-sm">
            <div className="flex items-center gap-1">
              <Star className="h-4 w-4 fill-accent text-accent" />
              <span className="font-medium">{contractor.rating}/100</span>
            </div>
            <div className="flex items-center gap-1 text-muted-foreground">
              <Clock className="h-4 w-4" />
              <span>
                {contractor.jobsToday}/{contractor.maxJobs} jobs
              </span>
            </div>
          </div>

          <div className="space-y-1.5">
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>Today's utilization</span>
              <span>{utilizationPercent}%</span>
            </div>
            <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
              <div className="h-full bg-primary transition-all" style={{ width: `${utilizationPercent}%` }} />
            </div>
          </div>

          <div className="flex flex-wrap gap-1">
            {contractor.skills.slice(0, 3).map((skill) => (
              <Badge key={skill} variant="outline" className="text-xs">
                {skill}
              </Badge>
            ))}
          </div>
        </CardContent>
      </Card>

      <ContractorDetailsDialog open={detailsOpen} onOpenChange={setDetailsOpen} contractor={contractor} />
    </>
  )
}
