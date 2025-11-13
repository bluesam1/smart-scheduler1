"use client"

import type React from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import { MapPin, Clock, Calendar, Sparkles, UserCircle, RefreshCw } from "lucide-react"
import { useState } from "react"
import { RecommendationsSheet } from "./recommendations-sheet"
import { useRouter } from "next/navigation"

interface JobCardProps {
  job: {
    id: string
    type: string
    address: string
    scheduledDate: string
    scheduledTime: string
    duration: string
    assignmentStatus: "Unassigned" | "Partially Assigned" | "Assigned"
    status: "Pending" | "In Progress" | "Completed" | "Canceled"
    priority: string
    requiredSkills: string[]
    assignedTo?: string
    timezone?: string
  }
  onRequestRecommendations?: (jobId: string) => void
}

export function JobCard({ job, onRequestRecommendations }: JobCardProps) {
  const router = useRouter()
  const isUnassigned = job.assignmentStatus === "Unassigned"
  const isRush = job.priority === "Rush"
  const [recommendationsOpen, setRecommendationsOpen] = useState(false)

  const handleCardClick = (e: React.MouseEvent) => {
    const target = e.target as HTMLElement
    if (target.closest("button") || target.closest('[role="button"]') || target.closest("a")) {
      return
    }
    router.push(`/jobs/${job.id}`)
  }

  return (
    <>
      <Card
        onClick={handleCardClick}
        className={`${isRush ? "border-destructive/50" : ""} cursor-pointer hover:border-primary/30`}
      >
        <CardHeader className="pb-3">
          <div className="flex items-start justify-between gap-4">
            <div className="flex-1 space-y-1">
              <div className="flex items-center gap-2">
                <h3 className="font-semibold text-balance">{job.type}</h3>
                {isRush && (
                  <Badge variant="destructive" className="text-xs">
                    Rush
                  </Badge>
                )}
              </div>
              <div className="flex items-center gap-1 text-sm text-muted-foreground">
                <MapPin className="h-3 w-3" />
                <span className="text-pretty">{job.address}</span>
              </div>
            </div>
            <div className="flex flex-col gap-1 items-end">
              <Badge
                variant={
                  isUnassigned ? "outline" : job.assignmentStatus === "Partially Assigned" ? "secondary" : "default"
                }
                className={isUnassigned ? "border-muted-foreground/30" : ""}
              >
                {job.assignmentStatus}
              </Badge>
              <Badge variant="outline" className="text-xs">
                {job.status}
              </Badge>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-3 gap-4 text-sm">
            <div className="flex items-center gap-1.5 text-muted-foreground">
              <Calendar className="h-4 w-4" />
              <span>{job.scheduledDate}</span>
            </div>
            <div className="flex items-center gap-1.5 text-muted-foreground">
              <Clock className="h-4 w-4" />
              <span>{job.scheduledTime}</span>
            </div>
            <div className="text-muted-foreground">
              <span>{job.duration}</span>
            </div>
          </div>

          {job.timezone && (
            <div className="text-xs text-muted-foreground">
              <Badge variant="outline" className="text-xs">
                {job.timezone}
              </Badge>
            </div>
          )}

          <div className="flex flex-wrap gap-1">
            {job.requiredSkills.map((skill) => (
              <Badge key={skill} variant="secondary" className="text-xs">
                {skill}
              </Badge>
            ))}
          </div>


          {job.assignedTo && (
            <div className="rounded-md bg-muted/50 p-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <UserCircle className="h-4 w-4 text-muted-foreground" />
                  <div>
                    <div className="text-xs text-muted-foreground">Assigned to</div>
                    <div className="font-medium text-sm">{job.assignedTo}</div>
                  </div>
                </div>
                <Button variant="ghost" size="sm" onClick={() => setRecommendationsOpen(true)}>
                  <RefreshCw className="h-3 w-3 mr-1" />
                  Reassign
                </Button>
              </div>
            </div>
          )}

          {isUnassigned && (
            <Button className="w-full" onClick={() => setRecommendationsOpen(true)}>
              <Sparkles className="mr-2 h-4 w-4" />
              Get Recommendations
            </Button>
          )}
        </CardContent>
      </Card>

      <RecommendationsSheet open={recommendationsOpen} onOpenChange={setRecommendationsOpen} job={job} />
    </>
  )
}
