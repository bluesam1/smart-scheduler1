"use client"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import { Progress } from "@/components/ui/progress"
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { MapPin, Star, Clock, TrendingUp, Info, Wrench } from "lucide-react"
import { useState } from "react"
import { SmartCalendarScheduler } from "./smart-calendar-scheduler"
import { ContractorDetailsDialog } from "@/components/contractors/contractor-details-dialog"
import { createApiClients } from "@/lib/api/api-client-config"
import { useAuth } from "@/lib/auth/auth-context"
import { toast } from "sonner"
import type { ContractorDto } from "@/lib/api/generated/api-client"

interface RecommendationCardProps {
  recommendation: {
    contractorId: string
    contractorName: string
    baseLocation: string
    rating: number
    totalScore: number
    scores: {
      availability: number
      rating: number
      distance: number
      rotation: number
    }
    travelTime: string
    travelDistance: string
    rationale: string
    suggestedSlots: Array<{
      time: string
      startUtc?: string
      endUtc?: string
      label: string
      confidence: string
    }>
    currentUtilization: number
    jobsToday: number
    skills?: string[]
    isAssigning?: boolean
    isAssigned?: boolean
  }
  rank: number
  onAssign: (contractorId: string, slotTime: string) => void
  jobDuration?: number
}

export function RecommendationCard({ recommendation, rank, onAssign, jobDuration = 2 }: RecommendationCardProps) {
  const { getTokenProvider } = useAuth()
  const [showScheduler, setShowScheduler] = useState(false)
  const [selectedContractor, setSelectedContractor] = useState<ContractorDto | null>(null)
  const [isLoadingContractor, setIsLoadingContractor] = useState(false)

  const getScoreColor = (score: number) => {
    if (score >= 90) return "text-chart-3"
    if (score >= 80) return "text-primary"
    if (score >= 70) return "text-accent"
    return "text-muted-foreground"
  }

  const getConfidenceBadge = (confidence: string) => {
    if (confidence === "High") return "default"
    if (confidence === "Medium") return "secondary"
    return "outline"
  }

  const handleSchedule = (sessions: any[]) => {
    console.log("[v0] Scheduling sessions:", sessions)
    setShowScheduler(false)
    onAssign(recommendation.contractorId, JSON.stringify(sessions))
  }

  const handleContractorClick = async () => {
    console.log("[v0] Opening contractor details for:", recommendation.contractorId)
    
    setIsLoadingContractor(true)
    try {
      const tokenProvider = getTokenProvider()
      if (!tokenProvider) {
        toast.error("Please log in to view contractor details")
        return
      }

      const { client } = createApiClients(tokenProvider)
      const contractor = await client.getContractorById(recommendation.contractorId)
      setSelectedContractor(contractor)
    } catch (error) {
      console.error("Failed to fetch contractor details:", error)
      toast.error("Failed to load contractor details")
    } finally {
      setIsLoadingContractor(false)
    }
  }

  const getRecommendedTimeDisplay = () => {
    if (!recommendation.suggestedSlots || recommendation.suggestedSlots.length === 0) {
      return "Click to view calendar and schedule"
    }

    // If slots have startUtc/endUtc, use those for proper date and time range display
    const firstSlot = recommendation.suggestedSlots[0]
    
    if (firstSlot.startUtc && firstSlot.endUtc) {
      const startDate = new Date(firstSlot.startUtc)
      const endDate = new Date(firstSlot.endUtc)
      
      // Format date: "Mon, Jul 13"
      const dateStr = startDate.toLocaleDateString("en-US", {
        weekday: "short",
        month: "short",
        day: "numeric"
      })
      
      // Format times: "9:00 AM - 11:00 AM"
      const startTime = startDate.toLocaleTimeString("en-US", {
        hour: "numeric",
        minute: "2-digit",
        hour12: true
      })
      
      const endTime = endDate.toLocaleTimeString("en-US", {
        hour: "numeric",
        minute: "2-digit",
        hour12: true
      })
      
      return `${dateStr} • ${startTime} - ${endTime}`
    }
    
    // Fallback to old format if datetime not available
    return recommendation.suggestedSlots.map(slot => slot.time).join(" and ")
  }

  const handleRecommendedTimeClick = () => {
    // If no suggested slots, open the calendar to manually schedule
    if (!recommendation.suggestedSlots || recommendation.suggestedSlots.length === 0) {
      console.log("[v0] No suggested slots, opening calendar scheduler")
      setShowScheduler(true)
      return
    }

    console.log("[v0] Quick scheduling with recommended time")

    // Create sessions from the suggested slots
    const sessions = recommendation.suggestedSlots.map((slot, index) => ({
      id: `session-${Date.now()}-${index}`,
      startTime: slot.time,
      // Calculate duration based on job requirements
      // For now, use equal distribution across slots
      duration: 1, // 1 hour per slot, adjust as needed
      date: new Date().toISOString().split("T")[0],
    }))

    // Immediately assign with the recommended slots
    handleSchedule(sessions)
  }

  return (
    <>
      <Card className="border-2 hover:border-primary/50 transition-colors">
        <CardHeader className="pb-4">
          <div className="flex items-start justify-between gap-4">
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 font-semibold text-primary">
                #{rank}
              </div>
              <div className="flex-1">
                <button
                  onClick={handleContractorClick}
                  disabled={isLoadingContractor}
                  className="font-semibold text-balance hover:text-primary transition-colors text-left disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isLoadingContractor ? "Loading..." : recommendation.contractorName}
                </button>
                <div className="mt-1 flex items-center gap-1 text-sm text-muted-foreground">
                  <MapPin className="h-3 w-3" />
                  <span>{recommendation.baseLocation}</span>
                </div>
                <div className="mt-2 flex flex-wrap gap-1">
                  {recommendation.skills?.map((skill: string) => (
                    <Badge key={skill} variant="outline" className="text-xs flex items-center gap-1">
                      <Wrench className="h-2.5 w-2.5" />
                      {skill}
                    </Badge>
                  ))}
                </div>
              </div>
            </div>
            <div className="text-right">
              <div className={`text-2xl font-bold ${getScoreColor(recommendation.totalScore)}`}>
                {recommendation.totalScore}
              </div>
              <div className="text-xs text-muted-foreground">Total Score</div>
            </div>
          </div>
        </CardHeader>

        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <TooltipProvider>
              <div className="space-y-1">
                <div className="flex items-center justify-between text-sm">
                  <div className="flex items-center gap-1">
                    <span className="text-muted-foreground">Availability</span>
                    <Tooltip>
                      <TooltipTrigger>
                        <Info className="h-3 w-3 text-muted-foreground" />
                      </TooltipTrigger>
                      <TooltipContent>
                        <p className="text-xs">Based on open time slots and current workload</p>
                      </TooltipContent>
                    </Tooltip>
                  </div>
                  <span className="font-medium">{recommendation.scores.availability}</span>
                </div>
                <Progress value={recommendation.scores.availability} className="h-1.5" />
              </div>

              <div className="space-y-1">
                <div className="flex items-center justify-between text-sm">
                  <div className="flex items-center gap-1">
                    <span className="text-muted-foreground">Rating</span>
                    <Tooltip>
                      <TooltipTrigger>
                        <Info className="h-3 w-3 text-muted-foreground" />
                      </TooltipTrigger>
                      <TooltipContent>
                        <p className="text-xs">Composite score: on-time, quality, CSAT</p>
                      </TooltipContent>
                    </Tooltip>
                  </div>
                  <span className="font-medium">{recommendation.scores.rating}</span>
                </div>
                <Progress value={recommendation.scores.rating} className="h-1.5" />
              </div>

              <div className="space-y-1">
                <div className="flex items-center justify-between text-sm">
                  <div className="flex items-center gap-1">
                    <span className="text-muted-foreground">Distance</span>
                    <Tooltip>
                      <TooltipTrigger>
                        <Info className="h-3 w-3 text-muted-foreground" />
                      </TooltipTrigger>
                      <TooltipContent>
                        <p className="text-xs">Travel time and distance from current location</p>
                      </TooltipContent>
                    </Tooltip>
                  </div>
                  <span className="font-medium">{recommendation.scores.distance}</span>
                </div>
                <Progress value={recommendation.scores.distance} className="h-1.5" />
              </div>

              <div className="space-y-1">
                <div className="flex items-center justify-between text-sm">
                  <div className="flex items-center gap-1">
                    <span className="text-muted-foreground">Rotation</span>
                    <Tooltip>
                      <TooltipTrigger>
                        <Info className="h-3 w-3 text-muted-foreground" />
                      </TooltipTrigger>
                      <TooltipContent>
                        <p className="text-xs">Fairness boost for underutilized contractors</p>
                      </TooltipContent>
                    </Tooltip>
                  </div>
                  <span className="font-medium">{recommendation.scores.rotation}</span>
                </div>
                <Progress value={recommendation.scores.rotation} className="h-1.5" />
              </div>
            </TooltipProvider>
          </div>

          <div className="grid grid-cols-3 gap-3 rounded-lg bg-muted/50 p-3">
            <div className="space-y-1">
              <div className="flex items-center gap-1 text-xs text-muted-foreground">
                <Star className="h-3 w-3" />
                <span>Rating</span>
              </div>
              <div className="font-semibold">{recommendation.rating}/100</div>
            </div>
            <div className="space-y-1">
              <div className="flex items-center gap-1 text-xs text-muted-foreground">
                <Clock className="h-3 w-3" />
                <span>Travel</span>
              </div>
              <div className="font-semibold">{recommendation.travelTime}</div>
            </div>
            <div className="space-y-1">
              <div className="flex items-center gap-1 text-xs text-muted-foreground">
                <TrendingUp className="h-3 w-3" />
                <span>Utilization</span>
              </div>
              <div className="font-semibold">{recommendation.currentUtilization}%</div>
            </div>
          </div>

          <div className="rounded-lg border border-border bg-card p-3 text-sm text-pretty">
            <p className="text-muted-foreground">{recommendation.rationale}</p>
          </div>

          <div className="space-y-2">
            <div className="text-xs text-muted-foreground uppercase tracking-wide">
              {(!recommendation.suggestedSlots || recommendation.suggestedSlots.length === 0) 
                ? "Availability" 
                : "Recommended Time"}
            </div>
            <button
              onClick={handleRecommendedTimeClick}
              className="w-full px-4 py-2.5 rounded-md border border-primary/20 bg-primary/5 text-sm font-medium hover:bg-primary/10 hover:border-primary/30 transition-colors text-left cursor-pointer"
            >
              {getRecommendedTimeDisplay()}
            </button>
          </div>

          <Button 
            className="w-full" 
            onClick={() => setShowScheduler(true)}
            disabled={recommendation.isAssigning || recommendation.isAssigned}
          >
            {recommendation.isAssigning 
              ? "Assigning..." 
              : recommendation.isAssigned 
                ? "Assigned" 
                : `Schedule with ${recommendation.contractorName}`}
          </Button>
        </CardContent>
      </Card>

      <Dialog open={showScheduler} onOpenChange={setShowScheduler}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Schedule with {recommendation.contractorName}</DialogTitle>
          </DialogHeader>
          <SmartCalendarScheduler
            contractorId={recommendation.contractorId}
            contractorName={recommendation.contractorName}
            jobDuration={jobDuration}
            selectedDate={new Date().toISOString().split("T")[0]}
            onSchedule={handleSchedule}
            onCancel={() => setShowScheduler(false)}
          />
        </DialogContent>
      </Dialog>

      {selectedContractor && (
        <ContractorDetailsDialog
          contractor={selectedContractor}
          open={!!selectedContractor}
          onOpenChange={(open) => {
            if (!open) {
              setSelectedContractor(null)
            }
          }}
        />
      )}
    </>
  )
}
