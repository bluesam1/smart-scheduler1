"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { ChevronLeft, ChevronRight, MapPin, Clock, XCircle } from "lucide-react"
import { cn } from "@/lib/utils"

interface CalendarViewProps {
  contractorId: string
  contractorName: string
}

type ViewType = "day" | "week" | "month"

// Mock data generator
const generateScheduleData = (date: Date, contractorId: string) => {
  const mockJobs = [
    {
      id: "J-001",
      title: "HVAC Repair",
      location: "123 Main St, New York, NY",
      startTime: "08:00",
      endTime: "10:00",
      status: "assigned",
      timezone: "America/New_York",
    },
    {
      id: "J-045",
      title: "Electrical Inspection",
      location: "456 Oak Ave, Brooklyn, NY",
      startTime: "11:00",
      endTime: "13:00",
      status: "assigned",
      timezone: "America/New_York",
    },
  ]

  const mockExceptions = [
    { date: new Date(2025, 10, 15), type: "holiday", title: "Thanksgiving Prep", allDay: true },
    { date: new Date(2025, 10, 20), type: "time-off", title: "Personal Time", startTime: "14:00", endTime: "17:00" },
  ]

  return { jobs: mockJobs, exceptions: mockExceptions }
}

export function CalendarView({ contractorId, contractorName }: CalendarViewProps) {
  const [currentDate, setCurrentDate] = useState(new Date())
  const [viewType, setViewType] = useState<ViewType>("week")
  const { jobs, exceptions } = generateScheduleData(currentDate, contractorId)

  const navigate = (direction: "prev" | "next") => {
    const newDate = new Date(currentDate)
    if (viewType === "day") {
      newDate.setDate(newDate.getDate() + (direction === "next" ? 1 : -1))
    } else if (viewType === "week") {
      newDate.setDate(newDate.getDate() + (direction === "next" ? 7 : -7))
    } else {
      newDate.setMonth(newDate.getMonth() + (direction === "next" ? 1 : -1))
    }
    setCurrentDate(newDate)
  }

  const getDateRange = () => {
    if (viewType === "day") {
      return currentDate.toLocaleDateString("en-US", {
        weekday: "long",
        year: "numeric",
        month: "long",
        day: "numeric",
      })
    } else if (viewType === "week") {
      const weekStart = new Date(currentDate)
      weekStart.setDate(currentDate.getDate() - currentDate.getDay())
      const weekEnd = new Date(weekStart)
      weekEnd.setDate(weekStart.getDate() + 6)
      return `${weekStart.toLocaleDateString("en-US", { month: "short", day: "numeric" })} - ${weekEnd.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" })}`
    } else {
      return currentDate.toLocaleDateString("en-US", { month: "long", year: "numeric" })
    }
  }

  return (
    <div className="space-y-4">
      {/* Calendar Controls */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Button variant="outline" size="icon" onClick={() => navigate("prev")}>
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <Button variant="outline" onClick={() => setCurrentDate(new Date())}>
            Today
          </Button>
          <Button variant="outline" size="icon" onClick={() => navigate("next")}>
            <ChevronRight className="h-4 w-4" />
          </Button>
          <h3 className="font-semibold ml-2">{getDateRange()}</h3>
        </div>

        <div className="flex gap-1">
          <Button variant={viewType === "day" ? "default" : "outline"} size="sm" onClick={() => setViewType("day")}>
            Day
          </Button>
          <Button variant={viewType === "week" ? "default" : "outline"} size="sm" onClick={() => setViewType("week")}>
            Week
          </Button>
          <Button variant={viewType === "month" ? "default" : "outline"} size="sm" onClick={() => setViewType("month")}>
            Month
          </Button>
        </div>
      </div>

      {/* Calendar Grid */}
      <div className="border rounded-lg">
        {viewType === "week" && <WeekView jobs={jobs} exceptions={exceptions} currentDate={currentDate} />}
        {viewType === "day" && <DayView jobs={jobs} exceptions={exceptions} currentDate={currentDate} />}
        {viewType === "month" && <MonthView jobs={jobs} exceptions={exceptions} currentDate={currentDate} />}
      </div>
    </div>
  )
}

function WeekView({
  jobs,
  exceptions,
  currentDate,
}: {
  jobs: any[]
  exceptions: any[]
  currentDate: Date
}) {
  const weekStart = new Date(currentDate)
  weekStart.setDate(currentDate.getDate() - currentDate.getDay())

  const weekDays = Array.from({ length: 7 }, (_, i) => {
    const date = new Date(weekStart)
    date.setDate(weekStart.getDate() + i)
    return date
  })

  const hours = Array.from({ length: 15 }, (_, i) => i + 6) // 6 AM to 8 PM

  return (
    <div className="overflow-auto max-h-[calc(92vh-280px)]">
      <div className="grid grid-cols-8 min-w-[800px]">
        {/* Header */}
        <div className="sticky top-0 bg-muted p-2 border-b border-r text-xs font-medium z-10">Time</div>
        {weekDays.map((day, i) => (
          <div
            key={i}
            className={cn(
              "sticky top-0 bg-muted p-2 border-b border-r text-center z-10",
              day.toDateString() === new Date().toDateString() && "bg-primary/10",
            )}
          >
            <div className="text-xs font-medium">{day.toLocaleDateString("en-US", { weekday: "short" })}</div>
            <div className="text-sm font-semibold">{day.getDate()}</div>
          </div>
        ))}

        {/* Time slots */}
        {hours.map((hour) => (
          <>
            <div key={`time-${hour}`} className="p-2 border-b border-r text-xs text-muted-foreground bg-muted/30">
              {hour > 12 ? hour - 12 : hour}:00 {hour >= 12 ? "PM" : "AM"}
            </div>
            {weekDays.map((day, dayIndex) => {
              const hasException = exceptions.some(
                (exc) =>
                  exc.date.toDateString() === day.toDateString() &&
                  (exc.allDay || (exc.startTime && Number.parseInt(exc.startTime.split(":")[0]) === hour)),
              )

              const hasJob = jobs.some((job) => Number.parseInt(job.startTime.split(":")[0]) === hour)

              return (
                <div
                  key={`${dayIndex}-${hour}`}
                  className={cn("p-2 border-b border-r min-h-[60px] relative", hasException && "bg-destructive/10")}
                >
                  {hasException &&
                    exceptions
                      .filter(
                        (exc) =>
                          exc.date.toDateString() === day.toDateString() &&
                          (exc.allDay || (exc.startTime && Number.parseInt(exc.startTime.split(":")[0]) === hour)),
                      )
                      .map((exc) => (
                        <div
                          key={exc.title}
                          className="text-xs p-1 rounded bg-destructive/20 mb-1 flex items-start gap-1"
                        >
                          <XCircle className="h-3 w-3 text-destructive flex-shrink-0 mt-0.5" />
                          <span className="font-medium">{exc.title}</span>
                        </div>
                      ))}
                  {hasJob &&
                    jobs
                      .filter((job) => Number.parseInt(job.startTime.split(":")[0]) === hour)
                      .map((job) => (
                        <div key={job.id} className="text-xs p-1.5 rounded bg-primary/20 border border-primary/30 mb-1">
                          <div className="font-medium text-balance">{job.title}</div>
                          <div className="text-muted-foreground flex items-center gap-1 mt-0.5">
                            <Clock className="h-2.5 w-2.5" />
                            {job.startTime}-{job.endTime}
                          </div>
                        </div>
                      ))}
                </div>
              )
            })}
          </>
        ))}
      </div>
    </div>
  )
}

function DayView({ jobs, exceptions, currentDate }: { jobs: any[]; exceptions: any[]; currentDate: Date }) {
  const hours = Array.from({ length: 15 }, (_, i) => i + 6) // 6 AM to 8 PM
  const dayExceptions = exceptions.filter((exc) => exc.date.toDateString() === currentDate.toDateString())

  // Calculate job height based on duration
  const calculateJobHeight = (startTime: string, endTime: string) => {
    const [startHour, startMin] = startTime.split(":").map(Number)
    const [endHour, endMin] = endTime.split(":").map(Number)
    const durationHours = endHour - startHour + (endMin - startMin) / 60
    return durationHours * 60 // 60px per hour
  }

  // Get the starting position offset for a job (in case it starts mid-hour)
  const getJobTopOffset = (startTime: string) => {
    const [, startMin] = startTime.split(":").map(Number)
    return startMin // pixels offset from hour mark
  }

  return (
    <div className="flex flex-col h-full max-h-[calc(92vh-280px)] overflow-hidden">
      {/* Exceptions - fixed at top */}
      {dayExceptions.length > 0 && (
        <div className="p-4 border-b space-y-2 flex-shrink-0">
          <h4 className="text-sm font-medium">Exceptions</h4>
          {dayExceptions.map((exc, i) => (
            <div key={i} className="p-3 rounded-lg bg-destructive/10 border border-destructive/30">
              <div className="flex items-start gap-2">
                <XCircle className="h-4 w-4 text-destructive mt-0.5" />
                <div className="flex-1">
                  <div className="font-medium text-sm">{exc.title}</div>
                  <div className="text-xs text-muted-foreground mt-1">
                    {exc.allDay ? "All Day" : `${exc.startTime} - ${exc.endTime}`}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Scrollable timeline */}
      <div className="flex-1 overflow-y-auto p-4">
        <div className="relative">
          {hours.map((hour, index) => {
            const hourJobs = jobs.filter((job) => Number.parseInt(job.startTime.split(":")[0]) === hour)

            return (
              <div key={hour} className="relative" style={{ height: "60px" }}>
                {/* Hour label and line */}
                <div className="absolute left-0 top-0 w-20 text-sm text-muted-foreground">
                  {hour > 12 ? hour - 12 : hour}:00 {hour >= 12 ? "PM" : "AM"}
                </div>
                <div className="absolute left-24 right-0 top-0 border-b" style={{ height: "60px" }}>
                  {/* Render jobs that start in this hour */}
                  {hourJobs.map((job) => {
                    const height = calculateJobHeight(job.startTime, job.endTime)
                    const topOffset = getJobTopOffset(job.startTime)

                    return (
                      <div
                        key={job.id}
                        className="absolute left-0 right-0 p-3 rounded-lg bg-primary/10 border border-primary/30"
                        style={{
                          top: `${topOffset}px`,
                          height: `${height}px`,
                          zIndex: 10,
                        }}
                      >
                        <div className="font-medium text-sm">{job.title}</div>
                        <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
                          <div className="flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            {job.startTime} - {job.endTime} ({job.timezone})
                          </div>
                          <div className="flex items-center gap-1">
                            <MapPin className="h-3 w-3" />
                            {job.location}
                          </div>
                        </div>
                        <Badge variant="outline" className="mt-2 text-xs">
                          {job.id}
                        </Badge>
                      </div>
                    )
                  })}
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}

function MonthView({ jobs, exceptions, currentDate }: { jobs: any[]; exceptions: any[]; currentDate: Date }) {
  const firstDay = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1)
  const lastDay = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0)
  const startingDayOfWeek = firstDay.getDay()
  const daysInMonth = lastDay.getDate()

  const days = Array.from({ length: daysInMonth }, (_, i) => i + 1)
  const blanks = Array.from({ length: startingDayOfWeek }, (_, i) => null)
  const calendar = [...blanks, ...days]

  return (
    <div>
      <div className="grid grid-cols-7 border-b">
        {["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"].map((day) => (
          <div key={day} className="p-2 text-center text-sm font-medium bg-muted">
            {day}
          </div>
        ))}
      </div>
      <div className="grid grid-cols-7">
        {calendar.map((day, index) => {
          if (!day) {
            return <div key={`blank-${index}`} className="min-h-[100px] border-b border-r bg-muted/30" />
          }

          const cellDate = new Date(currentDate.getFullYear(), currentDate.getMonth(), day)
          const isToday = cellDate.toDateString() === new Date().toDateString()
          const hasException = exceptions.some((exc) => exc.date.toDateString() === cellDate.toDateString())
          const dayJobCount = jobs.length // Simplified for demo

          return (
            <div
              key={day}
              className={cn(
                "min-h-[100px] border-b border-r p-2 relative",
                isToday && "bg-primary/5 border-primary",
                hasException && "bg-destructive/5",
              )}
            >
              <div className={cn("text-sm font-medium mb-1", isToday && "text-primary")}>{day}</div>
              {hasException && (
                <div className="mb-1">
                  <Badge variant="destructive" className="text-xs px-1 py-0">
                    Exception
                  </Badge>
                </div>
              )}
              {dayJobCount > 0 && <div className="text-xs text-muted-foreground">{dayJobCount} jobs</div>}
            </div>
          )
        })}
      </div>
    </div>
  )
}
