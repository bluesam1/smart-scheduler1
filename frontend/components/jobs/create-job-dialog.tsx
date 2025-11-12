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
import { AddressInput, type Address } from "@/components/ui/address-input"
import { SkillCombobox } from "@/components/ui/skill-combobox"
import { TimeSelect } from "@/components/ui/time-select"

interface CreateJobDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateJobDialog({ open, onOpenChange }: CreateJobDialogProps) {
  const { jobTypes, skills } = useSettings()
  const [selectedSkills, setSelectedSkills] = useState<string[]>([])
  const [jobAddress, setJobAddress] = useState<Address | undefined>()

  const handleSkillToggle = (skill: string) => {
    setSelectedSkills((prev) => (prev.includes(skill) ? prev.filter((s) => s !== skill) : [...prev, skill]))
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[550px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create New Job</DialogTitle>
          <DialogDescription>Add a new flooring job to the queue for assignment</DialogDescription>
        </DialogHeader>
        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="description">Job Description</Label>
            <Textarea id="description" placeholder="Describe the job details..." rows={3} />
          </div>

          <AddressInput onAddressChange={setJobAddress} />

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="job-type">Job Type</Label>
              <Select>
                <SelectTrigger id="job-type">
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  {jobTypes.map((type) => (
                    <SelectItem key={type} value={type.toLowerCase().replace(/\s+/g, "-")}>
                      {type}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="priority">Priority</Label>
              <Select defaultValue="normal">
                <SelectTrigger id="priority">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="normal">Normal</SelectItem>
                  <SelectItem value="high">High</SelectItem>
                  <SelectItem value="rush">Rush</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="status">Job Status</Label>
            <Select defaultValue="pending">
              <SelectTrigger id="status">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="pending">Pending</SelectItem>
                <SelectItem value="in-progress">In Progress</SelectItem>
                <SelectItem value="completed">Completed</SelectItem>
                <SelectItem value="canceled">Canceled</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="desired-date">Desired Start Date</Label>
              <Input id="desired-date" type="date" />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="desired-time">Desired Start Time</Label>
              <TimeSelect id="desired-time" placeholder="Select start time" />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="duration">Estimated Duration (hrs)</Label>
              <Input id="duration" type="number" placeholder="3" min="1" max="12" />
            </div>
          </div>

          <div className="grid gap-2">
            <Label>Required Skills</Label>
            <SkillCombobox
              availableSkills={skills}
              selectedSkills={selectedSkills}
              onSkillsChange={setSelectedSkills}
              placeholder="Select or type required skills..."
            />
          </div>

          <div className="grid gap-2">
            <Label htmlFor="notes">Notes (optional)</Label>
            <Textarea id="notes" placeholder="Any special requirements or access instructions..." rows={2} />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={() => onOpenChange(false)}>Create Job</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
