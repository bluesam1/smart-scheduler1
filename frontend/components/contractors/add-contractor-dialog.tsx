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
import { useSettings } from "@/lib/settings-context"
import { SkillCombobox } from "@/components/ui/skill-combobox"
import { useState, useEffect } from "react"
import { createApiClients } from "@/lib/api/api-client-config"
import { useAuth } from "@/lib/auth/auth-context"
import { formatErrorForDisplay, isAuthenticationError } from "@/lib/api/error-handling"
import { toast } from "sonner"
import { Spinner } from "@/components/ui/spinner"
import { GooglePlacesAutocomplete, type PlaceResult } from "@/components/ui/google-places-autocomplete"
import type { CreateContractorRequest, WorkingHoursDto, GeoLocationDto } from "@/lib/api/generated/api-client"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import { ChevronDown } from "lucide-react"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { estimateTimezoneFromCoordinates } from "@/lib/timezone-utils"

interface AddContractorDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onContractorAdded?: () => void
}

export function AddContractorDialog({ open, onOpenChange, onContractorAdded }: AddContractorDialogProps) {
  const { skills } = useSettings()
  const { getTokenProvider } = useAuth()
  const [selectedSkills, setSelectedSkills] = useState<string[]>([])
  const [name, setName] = useState("")
  const [address, setAddress] = useState("") // Street address
  const [formattedAddress, setFormattedAddress] = useState("") // Full formatted address
  const [city, setCity] = useState("")
  const [state, setState] = useState("")
  const [postalCode, setPostalCode] = useState("")
  const [latitude, setLatitude] = useState("")
  const [longitude, setLongitude] = useState("")
  const [timezone, setTimezone] = useState("America/New_York")
  const [rating, setRating] = useState<string>("50")
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isAddressDetailsOpen, setIsAddressDetailsOpen] = useState(false)

  // Debug logging for form state changes
  useEffect(() => {
    console.log("[AddContractorDialog] Form state updated:", {
      address,
      formattedAddress,
      city,
      state,
      postalCode,
      latitude,
      longitude
    })
  }, [address, formattedAddress, city, state, postalCode, latitude, longitude])

  const handleSubmit = async () => {
    if (!name.trim()) {
      toast.error("Name is required")
      return
    }

    if (!latitude || !longitude) {
      toast.error("Please select an address from the autocomplete suggestions to get location coordinates")
      return
    }

    setIsSubmitting(true)

    try {
      const tokenProvider = getTokenProvider()
      if (!tokenProvider) {
        toast.error("Please log in to create contractors")
        setIsSubmitting(false)
        return
      }
      const { client } = createApiClients(tokenProvider)

      // Create base location
      const baseLocation: GeoLocationDto = {
        latitude: parseFloat(latitude),
        longitude: parseFloat(longitude),
        address: address || undefined, // Street address
        city: city || undefined,
        state: state || undefined,
        postalCode: postalCode || undefined,
        country: "US",
        formattedAddress: formattedAddress || [address, city, state, postalCode].filter(Boolean).join(", ") || undefined,
      }

      // Create default working hours (Monday-Friday, 9 AM - 5 PM) using selected timezone
      const workingHours: WorkingHoursDto[] = [
        { dayOfWeek: 1, startTime: "09:00", endTime: "17:00", timeZone: timezone },
        { dayOfWeek: 2, startTime: "09:00", endTime: "17:00", timeZone: timezone },
        { dayOfWeek: 3, startTime: "09:00", endTime: "17:00", timeZone: timezone },
        { dayOfWeek: 4, startTime: "09:00", endTime: "17:00", timeZone: timezone },
        { dayOfWeek: 5, startTime: "09:00", endTime: "17:00", timeZone: timezone },
      ]

      const request: CreateContractorRequest = {
        name: name.trim(),
        baseLocation,
        workingHours,
        skills: selectedSkills.length > 0 ? selectedSkills : undefined,
        rating: rating ? parseInt(rating, 10) : undefined,
      }

      await client.createContractor(request)
      
      toast.success("Contractor created successfully")
      
      // Reset form
      setName("")
      setAddress("")
      setFormattedAddress("")
      setCity("")
      setState("")
      setPostalCode("")
      setLatitude("")
      setLongitude("")
      setTimezone("America/New_York")
      setRating("50")
      setSelectedSkills([])
      
      onOpenChange(false)
      onContractorAdded?.()
    } catch (err) {
      const errorMessage = formatErrorForDisplay(err)
      
      if (isAuthenticationError(err)) {
        toast.error("Please log in to create contractors")
      } else {
        toast.error(`Failed to create contractor: ${errorMessage}`)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Add New Contractor</DialogTitle>
          <DialogDescription>Create a new contractor profile with skills and availability</DialogDescription>
        </DialogHeader>
        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="name">Full Name *</Label>
            <Input 
              id="name" 
              placeholder="John Doe" 
              value={name}
              onChange={(e) => setName(e.target.value)}
              disabled={isSubmitting}
            />
          </div>
          
          <GooglePlacesAutocomplete
            label="Address"
            value={formattedAddress}
            onChange={setFormattedAddress}
            onPlaceSelect={(place: PlaceResult) => {
              console.log("[AddContractorDialog] onPlaceSelect called with:", place)
              console.log("[AddContractorDialog] Setting form fields:", {
                address: place.address,
                formattedAddress: place.formattedAddress,
                city: place.city,
                state: place.state,
                postalCode: place.postalCode,
                latitude: place.latitude,
                longitude: place.longitude
              })
              setAddress(place.address) // Street address (e.g., "123 Main St")
              setFormattedAddress(place.formattedAddress) // Full formatted address
              setCity(place.city)
              setState(place.state)
              setPostalCode(place.postalCode)
              setLatitude(place.latitude.toString())
              setLongitude(place.longitude.toString())
              // Auto-populate timezone from coordinates
              const estimatedTimezone = estimateTimezoneFromCoordinates(place.latitude, place.longitude)
              setTimezone(estimatedTimezone)
              console.log("[AddContractorDialog] Form fields set, timezone:", estimatedTimezone)
            }}
            placeholder="Start typing address..."
            disabled={isSubmitting}
            id="address"
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
                    onChange={(e) => setState(e.target.value)}
                    disabled={isSubmitting}
                  />
                </div>
              </div>
              
              <div className="grid grid-cols-2 gap-4">
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
              </div>
              
              <div className="grid grid-cols-2 gap-4">
                <div className="grid gap-2">
                  <Label>Latitude</Label>
                  <div className="px-3 py-2 text-sm border rounded-md bg-muted/50 text-muted-foreground">
                    {latitude ? parseFloat(latitude).toFixed(6) : "Not set"}
                  </div>
                </div>
                <div className="grid gap-2">
                  <Label>Longitude</Label>
                  <div className="px-3 py-2 text-sm border rounded-md bg-muted/50 text-muted-foreground">
                    {longitude ? parseFloat(longitude).toFixed(6) : "Not set"}
                  </div>
                </div>
              </div>
            </CollapsibleContent>
          </Collapsible>
          
          <div className="grid gap-2">
            <Label htmlFor="timezone">Timezone</Label>
            <Select value={timezone} onValueChange={setTimezone} disabled={isSubmitting}>
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
          
          <div className="grid gap-2">
            <Label htmlFor="rating">Initial Rating</Label>
            <Input 
              id="rating" 
              type="number" 
              placeholder="50" 
              min="0" 
              max="100"
              value={rating}
              onChange={(e) => setRating(e.target.value)}
              disabled={isSubmitting}
            />
          </div>
          
          <div className="grid gap-2">
            <Label>Skills & Certifications</Label>
            <SkillCombobox
              availableSkills={skills}
              selectedSkills={selectedSkills}
              onSkillsChange={setSelectedSkills}
              placeholder="Select or type skills..."
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={isSubmitting}>
            {isSubmitting ? (
              <>
                <Spinner className="mr-2 h-4 w-4" />
                Creating...
              </>
            ) : (
              "Add Contractor"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
