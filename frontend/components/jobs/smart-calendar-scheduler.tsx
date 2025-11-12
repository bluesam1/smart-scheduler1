"use client"

import type React from "react"

import { useState, useRef } from "react"
import { Button } from "@/components/ui/button"
import { ChevronLeft, ChevronRight, CalendarIcon, X, Clock } from "lucide-react"

interface ScheduleSession {
  id: string
  date: string
  startTime: string
  endTime: string
  durationHours: number
}

interface SmartCalendarSchedulerProps {
  contractorId: string
  contractorName: string
  jobDuration: number
  selectedDate: string
  onSchedule: (sessions: ScheduleSession[]) => void
  onCancel: () => void
}

export function SmartCalendarScheduler({
  contractorId,
  contractorName,
  jobDuration,
  selectedDate,
  onSchedule,
  onCancel,
}: SmartCalendarSchedulerProps) {
  const [currentDate, setCurrentDate] = useState(selectedDate)
  const [scheduledSessions, setScheduledSessions] = useState<ScheduleSession[]>([])
  const [draggingSession, setDraggingSession] = useState<string | null>(null)
  const [resizingSession, setResizingSession] = useState<{ id: string; handle: "start" | "end" } | null>(null)
  const [dragStartY, setDragStartY] = useState<number>(0)
  const [dragStartTime, setDragStartTime] = useState<number>(0)
  const [totalScheduledHours, setTotalScheduledHours] = useState<number>(0)
  const [remainingHours, setRemainingHours] = useState<number>(jobDuration)

  const workingHours = { start: 8, end: 17 }

  const existingJobs = [
    { id: "J-001", title: "HVAC Repair", start: "09:00", end: "11:00" },
    { id: "J-045", title: "Electrical Inspection", start: "14:00", end: "16:00" },
  ]

  const timeGridRef = useRef<HTMLDivElement>(null)

  const navigateDate = (direction: "prev" | "next") => {
    const date = new Date(currentDate)
    date.setDate(date.getDate() + (direction === "next" ? 1 : -1))
    setCurrentDate(date.toISOString().split("T")[0])
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

  const formatTime = (hour: number) => {
    const period = hour >= 12 ? "PM" : "AM"
    const displayHour = hour > 12 ? hour - 12 : hour === 0 ? 12 : hour
    return `${displayHour} ${period}`
  }

  const timeToMinutes = (time: string): number => {
    const [hours, minutes] = time.split(":").map(Number)
    return hours * 60 + minutes
  }

  const minutesToTime = (minutes: number): string => {
    const hours = Math.floor(minutes / 60)
    const mins = minutes % 60
    return `${hours.toString().padStart(2, "0")}:${mins.toString().padStart(2, "0")}`
  }

  const generateTimeSlots = () => {
    const slots = []
    for (let hour = workingHours.start; hour <= workingHours.end; hour++) {
      slots.push(hour)
    }
    return slots
  }

  const timeSlots = generateTimeSlots()
  const HOUR_HEIGHT = 60

  const getBufferZones = () => {
    const buffers = []
    const BUFFER_MINUTES = 15
    const workStart = workingHours.start * 60
    const workEnd = workingHours.end * 60

    for (const job of existingJobs) {
      const jobStart = timeToMinutes(job.start)
      const jobEnd = timeToMinutes(job.end)

      if (jobStart - BUFFER_MINUTES >= workStart) {
        buffers.push({
          start: minutesToTime(jobStart - BUFFER_MINUTES),
          end: job.start,
        })
      }

      if (jobEnd + BUFFER_MINUTES <= workEnd) {
        buffers.push({
          start: job.end,
          end: minutesToTime(jobEnd + BUFFER_MINUTES),
        })
      }
    }

    for (const session of scheduledSessions.filter((s) => s.date === currentDate)) {
      const sessionStart = timeToMinutes(session.startTime)
      const sessionEnd = timeToMinutes(session.endTime)

      if (sessionStart - BUFFER_MINUTES >= workStart) {
        buffers.push({
          start: minutesToTime(sessionStart - BUFFER_MINUTES),
          end: session.startTime,
        })
      }

      if (sessionEnd + BUFFER_MINUTES <= workEnd) {
        buffers.push({
          start: session.endTime,
          end: minutesToTime(sessionEnd + BUFFER_MINUTES),
        })
      }
    }

    return buffers
  }

  const bufferZones = getBufferZones()

  const getAvailableBlocks = () => {
    const workStart = workingHours.start * 60
    const workEnd = workingHours.end * 60

    const occupiedRanges = [
      ...existingJobs.map((job) => ({
        start: timeToMinutes(job.start),
        end: timeToMinutes(job.end),
      })),
      ...scheduledSessions
        .filter((s) => s.date === currentDate)
        .map((session) => ({
          start: timeToMinutes(session.startTime),
          end: timeToMinutes(session.endTime),
        })),
      ...bufferZones.map((buffer) => ({
        start: timeToMinutes(buffer.start),
        end: timeToMinutes(buffer.end),
      })),
    ]

    const availableBlocks = []
    let currentStart = workStart

    occupiedRanges.sort((a, b) => a.start - b.start)

    for (const occupied of occupiedRanges) {
      if (currentStart < occupied.start) {
        availableBlocks.push({
          start: minutesToTime(currentStart),
          end: minutesToTime(occupied.start),
          duration: (occupied.start - currentStart) / 60,
        })
      }
      currentStart = Math.max(currentStart, occupied.end)
    }

    if (currentStart < workEnd) {
      availableBlocks.push({
        start: minutesToTime(currentStart),
        end: minutesToTime(workEnd),
        duration: (workEnd - currentStart) / 60,
      })
    }

    return availableBlocks
  }

  const availableBlocks = getAvailableBlocks()

  const getBlockStyle = (startTime: string, endTime: string) => {
    const startMinutes = timeToMinutes(startTime)
    const endMinutes = timeToMinutes(endTime)
    const workStartMinutes = workingHours.start * 60

    const top = ((startMinutes - workStartMinutes) / 60) * HOUR_HEIGHT
    const height = ((endMinutes - startMinutes) / 60) * HOUR_HEIGHT

    return { top: `${top}px`, height: `${height}px` }
  }

  const handleDateInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.value) {
      setCurrentDate(e.target.value)
    }
  }

  const getOverlappingJobs = (startTime: string, endTime: string) => {
    const start = timeToMinutes(startTime)
    const end = timeToMinutes(endTime)

    return existingJobs.filter((job) => {
      const jobStart = timeToMinutes(job.start)
      const jobEnd = timeToMinutes(job.end)
      return start < jobEnd && end > jobStart
    })
  }

  const handleBlockClick = (blockStart: string, blockEnd: string) => {
    // Don't allow adding sessions if job is fully scheduled
    if (totalScheduledHours >= jobDuration) {
      return
    }

    const startMinutes = timeToMinutes(blockStart)
    const blockEndMinutes = timeToMinutes(blockEnd)
    const availableDuration = (blockEndMinutes - startMinutes) / 60
    const remainingJobHours = jobDuration - totalScheduledHours

    // Take the minimum of: available block time, remaining job hours
    const sessionDuration = Math.min(availableDuration, remainingJobHours)
    const endMinutes = startMinutes + sessionDuration * 60
    const endTime = minutesToTime(endMinutes)

    const session: ScheduleSession = {
      id: `session-${Date.now()}`,
      date: currentDate,
      startTime: blockStart,
      endTime: endTime,
      durationHours: sessionDuration,
    }
    setScheduledSessions([...scheduledSessions, session])
    setTotalScheduledHours(totalScheduledHours + sessionDuration)
    setRemainingHours(jobDuration - totalScheduledHours)
  }

  const getRecommendedTime = () => {
    const remainingJobHours = jobDuration - totalScheduledHours
    if (remainingJobHours <= 0) return null

    const sessions: Array<{ start: string; end: string }> = []
    let hoursNeeded = remainingJobHours

    for (const block of availableBlocks) {
      if (hoursNeeded <= 0) break

      const blockDuration = (timeToMinutes(block.end) - timeToMinutes(block.start)) / 60
      const sessionDuration = Math.min(blockDuration, hoursNeeded)

      sessions.push({
        start: block.start,
        end: minutesToTime(timeToMinutes(block.start) + sessionDuration * 60),
      })

      hoursNeeded -= sessionDuration
    }

    if (sessions.length === 0) return null

    // Format time like "8am" or "12:30pm"
    const formatTimeShort = (time: string) => {
      const [hours, minutes] = time.split(":").map(Number)
      const period = hours >= 12 ? "pm" : "am"
      const displayHour = hours > 12 ? hours - 12 : hours === 0 ? 12 : hours
      if (minutes === 0) {
        return `${displayHour}${period}`
      }
      return `${displayHour}:${minutes.toString().padStart(2, "0")}${period}`
    }

    // Show ranges like "8am-9:45am and 11:15am-12pm"
    const ranges = sessions.map((s) => `${formatTimeShort(s.start)}-${formatTimeShort(s.end)}`)
    return ranges.join(" and ")
  }

  const recommendedTime = getRecommendedTime()

  const handleRecommendedTimeClick = () => {
    const remainingJobHours = jobDuration - totalScheduledHours
    if (remainingJobHours <= 0) return

    const newSessions: ScheduleSession[] = []
    let hoursNeeded = remainingJobHours

    for (const block of availableBlocks) {
      if (hoursNeeded <= 0) break

      const blockDuration = (timeToMinutes(block.end) - timeToMinutes(block.start)) / 60
      const sessionDuration = Math.min(blockDuration, hoursNeeded)
      const endTime = minutesToTime(timeToMinutes(block.start) + sessionDuration * 60)

      newSessions.push({
        id: `session-${Date.now()}-${newSessions.length}`,
        date: currentDate,
        startTime: block.start,
        endTime: endTime,
        durationHours: sessionDuration,
      })

      hoursNeeded -= sessionDuration
    }

    setScheduledSessions([...scheduledSessions, ...newSessions])
    setTotalScheduledHours(totalScheduledHours + hoursNeeded)
    setRemainingHours(jobDuration - totalScheduledHours)
  }

  const handleRemoveSession = (sessionId: string) => {
    const sessionToRemove = scheduledSessions.find((s) => s.id === sessionId)
    if (sessionToRemove) {
      setScheduledSessions(scheduledSessions.filter((s) => s.id !== sessionId))
      setTotalScheduledHours(totalScheduledHours - sessionToRemove.durationHours)
      setRemainingHours(jobDuration - totalScheduledHours)
    }
  }

  const handleFinalSchedule = () => {
    onSchedule(scheduledSessions)
  }

  const handleMouseDown = (e: React.MouseEvent, sessionId: string, type: "drag" | "resize-start" | "resize-end") => {
    e.stopPropagation()
    const session = scheduledSessions.find((s) => s.id === sessionId)
    if (!session) return

    setDragStartY(e.clientY)

    if (type === "drag") {
      setDraggingSession(sessionId)
      setDragStartTime(timeToMinutes(session.startTime))
    } else if (type === "resize-start") {
      setResizingSession({ id: sessionId, handle: "start" })
      setDragStartTime(timeToMinutes(session.startTime))
    } else if (type === "resize-end") {
      setResizingSession({ id: sessionId, handle: "end" })
      setDragStartTime(timeToMinutes(session.endTime))
    }
  }

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!draggingSession && !resizingSession) return

    const deltaY = e.clientY - dragStartY
    const deltaMinutes = Math.round((deltaY / HOUR_HEIGHT) * 60)
    const snapMinutes = Math.round(deltaMinutes / 15) * 15

    if (draggingSession) {
      const session = scheduledSessions.find((s) => s.id === draggingSession)
      if (!session) return

      const originalDuration = timeToMinutes(session.endTime) - timeToMinutes(session.startTime)
      let newStartMinutes = dragStartTime + snapMinutes
      newStartMinutes = Math.max(
        workingHours.start * 60,
        Math.min(workingHours.end * 60 - originalDuration, newStartMinutes),
      )

      const newEndMinutes = newStartMinutes + originalDuration

      setScheduledSessions(
        scheduledSessions.map((s) =>
          s.id === draggingSession
            ? { ...s, startTime: minutesToTime(newStartMinutes), endTime: minutesToTime(newEndMinutes) }
            : s,
        ),
      )
    } else if (resizingSession) {
      const session = scheduledSessions.find((s) => s.id === resizingSession.id)
      if (!session) return

      if (resizingSession.handle === "start") {
        let newStartMinutes = dragStartTime + snapMinutes
        const endMinutes = timeToMinutes(session.endTime)
        newStartMinutes = Math.max(workingHours.start * 60, Math.min(endMinutes - 15, newStartMinutes))

        const newDuration = (endMinutes - newStartMinutes) / 60
        const totalOtherHours = scheduledSessions
          .filter((s) => s.id !== resizingSession.id)
          .reduce((sum, s) => sum + s.durationHours, 0)

        if (totalOtherHours + newDuration <= jobDuration) {
          setScheduledSessions(
            scheduledSessions.map((s) =>
              s.id === resizingSession.id
                ? { ...s, startTime: minutesToTime(newStartMinutes), durationHours: newDuration }
                : s,
            ),
          )
          setTotalScheduledHours(totalOtherHours + newDuration)
          setRemainingHours(jobDuration - totalOtherHours - newDuration)
        }
      } else if (resizingSession.handle === "end") {
        let newEndMinutes = dragStartTime + snapMinutes
        const startMinutes = timeToMinutes(session.startTime)
        newEndMinutes = Math.max(startMinutes + 15, Math.min(workingHours.end * 60, newEndMinutes))

        const newDuration = (newEndMinutes - startMinutes) / 60
        const totalOtherHours = scheduledSessions
          .filter((s) => s.id !== resizingSession.id)
          .reduce((sum, s) => sum + s.durationHours, 0)

        if (totalOtherHours + newDuration <= jobDuration) {
          setScheduledSessions(
            scheduledSessions.map((s) =>
              s.id === resizingSession.id
                ? { ...s, endTime: minutesToTime(newEndMinutes), durationHours: newDuration }
                : s,
            ),
          )
          setTotalScheduledHours(totalOtherHours + newDuration)
          setRemainingHours(jobDuration - totalOtherHours - newDuration)
        }
      }
    }
  }

  const handleMouseUp = () => {
    setDraggingSession(null)
    setResizingSession(null)
  }

  return (
    <div className="space-y-4" onMouseMove={handleMouseMove} onMouseUp={handleMouseUp}>
      <div className="flex items-center gap-2 p-2 bg-muted/30 rounded-lg border">
        <Button variant="ghost" size="icon" onClick={() => navigateDate("prev")} className="h-8 w-8">
          <ChevronLeft className="h-4 w-4" />
        </Button>

        <div className="flex items-center gap-2 flex-1">
          <CalendarIcon className="h-4 w-4 text-muted-foreground" />
          <input
            type="date"
            value={currentDate}
            onChange={handleDateInputChange}
            className="bg-transparent border-none text-sm font-medium focus:outline-none cursor-pointer"
          />
        </div>

        <div className="text-sm font-medium">{formatDisplayDate(currentDate)}</div>

        <Button variant="ghost" size="icon" onClick={() => navigateDate("next")} className="h-8 w-8">
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>

      <div className="border rounded-lg bg-card overflow-hidden">
        <div className="max-h-[400px] overflow-y-auto" ref={timeGridRef}>
          <div className="relative" style={{ height: `${timeSlots.length * HOUR_HEIGHT}px` }}>
            {timeSlots.map((hour, index) => (
              <div
                key={hour}
                className="absolute left-0 right-0 border-t border-border/50 flex"
                style={{ top: `${index * HOUR_HEIGHT}px`, height: `${HOUR_HEIGHT}px` }}
              >
                <div className="w-16 flex-shrink-0 p-2 text-xs text-muted-foreground font-medium">
                  {formatTime(hour)}
                </div>
                <div className="flex-1 relative">
                  <div
                    className="absolute top-0 left-0 right-0 h-2 cursor-ns-resize hover:bg-primary/20 flex items-center justify-center"
                    style={{ top: "50%" }}
                  />
                </div>
              </div>
            ))}

            {bufferZones.map((buffer, index) => {
              const style = getBlockStyle(buffer.start, buffer.end)
              return (
                <div
                  key={`buffer-${index}`}
                  className="absolute left-16 right-2 bg-muted/40 border border-border/20 rounded-sm pointer-events-none"
                  style={style}
                />
              )
            })}

            {availableBlocks.map((block, index) => {
              const style = getBlockStyle(block.start, block.end)
              const canSchedule = totalScheduledHours < jobDuration
              return (
                <div
                  key={`available-${index}`}
                  className={`absolute left-16 right-2 border-2 rounded-md flex items-center justify-center text-xs font-medium transition-colors ${
                    canSchedule
                      ? "bg-emerald-500/20 border-emerald-500/40 text-emerald-700 dark:text-emerald-300 cursor-pointer hover:bg-emerald-500/30"
                      : "bg-muted/20 border-muted cursor-not-allowed text-muted-foreground"
                  }`}
                  style={style}
                  onClick={() => canSchedule && handleBlockClick(block.start, block.end)}
                >
                  {block.start} - {block.end}
                </div>
              )
            })}

            {existingJobs.map((job) => {
              const style = getBlockStyle(job.start, job.end)
              return (
                <div
                  key={job.id}
                  className="absolute left-16 right-2 bg-muted border border-border rounded-md p-2 text-xs"
                  style={style}
                >
                  <div className="font-medium truncate">{job.title}</div>
                  <div className="text-muted-foreground">{job.id}</div>
                  <div className="text-muted-foreground">
                    {job.start} - {job.end}
                  </div>
                </div>
              )
            })}

            {scheduledSessions.map((session) => {
              if (session.date !== currentDate) return null
              const style = getBlockStyle(session.startTime, session.endTime)
              return (
                <div
                  key={session.id}
                  className="absolute left-16 right-2 bg-primary/30 border-2 border-primary rounded-md group cursor-move select-none"
                  style={style}
                  onMouseDown={(e) => handleMouseDown(e, session.id, "drag")}
                >
                  <div
                    className="absolute top-0 left-0 right-0 h-2 cursor-ns-resize hover:bg-primary/20 flex items-center justify-center"
                    onMouseDown={(e) => handleMouseDown(e, session.id, "resize-start")}
                  >
                    <div className="w-8 h-1 bg-primary rounded-full opacity-60" />
                  </div>

                  <div className="p-2 text-xs pointer-events-none">
                    <div className="flex items-center justify-between">
                      <div>
                        <div className="font-medium">Scheduled Session</div>
                        <div className="text-xs mt-0.5">
                          {session.startTime} - {session.endTime}
                        </div>
                        <div className="text-xs text-muted-foreground mt-0.5">
                          {session.durationHours.toFixed(1)} hours
                        </div>
                      </div>
                      <button
                        onClick={(e) => {
                          e.stopPropagation()
                          handleRemoveSession(session.id)
                        }}
                        className="opacity-0 group-hover:opacity-100 transition-opacity p-1 hover:bg-destructive/20 rounded pointer-events-auto"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </div>
                  </div>

                  <div
                    className="absolute bottom-0 left-0 right-0 h-2 cursor-ns-resize hover:bg-primary/20 flex items-center justify-center"
                    onMouseDown={(e) => handleMouseDown(e, session.id, "resize-end")}
                  >
                    <div className="w-8 h-1 bg-primary rounded-full opacity-60" />
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      </div>

      {/* Replace multiple suggested time chips with single recommended time */}
      {recommendedTime && scheduledSessions.length === 0 && totalScheduledHours < jobDuration && (
        <div className="space-y-2">
          <div className="text-xs text-muted-foreground font-medium">Recommended Time</div>
          <button
            onClick={handleRecommendedTimeClick}
            className="w-full px-3 py-2 text-sm rounded-md border border-primary/40 bg-primary/10 hover:bg-primary/20 transition-colors font-medium text-left"
          >
            {recommendedTime}
          </button>
        </div>
      )}
      {/* </CHANGE> */}

      <div className="flex gap-2 pt-2">
        <Button
          variant="outline"
          onClick={() => {
            onCancel()
          }}
          className="flex-1 bg-transparent"
        >
          Cancel
        </Button>
        {scheduledSessions.length > 0 ? (
          <Button onClick={handleFinalSchedule} className="flex-1">
            Assign Job ({scheduledSessions.length} session{scheduledSessions.length > 1 ? "s" : ""})
          </Button>
        ) : (
          <Button
            onClick={() => availableBlocks[0] && handleBlockClick(availableBlocks[0].start, availableBlocks[0].end)}
            className="flex-1"
            disabled={totalScheduledHours >= jobDuration || availableBlocks.length === 0}
          >
            Schedule with {contractorName}
          </Button>
        )}
      </div>

      {/* Move scheduled hours indicator to bottom above action buttons */}
      {scheduledSessions.length > 0 && (
        <div
          className={`p-3 rounded-lg border ${remainingHours > 0 ? "bg-amber-500/10 border-amber-500/30" : "bg-emerald-500/10 border-emerald-500/30"}`}
        >
          <div className="flex items-center justify-between text-sm">
            <div className="flex items-center gap-2">
              <Clock className="h-4 w-4" />
              <span className="font-medium">
                {totalScheduledHours.toFixed(1)} of {jobDuration} hours scheduled
              </span>
            </div>
            {remainingHours > 0 && (
              <span className="text-amber-700 dark:text-amber-300 font-medium">
                {remainingHours.toFixed(1)} hours remaining
              </span>
            )}
          </div>
        </div>
      )}
      {/* </CHANGE> */}
    </div>
  )
}
