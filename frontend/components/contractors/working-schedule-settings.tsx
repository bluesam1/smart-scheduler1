"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Card, CardContent } from "@/components/ui/card"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { TimeSelect } from "@/components/ui/time-select"
import { createApiClients } from "@/lib/api/api-client-config"
import { useAuth } from "@/lib/auth/auth-context"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import type { WorkingHoursDto } from "@/lib/api/generated/api-client"

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
const DAY_OF_WEEK_MAP: Record<string, number> = {
  Monday: 1,
  Tuesday: 2,
  Wednesday: 3,
  Thursday: 4,
  Friday: 5,
  Saturday: 6,
  Sunday: 0,
}

interface ScheduleItem {
  day: string
  enabled: boolean
  startTime: string
  endTime: string
}

export function WorkingScheduleSettings({ contractorId }: WorkingScheduleSettingsProps) {
  const { getTokenProvider } = useAuth()
  const [timezone, setTimezone] = useState("America/New_York")
  const [schedule, setSchedule] = useState<ScheduleItem[]>(
    DAYS.map((day) => ({
      day,
      enabled: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"].includes(day),
      startTime: "08:00",
      endTime: "17:00",
    })),
  )
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  // Load existing working hours
  useEffect(() => {
    const loadWorkingHours = async () => {
      setIsLoading(true)
      try {
        const tokenProvider = getTokenProvider()
        const { client } = createApiClients(tokenProvider)
        const contractor = await client.getContractorById(contractorId)
        
        if (contractor?.workingHours) {
          // Map working hours to schedule
          const scheduleMap = new Map<string, ScheduleItem>()
          
          // Initialize all days as disabled
          DAYS.forEach((day) => {
            scheduleMap.set(day, {
              day,
              enabled: false,
              startTime: "08:00",
              endTime: "17:00",
            })
          })
          
          // Populate enabled days from API
          contractor.workingHours.forEach((wh) => {
            const dayName = Object.keys(DAY_OF_WEEK_MAP).find(
              (key) => DAY_OF_WEEK_MAP[key] === wh.dayOfWeek
            )
            if (dayName) {
              scheduleMap.set(dayName, {
                day: dayName,
                enabled: true,
                startTime: wh.startTime || "08:00",
                endTime: wh.endTime || "17:00",
              })
              // Set timezone from first working hours entry
              if (wh.timeZone) {
                setTimezone(wh.timeZone)
              }
            }
          })
          
          setSchedule(DAYS.map((day) => scheduleMap.get(day)!))
        }
      } catch (err) {
        const errorMessage = formatErrorForDisplay(err)
        if (!isAuthenticationError(err)) {
          toast.error(`Failed to load working hours: ${errorMessage}`)
        }
      } finally {
        setIsLoading(false)
      }
    }

    if (contractorId) {
      loadWorkingHours()
    }
  }, [contractorId, getTokenProvider])

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

  const handleSave = async () => {
    setIsSaving(true)
    try {
      const tokenProvider = getTokenProvider()
      const { client } = createApiClients(tokenProvider)
      
      // Convert schedule to WorkingHoursDto array
      const workingHours: WorkingHoursDto[] = schedule
        .filter((item) => item.enabled)
        .map((item) => ({
          dayOfWeek: DAY_OF_WEEK_MAP[item.day],
          startTime: item.startTime,
          endTime: item.endTime,
          timeZone: timezone,
        }))

      if (workingHours.length === 0) {
        toast.error("At least one day must be enabled")
        return
      }

      await client.updateContractorWorkingHours(contractorId, workingHours)
      toast.success("Working schedule saved successfully")
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      
      if (isAuthenticationError(err)) {
        toast.error("Please log in to save working hours")
      } else {
        toast.error(`Failed to save working schedule: ${errorMessage}`)
      }
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Spinner className="h-8 w-8" />
      </div>
    )
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
                    <Switch 
                      id={`day-${index}`} 
                      checked={item.enabled} 
                      onCheckedChange={() => handleToggleDay(index)}
                      disabled={isSaving}
                    />
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
                        disabled={isSaving}
                      />
                      <span className="text-muted-foreground">to</span>
                      <TimeSelect
                        value={item.endTime}
                        onChange={(value) => handleTimeChange(index, "endTime", value)}
                        className="w-32"
                        disabled={isSaving}
                      />
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>

      <Button 
        className="w-full flex-shrink-0" 
        onClick={handleSave}
        disabled={isSaving}
      >
        {isSaving ? (
          <>
            <Spinner className="mr-2 h-4 w-4" />
            Saving...
          </>
        ) : (
          "Save Working Schedule"
        )}
      </Button>
    </div>
  )
}
