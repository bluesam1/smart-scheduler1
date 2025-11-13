"use client"

import { useState, useEffect } from "react"
import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { useAuth } from "@/lib/auth/auth-context"
import { createAuthenticatedFetch, API_BASE_URL } from "@/lib/api/api-client-config"
import { toast } from "sonner"
import { Loader2, RotateCcw, History } from "lucide-react"
import { Spinner } from "@/components/ui/spinner"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface WeightsConfigResponse {
  version: number
  weights: {
    availability: number
    rating: number
    distance: number
  }
  tieBreakers: string[]
  rotation: {
    enabled: boolean
    boost: number
    underUtilizationThreshold: number
  }
  changeNotes: string
  createdBy: string
  createdAt: string
}

interface WeightsConfigHistoryItem {
  version: number
  isActive: boolean
  changeNotes: string
  createdBy: string
  createdAt: string
}

export function WeightsSection() {
  const { getTokenProvider } = useAuth()
  const [config, setConfig] = useState<WeightsConfigResponse | null>(null)
  const [history, setHistory] = useState<WeightsConfigHistoryItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [historyOpen, setHistoryOpen] = useState(false)

  // Form state
  const [availability, setAvailability] = useState("0.20")
  const [rating, setRating] = useState("0.35")
  const [distance, setDistance] = useState("0.45")
  const [rotationEnabled, setRotationEnabled] = useState(true)
  const [rotationBoost, setRotationBoost] = useState("3.0")
  const [underUtilizationThreshold, setUnderUtilizationThreshold] = useState("0.20")
  const [changeNotes, setChangeNotes] = useState("")

  const getAuthenticatedFetch = () => {
    const tokenProvider = getTokenProvider()
    if (!tokenProvider) return null
    return createAuthenticatedFetch(tokenProvider)
  }

  const fetchCurrentConfig = async () => {
    const authenticatedFetch = getAuthenticatedFetch()
    if (!authenticatedFetch) {
      toast.error("Authentication required")
      return
    }

    try {
      const response = await authenticatedFetch(`${API_BASE_URL}/api/admin/weights/current`, {
        method: "GET",
      })

      if (!response.ok) {
        throw new Error(`Failed to fetch weights config: ${response.statusText}`)
      }

      const data: WeightsConfigResponse = await response.json()
      setConfig(data)
      setAvailability(data.weights.availability.toString())
      setRating(data.weights.rating.toString())
      setDistance(data.weights.distance.toString())
      setRotationEnabled(data.rotation.enabled)
      setRotationBoost(data.rotation.boost.toString())
      setUnderUtilizationThreshold(data.rotation.underUtilizationThreshold.toString())
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "Failed to fetch weights config"
      toast.error(errorMessage)
      console.error("Error fetching weights config:", err)
    }
  }

  const fetchHistory = async () => {
    const authenticatedFetch = getAuthenticatedFetch()
    if (!authenticatedFetch) return

    try {
      const response = await authenticatedFetch(`${API_BASE_URL}/api/admin/weights/history`, {
        method: "GET",
      })

      if (!response.ok) {
        throw new Error(`Failed to fetch history: ${response.statusText}`)
      }

      const data: WeightsConfigHistoryItem[] = await response.json()
      setHistory(data)
    } catch (err) {
      console.error("Error fetching history:", err)
    }
  }

  useEffect(() => {
    const loadData = async () => {
      setIsLoading(true)
      await Promise.all([fetchCurrentConfig(), fetchHistory()])
      setIsLoading(false)
    }
    loadData()
  }, [])

  const handleSave = async () => {
    if (!changeNotes.trim()) {
      toast.error("Change notes are required")
      return
    }

    const authenticatedFetch = getAuthenticatedFetch()
    if (!authenticatedFetch) {
      toast.error("Authentication required")
      return
    }

    setIsSaving(true)
    try {
      const request = {
        weights: {
          availability: parseFloat(availability),
          rating: parseFloat(rating),
          distance: parseFloat(distance),
        },
        tieBreakers: ["earliestStart", "lowerDayUtilization", "shortestNextLeg"],
        rotation: {
          enabled: rotationEnabled,
          boost: parseFloat(rotationBoost),
          underUtilizationThreshold: parseFloat(underUtilizationThreshold),
        },
        changeNotes: changeNotes.trim(),
      }

      const response = await authenticatedFetch(`${API_BASE_URL}/api/admin/weights`, {
        method: "POST",
        body: JSON.stringify(request),
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || `Failed to save weights config: ${response.statusText}`)
      }

      toast.success("Weights configuration saved successfully")
      setChangeNotes("")
      await Promise.all([fetchCurrentConfig(), fetchHistory()])
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "Failed to save weights config"
      toast.error(errorMessage)
      console.error("Error saving weights config:", err)
    } finally {
      setIsSaving(false)
    }
  }

  const handleRollback = async (version: number) => {
    if (!changeNotes.trim()) {
      toast.error("Change notes are required for rollback")
      return
    }

    const authenticatedFetch = getAuthenticatedFetch()
    if (!authenticatedFetch) {
      toast.error("Authentication required")
      return
    }

    setIsSaving(true)
    try {
      const request = {
        version,
        changeNotes: changeNotes.trim(),
      }

      const response = await authenticatedFetch(`${API_BASE_URL}/api/admin/weights/rollback`, {
        method: "POST",
        body: JSON.stringify(request),
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || `Failed to rollback: ${response.statusText}`)
      }

      toast.success(`Rolled back to version ${version}`)
      setChangeNotes("")
      setHistoryOpen(false)
      await Promise.all([fetchCurrentConfig(), fetchHistory()])
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "Failed to rollback"
      toast.error(errorMessage)
      console.error("Error rolling back:", err)
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return (
      <Card className="p-6">
        <div className="flex items-center justify-center p-8">
          <Spinner />
        </div>
      </Card>
    )
  }

  const weightSum = parseFloat(availability) + parseFloat(rating) + parseFloat(distance)
  const weightSumWarning = Math.abs(weightSum - 1.0) > 0.1

  return (
    <Card className="p-6">
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-xl font-semibold">Scoring Weights Configuration</h2>
            <p className="text-sm text-muted-foreground">Tune the recommendation algorithm weights</p>
            {config && (
              <p className="text-xs text-muted-foreground mt-1">Version: {config.version}</p>
            )}
          </div>
          <Button variant="outline" onClick={() => setHistoryOpen(true)}>
            <History className="h-4 w-4 mr-2" />
            History
          </Button>
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          {/* Weight Factors */}
          <div className="space-y-4">
            <h3 className="font-semibold">Weight Factors</h3>
            <div className="space-y-3">
              <div>
                <Label htmlFor="availability">Availability Weight</Label>
                <Input
                  id="availability"
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  value={availability}
                  onChange={(e) => setAvailability(e.target.value)}
                />
              </div>
              <div>
                <Label htmlFor="rating">Rating Weight</Label>
                <Input
                  id="rating"
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  value={rating}
                  onChange={(e) => setRating(e.target.value)}
                />
              </div>
              <div>
                <Label htmlFor="distance">Distance Weight</Label>
                <Input
                  id="distance"
                  type="number"
                  step="0.01"
                  min="0"
                  max="1"
                  value={distance}
                  onChange={(e) => setDistance(e.target.value)}
                />
              </div>
              {weightSumWarning && (
                <div className="p-2 bg-yellow-50 text-yellow-800 rounded text-sm">
                  Weight sum: {weightSum.toFixed(2)} (recommended: 1.0)
                </div>
              )}
            </div>
          </div>

          {/* Rotation Config */}
          <div className="space-y-4">
            <h3 className="font-semibold">Rotation Configuration</h3>
            <div className="space-y-3">
              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="rotationEnabled"
                  checked={rotationEnabled}
                  onChange={(e) => setRotationEnabled(e.target.checked)}
                  className="rounded"
                />
                <Label htmlFor="rotationEnabled">Enable Rotation Boost</Label>
              </div>
              {rotationEnabled && (
                <>
                  <div>
                    <Label htmlFor="rotationBoost">Rotation Boost</Label>
                    <Input
                      id="rotationBoost"
                      type="number"
                      step="0.1"
                      min="0"
                      max="20"
                      value={rotationBoost}
                      onChange={(e) => setRotationBoost(e.target.value)}
                    />
                  </div>
                  <div>
                    <Label htmlFor="underUtilizationThreshold">Under Utilization Threshold</Label>
                    <Input
                      id="underUtilizationThreshold"
                      type="number"
                      step="0.01"
                      min="0"
                      max="1"
                      value={underUtilizationThreshold}
                      onChange={(e) => setUnderUtilizationThreshold(e.target.value)}
                    />
                  </div>
                </>
              )}
            </div>
          </div>
        </div>

        <div>
          <Label htmlFor="changeNotes">Change Notes *</Label>
          <Textarea
            id="changeNotes"
            value={changeNotes}
            onChange={(e) => setChangeNotes(e.target.value)}
            placeholder="Describe why you're changing these weights..."
            rows={3}
          />
        </div>

        <Button onClick={handleSave} disabled={isSaving || !changeNotes.trim()}>
          {isSaving ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              Saving...
            </>
          ) : (
            "Save Configuration"
          )}
        </Button>
      </div>

      {/* History Dialog */}
      <Dialog open={historyOpen} onOpenChange={setHistoryOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Configuration History</DialogTitle>
            <DialogDescription>View and rollback to previous versions</DialogDescription>
          </DialogHeader>
          <div className="space-y-2 max-h-[400px] overflow-auto">
            {history.map((item) => (
              <div
                key={item.version}
                className="flex items-center justify-between p-3 border rounded-lg"
              >
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold">Version {item.version}</span>
                    {item.isActive && (
                      <span className="text-xs bg-green-100 text-green-800 px-2 py-1 rounded">
                        Active
                      </span>
                    )}
                  </div>
                  <p className="text-sm text-muted-foreground mt-1">{item.changeNotes}</p>
                  <p className="text-xs text-muted-foreground mt-1">
                    By {item.createdBy} on {new Date(item.createdAt).toLocaleString()}
                  </p>
                </div>
                {!item.isActive && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleRollback(item.version)}
                    disabled={isSaving}
                  >
                    <RotateCcw className="h-4 w-4 mr-1" />
                    Rollback
                  </Button>
                )}
              </div>
            ))}
          </div>
        </DialogContent>
      </Dialog>
    </Card>
  )
}


