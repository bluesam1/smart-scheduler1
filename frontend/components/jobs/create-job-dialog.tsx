"use client"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useSettings } from "@/lib/settings-context"
import { useState } from "react"
import { GooglePlacesAutocomplete, type PlaceResult } from "@/components/ui/google-places-autocomplete"
import { SkillCombobox } from "@/components/ui/skill-combobox"
import { TimeSelect } from "@/components/ui/time-select"
import { createApiClients } from "@/lib/api/api-client-config"
import { useAuth } from "@/lib/auth/auth-context"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import type { CreateJobRequest, GeoLocationDto, TimeWindowDto } from "@/lib/api/generated/api-client"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import { ChevronDown } from "lucide-react"

interface CreateJobDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onJobCreated?: () => void
}

export function CreateJobDialog({ open, onOpenChange, onJobCreated }: CreateJobDialogProps) {
  const { jobTypes, skills } = useSettings()
  const { getTokenProvider } = useAuth()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [selectedJobType, setSelectedJobType] = useState<string>("")
  const [description, setDescription] = useState("")
  const [address, setAddress] = useState("")
  const [formattedAddress, setFormattedAddress] = useState("")
  const [city, setCity] = useState("")
  const [state, setState] = useState("")
  const [postalCode, setPostalCode] = useState("")
  const [latitude, setLatitude] = useState("")
  const [longitude, setLongitude] = useState("")
  const [isAddressDetailsOpen, setIsAddressDetailsOpen] = useState(false)
  const [priority, setPriority] = useState<string>("Normal")
  const [desiredDate, setDesiredDate] = useState("")
  const [desiredTime, setDesiredTime] = useState("")
  const [duration, setDuration] = useState("")
  const [selectedSkills, setSelectedSkills] = useState<string[]>([])
  const [accessNotes, setAccessNotes] = useState("")

  const handleSubmit = async () => {
    console.log("[CreateJobDialog] handleSubmit called")
    console.log("[CreateJobDialog] Form state:", {
      selectedJobType,
      latitude,
      longitude,
      desiredDate,
      desiredTime,
      duration,
      selectedSkills: selectedSkills.length,
      isSubmitting
    })
    
    // Collect all validation errors
    const errors: string[] = []
    
    if (!selectedJobType) {
      errors.push("Please select a job type")
    }
    if (!latitude || !longitude) {
      errors.push("Please select an address from the autocomplete suggestions")
    }
    if (!desiredDate) {
      errors.push("Please select a desired date")
    }
    if (!desiredTime) {
      errors.push("Please select a desired time")
    }
    if (!duration || parseInt(duration) <= 0) {
      errors.push("Please enter a valid duration (hours)")
    }
    // Skills are optional - removed validation requirement
    
    // Show all errors at once
    if (errors.length > 0) {
      console.warn("[CreateJobDialog] Validation failed:", errors)
      toast.error(errors.join(". "))
      return
    }

    setIsSubmitting(true)

    try {
      const tokenProvider = getTokenProvider()
      if (!tokenProvider) {
        toast.error("Please log in to create jobs")
        setIsSubmitting(false)
        return
      }
      const { client } = createApiClients(tokenProvider)

      // Parse time (HH:mm format)
      const [hours, minutes] = desiredTime.split(":")
      const startDateTime = new Date(`${desiredDate}T${hours}:${minutes}:00`)
      
      // Validate date parsing
      if (isNaN(startDateTime.getTime())) {
        toast.error("Invalid date or time. Please check your selections.")
        setIsSubmitting(false)
        return
      }
      
      const durationMinutes = parseInt(duration) * 60
      const endDateTime = new Date(startDateTime.getTime() + durationMinutes * 60000)

      // Create location from address fields
      const lat = parseFloat(latitude)
      const lng = parseFloat(longitude)
      
      if (isNaN(lat) || isNaN(lng)) {
        toast.error("Invalid location coordinates. Please select an address from the autocomplete.")
        setIsSubmitting(false)
        return
      }
      
      const location: GeoLocationDto = {
        latitude: lat,
        longitude: lng,
        address: address || undefined, // Street address
        city: city || undefined,
        state: state || undefined,
        postalCode: postalCode || undefined,
        country: "US",
        formattedAddress: formattedAddress || [address, city, state, postalCode].filter(Boolean).join(", ") || undefined,
      }

      const serviceWindow: TimeWindowDto = {
        start: startDateTime.toISOString(),
        end: endDateTime.toISOString(),
      }

      const request: CreateJobRequest = {
        type: selectedJobType,
        description: description || undefined,
        duration: durationMinutes,
        location,
        serviceWindow,
        priority: priority.charAt(0).toUpperCase() + priority.slice(1).toLowerCase(), // Normalize to "Normal", "High", "Rush"
        requiredSkills: selectedSkills.length > 0 ? selectedSkills : undefined,
        accessNotes: accessNotes || undefined,
        desiredDate: startDateTime.toISOString(),
      }

      console.log("[CreateJobDialog] Submitting job request:", request)
      await client.createJob(request)
      
      toast.success("Job created successfully!")
      
      // Reset form
      setSelectedJobType("")
      setDescription("")
      setAddress("")
      setFormattedAddress("")
      setCity("")
      setState("")
      setPostalCode("")
      setLatitude("")
      setLongitude("")
      setIsAddressDetailsOpen(false)
      setPriority("Normal")
      setDesiredDate("")
      setDesiredTime("")
      setDuration("")
      setSelectedSkills([])
      setAccessNotes("")
      
      onOpenChange(false)
      onJobCreated?.()
    } catch (err) {
      console.error("[CreateJobDialog] Error creating job:", err)
      
      // Extract detailed error information from backend
      let errorMessage = "Failed to create job"
      
      // Check if it's an ApiException with response details
      if (err && typeof err === 'object' && 'message' in err) {
        const apiErr = err as any
        
        // Try to parse the response text if available
        if (apiErr.response) {
          try {
            const parsed = typeof apiErr.response === 'string' 
              ? JSON.parse(apiErr.response) 
              : apiErr.response
            if (parsed?.message) {
              errorMessage = parsed.message
            }
          } catch {
            // If parsing fails, use the response as-is if it's a string
            if (typeof apiErr.response === 'string') {
              errorMessage = apiErr.response
            }
          }
        } else if (apiErr.message) {
          errorMessage = apiErr.message
        }
      } else {
        errorMessage = formatErrorForDisplay(err)
      }
      
      // Show user-friendly error message
      if (isAuthenticationError(err)) {
        toast.error("Please log in to create jobs")
      } else {
        toast.error(errorMessage, {
          duration: 5000, // Show for 5 seconds so user can read it
        })
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[550px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create New Job</DialogTitle>
          <DialogDescription>Add a new flooring job to the queue for assignment</DialogDescription>
        </DialogHeader>
        <form
          onSubmit={async (e) => {
            e.preventDefault()
            e.stopPropagation()
            await handleSubmit()
          }}
        >
        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="description">Job Description</Label>
            <Textarea 
              id="description" 
              placeholder="Describe the job details..." 
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>

          <GooglePlacesAutocomplete
            label="Job Address"
            value={formattedAddress}
            onChange={setFormattedAddress}
            onPlaceSelect={(place: PlaceResult) => {
              setAddress(place.address) // Street address (e.g., "123 Main St")
              setFormattedAddress(place.formattedAddress) // Full formatted address
              setCity(place.city)
              setState(place.state)
              setPostalCode(place.postalCode)
              setLatitude(place.latitude.toString())
              setLongitude(place.longitude.toString())
            }}
            placeholder="Start typing address..."
            disabled={isSubmitting}
            id="job-address"
          />
          
          <Collapsible open={isAddressDetailsOpen} onOpenChange={setIsAddressDetailsOpen}>
            <CollapsibleTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                className="w-full justify-between p-0 h-auto font-normal text-sm text-muted-foreground hover:text-foreground"
              >
                <span className="truncate text-left">
                  {formattedAddress || [address, city, state, postalCode].filter(Boolean).join(", ") || "Address Details (Auto-filled from address)"}
                </span>
                <ChevronDown className={`h-4 w-4 transition-transform flex-shrink-0 ml-2 ${isAddressDetailsOpen ? 'rotate-180' : ''}`} />
              </Button>
            </CollapsibleTrigger>
            <CollapsibleContent className="space-y-4 pt-2">
              <div className="grid grid-cols-2 gap-4">
                <div className="grid gap-2">
                  <Label htmlFor="city">City</Label>
                  <Input 
                    id="city" 
                    placeholder="New York" 
                    value={city}
                    onChange={(e) => setCity(e.target.value)}
                    disabled={isSubmitting}
                  />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="state">State</Label>
                  <Input 
                    id="state" 
                    placeholder="NY" 
                    value={state}
                    onChange={(e) => setState(e.target.value.toUpperCase())}
                    disabled={isSubmitting}
                    maxLength={2}
                    className="uppercase"
                  />
                </div>
              </div>
              <div className="grid gap-2">
                <Label htmlFor="postalCode">Postal Code</Label>
                <Input 
                  id="postalCode" 
                  placeholder="10001" 
                  value={postalCode}
                  onChange={(e) => setPostalCode(e.target.value)}
                  disabled={isSubmitting}
                />
              </div>
            </CollapsibleContent>
          </Collapsible>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="job-type">Job Type</Label>
              <Select value={selectedJobType} onValueChange={setSelectedJobType}>
                <SelectTrigger id="job-type">
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  {jobTypes.map((type) => (
                    <SelectItem key={type} value={type}>
                      {type}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="priority">Priority</Label>
              <Select value={priority} onValueChange={setPriority}>
                <SelectTrigger id="priority">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Normal">Normal</SelectItem>
                  <SelectItem value="High">High</SelectItem>
                  <SelectItem value="Rush">Rush</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="desired-date">Desired Start Date</Label>
              <Input 
                id="desired-date" 
                type="date" 
                value={desiredDate}
                onChange={(e) => setDesiredDate(e.target.value)}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="desired-time">Desired Start Time</Label>
              <TimeSelect 
                id="desired-time" 
                placeholder="Select start time"
                value={desiredTime}
                onChange={setDesiredTime}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="duration">Estimated Duration (hrs)</Label>
              <Input 
                id="duration" 
                type="number" 
                placeholder="3" 
                min="1" 
                max="12"
                value={duration}
                onChange={(e) => setDuration(e.target.value)}
              />
            </div>
          </div>

          <div className="grid gap-2">
            <Label>Required Skills (Optional)</Label>
            <SkillCombobox
              availableSkills={skills}
              selectedSkills={selectedSkills}
              onSkillsChange={setSelectedSkills}
              placeholder="Select or type required skills (optional)..."
            />
          </div>

          <div className="grid gap-2">
            <Label htmlFor="notes">Notes (optional)</Label>
            <Textarea 
              id="notes" 
              placeholder="Any special requirements or access instructions..." 
              rows={2}
              value={accessNotes}
              onChange={(e) => setAccessNotes(e.target.value)}
            />
          </div>
        </div>
        <DialogFooter>
          <Button 
            type="button"
            variant="outline" 
            onClick={() => onOpenChange(false)}
            disabled={isSubmitting}
          >
            Cancel
          </Button>
          <Button 
            type="submit"
            disabled={isSubmitting}
          >
            {isSubmitting ? "Creating..." : "Create Job"}
          </Button>
        </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
