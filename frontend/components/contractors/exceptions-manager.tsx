"use client"

import { useState, useMemo, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import { Card, CardContent } from "@/components/ui/card"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Calendar, Plus, Trash2, XCircle } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { TimeSelect } from "@/components/ui/time-select"
import { createApiClients } from "@/lib/api/api-client-config"
import { useAuth } from "@/lib/auth/auth-context"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import type { CalendarExceptionDto, WorkingHoursDto } from "@/lib/api/generated/api-client"

interface Exception {
  id: string
  title: string
  date: Date
  allDay: boolean
  startTime?: string
  endTime?: string
  type: "holiday" | "time-off" | "unavailable"
}

interface ExceptionsManagerProps {
  contractorId: string
}

export function ExceptionsManager({ contractorId }: ExceptionsManagerProps) {
  const { getTokenProvider } = useAuth()
  const [exceptions, setExceptions] = useState<Exception[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [exceptionFilter, setExceptionFilter] = useState<"active" | "past">("active")
  const [newException, setNewException] = useState<Partial<Exception>>({
    title: "",
    date: new Date(),
    allDay: true,
    type: "time-off",
  })

  // Load existing exceptions from contractor
  useEffect(() => {
    const loadExceptions = async () => {
      setIsLoading(true)
      try {
        const tokenProvider = getTokenProvider()
        const { client } = createApiClients(tokenProvider)
        const contractor = await client.getContractorById(contractorId)
        
        if (contractor?.calendar?.exceptions) {
          // Map API exceptions to frontend format
          const mappedExceptions: Exception[] = contractor.calendar.exceptions.map((exc, index) => {
            const date = new Date(exc.date || "")
            const isHoliday = exc.type === "Holiday"
            const hasWorkingHours = exc.workingHours != null
            
            return {
              id: `${contractorId}-${exc.date || index}`,
              title: isHoliday ? "Holiday" : "Custom Override",
              date,
              allDay: !hasWorkingHours,
              startTime: exc.workingHours?.startTime,
              endTime: exc.workingHours?.endTime,
              type: isHoliday ? "holiday" : "time-off",
            }
          })
          
          setExceptions(mappedExceptions)
        }
      } catch (err) {
        const errorMessage = formatErrorForDisplay(err)
        if (!isAuthenticationError(err)) {
          toast.error(`Failed to load exceptions: ${errorMessage}`)
        }
      } finally {
        setIsLoading(false)
      }
    }

    if (contractorId) {
      loadExceptions()
    }
  }, [contractorId, getTokenProvider])

  const filteredExceptions = useMemo(() => {
    const today = new Date()
    today.setHours(0, 0, 0, 0)

    return exceptions
      .filter((exception) => {
        const exceptionDate = new Date(exception.date)
        exceptionDate.setHours(0, 0, 0, 0)

        if (exceptionFilter === "active") {
          return exceptionDate >= today
        } else {
          return exceptionDate < today
        }
      })
      .sort((a, b) => {
        return exceptionFilter === "past" ? b.date.getTime() - a.date.getTime() : a.date.getTime() - b.date.getTime()
      })
  }, [exceptions, exceptionFilter])

  const addException = async () => {
    if (!newException.title || !newException.date) {
      toast.error("Title and date are required")
      return
    }

    try {
      const tokenProvider = getTokenProvider()
      const { client } = createApiClients(tokenProvider)
      
      // Convert to API format
      const dateOnly = new Date(newException.date!)
      const dateStr = dateOnly.toISOString().split("T")[0]
      const dateOnlyObj = dateStr // Will be parsed as DateOnly on backend
      
      const exceptionDto: CalendarExceptionDto = {
        date: dateStr,
        type: newException.type === "holiday" ? "Holiday" : "Override",
        workingHours: newException.allDay ? undefined : (newException.startTime && newException.endTime ? {
          dayOfWeek: dateOnly.getDay(),
          startTime: newException.startTime,
          endTime: newException.endTime,
          timeZone: "America/New_York", // TODO: Get from contractor
        } : undefined),
      }

      await client.addCalendarException(contractorId, exceptionDto)
      
      // Reload exceptions
      const contractor = await client.getContractorById(contractorId)
      if (contractor?.calendar?.exceptions) {
        const mappedExceptions: Exception[] = contractor.calendar.exceptions.map((exc, index) => {
          const date = new Date(exc.date || "")
          const isHoliday = exc.type === "Holiday"
          const hasWorkingHours = exc.workingHours != null
          
          return {
            id: `${contractorId}-${exc.date || index}`,
            title: isHoliday ? "Holiday" : "Custom Override",
            date,
            allDay: !hasWorkingHours,
            startTime: exc.workingHours?.startTime,
            endTime: exc.workingHours?.endTime,
            type: isHoliday ? "holiday" : "time-off",
          }
        })
        setExceptions(mappedExceptions)
      }
      
      setNewException({ title: "", date: new Date(), allDay: true, type: "time-off" })
      setDialogOpen(false)
      toast.success("Exception added successfully")
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      
      if (isAuthenticationError(err)) {
        toast.error("Please log in to add exceptions")
      } else {
        toast.error(`Failed to add exception: ${errorMessage}`)
      }
    }
  }

  const removeException = async (exception: Exception) => {
    try {
      const tokenProvider = getTokenProvider()
      const { client } = createApiClients(tokenProvider)
      
      // Format date as YYYY-MM-DD
      const dateStr = exception.date.toISOString().split("T")[0]
      
      await client.removeCalendarException(contractorId, dateStr)
      
      // Remove from local state
      setExceptions(exceptions.filter((exc) => exc.id !== exception.id))
      toast.success("Exception removed successfully")
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      
      if (isAuthenticationError(err)) {
        toast.error("Please log in to remove exceptions")
      } else {
        toast.error(`Failed to remove exception: ${errorMessage}`)
      }
    }
  }

  const getTypeColor = (type: string) => {
    switch (type) {
      case "holiday":
        return "bg-blue-500/10 text-blue-500 border-blue-500/30"
      case "time-off":
        return "bg-amber-500/10 text-amber-500 border-amber-500/30"
      default:
        return "bg-destructive/10 text-destructive border-destructive/30"
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
    <div className="space-y-4 flex flex-col h-full">
      <div className="flex items-center justify-between gap-4 flex-shrink-0">
        <div className="flex-1">
          <h4 className="text-sm font-medium mb-3">Schedule Exceptions</h4>
          <Tabs value={exceptionFilter} onValueChange={(val) => setExceptionFilter(val as "active" | "past")}>
            <TabsList className="grid w-full max-w-[300px] grid-cols-2">
              <TabsTrigger value="active">
                Active (
                {
                  exceptions.filter((e) => {
                    const d = new Date(e.date)
                    d.setHours(0, 0, 0, 0)
                    const t = new Date()
                    t.setHours(0, 0, 0, 0)
                    return d >= t
                  }).length
                }
                )
              </TabsTrigger>
              <TabsTrigger value="past">
                Past (
                {
                  exceptions.filter((e) => {
                    const d = new Date(e.date)
                    d.setHours(0, 0, 0, 0)
                    const t = new Date()
                    t.setHours(0, 0, 0, 0)
                    return d < t
                  }).length
                }
                )
              </TabsTrigger>
            </TabsList>
          </Tabs>
        </div>
        <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
          <DialogTrigger asChild>
            <Button size="sm" className="flex-shrink-0">
              <Plus className="h-4 w-4 mr-2" />
              Add Exception
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Add Schedule Exception</DialogTitle>
            </DialogHeader>
            <div className="space-y-4 py-4">
              <div className="space-y-2">
                <Label htmlFor="exception-title">Title</Label>
                <Input
                  id="exception-title"
                  placeholder="e.g., Vacation, Holiday, Personal Day"
                  value={newException.title}
                  onChange={(e) => setNewException({ ...newException, title: e.target.value })}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="exception-date">Date</Label>
                <Input
                  id="exception-date"
                  type="date"
                  value={newException.date?.toISOString().split("T")[0]}
                  onChange={(e) => setNewException({ ...newException, date: new Date(e.target.value) })}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="exception-type">Type</Label>
                <select
                  id="exception-type"
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  value={newException.type}
                  onChange={(e) => setNewException({ ...newException, type: e.target.value as Exception["type"] })}
                >
                  <option value="time-off">Time Off</option>
                  <option value="holiday">Holiday</option>
                  <option value="unavailable">Unavailable</option>
                </select>
              </div>

              <div className="flex items-center gap-2">
                <Switch
                  id="all-day"
                  checked={newException.allDay}
                  onCheckedChange={(checked) => setNewException({ ...newException, allDay: checked })}
                />
                <Label htmlFor="all-day">All Day</Label>
              </div>

              {!newException.allDay && (
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="start-time">Start Time</Label>
                    <TimeSelect
                      id="start-time"
                      value={newException.startTime}
                      onChange={(value) => setNewException({ ...newException, startTime: value })}
                      placeholder="Start time"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="end-time">End Time</Label>
                    <TimeSelect
                      id="end-time"
                      value={newException.endTime}
                      onChange={(value) => setNewException({ ...newException, endTime: value })}
                      placeholder="End time"
                    />
                  </div>
                </div>
              )}

              <Button onClick={addException} className="w-full">
                Add Exception
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <div className="space-y-2 overflow-auto pr-2 flex-1">
        {filteredExceptions.length === 0 ? (
          <Card>
            <CardContent className="p-4 text-center text-muted-foreground text-sm">
              {exceptionFilter === "active"
                ? "No active exceptions. Add holidays, time off, or other unavailable periods."
                : "No past exceptions."}
            </CardContent>
          </Card>
        ) : (
          filteredExceptions.map((exception) => (
            <Card key={exception.id}>
              <CardContent className="p-3">
                <div className="flex items-center justify-between gap-3">
                  <div className="flex items-center gap-3 flex-1 min-w-0">
                    <XCircle className="h-4 w-4 text-destructive flex-shrink-0" />
                    <div className="flex-1 min-w-0">
                      <h4 className="font-medium text-sm truncate">{exception.title}</h4>
                      <div className="flex items-center gap-2 mt-0.5 text-xs text-muted-foreground">
                        <Calendar className="h-3 w-3 flex-shrink-0" />
                        <span className="truncate">
                          {exception.date.toLocaleDateString("en-US", {
                            month: "short",
                            day: "numeric",
                            year: "numeric",
                          })}
                        </span>
                      </div>
                    </div>
                    <div className="flex items-center gap-1.5 flex-shrink-0">
                      <Badge variant="outline" className={`${getTypeColor(exception.type)} text-xs px-2 py-0`}>
                        {exception.type}
                      </Badge>
                      <Badge variant="outline" className="text-xs px-2 py-0">
                        {exception.allDay ? "All Day" : `${exception.startTime}-${exception.endTime}`}
                      </Badge>
                    </div>
                  </div>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => removeException(exception)}
                    className="flex-shrink-0 h-8 w-8"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))
        )}
      </div>
    </div>
  )
}
