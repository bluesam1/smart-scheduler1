"use client"

import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
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

interface MapViewDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  locations: MapLocation[]
  title?: string
}

export function MapViewDialog({ open, onOpenChange, locations, title = "Map View" }: MapViewDialogProps) {
  // Mock map view - in production this would integrate with Google Maps, Mapbox, etc.
  const jobLocation = locations.find((loc) => loc.type === "job")
  const contractors = locations.filter((loc) => loc.type === "contractor")

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl h-[600px]">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <div className="flex-1 bg-muted/20 rounded-lg border-2 border-border relative overflow-hidden">
          {/* Mock Map Background */}
          <div className="absolute inset-0 bg-gradient-to-br from-blue-50 to-green-50 dark:from-slate-800 dark:to-slate-900">
            <svg className="w-full h-full opacity-10 dark:opacity-20">
              <pattern id="grid-dialog" width="40" height="40" patternUnits="userSpaceOnUse">
                <path d="M 40 0 L 0 0 0 40" fill="none" stroke="currentColor" strokeWidth="1" />
              </pattern>
              <rect width="100%" height="100%" fill="url(#grid-dialog)" />
            </svg>
          </div>

          {/* Job Location Marker */}
          {jobLocation && (
            <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 z-10">
              <div className="flex flex-col items-center">
                <div className="bg-red-500 text-white p-3 rounded-full shadow-lg animate-pulse">
                  <Home className="h-6 w-6" />
                </div>
                <div className="mt-2 bg-background border-2 border-red-500 rounded-lg shadow-lg p-3 max-w-xs">
                  <div className="font-semibold text-sm">{jobLocation.name}</div>
                  <div className="text-xs text-muted-foreground flex items-start gap-1 mt-1">
                    <MapPin className="h-3 w-3 mt-0.5 flex-shrink-0" />
                    <span className="text-balance">{jobLocation.address}</span>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Contractor Location Markers */}
          {contractors.map((contractor, index) => {
            // Position contractors in a circle around the job location
            const angle = (index / contractors.length) * 2 * Math.PI
            const radius = 120 + index * 20
            const x = 50 + Math.cos(angle) * radius
            const y = 50 + Math.sin(angle) * radius

            return (
              <div
                key={contractor.name}
                className="absolute transform -translate-x-1/2 -translate-y-1/2 z-20"
                style={{ left: `${x}%`, top: `${y}%` }}
              >
                <div className="flex flex-col items-center">
                  <div className="bg-primary text-primary-foreground p-2.5 rounded-full shadow-lg border-2 border-background">
                    <User className="h-5 w-5" />
                  </div>
                  <div className="mt-2 bg-background border-2 border-primary rounded-lg shadow-lg p-2.5 max-w-[180px]">
                    <div className="font-semibold text-xs">{contractor.name}</div>
                    <div className="text-[10px] text-muted-foreground flex items-start gap-1 mt-1">
                      <MapPin className="h-2.5 w-2.5 mt-0.5 flex-shrink-0" />
                      <span className="text-balance">{contractor.address}</span>
                    </div>
                    {contractor.skills && contractor.skills.length > 0 && (
                      <div className="flex flex-wrap gap-1 mt-1.5">
                        {contractor.skills.slice(0, 2).map((skill) => (
                          <Badge key={skill} variant="secondary" className="text-[9px] px-1 py-0">
                            {skill}
                          </Badge>
                        ))}
                        {contractor.skills.length > 2 && (
                          <Badge variant="outline" className="text-[9px] px-1 py-0">
                            +{contractor.skills.length - 2}
                          </Badge>
                        )}
                      </div>
                    )}
                  </div>
                  {/* Distance line to job */}
                  <svg
                    className="absolute pointer-events-none"
                    style={{
                      left: "50%",
                      top: "50%",
                      width: "200%",
                      height: "200%",
                      transform: "translate(-50%, -50%)",
                    }}
                  >
                    <line
                      x1="50%"
                      y1="50%"
                      x2={`${50 - (x - 50)}%`}
                      y2={`${50 - (y - 50)}%`}
                      stroke="currentColor"
                      strokeWidth="1"
                      strokeDasharray="4 4"
                      className="text-primary/30"
                    />
                  </svg>
                </div>
              </div>
            )
          })}

          {/* Legend */}
          <div className="absolute bottom-4 left-4 bg-background/95 backdrop-blur border rounded-lg p-3 shadow-lg">
            <div className="text-xs font-semibold mb-2">Legend</div>
            <div className="space-y-1.5">
              <div className="flex items-center gap-2 text-xs">
                <div className="bg-red-500 p-1.5 rounded-full">
                  <Home className="h-3 w-3 text-white" />
                </div>
                <span>Job Location</span>
              </div>
              <div className="flex items-center gap-2 text-xs">
                <div className="bg-primary p-1.5 rounded-full">
                  <User className="h-3 w-3 text-primary-foreground" />
                </div>
                <span>Contractor</span>
              </div>
            </div>
          </div>

          {/* Note */}
          <div className="absolute bottom-4 right-4 bg-background/95 backdrop-blur border rounded-lg p-2 shadow-lg max-w-[200px]">
            <div className="text-[10px] text-muted-foreground">
              Note: This is a visual representation. Actual distances may vary.
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
