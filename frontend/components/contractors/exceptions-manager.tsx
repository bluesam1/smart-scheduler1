"use client"

import { useState, useMemo } from "react"
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
  const [exceptions, setExceptions] = useState<Exception[]>([
    {
      id: "1",
      title: "Thanksgiving",
      date: new Date(2025, 10, 28),
      allDay: true,
      type: "holiday",
    },
    {
      id: "2",
      title: "Doctor's Appointment",
      date: new Date(2025, 10, 20),
      allDay: false,
      startTime: "14:00",
      endTime: "16:00",
      type: "time-off",
    },
    {
      id: "3",
      title: "asdfasdf",
      date: new Date(2025, 10, 11),
      allDay: true,
      type: "time-off",
    },
  ])

  const [dialogOpen, setDialogOpen] = useState(false)
  const [exceptionFilter, setExceptionFilter] = useState<"active" | "past">("active")
  const [newException, setNewException] = useState<Partial<Exception>>({
    title: "",
    date: new Date(),
    allDay: true,
    type: "time-off",
  })

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

  const addException = () => {
    if (newException.title && newException.date) {
      setExceptions([
        ...exceptions,
        {
          ...newException,
          id: Date.now().toString(),
        } as Exception,
      ])
      setNewException({ title: "", date: new Date(), allDay: true, type: "time-off" })
      setDialogOpen(false)
    }
  }

  const removeException = (id: string) => {
    setExceptions(exceptions.filter((exc) => exc.id !== id))
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
                    onClick={() => removeException(exception.id)}
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
