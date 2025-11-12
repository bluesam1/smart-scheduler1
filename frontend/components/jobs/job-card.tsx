"use client"

import type React from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { MapPin, Clock, Calendar, Sparkles, UserCircle, RefreshCw } from "lucide-react"
import { useState, useEffect } from "react"
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

const generateTopRecommendation = (jobId: string, dateString: string) => {
  const dateSeed = new Date(dateString).getTime() + Number.parseInt(jobId) * 1000
  const random = (min: number, max: number, seed: number) => {
    const x = Math.sin(seed++) * 10000
    return Math.floor((x - Math.floor(x)) * (max - min + 1)) + min
  }

  const contractors = ["John Martinez", "Sarah Chen", "Mike Johnson"]
  const contractorIndex = random(0, contractors.length - 1, dateSeed)
  const contractor = contractors[contractorIndex]

  const morningTime = random(8, 10, dateSeed + 1)
  const middayTime = random(11, 13, dateSeed + 2)
  const afternoonTime = random(14, 16, dateSeed + 3)

  const slots = [
    `${morningTime.toString().padStart(2, "0")}:00am`,
    `${middayTime === 12 ? 12 : middayTime}:${random(0, 1, dateSeed + 4) === 0 ? "00" : "30"}${middayTime >= 12 ? "pm" : "am"}`,
    `${afternoonTime === 12 ? 12 : afternoonTime - 12}:${random(0, 1, dateSeed + 5) === 0 ? "00" : "30"}pm`,
  ]

  // Randomly decide if we need multiple slots (for longer jobs)
  const needsMultipleSlots = random(0, 1, dateSeed + 6) === 1
  const timeframe = needsMultipleSlots
    ? `${slots[0]}-${slots[0].replace(/\d+:\d+/, (match) => {
        const [hour, min] = match.split(":").map(Number)
        const newHour = hour + 2
        return `${newHour}:${min.toString().padStart(2, "0")}`
      })} and ${slots[1]}-${slots[2]}`
    : slots[0]

  return {
    contractor,
    timeframe,
  }
}

export function JobCard({ job, onRequestRecommendations }: JobCardProps) {
  const router = useRouter()
  const isUnassigned = job.assignmentStatus === "Unassigned"
  const isRush = job.priority === "Rush"
  const [recommendationsOpen, setRecommendationsOpen] = useState(false)
  const [loadingRecommendation, setLoadingRecommendation] = useState(isUnassigned)
  const [topRecommendation, setTopRecommendation] = useState<{ contractor: string; timeframe: string } | null>(null)
  const [showConfirmDialog, setShowConfirmDialog] = useState(false)
  const [isScheduling, setIsScheduling] = useState(false)

  useEffect(() => {
    if (isUnassigned) {
      setLoadingRecommendation(true)
      const timer = setTimeout(() => {
        const recommendation = generateTopRecommendation(job.id, job.scheduledDate)
        setTopRecommendation(recommendation)
        setLoadingRecommendation(false)
      }, 2000) // 2 second delay to simulate AI processing
      return () => clearTimeout(timer)
    }
  }, [isUnassigned, job.id, job.scheduledDate])

  const handleQuickSchedule = () => {
    setShowConfirmDialog(false)
    setIsScheduling(true)
    console.log("[v0] Quick scheduling job:", job.id, "with contractor:", topRecommendation?.contractor)

    setTimeout(() => {
      console.log("[v0] Job card removed from view")
    }, 1000)
  }

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
        className={`${isRush ? "border-destructive/50" : ""} transition-all duration-1000 cursor-pointer hover:border-primary/30 ${
          isScheduling ? "opacity-0 scale-95 -translate-y-2" : "opacity-100 scale-100 translate-y-0"
        }`}
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

          {isUnassigned && (
            <>
              {loadingRecommendation ? (
                <div className="rounded-md bg-muted/30 p-3 border border-dashed border-muted-foreground/30">
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Sparkles className="h-4 w-4 animate-pulse" />
                    <span className="animate-pulse">Finding best contractor...</span>
                  </div>
                </div>
              ) : topRecommendation ? (
                <button
                  onClick={() => setShowConfirmDialog(true)}
                  className="w-full rounded-md bg-primary/5 p-3 border border-primary/20 hover:bg-primary/10 hover:border-primary/30 transition-colors cursor-pointer text-left"
                >
                  <div className="flex items-center gap-2 mb-1">
                    <Sparkles className="h-4 w-4 text-primary" />
                    <span className="text-xs font-medium text-primary">Top Recommendation</span>
                  </div>
                  <div className="space-y-0.5">
                    <div className="text-sm font-medium">{topRecommendation.contractor}</div>
                    <div className="text-xs text-muted-foreground">{topRecommendation.timeframe}</div>
                  </div>
                </button>
              ) : null}
            </>
          )}

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

      <AlertDialog open={showConfirmDialog} onOpenChange={setShowConfirmDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Schedule Job?</AlertDialogTitle>
            <AlertDialogDescription>
              {topRecommendation && (
                <>
                  Assign <span className="font-semibold text-foreground">{topRecommendation.contractor}</span> to{" "}
                  <span className="font-semibold text-foreground">{job.type}</span> on{" "}
                  <span className="font-semibold text-foreground">{job.scheduledDate}</span> at{" "}
                  <span className="font-semibold text-foreground">{topRecommendation.timeframe}</span>?
                </>
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleQuickSchedule}>Schedule Job</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <RecommendationsSheet open={recommendationsOpen} onOpenChange={setRecommendationsOpen} job={job} />
    </>
  )
}
