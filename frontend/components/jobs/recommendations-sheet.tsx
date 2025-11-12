"use client"

import type React from "react"
import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from "@/components/ui/sheet"
import { Badge } from "@/components/ui/badge"
import { RecommendationCard } from "./recommendation-card"
import { Sparkles, Clock, MapPin, ChevronLeft, ChevronRight, CalendarIcon, Map } from "lucide-react"
import { MapViewDialog } from "@/components/map-view-dialog"

interface Job {
  id: string
  type: string
  address: string
  scheduledDate: string
  scheduledTime: string
  duration: string
  priority: string
  requiredSkills: string[]
}

interface RecommendationsSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  job: Job
}

const generateRecommendations = (dateString: string) => {
  // Use date as seed for consistent but different results per day
  const dateSeed = new Date(dateString).getTime()
  const random = (min: number, max: number, seed: number) => {
    const x = Math.sin(seed++) * 10000
    return Math.floor((x - Math.floor(x)) * (max - min + 1)) + min
  }

  const baseContractors = [
    {
      contractorId: "1",
      contractorName: "John Martinez",
      baseLocation: "Downtown District",
      rating: 92,
      skills: ["HVAC", "Electrical", "Plumbing"],
    },
    {
      contractorId: "2",
      contractorName: "Sarah Chen",
      baseLocation: "North Side",
      rating: 88,
      skills: ["Electrical", "Solar", "Smart Home"],
    },
    {
      contractorId: "3",
      contractorName: "Mike Johnson",
      baseLocation: "West End",
      rating: 95,
      skills: ["HVAC", "Refrigeration", "Ventilation"],
    },
  ]

  // Shuffle and generate varied scores for each day
  return baseContractors
    .map((contractor, index) => {
      const seed = dateSeed + index * 1000
      const availability = random(70, 98, seed)
      const distance = random(75, 95, seed + 1)
      const rotation = random(80, 95, seed + 2)
      const totalScore = Math.floor((availability + contractor.rating + distance + rotation) / 4)

      const travelMinutes = random(5, 25, seed + 3)
      const travelMiles = (travelMinutes * 0.3).toFixed(1)

      // Generate different time slots based on availability
      const slots = []
      const morningTime = random(8, 10, seed + 4)
      slots.push({
        time: `${morningTime.toString().padStart(2, "0")}:00 AM`,
        label: "Earliest",
        confidence: availability > 90 ? "High" : "Medium",
      })

      const middayTime = random(11, 13, seed + 5)
      const middayLabel = distance > 85 ? "Lowest Travel" : "Best Fit"
      slots.push({
        time: `${middayTime === 12 ? 12 : middayTime}:${random(0, 30, seed + 6) === 0 ? "00" : "30"} ${
          middayTime >= 12 ? "PM" : "AM"
        }`,
        label: middayLabel,
        confidence: totalScore > 85 ? "High" : "Medium",
      })

      if (availability > 75) {
        const afternoonTime = random(14, 16, seed + 7)
        slots.push({
          time: `${afternoonTime === 12 ? 12 : afternoonTime - 12}:${random(0, 30, seed + 8) === 0 ? "00" : "30"} PM`,
          label: "Alternative",
          confidence: availability > 85 ? "Medium" : "Low",
        })
      }

      const utilization = random(25, 75, seed + 9)
      const jobsToday = Math.floor(utilization / 30)

      return {
        ...contractor,
        totalScore,
        scores: {
          availability,
          rating: contractor.rating,
          distance,
          rotation,
        },
        travelTime: `${travelMinutes} min`,
        travelDistance: `${travelMiles} miles`,
        rationale:
          totalScore > 90
            ? "High availability with excellent rating. Minimal travel time from base location."
            : totalScore > 85
              ? "Good availability and rating. Reasonable travel distance."
              : "Moderate availability and distance. Good rating with rotation boost.",
        suggestedSlots: slots,
        currentUtilization: utilization,
        jobsToday,
      }
    })
    .sort((a, b) => b.totalScore - a.totalScore)
}

export function RecommendationsSheet({ open, onOpenChange, job }: RecommendationsSheetProps) {
  const [loading, setLoading] = useState(false)
  const [selectedDate, setSelectedDate] = useState(() => {
    const date = new Date(job.scheduledDate)
    return date.toISOString().split("T")[0]
  })

  const [recommendations, setRecommendations] = useState(() => generateRecommendations(selectedDate))
  const [mapOpen, setMapOpen] = useState(false)

  useEffect(() => {
    console.log("[v0] Date changed to:", selectedDate, "- refreshing recommendations")
    setLoading(true)
    // Simulate API call delay
    const timer = setTimeout(() => {
      setRecommendations(generateRecommendations(selectedDate))
      setLoading(false)
    }, 300)
    return () => clearTimeout(timer)
  }, [selectedDate])

  const navigateDate = (direction: "prev" | "next") => {
    const currentDate = new Date(selectedDate)
    currentDate.setDate(currentDate.getDate() + (direction === "next" ? 1 : -1))
    setSelectedDate(currentDate.toISOString().split("T")[0])
  }

  const handleDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSelectedDate(e.target.value)
  }

  const formatDisplayDate = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleDateString("en-US", {
      weekday: "short",
      month: "short",
      day: "numeric",
      year: "numeric",
    })
  }

  const handleAssign = (contractorId: string, slotTime: string) => {
    console.log(`Assigning contractor ${contractorId} to job ${job.id} at ${slotTime} on ${selectedDate}`)
    onOpenChange(false)
  }

  const mapLocations = [
    {
      type: "job" as const,
      name: job.type,
      address: job.address,
    },
    ...recommendations.map((rec) => ({
      type: "contractor" as const,
      name: rec.contractorName,
      address: rec.baseLocation,
      skills: rec.skills,
    })),
  ]

  return (
    <>
      <Sheet open={open} onOpenChange={onOpenChange}>
        <SheetContent className="w-full sm:max-w-2xl overflow-y-auto">
          <SheetHeader>
            <SheetTitle className="flex items-center gap-2">
              <Sparkles className="h-5 w-5 text-primary" />
              Contractor Recommendations
            </SheetTitle>
            <SheetDescription className="space-y-2 pt-2">
              <div className="font-semibold text-foreground text-balance">{job.type}</div>
              <div className="flex flex-col gap-1 text-sm">
                <div className="flex items-center gap-1.5">
                  <MapPin className="h-3 w-3" />
                  <span className="text-pretty">{job.address}</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <Clock className="h-3 w-3" />
                  <span>
                    {job.scheduledDate} at {job.scheduledTime} ({job.duration})
                  </span>
                </div>
              </div>
              <div className="flex flex-wrap gap-1 pt-1">
                {job.requiredSkills.map((skill) => (
                  <Badge key={skill} variant="secondary" className="text-xs">
                    {skill}
                  </Badge>
                ))}
              </div>
              <Button
                variant="outline"
                size="sm"
                className="mt-2 w-full bg-transparent"
                onClick={() => setMapOpen(true)}
              >
                <Map className="mr-2 h-4 w-4" />
                View Map
              </Button>
            </SheetDescription>
          </SheetHeader>

          <div className="mt-6 flex items-center justify-between gap-2 p-3 bg-muted/50 rounded-lg border border-border">
            <Button variant="ghost" size="icon" onClick={() => navigateDate("prev")} className="h-8 w-8">
              <ChevronLeft className="h-4 w-4" />
            </Button>

            <div className="flex items-center gap-2 flex-1 justify-center">
              <CalendarIcon className="h-4 w-4 text-muted-foreground" />
              <div className="relative">
                <input
                  type="date"
                  value={selectedDate}
                  onChange={handleDateChange}
                  className="px-3 py-1 text-sm font-medium bg-background border border-input rounded-md cursor-pointer hover:bg-accent transition-colors"
                />
              </div>
              <span className="text-sm font-medium text-foreground">{formatDisplayDate(selectedDate)}</span>
            </div>

            <Button variant="ghost" size="icon" onClick={() => navigateDate("next")} className="h-8 w-8">
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>

          <div className="mt-6 space-y-4">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {loading ? "Refreshing..." : `Showing ${recommendations.length} qualified contractors`}
              </p>
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setLoading(true)
                  setTimeout(() => {
                    setRecommendations(generateRecommendations(selectedDate))
                    setLoading(false)
                  }, 300)
                }}
                disabled={loading}
              >
                {loading ? "Refreshing..." : "Refresh"}
              </Button>
            </div>

            <div className="space-y-4">
              {recommendations.map((recommendation, index) => (
                <RecommendationCard
                  key={recommendation.contractorId}
                  recommendation={recommendation}
                  rank={index + 1}
                  onAssign={handleAssign}
                />
              ))}
            </div>
          </div>
        </SheetContent>
      </Sheet>

      <MapViewDialog
        open={mapOpen}
        onOpenChange={setMapOpen}
        locations={mapLocations}
        title={`Contractor Locations: ${job.type}`}
      />
    </>
  )
}
