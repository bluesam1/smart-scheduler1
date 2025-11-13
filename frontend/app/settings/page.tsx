"use client"

import { DashboardShell } from "@/components/dashboard-shell"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card } from "@/components/ui/card"
import { useSettings } from "@/lib/settings-context"
import { useAuth } from "@/lib/auth/auth-context"
import { isAdmin, isDispatcher } from "@/lib/auth/role-utils"
import { Plus, X, Edit2, Check, Loader2 } from "lucide-react"
import { useState, useEffect } from "react"
import { Spinner } from "@/components/ui/spinner"
import { WeightsSection } from "./weights-section"
import { generateDemoData, DemoDataResult, deleteAllData, CleanupResult } from "@/lib/api/demo-data"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"

export default function SettingsPage() {
  const { accessToken } = useAuth()
  const [canManageSettings, setCanManageSettings] = useState(false)
  const {
    jobTypes,
    skills,
    isLoading,
    error,
    addJobType,
    removeJobType,
    updateJobType,
    addSkill,
    removeSkill,
    updateSkill,
  } = useSettings()

  const [newJobType, setNewJobType] = useState("")
  const [newSkill, setNewSkill] = useState("")
  const [editingJobType, setEditingJobType] = useState<string | null>(null)
  const [editingSkill, setEditingSkill] = useState<string | null>(null)
  const [editValue, setEditValue] = useState("")
  const [isAddingJobType, setIsAddingJobType] = useState(false)
  const [isAddingSkill, setIsAddingSkill] = useState(false)
  const [isUpdatingJobType, setIsUpdatingJobType] = useState(false)
  const [isUpdatingSkill, setIsUpdatingSkill] = useState(false)
  const [removingJobType, setRemovingJobType] = useState<string | null>(null)
  const [removingSkill, setRemovingSkill] = useState<string | null>(null)
  const [isGeneratingDemoData, setIsGeneratingDemoData] = useState(false)
  const [demoDataResult, setDemoDataResult] = useState<DemoDataResult | null>(null)
  const [demoDataError, setDemoDataError] = useState<string | null>(null)
  const [isDeletingData, setIsDeletingData] = useState(false)
  const [cleanupResult, setCleanupResult] = useState<CleanupResult | null>(null)
  const [cleanupError, setCleanupError] = useState<string | null>(null)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)

  useEffect(() => {
    setCanManageSettings(isAdmin(accessToken) || isDispatcher(accessToken))
  }, [accessToken])

  const handleAddJobType = async () => {
    if (newJobType.trim() && !isAddingJobType) {
      setIsAddingJobType(true)
      try {
        await addJobType(newJobType)
        setNewJobType("")
      } finally {
        setIsAddingJobType(false)
      }
    }
  }

  const handleAddSkill = async () => {
    if (newSkill.trim() && !isAddingSkill) {
      setIsAddingSkill(true)
      try {
        await addSkill(newSkill)
        setNewSkill("")
      } finally {
        setIsAddingSkill(false)
      }
    }
  }

  const startEditJobType = (type: string) => {
    setEditingJobType(type)
    setEditValue(type)
  }

  const finishEditJobType = async () => {
    if (editingJobType && editValue.trim() && !isUpdatingJobType) {
      setIsUpdatingJobType(true)
      try {
        await updateJobType(editingJobType, editValue)
        setEditingJobType(null)
        setEditValue("")
      } finally {
        setIsUpdatingJobType(false)
      }
    }
  }

  const handleRemoveJobType = async (type: string) => {
    if (removingJobType) return
    setRemovingJobType(type)
    try {
      await removeJobType(type)
    } finally {
      setRemovingJobType(null)
    }
  }

  const startEditSkill = (skill: string) => {
    setEditingSkill(skill)
    setEditValue(skill)
  }

  const finishEditSkill = async () => {
    if (editingSkill && editValue.trim() && !isUpdatingSkill) {
      setIsUpdatingSkill(true)
      try {
        await updateSkill(editingSkill, editValue)
        setEditingSkill(null)
        setEditValue("")
      } finally {
        setIsUpdatingSkill(false)
      }
    }
  }

  const handleRemoveSkill = async (skill: string) => {
    if (removingSkill) return
    setRemovingSkill(skill)
    try {
      await removeSkill(skill)
    } finally {
      setRemovingSkill(null)
    }
  }

  const handleGenerateDemoData = async () => {
    if (!accessToken || isGeneratingDemoData) return
    
    setIsGeneratingDemoData(true)
    setDemoDataError(null)
    setDemoDataResult(null)
    setCleanupResult(null)
    setCleanupError(null)
    
    try {
      const result = await generateDemoData(accessToken)
      setDemoDataResult(result)
    } catch (err) {
      setDemoDataError(err instanceof Error ? err.message : 'Failed to generate demo data')
    } finally {
      setIsGeneratingDemoData(false)
    }
  }

  const handleDeleteAllData = async () => {
    if (!accessToken || isDeletingData) return
    
    setShowDeleteConfirm(false)
    setIsDeletingData(true)
    setCleanupError(null)
    setCleanupResult(null)
    setDemoDataResult(null)
    setDemoDataError(null)
    
    try {
      const result = await deleteAllData(accessToken)
      setCleanupResult(result)
    } catch (err) {
      setCleanupError(err instanceof Error ? err.message : 'Failed to delete data')
    } finally {
      setIsDeletingData(false)
    }
  }

  if (!canManageSettings) {
    return (
      <DashboardShell>
        <div className="space-y-6">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Settings</h1>
            <p className="text-muted-foreground">Manage job types and skills for your scheduling system</p>
          </div>
          <Card className="p-6">
            <div className="text-center py-8">
              <p className="text-muted-foreground">Access denied. Dispatcher or Admin role required to manage settings.</p>
            </div>
          </Card>
        </div>
      </DashboardShell>
    )
  }

  return (
    <DashboardShell>
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Settings</h1>
          <p className="text-muted-foreground">Manage job types and skills for your scheduling system</p>
          {error && (
            <div className="mt-4 p-3 bg-destructive/10 text-destructive rounded-md text-sm">{error}</div>
          )}
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          {/* Job Types Section */}
          <Card className="p-6">
            <div className="space-y-4">
              <div>
                <h2 className="text-xl font-semibold">Job Types</h2>
                <p className="text-sm text-muted-foreground">Define the types of jobs available for scheduling</p>
              </div>

              <div className="flex gap-2">
                <Input
                  value={newJobType}
                  onChange={(e) => setNewJobType(e.target.value)}
                  placeholder="Add new job type..."
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault()
                      handleAddJobType()
                    }
                  }}
                />
                <Button onClick={handleAddJobType} size="icon" disabled={isAddingJobType || isLoading}>
                  {isAddingJobType ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Plus className="h-4 w-4" />
                  )}
                </Button>
              </div>

              <div className="space-y-2 max-h-[400px] overflow-auto">
                {isLoading ? (
                  <div className="flex items-center justify-center p-8">
                    <Spinner />
                  </div>
                ) : jobTypes.length === 0 ? (
                  <div className="text-center text-sm text-muted-foreground p-4">No job types configured</div>
                ) : (
                  jobTypes.map((type) => (
                  <div key={type} className="flex items-center justify-between gap-2 p-2 rounded-lg border">
                    {editingJobType === type ? (
                      <>
                        <Input
                          value={editValue}
                          onChange={(e) => setEditValue(e.target.value)}
                          onKeyDown={(e) => {
                            if (e.key === "Enter") {
                              e.preventDefault()
                              finishEditJobType()
                            } else if (e.key === "Escape") {
                              setEditingJobType(null)
                              setEditValue("")
                            }
                          }}
                          autoFocus
                          className="h-8"
                        />
                        <Button
                          onClick={finishEditJobType}
                          size="icon"
                          variant="ghost"
                          className="h-8 w-8"
                          disabled={isUpdatingJobType}
                        >
                          {isUpdatingJobType ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Check className="h-4 w-4" />
                          )}
                        </Button>
                      </>
                    ) : (
                      <>
                        <span className="flex-1 text-sm">{type}</span>
                        <div className="flex gap-1">
                          <Button
                            onClick={() => startEditJobType(type)}
                            size="icon"
                            variant="ghost"
                            className="h-8 w-8"
                          >
                            <Edit2 className="h-3.5 w-3.5" />
                          </Button>
                          <Button
                            onClick={() => handleRemoveJobType(type)}
                            size="icon"
                            variant="ghost"
                            className="h-8 w-8 text-destructive hover:text-destructive"
                            disabled={removingJobType === type}
                          >
                            {removingJobType === type ? (
                              <Loader2 className="h-3.5 w-3.5 animate-spin" />
                            ) : (
                              <X className="h-3.5 w-3.5" />
                            )}
                          </Button>
                        </div>
                      </>
                    )}
                  </div>
                    ))
                )}
              </div>
            </div>
          </Card>

          {/* Skills Section */}
          <Card className="p-6">
            <div className="space-y-4">
              <div>
                <h2 className="text-xl font-semibold">Skills</h2>
                <p className="text-sm text-muted-foreground">
                  Define available skills for contractors and job requirements
                </p>
              </div>

              <div className="flex gap-2">
                <Input
                  value={newSkill}
                  onChange={(e) => setNewSkill(e.target.value)}
                  placeholder="Add new skill..."
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault()
                      handleAddSkill()
                    }
                  }}
                />
                <Button onClick={handleAddSkill} size="icon" disabled={isAddingSkill || isLoading}>
                  {isAddingSkill ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Plus className="h-4 w-4" />
                  )}
                </Button>
              </div>

              <div className="space-y-2 max-h-[400px] overflow-auto">
                {isLoading ? (
                  <div className="flex items-center justify-center p-8">
                    <Spinner />
                  </div>
                ) : skills.length === 0 ? (
                  <div className="text-center text-sm text-muted-foreground p-4">No skills configured</div>
                ) : (
                  skills.map((skill) => (
                  <div key={skill} className="flex items-center justify-between gap-2 p-2 rounded-lg border">
                    {editingSkill === skill ? (
                      <>
                        <Input
                          value={editValue}
                          onChange={(e) => setEditValue(e.target.value)}
                          onKeyDown={(e) => {
                            if (e.key === "Enter") {
                              e.preventDefault()
                              finishEditSkill()
                            } else if (e.key === "Escape") {
                              setEditingSkill(null)
                              setEditValue("")
                            }
                          }}
                          autoFocus
                          className="h-8"
                        />
                        <Button
                          onClick={finishEditSkill}
                          size="icon"
                          variant="ghost"
                          className="h-8 w-8"
                          disabled={isUpdatingSkill}
                        >
                          {isUpdatingSkill ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Check className="h-4 w-4" />
                          )}
                        </Button>
                      </>
                    ) : (
                      <>
                        <span className="flex-1 text-sm">{skill}</span>
                        <div className="flex gap-1">
                          <Button onClick={() => startEditSkill(skill)} size="icon" variant="ghost" className="h-8 w-8">
                            <Edit2 className="h-3.5 w-3.5" />
                          </Button>
                          <Button
                            onClick={() => handleRemoveSkill(skill)}
                            size="icon"
                            variant="ghost"
                            className="h-8 w-8 text-destructive hover:text-destructive"
                            disabled={removingSkill === skill}
                          >
                            {removingSkill === skill ? (
                              <Loader2 className="h-3.5 w-3.5 animate-spin" />
                            ) : (
                              <X className="h-3.5 w-3.5" />
                            )}
                          </Button>
                        </div>
                      </>
                    )}
                  </div>
                    ))
                )}
              </div>
            </div>
          </Card>
        </div>

        {/* Weights Configuration Section */}
        <WeightsSection />

        {/* Demo Data Section */}
        <Card className="p-6">
          <div className="space-y-4">
            <div>
              <h2 className="text-xl font-semibold">Demo Data</h2>
              <p className="text-sm text-muted-foreground">
                Populate the database with realistic demo data for testing
              </p>
            </div>

            <div className="space-y-3">
              <div className="flex flex-wrap gap-2">
                <Button
                  onClick={handleGenerateDemoData}
                  disabled={isGeneratingDemoData || isDeletingData || isLoading}
                  className="flex-1 sm:flex-none"
                >
                  {isGeneratingDemoData ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Generating Demo Data...
                    </>
                  ) : (
                    "Populate Demo Data"
                  )}
                </Button>

                <Button
                  onClick={() => setShowDeleteConfirm(true)}
                  disabled={isGeneratingDemoData || isDeletingData || isLoading}
                  variant="destructive"
                  className="flex-1 sm:flex-none"
                >
                  {isDeletingData ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Deleting All Data...
                    </>
                  ) : (
                    "Delete All Data"
                  )}
                </Button>
              </div>

              {demoDataResult && (
                <div className="p-4 bg-green-50 dark:bg-green-950 text-green-900 dark:text-green-100 rounded-md">
                  <p className="font-semibold mb-2">Demo data generated successfully!</p>
                  <ul className="text-sm space-y-1">
                    <li>• Contractors created: {demoDataResult.contractorsCreated}</li>
                    <li>• Jobs created: {demoDataResult.jobsCreated}</li>
                    <li>• Assignments created: {demoDataResult.assignmentsCreated}</li>
                    <li>• Audit records created: {demoDataResult.auditRecordsCreated}</li>
                    <li>• Duration: {(demoDataResult.durationMs / 1000).toFixed(2)}s</li>
                  </ul>
                </div>
              )}

              {cleanupResult && (
                <div className="p-4 bg-orange-50 dark:bg-orange-950 text-orange-900 dark:text-orange-100 rounded-md">
                  <p className="font-semibold mb-2">All data deleted successfully!</p>
                  <ul className="text-sm space-y-1">
                    <li>• Contractors deleted: {cleanupResult.contractorsDeleted}</li>
                    <li>• Jobs deleted: {cleanupResult.jobsDeleted}</li>
                    <li>• Assignments deleted: {cleanupResult.assignmentsDeleted}</li>
                    <li>• Audit records deleted: {cleanupResult.auditRecordsDeleted}</li>
                    <li>• Event logs deleted: {cleanupResult.eventLogsDeleted}</li>
                    <li>• Duration: {(cleanupResult.durationMs / 1000).toFixed(2)}s</li>
                  </ul>
                </div>
              )}

              {demoDataError && (
                <div className="p-4 bg-destructive/10 text-destructive rounded-md">
                  <p className="text-sm font-semibold">Error generating demo data</p>
                  <p className="text-sm">{demoDataError}</p>
                </div>
              )}

              {cleanupError && (
                <div className="p-4 bg-destructive/10 text-destructive rounded-md">
                  <p className="text-sm font-semibold">Error deleting data</p>
                  <p className="text-sm">{cleanupError}</p>
                </div>
              )}

              <div className="text-xs text-muted-foreground">
                <p className="mb-2">
                  <strong>Populate Demo Data:</strong> Generates 50-100 contractors, 200-500 jobs, and corresponding assignments
                  across multiple US timezones. Data varies on each run to avoid duplicates.
                </p>
                <p className="text-destructive">
                  <strong>Delete All Data:</strong> WARNING - This will permanently delete ALL data from the database
                  (contractors, jobs, assignments, audit records, event logs). This operation cannot be undone!
                </p>
              </div>
            </div>
          </div>
        </Card>

        {/* Delete Confirmation Dialog */}
        <AlertDialog open={showDeleteConfirm} onOpenChange={setShowDeleteConfirm}>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Delete All Data?</AlertDialogTitle>
              <AlertDialogDescription>
                This will permanently delete ALL data from the database including:
                <ul className="mt-2 space-y-1 list-disc list-inside">
                  <li>All contractors</li>
                  <li>All jobs</li>
                  <li>All assignments</li>
                  <li>All audit records</li>
                  <li>All event logs</li>
                </ul>
                <p className="mt-3 font-semibold text-destructive">
                  This operation cannot be undone. Are you absolutely sure?
                </p>
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Cancel</AlertDialogCancel>
              <AlertDialogAction
                onClick={handleDeleteAllData}
                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              >
                Yes, Delete All Data
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </DashboardShell>
  )
}
