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
import { useState } from "react"

interface AddContractorDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function AddContractorDialog({ open, onOpenChange }: AddContractorDialogProps) {
  const { skills } = useSettings()
  const [selectedSkills, setSelectedSkills] = useState<string[]>([])

  const handleSkillToggle = (skill: string) => {
    setSelectedSkills((prev) => (prev.includes(skill) ? prev.filter((s) => s !== skill) : [...prev, skill]))
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Add New Contractor</DialogTitle>
          <DialogDescription>Create a new contractor profile with skills and availability</DialogDescription>
        </DialogHeader>
        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="name">Full Name</Label>
            <Input id="name" placeholder="John Doe" />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="location">Base Location</Label>
            <Input id="location" placeholder="123 Main St, City, State" />
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
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="hours">Working Hours</Label>
              <Input id="hours" placeholder="8:00 AM - 5:00 PM" />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="rating">Initial Rating</Label>
              <Input id="rating" type="number" placeholder="50" min="0" max="100" />
            </div>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={() => onOpenChange(false)}>Add Contractor</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
