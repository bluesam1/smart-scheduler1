"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Clock, CheckCircle2, UserCheck } from "lucide-react"

interface Activity {
  id: string
  type: "assignment" | "completion"
  title: string
  description: string
  timestamp: string
}

export function ActivityFeed() {
  const [activities, setActivities] = useState<Activity[]>([
    {
      id: "1",
      type: "assignment",
      title: "Job Assigned",
      description: "Hardwood Installation assigned to John Martinez",
      timestamp: "2 min ago",
    },
    {
      id: "3",
      type: "completion",
      title: "Job Completed",
      description: "Carpet Installation completed by Sarah Chen",
      timestamp: "15 min ago",
    },
  ])

  const getActivityIcon = (type: Activity["type"]) => {
    switch (type) {
      case "assignment":
        return <UserCheck className="h-4 w-4 text-accent" />
      case "completion":
        return <CheckCircle2 className="h-4 w-4 text-chart-3" />
    }
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-lg flex items-center gap-2">Recent Activity</CardTitle>
      </CardHeader>
      <CardContent>
        <ScrollArea className="h-[300px] pr-4">
          <div className="space-y-4">
            {activities.map((activity) => (
              <div key={activity.id} className="flex gap-3">
                <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted">
                  {getActivityIcon(activity.type)}
                </div>
                <div className="flex-1 space-y-1">
                  <div className="flex items-start justify-between gap-2">
                    <p className="text-sm font-medium leading-tight text-balance">{activity.title}</p>
                    <Badge variant="outline" className="shrink-0 text-xs">
                      <Clock className="mr-1 h-3 w-3" />
                      {activity.timestamp}
                    </Badge>
                  </div>
                  <p className="text-sm text-muted-foreground text-pretty">{activity.description}</p>
                </div>
              </div>
            ))}
          </div>
        </ScrollArea>
      </CardContent>
    </Card>
  )
}
