"use client"

import { useEffect, useRef, useState } from "react"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { MapPin } from "lucide-react"

declare global {
  interface Window {
    google: typeof google
    initGooglePlaces: () => void
  }
  
  // PlaceAutocompleteElement is a web component
  namespace JSX {
    interface IntrinsicElements {
      'gmp-place-autocomplete': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & {
        'requested-result-types'?: string
        'location-bias'?: string
        'country-restrictions'?: string
      }
    }
  }
}

export interface PlaceResult {
  address: string
  city: string
  state: string
  postalCode: string
  country: string
  formattedAddress: string
  latitude: number
  longitude: number
  placeId: string
}

interface GooglePlacesAutocompleteProps {
  value: string
  onChange: (value: string) => void
  onPlaceSelect: (place: PlaceResult) => void
  placeholder?: string
  label?: string
  disabled?: boolean
  id?: string
}

export function GooglePlacesAutocomplete({
  value,
  onChange,
  onPlaceSelect,
  placeholder = "Start typing address...",
  label,
  disabled = false,
  id = "google-places-autocomplete",
}: GooglePlacesAutocompleteProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const autocompleteElementRef = useRef<HTMLElement | null>(null)
  const [isLoaded, setIsLoaded] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Load Google Places API (New) script
  useEffect(() => {
    const apiKey = process.env.NEXT_PUBLIC_GOOGLE_PLACES_API_KEY

    console.log("[GooglePlacesAutocomplete] API Key present:", !!apiKey)
    console.log("[GooglePlacesAutocomplete] API Key length:", apiKey?.length || 0)

    if (!apiKey) {
      const errorMsg = "NEXT_PUBLIC_GOOGLE_PLACES_API_KEY not found. Please add it to .env.local"
      console.warn(`[GooglePlacesAutocomplete] ${errorMsg}`)
      setError(errorMsg)
      return
    }

    // Check if already loaded (check for PlaceAutocompleteElement)
    if (customElements.get('gmp-place-autocomplete')) {
      console.log("[GooglePlacesAutocomplete] Places API (New) already loaded")
      setIsLoaded(true)
      setIsLoading(false)
      return
    }

    // Check if script is already being loaded
    const existingScript = document.querySelector(`script[src*="maps.googleapis.com"]`)
    if (existingScript) {
      console.log("[GooglePlacesAutocomplete] Script already exists, waiting for load...")
      // Wait for it to load
      const checkLoaded = setInterval(() => {
        if (customElements.get('gmp-place-autocomplete')) {
          console.log("[GooglePlacesAutocomplete] Places API (New) loaded successfully")
          setIsLoaded(true)
          setIsLoading(false)
          clearInterval(checkLoaded)
        }
      }, 100)
      
      // Timeout after 10 seconds
      setTimeout(() => {
        clearInterval(checkLoaded)
        if (!customElements.get('gmp-place-autocomplete')) {
          setError("Places API (New) failed to load. Check that Places API (New) is enabled in Google Cloud Console.")
        }
      }, 10000)
      
      return () => clearInterval(checkLoaded)
    }

    setIsLoading(true)
    setError(null)

    // Set up global callback for Google Maps
    window.initGooglePlaces = () => {
      console.log("[GooglePlacesAutocomplete] Google Maps callback executed")
      // Check for PlaceAutocompleteElement after callback
      setTimeout(() => {
        if (customElements.get('gmp-place-autocomplete')) {
          console.log("[GooglePlacesAutocomplete] Places API (New) available after callback")
          setIsLoaded(true)
          setIsLoading(false)
          setError(null)
        } else {
          console.error("[GooglePlacesAutocomplete] Places API (New) not available after callback")
          setError("Places API (New) not available. Make sure Places API (New) is enabled in Google Cloud Console.")
          setIsLoading(false)
        }
      }, 100)
    }

    // Load Google Places API (New) script
    const script = document.createElement("script")
    script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&loading=async&libraries=places&callback=initGooglePlaces`
    script.async = true
    script.defer = true
    
    script.onload = () => {
      console.log("[GooglePlacesAutocomplete] Script onload fired")
    }
    
    script.onerror = (err) => {
      console.error("[GooglePlacesAutocomplete] Script failed to load:", err)
      setError("Failed to load Google Maps API script. Check your API key and network connection.")
      setIsLoading(false)
    }
    
    document.head.appendChild(script)
    console.log("[GooglePlacesAutocomplete] Script tag added to document head")

    return () => {
      // Cleanup handled by React
    }
  }, [])

  // Initialize PlaceAutocompleteElement when loaded
  useEffect(() => {
    if (!isLoaded || !containerRef.current || !customElements.get('gmp-place-autocomplete')) {
      return
    }

    // Clean up existing element
    if (autocompleteElementRef.current && containerRef.current) {
      containerRef.current.innerHTML = ""
    }

    // Create PlaceAutocompleteElement
    const autocompleteElement = document.createElement('gmp-place-autocomplete') as any
    console.log("[GooglePlacesAutocomplete] Created autocomplete element:", autocompleteElement)
    
    autocompleteElement.setAttribute('requested-result-types', 'address')
    autocompleteElement.setAttribute('country-restrictions', 'us')
    console.log("[GooglePlacesAutocomplete] Set attributes on element")
    
    // Handle place selection - Google Places API (New) uses 'gmp-select' event
    const handlePlaceSelect = async (event: any) => {
      console.log("[GooglePlacesAutocomplete] gmp-select event fired", event)
      console.log("[GooglePlacesAutocomplete] Event detail:", event.detail)
      console.log("[GooglePlacesAutocomplete] Event detail keys:", event.detail ? Object.keys(event.detail) : "no detail")
      
      // Try to inspect event properties safely
      const eventProps: any = {}
      for (const key in event) {
        try {
          if (typeof event[key] !== 'function' && key !== 'target' && key !== 'currentTarget') {
            eventProps[key] = event[key]
          }
        } catch (e) {
          eventProps[key] = '[Error accessing]'
        }
      }
      console.log("[GooglePlacesAutocomplete] Event properties:", eventProps)
      
      // Google Places API (New) provides placePrediction in the event
      // Try multiple possible property names and also check the autocomplete element
      let placePrediction = event.detail?.placePrediction || 
                            event.detail?.place || 
                            event.placePrediction ||
                            event.place ||
                            event.detail
      
      // Also try getting it from the autocomplete element
      if (!placePrediction && autocompleteElement) {
        console.log("[GooglePlacesAutocomplete] Trying to get place from autocomplete element")
        try {
          // Check if element has a getPlace or similar method
          if (typeof (autocompleteElement as any).getPlace === 'function') {
            placePrediction = (autocompleteElement as any).getPlace()
            console.log("[GooglePlacesAutocomplete] Got place from getPlace():", placePrediction)
          }
          // Check for value property that might contain place data
          if (!placePrediction && (autocompleteElement as any).value) {
            console.log("[GooglePlacesAutocomplete] Element value:", (autocompleteElement as any).value)
          }
        } catch (e) {
          console.log("[GooglePlacesAutocomplete] Error accessing element methods:", e)
        }
      }
      
      console.log("[GooglePlacesAutocomplete] placePrediction:", placePrediction)
      console.log("[GooglePlacesAutocomplete] placePrediction type:", typeof placePrediction)
      console.log("[GooglePlacesAutocomplete] placePrediction keys:", placePrediction ? Object.keys(placePrediction) : "no placePrediction")
      
      if (!placePrediction) {
        console.warn("[GooglePlacesAutocomplete] No placePrediction found in event or element")
        console.warn("[GooglePlacesAutocomplete] Event detail was:", event.detail)
        return
      }

      try {
        const apiKey = process.env.NEXT_PUBLIC_GOOGLE_PLACES_API_KEY
        
        // Convert placePrediction to Place object and fetch required fields
        let place: any
        if (typeof placePrediction.toPlace === 'function') {
          console.log("[GooglePlacesAutocomplete] Converting placePrediction to Place")
          place = placePrediction.toPlace()
          
          // Fetch the required fields
          console.log("[GooglePlacesAutocomplete] Fetching place fields")
          await place.fetchFields({ 
            fields: ['addressComponents', 'location', 'formattedAddress', 'id'] 
          })
          console.log("[GooglePlacesAutocomplete] Place fields fetched:", place)
        } else {
          // Fallback: use placePrediction directly if it's already a Place
          place = placePrediction
          console.log("[GooglePlacesAutocomplete] Using placePrediction directly as Place")
        }

        if (!place) {
          console.warn("[GooglePlacesAutocomplete] Failed to get Place object")
          return
        }

        // Extract data from Place object
        let placeId = place.id || ""
        let formattedAddress = place.formattedAddress || ""
        const location = place.location
        console.log("[GooglePlacesAutocomplete] Location object:", location)
        console.log("[GooglePlacesAutocomplete] Location type:", typeof location)
        console.log("[GooglePlacesAutocomplete] Location.lat type:", typeof location?.lat)
        console.log("[GooglePlacesAutocomplete] Location.lng type:", typeof location?.lng)
        
        // lat() and lng() are methods, not properties in Google Places API
        let latitude = 0
        let longitude = 0
        
        if (location) {
          if (typeof location.lat === 'function') {
            latitude = location.lat()
            console.log("[GooglePlacesAutocomplete] Called lat() method, got:", latitude)
          } else if (typeof location.lat === 'number') {
            latitude = location.lat
            console.log("[GooglePlacesAutocomplete] Got lat as property:", latitude)
          }
          
          if (typeof location.lng === 'function') {
            longitude = location.lng()
            console.log("[GooglePlacesAutocomplete] Called lng() method, got:", longitude)
          } else if (typeof location.lng === 'number') {
            longitude = location.lng
            console.log("[GooglePlacesAutocomplete] Got lng as property:", longitude)
          }
        }
        
        console.log("[GooglePlacesAutocomplete] Place data:", {
          placeId,
          formattedAddress,
          latitude,
          longitude,
          latitudeType: typeof latitude,
          longitudeType: typeof longitude,
          hasAddressComponents: !!place.addressComponents
        })
        
        // If lat/long are missing, fetch from Place Details API
        if ((!latitude || !longitude) && placeId && apiKey) {
          console.log("[GooglePlacesAutocomplete] Missing coordinates, fetching from Place Details API")
          try {
            const detailsUrl = `https://maps.googleapis.com/maps/api/place/details/json?place_id=${encodeURIComponent(placeId)}&fields=geometry,formatted_address&key=${encodeURIComponent(apiKey)}`
            const response = await fetch(detailsUrl)
            const data = await response.json()
            
            if (data.result) {
              if (data.result.geometry?.location) {
                latitude = data.result.geometry.location.lat || latitude
                longitude = data.result.geometry.location.lng || longitude
                console.log("[GooglePlacesAutocomplete] Got coordinates from Place Details:", { latitude, longitude })
              }
              if (data.result.formatted_address && !formattedAddress) {
                formattedAddress = data.result.formatted_address
              }
            }
          } catch (error) {
            console.error("[GooglePlacesAutocomplete] Error fetching coordinates from Place Details:", error)
          }
        }
        
        // If still missing and we have an address, try Geocoding API
        if ((!latitude || !longitude) && formattedAddress && apiKey) {
          console.log("[GooglePlacesAutocomplete] Still missing coordinates, trying Geocoding API")
          try {
            const geocodeUrl = `https://maps.googleapis.com/maps/api/geocode/json?address=${encodeURIComponent(formattedAddress)}&key=${encodeURIComponent(apiKey)}`
            const response = await fetch(geocodeUrl)
            const data = await response.json()
            
            if (data.results && data.results.length > 0 && data.results[0].geometry?.location) {
              latitude = data.results[0].geometry.location.lat || latitude
              longitude = data.results[0].geometry.location.lng || longitude
              console.log("[GooglePlacesAutocomplete] Got coordinates from Geocoding API:", { latitude, longitude })
            }
          } catch (error) {
            console.error("[GooglePlacesAutocomplete] Error fetching coordinates from Geocoding API:", error)
          }
        }

        // Parse address components
        let streetNumber = ""
        let route = ""
        let city = ""
        let state = ""
        let postalCode = ""
        let country = ""

        if (place.addressComponents && Array.isArray(place.addressComponents)) {
          console.log("[GooglePlacesAutocomplete] Parsing addressComponents:", place.addressComponents)
          place.addressComponents.forEach((component: any) => {
            const types = component.types || []
            console.log("[GooglePlacesAutocomplete] Component:", { types, component })

            if (types.includes("street_number")) {
              streetNumber = component.longText || component.long_name || ""
              console.log("[GooglePlacesAutocomplete] Found street_number:", streetNumber)
            } else if (types.includes("route")) {
              route = component.longText || component.long_name || ""
              console.log("[GooglePlacesAutocomplete] Found route:", route)
            } else if (types.includes("locality")) {
              city = component.longText || component.long_name || ""
              console.log("[GooglePlacesAutocomplete] Found locality:", city)
            } else if (types.includes("sublocality") && !city) {
              city = component.longText || component.long_name || ""
              console.log("[GooglePlacesAutocomplete] Found sublocality:", city)
            } else if (types.includes("administrative_area_level_1")) {
              state = component.shortText || component.short_name || ""
              console.log("[GooglePlacesAutocomplete] Found state:", state)
            } else if (types.includes("postal_code")) {
              postalCode = component.longText || component.long_name || ""
              console.log("[GooglePlacesAutocomplete] Found postal_code:", postalCode)
            } else if (types.includes("country")) {
              country = component.shortText || component.short_name || ""
              console.log("[GooglePlacesAutocomplete] Found country:", country)
            }
          })
          console.log("[GooglePlacesAutocomplete] After parsing components:", { city, state, postalCode, country })
        } else {
          console.warn("[GooglePlacesAutocomplete] No addressComponents in Place object")
        }

        const address = streetNumber && route ? `${streetNumber} ${route}` : route || streetNumber || ""

        const result: PlaceResult = {
          address,
          city,
          state,
          postalCode,
          country,
          formattedAddress,
          latitude,
          longitude,
          placeId,
        }

        console.log("[GooglePlacesAutocomplete] Final result being passed to onPlaceSelect:", result)

        // Update input value
        onChange(result.formattedAddress)

        // Notify parent
        console.log("[GooglePlacesAutocomplete] Calling onPlaceSelect with result")
        onPlaceSelect(result)
        console.log("[GooglePlacesAutocomplete] onPlaceSelect called")
      } catch (error) {
        console.error("[GooglePlacesAutocomplete] Error handling place selection:", error)
      }
    }
    
    // Google Places API (New) uses 'gmp-select' event
    console.log("[GooglePlacesAutocomplete] Attaching gmp-select event listener")
    autocompleteElement.addEventListener('gmp-select', handlePlaceSelect)
    
    // Also try listening on the shadow DOM input if it exists
    setTimeout(() => {
      if (autocompleteElement.shadowRoot) {
        const input = autocompleteElement.shadowRoot.querySelector('input')
        console.log("[GooglePlacesAutocomplete] Found shadow root input:", input)
        if (input) {
          // Listen for blur/change events as fallback
          input.addEventListener('blur', () => {
            console.log("[GooglePlacesAutocomplete] Input blur event fired")
          })
        }
      }
    }, 1000)
    
    console.log("[GooglePlacesAutocomplete] Event listeners attached")

    // Handle input changes
    autocompleteElement.addEventListener('input', (event: any) => {
      const inputValue = event.target?.value || ""
      if (inputValue) {
        onChange(inputValue)
      }
    })

    containerRef.current.appendChild(autocompleteElement)
    autocompleteElementRef.current = autocompleteElement
    console.log("[GooglePlacesAutocomplete] Element appended to container")

    // Wait for element to be ready and check for available events
    setTimeout(() => {
      console.log("[GooglePlacesAutocomplete] Checking element after timeout:", {
        element: autocompleteElement,
        shadowRoot: !!autocompleteElement.shadowRoot,
        innerHTML: autocompleteElement.innerHTML,
        attributes: Array.from(autocompleteElement.attributes).map(attr => ({ name: attr.name, value: attr.value }))
      })
      
      // Try to access the internal input
      if (autocompleteElement.shadowRoot) {
        const input = autocompleteElement.shadowRoot.querySelector('input')
        console.log("[GooglePlacesAutocomplete] Shadow root input:", input)
        if (input) {
          console.log("[GooglePlacesAutocomplete] Input element found, value:", input.value)
        }
      }
    }, 500)

    // Set initial value if provided
    if (value && autocompleteElement.shadowRoot) {
      const input = autocompleteElement.shadowRoot.querySelector('input')
      if (input) {
        input.value = value
        console.log("[GooglePlacesAutocomplete] Set initial value:", value)
      }
    }

    return () => {
      if (autocompleteElementRef.current && containerRef.current) {
        containerRef.current.innerHTML = ""
        autocompleteElementRef.current = null
      }
    }
  }, [isLoaded, onChange, onPlaceSelect, value])

  return (
    <div className="grid gap-2">
      {label && <Label htmlFor={id}>{label}</Label>}
      <div className="relative">
        <div 
          ref={containerRef}
          id={id}
          className="w-full"
          style={{ 
            pointerEvents: disabled || isLoading ? 'none' : 'auto',
            opacity: disabled || isLoading ? 0.6 : 1
          }}
        >
          {!isLoaded && (
            <Input
              value={value}
              onChange={(e) => onChange(e.target.value)}
              placeholder={placeholder}
              disabled={disabled || isLoading}
              autoComplete="off"
            />
          )}
        </div>
        {isLoading && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-muted-foreground">
            Loading...
          </div>
        )}
        {!isLoaded && !isLoading && !error && process.env.NEXT_PUBLIC_GOOGLE_PLACES_API_KEY && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2">
            <MapPin className="h-4 w-4 text-muted-foreground" />
          </div>
        )}
      </div>
      {error && (
        <div className="text-xs text-destructive mt-1">
          {error}
          <div className="mt-1 text-muted-foreground">
            <a 
              href="https://console.cloud.google.com/apis/library" 
              target="_blank" 
              rel="noopener noreferrer"
              className="underline"
            >
              Enable Places API (New) in Google Cloud Console
            </a>
          </div>
        </div>
      )}
    </div>
  )
}

