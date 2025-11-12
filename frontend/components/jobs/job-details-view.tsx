"use client"

import { useState } from "react"
import { ArrowLeft, MapPin, Calendar, Clock, Sparkles, User, Edit2, Map } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { RecommendationsSheet } from "./recommendations-sheet"
import Link from "next/link"
import { MapViewDialog } from "@/components/map-view-dialog"

const mockJob = {
  id: "1",
  type: "Hardwood Installation",
  description: "Install hardwood flooring in living room and hallway",
  address: "123 Oak Street, Downtown District, NY 10001",
  scheduledDate: "2025-11-12",
  desiredStartDate: "2025-11-12",
  desiredStartTime: "09:00",
  scheduledTime: "09:00 AM",
  duration: 3,
  assignmentStatus: "Unassigned" as const,
  status: "Pending" as const,
  priority: "Normal",
  requiredSkills: ["Hardwood Installation", "Finishing", "Flooring"],
  assignedTo: null,
  timezone: "America/New_York",
  createdAt: "2025-11-10",
  updatedAt: "2025-11-11",
}

export function JobDetailsView({ jobId }: { jobId: string }) {
  const [recommendationsOpen, setRecommendationsOpen] = useState(false)
  const [mapOpen, setMapOpen] = useState(false)
  const job = mockJob

  const mapLocations = [
    {
      type: "job" as const,
      name: job.type,
      address: job.address,
    },
    ...(job.assignedTo
      ? [
          {
            type: "contractor" as const,
            name: job.assignedTo,
            address: "Downtown District", // In production, fetch contractor's actual address
            skills: ["HVAC", "Electrical"],
          },
        ]
      : []),
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link href="/jobs">
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div className="flex-1">
          <h1 className="text-3xl font-bold">{job.type}</h1>
          <p className="text-muted-foreground">Job ID: {job.id}</p>
        </div>
        <div className="flex gap-2">
          <Badge
            variant={
              job.assignmentStatus === "Unassigned"
                ? "outline"
                : job.assignmentStatus === "Partially Assigned"
                  ? "secondary"
                  : "default"
            }
          >
            {job.assignmentStatus}
          </Badge>
          <Badge variant="outline">{job.status}</Badge>
          {job.priority === "Rush" && <Badge variant="destructive">Rush</Badge>}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2 space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Job Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <div className="text-sm font-medium text-muted-foreground mb-1">Description</div>
                <p>{job.description}</p>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <div className="text-sm font-medium text-muted-foreground mb-1">Desired Start Date</div>
                  <div className="flex items-center gap-2">
                    <Calendar className="h-4 w-4 text-muted-foreground" />
                    {job.desiredStartDate}
                  </div>
                </div>
                <div>
                  <div className="text-sm font-medium text-muted-foreground mb-1">Desired Start Time</div>
                  <div className="flex items-center gap-2">
                    <Clock className="h-4 w-4 text-muted-foreground" />
                    {job.desiredStartTime}
                  </div>
                </div>
              </div>

              <div>
                <div className="text-sm font-medium text-muted-foreground mb-1">Location</div>
                <div className="flex items-start gap-2">
                  <MapPin className="h-4 w-4 text-muted-foreground mt-0.5" />
                  <span>{job.address}</span>
                </div>
                <Button variant="outline" size="sm" className="mt-2 bg-transparent" onClick={() => setMapOpen(true)}>
                  <Map className="mr-2 h-4 w-4" />
                  View Map
                </Button>
              </div>

              <div>
                <div className="text-sm font-medium text-muted-foreground mb-1">Estimated Duration</div>
                <p>{job.duration} hours</p>
              </div>

              <div>
                <div className="text-sm font-medium text-muted-foreground mb-1">Timezone</div>
                <Badge variant="outline">{job.timezone}</Badge>
              </div>

              <div>
                <div className="text-sm font-medium text-muted-foreground mb-1">Required Skills</div>
                <div className="flex flex-wrap gap-2">
                  {job.requiredSkills.map((skill) => (
                    <Badge key={skill} variant="secondary">
                      {skill}
                    </Badge>
                  ))}
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Activity History</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex gap-3 pb-4 border-b border-border">
                  <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
                    <User className="h-4 w-4 text-primary" />
                  </div>
                  <div className="flex-1">
                    <div className="font-medium text-sm">Job Created</div>
                    <div className="text-xs text-muted-foreground">{job.createdAt}</div>
                  </div>
                </div>
                <div className="flex gap-3">
                  <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
                    <Edit2 className="h-4 w-4 text-primary" />
                  </div>
                  <div className="flex-1">
                    <div className="font-medium text-sm">Job Updated</div>
                    <div className="text-xs text-muted-foreground">{job.updatedAt}</div>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Assignment</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {job.assignedTo ? (
                <>
                  <div>
                    <div className="text-sm font-medium text-muted-foreground mb-1">Assigned To</div>
                    <div className="font-medium">{job.assignedTo}</div>
                  </div>
                  <Button
                    variant="outline"
                    className="w-full bg-transparent"
                    onClick={() => setRecommendationsOpen(true)}
                  >
                    Reassign
                  </Button>
                </>
              ) : (
                <Button className="w-full" onClick={() => setRecommendationsOpen(true)}>
                  <Sparkles className="mr-2 h-4 w-4" />
                  Get Recommendations
                </Button>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button variant="outline" className="w-full justify-start bg-transparent">
                <Edit2 className="mr-2 h-4 w-4" />
                Edit Job
              </Button>
              <Button variant="outline" className="w-full justify-start bg-transparent">
                <Calendar className="mr-2 h-4 w-4" />
                Reschedule
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>

      <RecommendationsSheet open={recommendationsOpen} onOpenChange={setRecommendationsOpen} job={job} />
      <MapViewDialog open={mapOpen} onOpenChange={setMapOpen} locations={mapLocations} title={`Map: ${job.type}`} />
    </div>
  )
}
