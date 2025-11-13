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
import { createApiClients } from "@/lib/api/api-client-config"
import { useAuth } from "@/lib/auth/auth-context"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import { GooglePlacesAutocomplete, type PlaceResult } from "@/components/ui/google-places-autocomplete"
import type { ContractorDto, UpdateContractorRequest, GeoLocationDto } from "@/lib/api/generated/api-client"
import { estimateTimezoneFromCoordinates } from "@/lib/timezone-utils"

interface ContractorDetailsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  contractor: ContractorDto
  onContractorUpdated?: () => void
}

export function ContractorDetailsDialog({ open, onOpenChange, contractor, onContractorUpdated }: ContractorDetailsDialogProps) {
  const { skills: availableSkills } = useSettings()
  const { getTokenProvider } = useAuth()
  const [activeTab, setActiveTab] = useState("details")
  const [editedAddress, setEditedAddress] = useState(contractor.baseLocation?.formattedAddress || contractor.baseLocation?.address || "")
  const [editedTimezone, setEditedTimezone] = useState(contractor.timezone || "America/New_York")
  const [editedSkills, setEditedSkills] = useState<string[]>(contractor.skills || [])
  const [newSkill, setNewSkill] = useState("")
  const [isSaving, setIsSaving] = useState(false)
  const [selectedPlace, setSelectedPlace] = useState<PlaceResult | null>(null)

  useEffect(() => {
    if (open) {
      setEditedAddress(contractor.baseLocation?.formattedAddress || contractor.baseLocation?.address || "")
      setEditedTimezone(contractor.timezone || "America/New_York")
      setEditedSkills(contractor.skills || [])
      setSelectedPlace(null) // Reset selected place when dialog opens
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

  const handleSaveDetails = async () => {
    if (!contractor.id) {
      toast.error("Contractor ID is missing")
      return
    }

    setIsSaving(true)

    try {
      const tokenProvider = getTokenProvider()
      if (!tokenProvider) {
        toast.error("Please log in to update contractors")
        setIsSaving(false)
        return
      }
      const { client } = createApiClients(tokenProvider)

      // Prepare base location update - use new coordinates if place was selected, otherwise preserve existing
      let baseLocation: GeoLocationDto | undefined
      const originalAddress = contractor.baseLocation?.formattedAddress || contractor.baseLocation?.address || ""
      if (editedAddress !== originalAddress && editedAddress.trim()) {
        // If a place was selected, use its coordinates; otherwise preserve existing coordinates
        if (selectedPlace) {
          baseLocation = {
            latitude: selectedPlace.latitude,
            longitude: selectedPlace.longitude,
            address: selectedPlace.address || undefined,
            city: selectedPlace.city || undefined,
            state: selectedPlace.state || undefined,
            postalCode: selectedPlace.postalCode || undefined,
            country: selectedPlace.country || undefined,
            formattedAddress: selectedPlace.formattedAddress || undefined,
            placeId: selectedPlace.placeId || undefined,
          } as GeoLocationDto
        } else {
          // Preserve existing coordinates and update address fields only
          baseLocation = {
            ...(contractor.baseLocation || {}),
            address: editedAddress || undefined,
            formattedAddress: editedAddress || undefined,
          } as GeoLocationDto
        }
      }

      // Prepare update request - only include fields that have values
      const updateRequest: UpdateContractorRequest = {}

      // Check if skills changed (compare sorted arrays)
      const originalSkills = (contractor.skills || []).slice().sort()
      const newSkills = editedSkills.slice().sort()
      const skillsChanged = JSON.stringify(originalSkills) !== JSON.stringify(newSkills)
      
      // Include skills if they changed (even if empty array - user cleared all skills)
      if (skillsChanged) {
        updateRequest.skills = editedSkills.length > 0 ? editedSkills : []
      }

      // Only include baseLocation if it changed
      if (baseLocation) {
        updateRequest.baseLocation = baseLocation
      }

      // If nothing changed, show message and return
      if (Object.keys(updateRequest).length === 0) {
        toast.info("No changes to save")
        setIsSaving(false)
        return
      }

      // Note: Timezone changes should be handled via working hours update (separate tab)
      // We don't update timezone here as it's derived from working hours

      await client.updateContractor(contractor.id, updateRequest)

      toast.success("Contractor updated successfully")
      
      // Notify parent to refresh the list
      onContractorUpdated?.()
      
      onOpenChange(false)
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      
      if (isAuthenticationError(err)) {
        toast.error("Please log in to update contractors")
      } else {
        toast.error(`Failed to update contractor: ${errorMessage}`)
      }
    } finally {
      setIsSaving(false)
    }
  }

  // Handle dialog close - refresh parent if needed
  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      // Dialog is closing - refresh parent to show any updates
      onContractorUpdated?.()
    }
    onOpenChange(newOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="!max-w-none !w-[96vw] !h-[92vh] flex flex-col p-0">
        <DialogHeader className="px-6 pt-6 pb-4 border-b">
          <div className="flex items-start justify-between">
            <div>
              <DialogTitle className="text-balance">{contractor.name}</DialogTitle>
              <div className="flex items-center gap-2 text-sm text-muted-foreground pt-1">
                <MapPin className="h-3 w-3" />
                <span>{contractor.baseLocation?.formattedAddress || contractor.baseLocation?.address || "No location"}</span>
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
                    <GooglePlacesAutocomplete
                      label="Base Address"
                      value={editedAddress}
                      onChange={setEditedAddress}
                      onPlaceSelect={(place: PlaceResult) => {
                        setEditedAddress(place.formattedAddress)
                        setSelectedPlace(place)
                        // Auto-populate timezone from coordinates
                        const estimatedTimezone = estimateTimezoneFromCoordinates(place.latitude, place.longitude)
                        setEditedTimezone(estimatedTimezone)
                        // The save handler will use the place's coordinates and address components
                      }}
                      placeholder="Start typing address..."
                      disabled={isSaving}
                      id="address"
                    />
                    <div className="grid grid-cols-2 gap-4">
                      <div className="grid gap-2">
                        <Label>Latitude</Label>
                        <div className="px-3 py-2 text-sm border rounded-md bg-muted/50 text-muted-foreground">
                          {selectedPlace 
                            ? selectedPlace.latitude.toFixed(6) 
                            : contractor.baseLocation?.latitude 
                              ? contractor.baseLocation.latitude.toFixed(6)
                              : "Not set"}
                        </div>
                      </div>
                      <div className="grid gap-2">
                        <Label>Longitude</Label>
                        <div className="px-3 py-2 text-sm border rounded-md bg-muted/50 text-muted-foreground">
                          {selectedPlace 
                            ? selectedPlace.longitude.toFixed(6) 
                            : contractor.baseLocation?.longitude 
                              ? contractor.baseLocation.longitude.toFixed(6)
                              : "Not set"}
                        </div>
                      </div>
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
                <Button onClick={handleSaveDetails} className="w-full" disabled={isSaving}>
                  {isSaving ? (
                    <>
                      <Spinner className="h-4 w-4 mr-2" />
                      Saving...
                    </>
                  ) : (
                    "Save Changes"
                  )}
                </Button>
              </div>
            </div>
          </TabsContent>

          <TabsContent value="calendar" className="flex-1 mt-4 pb-6 min-h-0">
            <CalendarView contractorId={contractor.id || ""} contractorName={contractor.name || ""} />
          </TabsContent>

          <TabsContent value="schedule" className="flex-1 mt-4 pb-6 min-h-0">
            <WorkingScheduleSettings contractorId={contractor.id || ""} />
          </TabsContent>

          <TabsContent value="exceptions" className="flex-1 mt-4 pb-6 min-h-0">
            <ExceptionsManager contractorId={contractor.id || ""} />
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  )
}
