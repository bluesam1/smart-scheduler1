"use client"

import { DashboardShell } from "@/components/dashboard-shell"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card } from "@/components/ui/card"
import { useSettings } from "@/lib/settings-context"
import { Plus, X, Edit2, Check } from "lucide-react"
import { useState } from "react"

export default function SettingsPage() {
  const { jobTypes, skills, addJobType, removeJobType, updateJobType, addSkill, removeSkill, updateSkill } =
    useSettings()

  const [newJobType, setNewJobType] = useState("")
  const [newSkill, setNewSkill] = useState("")
  const [editingJobType, setEditingJobType] = useState<string | null>(null)
  const [editingSkill, setEditingSkill] = useState<string | null>(null)
  const [editValue, setEditValue] = useState("")

  const handleAddJobType = () => {
    if (newJobType.trim()) {
      addJobType(newJobType)
      setNewJobType("")
    }
  }

  const handleAddSkill = () => {
    if (newSkill.trim()) {
      addSkill(newSkill)
      setNewSkill("")
    }
  }

  const startEditJobType = (type: string) => {
    setEditingJobType(type)
    setEditValue(type)
  }

  const finishEditJobType = () => {
    if (editingJobType && editValue.trim()) {
      updateJobType(editingJobType, editValue)
      setEditingJobType(null)
      setEditValue("")
    }
  }

  const startEditSkill = (skill: string) => {
    setEditingSkill(skill)
    setEditValue(skill)
  }

  const finishEditSkill = () => {
    if (editingSkill && editValue.trim()) {
      updateSkill(editingSkill, editValue)
      setEditingSkill(null)
      setEditValue("")
    }
  }

  return (
    <DashboardShell>
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Settings</h1>
          <p className="text-muted-foreground">Manage job types and skills for your scheduling system</p>
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
                <Button onClick={handleAddJobType} size="icon">
                  <Plus className="h-4 w-4" />
                </Button>
              </div>

              <div className="space-y-2 max-h-[400px] overflow-auto">
                {jobTypes.map((type) => (
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
                        <Button onClick={finishEditJobType} size="icon" variant="ghost" className="h-8 w-8">
                          <Check className="h-4 w-4" />
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
                            onClick={() => removeJobType(type)}
                            size="icon"
                            variant="ghost"
                            className="h-8 w-8 text-destructive hover:text-destructive"
                          >
                            <X className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      </>
                    )}
                  </div>
                ))}
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
                <Button onClick={handleAddSkill} size="icon">
                  <Plus className="h-4 w-4" />
                </Button>
              </div>

              <div className="space-y-2 max-h-[400px] overflow-auto">
                {skills.map((skill) => (
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
                        <Button onClick={finishEditSkill} size="icon" variant="ghost" className="h-8 w-8">
                          <Check className="h-4 w-4" />
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
                            onClick={() => removeSkill(skill)}
                            size="icon"
                            variant="ghost"
                            className="h-8 w-8 text-destructive hover:text-destructive"
                          >
                            <X className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      </>
                    )}
                  </div>
                ))}
              </div>
            </div>
          </Card>
        </div>
      </div>
    </DashboardShell>
  )
}
