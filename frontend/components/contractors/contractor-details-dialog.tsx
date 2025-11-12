"use client"

import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { MapPin, Settings, CalendarDays, User, Star } from "lucide-react"
import { useState, useEffect } from "react"
import { CalendarView } from "./calendar-view"
import { WorkingScheduleSettings } from "./working-schedule-settings"
import { ExceptionsManager } from "./exceptions-manager"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useSettings } from "@/lib/settings-context"
import { SkillCombobox } from "@/components/ui/skill-combobox"

interface ContractorDetailsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  contractor: {
    id: string
    name: string
    skills: string[]
    rating: number
    availability: string
    baseLocation: string
    timezone?: string
  }
}

export function ContractorDetailsDialog({ open, onOpenChange, contractor }: ContractorDetailsDialogProps) {
  const { skills: availableSkills } = useSettings()
  const [activeTab, setActiveTab] = useState("details")
  const [editedAddress, setEditedAddress] = useState(contractor.baseLocation)
  const [editedTimezone, setEditedTimezone] = useState(contractor.timezone || "America/New_York")
  const [editedSkills, setEditedSkills] = useState<string[]>(contractor.skills)
  const [newSkill, setNewSkill] = useState("")

  useEffect(() => {
    if (open) {
      setEditedAddress(contractor.baseLocation)
      setEditedTimezone(contractor.timezone || "America/New_York")
      setEditedSkills(contractor.skills)
      setActiveTab("details")
    }
  }, [open, contractor])

  const handleAddSkill = () => {
    if (newSkill.trim() && !editedSkills.includes(newSkill.trim())) {
      setEditedSkills([...editedSkills, newSkill.trim()])
      setNewSkill("")
    }
  }

  const handleRemoveSkill = (skill: string) => {
    setEditedSkills(editedSkills.filter((s) => s !== skill))
  }

  const handleSaveDetails = () => {
    console.log("[v0] Saving contractor details:", {
      address: editedAddress,
      timezone: editedTimezone,
      skills: editedSkills,
    })
    // TODO: In production, make API call to save changes
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="!max-w-none !w-[96vw] !h-[92vh] flex flex-col p-0">
        <DialogHeader className="px-6 pt-6 pb-4 border-b">
          <div className="flex items-start justify-between">
            <div>
              <DialogTitle className="text-balance">{contractor.name}</DialogTitle>
              <div className="flex items-center gap-2 text-sm text-muted-foreground pt-1">
                <MapPin className="h-3 w-3" />
                <span>{contractor.baseLocation}</span>
                <span>•</span>
                <Badge variant="outline" className="text-xs">
                  {contractor.timezone || "America/New_York"}
                </Badge>
              </div>
            </div>
          </div>
        </DialogHeader>

        <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col px-6 min-h-0">
          <TabsList className="grid w-full grid-cols-4 mt-4">
            <TabsTrigger value="details">
              <User className="h-4 w-4 mr-2" />
              Details
            </TabsTrigger>
            <TabsTrigger value="calendar">
              <CalendarDays className="h-4 w-4 mr-2" />
              Calendar
            </TabsTrigger>
            <TabsTrigger value="schedule">
              <Settings className="h-4 w-4 mr-2" />
              Working Hours
            </TabsTrigger>
            <TabsTrigger value="exceptions">
              <CalendarDays className="h-4 w-4 mr-2" />
              Exceptions
            </TabsTrigger>
          </TabsList>

          <TabsContent value="details" className="flex-1 mt-4 pb-6 min-h-0">
            <div className="flex flex-col h-full">
              <div className="flex-1 overflow-auto space-y-6 pr-2">
                <div className="space-y-3">
                  <h3 className="text-lg font-semibold">Rating Details</h3>
                  <div className="grid gap-4 p-4 border rounded-lg bg-card">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium">Overall Rating</span>
                      <div className="flex items-center gap-2">
                        <Star className="h-5 w-5 fill-accent text-accent" />
                        <span className="text-2xl font-bold">{contractor.rating}/100</span>
                      </div>
                    </div>
                    <div className="space-y-2">
                      <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Quality Score</span>
                        <span className="font-medium">95/100</span>
                      </div>
                      <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Timeliness Score</span>
                        <span className="font-medium">90/100</span>
                      </div>
                      <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Customer Satisfaction</span>
                        <span className="font-medium">92/100</span>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="space-y-3">
                  <h3 className="text-lg font-semibold">Location & Timezone</h3>
                  <div className="space-y-4">
                    <div className="space-y-2">
                      <Label htmlFor="address">Base Address</Label>
                      <Input
                        id="address"
                        value={editedAddress}
                        onChange={(e) => setEditedAddress(e.target.value)}
                        placeholder="Enter contractor address"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="timezone">Timezone</Label>
                      <Select value={editedTimezone} onValueChange={setEditedTimezone}>
                        <SelectTrigger id="timezone">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="America/New_York">Eastern Time (ET)</SelectItem>
                          <SelectItem value="America/Chicago">Central Time (CT)</SelectItem>
                          <SelectItem value="America/Denver">Mountain Time (MT)</SelectItem>
                          <SelectItem value="America/Phoenix">Mountain Time - Arizona (MT)</SelectItem>
                          <SelectItem value="America/Los_Angeles">Pacific Time (PT)</SelectItem>
                          <SelectItem value="America/Anchorage">Alaska Time (AKT)</SelectItem>
                          <SelectItem value="Pacific/Honolulu">Hawaii Time (HT)</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                </div>

                <div className="space-y-3">
                  <h3 className="text-lg font-semibold">Skills & Certifications</h3>
                  <SkillCombobox
                    availableSkills={availableSkills}
                    selectedSkills={editedSkills}
                    onSkillsChange={setEditedSkills}
                    placeholder="Select or type skills..."
                  />
                </div>
              </div>

              <div className="pt-4 border-t mt-4">
                <Button onClick={handleSaveDetails} className="w-full">
                  Save Changes
                </Button>
              </div>
            </div>
          </TabsContent>

          <TabsContent value="calendar" className="flex-1 mt-4 pb-6 min-h-0">
            <CalendarView contractorId={contractor.id} contractorName={contractor.name} />
          </TabsContent>

          <TabsContent value="schedule" className="flex-1 mt-4 pb-6 min-h-0">
            <WorkingScheduleSettings contractorId={contractor.id} />
          </TabsContent>

          <TabsContent value="exceptions" className="flex-1 mt-4 pb-6 min-h-0">
            <ExceptionsManager contractorId={contractor.id} />
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  )
}
