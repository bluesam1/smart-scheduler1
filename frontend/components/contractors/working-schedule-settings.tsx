"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Card, CardContent } from "@/components/ui/card"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { TimeSelect } from "@/components/ui/time-select"

interface WorkingScheduleSettingsProps {
  contractorId: string
}

const US_TIMEZONES = [
  { value: "America/New_York", label: "Eastern Time (ET)" },
  { value: "America/Chicago", label: "Central Time (CT)" },
  { value: "America/Denver", label: "Mountain Time (MT)" },
  { value: "America/Phoenix", label: "Arizona Time (MST)" },
  { value: "America/Los_Angeles", label: "Pacific Time (PT)" },
  { value: "America/Anchorage", label: "Alaska Time (AKT)" },
  { value: "Pacific/Honolulu", label: "Hawaii Time (HST)" },
]

const DAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]

export function WorkingScheduleSettings({ contractorId }: WorkingScheduleSettingsProps) {
  const [timezone, setTimezone] = useState("America/New_York")
  const [schedule, setSchedule] = useState(
    DAYS.map((day) => ({
      day,
      enabled: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"].includes(day),
      startTime: "08:00",
      endTime: "17:00",
    })),
  )

  const handleToggleDay = (index: number) => {
    const newSchedule = [...schedule]
    newSchedule[index].enabled = !newSchedule[index].enabled
    setSchedule(newSchedule)
  }

  const handleTimeChange = (index: number, field: "startTime" | "endTime", value: string) => {
    const newSchedule = [...schedule]
    newSchedule[index][field] = value
    setSchedule(newSchedule)
  }

  return (
    <div className="space-y-6 flex flex-col h-full">
      <div className="space-y-2 flex-shrink-0">
        <Label htmlFor="timezone">Contractor Timezone</Label>
        <Select value={timezone} onValueChange={setTimezone}>
          <SelectTrigger id="timezone">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {US_TIMEZONES.map((tz) => (
              <SelectItem key={tz.value} value={tz.value}>
                {tz.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex-1 flex flex-col min-h-0">
        <h4 className="text-sm font-medium mb-4 flex-shrink-0">Weekly Availability</h4>
        <div className="space-y-3 overflow-auto pr-2 flex-1">
          {schedule.map((item, index) => (
            <Card key={item.day}>
              <CardContent className="p-4">
                <div className="flex items-center gap-4">
                  <div className="flex items-center gap-2 w-32">
                    <Switch id={`day-${index}`} checked={item.enabled} onCheckedChange={() => handleToggleDay(index)} />
                    <Label htmlFor={`day-${index}`} className="font-medium cursor-pointer">
                      {item.day}
                    </Label>
                  </div>

                  {item.enabled && (
                    <div className="flex items-center gap-2 flex-1">
                      <TimeSelect
                        value={item.startTime}
                        onChange={(value) => handleTimeChange(index, "startTime", value)}
                        className="w-32"
                      />
                      <span className="text-muted-foreground">to</span>
                      <TimeSelect
                        value={item.endTime}
                        onChange={(value) => handleTimeChange(index, "endTime", value)}
                        className="w-32"
                      />
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>

      <Button className="w-full flex-shrink-0">Save Working Schedule</Button>
    </div>
  )
}
