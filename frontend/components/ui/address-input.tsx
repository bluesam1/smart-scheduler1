"use client"

import { useState, useRef, useEffect } from "react"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { MapPin } from "lucide-react"

interface AddressInputProps {
  onAddressChange?: (address: Address) => void
  defaultValue?: Address
}

export interface Address {
  street: string
  city: string
  state: string
  zipCode: string
  fullAddress: string
}

export function AddressInput({ onAddressChange, defaultValue }: AddressInputProps) {
  const [street, setStreet] = useState(defaultValue?.street || "")
  const [city, setCity] = useState(defaultValue?.city || "")
  const [state, setState] = useState(defaultValue?.state || "")
  const [zipCode, setZipCode] = useState(defaultValue?.zipCode || "")
  const [suggestions, setSuggestions] = useState<Address[]>([])
  const [showSuggestions, setShowSuggestions] = useState(false)
  const [fullAddressInput, setFullAddressInput] = useState(defaultValue?.fullAddress || "")
  const wrapperRef = useRef<HTMLDivElement>(null)

  // US States
  const usStates = [
    "AL",
    "AK",
    "AZ",
    "AR",
    "CA",
    "CO",
    "CT",
    "DE",
    "FL",
    "GA",
    "HI",
    "ID",
    "IL",
    "IN",
    "IA",
    "KS",
    "KY",
    "LA",
    "ME",
    "MD",
    "MA",
    "MI",
    "MN",
    "MS",
    "MO",
    "MT",
    "NE",
    "NV",
    "NH",
    "NJ",
    "NM",
    "NY",
    "NC",
    "ND",
    "OH",
    "OK",
    "OR",
    "PA",
    "RI",
    "SC",
    "SD",
    "TN",
    "TX",
    "UT",
    "VT",
    "VA",
    "WA",
    "WV",
    "WI",
    "WY",
  ]

  // Close suggestions when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
        setShowSuggestions(false)
      }
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [])

  // Update parent when address changes
  useEffect(() => {
    if (street && city && state && zipCode) {
      const fullAddress = `${street}, ${city}, ${state} ${zipCode}`
      onAddressChange?.({ street, city, state, zipCode, fullAddress })
    }
  }, [street, city, state, zipCode, onAddressChange])

  // Simple mock autocomplete (in production, use Google Places API or similar)
  const handleFullAddressChange = (value: string) => {
    setFullAddressInput(value)

    if (value.length > 3) {
      // Mock suggestions - in production, call an actual geocoding API
      const mockSuggestions: Address[] = [
        {
          street: "123 Main St",
          city: "New York",
          state: "NY",
          zipCode: "10001",
          fullAddress: "123 Main St, New York, NY 10001",
        },
        {
          street: "456 Oak Ave",
          city: "Brooklyn",
          state: "NY",
          zipCode: "11201",
          fullAddress: "456 Oak Ave, Brooklyn, NY 11201",
        },
      ].filter((addr) => addr.fullAddress.toLowerCase().includes(value.toLowerCase()))

      setSuggestions(mockSuggestions)
      setShowSuggestions(mockSuggestions.length > 0)
    } else {
      setSuggestions([])
      setShowSuggestions(false)
    }
  }

  const selectSuggestion = (address: Address) => {
    setStreet(address.street)
    setCity(address.city)
    setState(address.state)
    setZipCode(address.zipCode)
    setFullAddressInput(address.fullAddress)
    setShowSuggestions(false)
  }

  return (
    <div className="grid gap-4">
      {/* Full Address Search with Autocomplete */}
      <div className="relative" ref={wrapperRef}>
        <Label htmlFor="full-address" className="flex items-center gap-2">
          <MapPin className="h-4 w-4" />
          Job Address
        </Label>
        <Input
          id="full-address"
          value={fullAddressInput}
          onChange={(e) => handleFullAddressChange(e.target.value)}
          placeholder="Start typing address..."
          className="mt-1.5"
          autoComplete="off"
        />

        {/* Autocomplete Suggestions */}
        {showSuggestions && suggestions.length > 0 && (
          <div className="absolute z-50 w-full mt-1 bg-popover border border-border rounded-md shadow-lg max-h-60 overflow-auto">
            {suggestions.map((suggestion, index) => (
              <button
                key={index}
                type="button"
                onClick={() => selectSuggestion(suggestion)}
                className="w-full px-3 py-2.5 text-left hover:bg-accent transition-colors flex items-start gap-2 border-b border-border last:border-b-0"
              >
                <MapPin className="h-4 w-4 mt-0.5 text-muted-foreground flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <div className="font-medium text-sm">{suggestion.street}</div>
                  <div className="text-xs text-muted-foreground">
                    {suggestion.city}, {suggestion.state} {suggestion.zipCode}
                  </div>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Manual Address Entry (collapsed by default, expands when fields are filled) */}
      <div className="grid grid-cols-6 gap-3">
        <div className="col-span-6">
          <Label htmlFor="street" className="text-xs text-muted-foreground">
            Street Address
          </Label>
          <Input
            id="street"
            value={street}
            onChange={(e) => setStreet(e.target.value)}
            placeholder="123 Main St"
            className="mt-1"
          />
        </div>
        <div className="col-span-3">
          <Label htmlFor="city" className="text-xs text-muted-foreground">
            City
          </Label>
          <Input
            id="city"
            value={city}
            onChange={(e) => setCity(e.target.value)}
            placeholder="New York"
            className="mt-1"
          />
        </div>
        <div className="col-span-1">
          <Label htmlFor="state" className="text-xs text-muted-foreground">
            State
          </Label>
          <Input
            id="state"
            value={state}
            onChange={(e) => setState(e.target.value.toUpperCase())}
            placeholder="NY"
            maxLength={2}
            className="mt-1 uppercase"
            list="states"
          />
          <datalist id="states">
            {usStates.map((s) => (
              <option key={s} value={s} />
            ))}
          </datalist>
        </div>
        <div className="col-span-2">
          <Label htmlFor="zipCode" className="text-xs text-muted-foreground">
            ZIP Code
          </Label>
          <Input
            id="zipCode"
            value={zipCode}
            onChange={(e) => setZipCode(e.target.value)}
            placeholder="10001"
            maxLength={5}
            className="mt-1"
          />
        </div>
      </div>
    </div>
  )
}
