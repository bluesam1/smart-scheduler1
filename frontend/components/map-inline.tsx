"use client"

import { MapPin, User, Home } from "lucide-react"
import { Badge } from "@/components/ui/badge"

interface MapLocation {
  type: "job" | "contractor"
  name: string
  address: string
  lat?: number
  lng?: number
  skills?: string[]
}

interface MapInlineProps {
  locations: MapLocation[]
  height?: string
}

export function MapInline({ locations, height = "300px" }: MapInlineProps) {
  // Mock map view - in production this would integrate with Google Maps, Mapbox, etc.
  const jobLocation = locations.find((loc) => loc.type === "job")
  const contractors = locations.filter((loc) => loc.type === "contractor")

  return (
    <div className="w-full bg-muted/20 rounded-lg border-2 border-border relative overflow-hidden" style={{ height }}>
      {/* Mock Map Background */}
      <div className="absolute inset-0 bg-gradient-to-br from-blue-50 to-green-50 dark:from-blue-950/20 dark:to-green-950/20">
        <svg className="w-full h-full opacity-10">
          <pattern id="grid" width="40" height="40" patternUnits="userSpaceOnUse">
            <path d="M 40 0 L 0 0 0 40" fill="none" stroke="currentColor" strokeWidth="1" />
          </pattern>
          <rect width="100%" height="100%" fill="url(#grid)" />
        </svg>
      </div>

      {/* Job Location Marker */}
      {jobLocation && (
        <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 z-10">
          <div className="flex flex-col items-center">
            <div className="bg-red-500 text-white p-2 rounded-full shadow-lg">
              <Home className="h-5 w-5" />
            </div>
            <div className="mt-1.5 bg-background border-2 border-red-500 rounded-lg shadow-lg p-2 max-w-[200px]">
              <div className="font-semibold text-xs">{jobLocation.name}</div>
              <div className="text-[10px] text-muted-foreground flex items-start gap-1 mt-0.5">
                <MapPin className="h-3 w-3 mt-0.5 flex-shrink-0" />
                <span className="text-balance line-clamp-2">{jobLocation.address}</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Contractor Location Markers */}
      {contractors.map((contractor, index) => {
        // Position contractors in a circle around the job location
        const angle = (index / Math.max(contractors.length, 1)) * 2 * Math.PI
        const radius = 30 + index * 5
        const x = 50 + Math.cos(angle) * radius
        const y = 50 + Math.sin(angle) * radius

        return (
          <div
            key={contractor.name}
            className="absolute transform -translate-x-1/2 -translate-y-1/2 z-20"
            style={{ left: `${x}%`, top: `${y}%` }}
          >
            <div className="flex flex-col items-center">
              <div className="bg-primary text-primary-foreground p-2 rounded-full shadow-lg border-2 border-background">
                <User className="h-4 w-4" />
              </div>
              <div className="mt-1 bg-background border-2 border-primary rounded-lg shadow-lg p-1.5 max-w-[150px]">
                <div className="font-semibold text-[10px]">{contractor.name}</div>
                <div className="text-[9px] text-muted-foreground flex items-start gap-0.5 mt-0.5">
                  <MapPin className="h-2 w-2 mt-0.5 flex-shrink-0" />
                  <span className="text-balance line-clamp-1">{contractor.address}</span>
                </div>
              </div>
            </div>
          </div>
        )
      })}

      {/* Legend */}
      <div className="absolute bottom-2 left-2 bg-background/95 backdrop-blur border rounded-lg p-2 shadow-lg">
        <div className="text-[10px] font-semibold mb-1">Legend</div>
        <div className="space-y-1">
          <div className="flex items-center gap-1.5 text-[10px]">
            <div className="bg-red-500 p-1 rounded-full">
              <Home className="h-2.5 w-2.5 text-white" />
            </div>
            <span>Job</span>
          </div>
          {contractors.length > 0 && (
            <div className="flex items-center gap-1.5 text-[10px]">
              <div className="bg-primary p-1 rounded-full">
                <User className="h-2.5 w-2.5 text-primary-foreground" />
              </div>
              <span>Contractor</span>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

