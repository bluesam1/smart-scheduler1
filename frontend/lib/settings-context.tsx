"use client"

import type React from "react"
import { createContext, useContext, useState, useEffect } from "react"

interface SettingsContextType {
  jobTypes: string[]
  skills: string[]
  addJobType: (type: string) => void
  removeJobType: (type: string) => void
  updateJobType: (oldType: string, newType: string) => void
  addSkill: (skill: string) => void
  removeSkill: (skill: string) => void
  updateSkill: (oldSkill: string, newSkill: string) => void
}

const SettingsContext = createContext<SettingsContextType | undefined>(undefined)

// Default values
const DEFAULT_JOB_TYPES = [
  "Hardwood Installation",
  "Tile Installation",
  "Carpet Installation",
  "Laminate Installation",
  "HVAC Repair",
  "Electrical Inspection",
  "Repair/Maintenance",
]

const DEFAULT_SKILLS = [
  "Hardwood Installation",
  "Tile",
  "Laminate",
  "Carpet",
  "Finishing",
  "HVAC",
  "Electrical",
  "Plumbing",
]

export function SettingsProvider({ children }: { children: React.ReactNode }) {
  const [jobTypes, setJobTypes] = useState<string[]>(DEFAULT_JOB_TYPES)
  const [skills, setSkills] = useState<string[]>(DEFAULT_SKILLS)

  // Load from localStorage on mount
  useEffect(() => {
    const savedJobTypes = localStorage.getItem("smartscheduler_job_types")
    const savedSkills = localStorage.getItem("smartscheduler_skills")

    if (savedJobTypes) setJobTypes(JSON.parse(savedJobTypes))
    if (savedSkills) setSkills(JSON.parse(savedSkills))
  }, [])

  // Save to localStorage when changed
  useEffect(() => {
    localStorage.setItem("smartscheduler_job_types", JSON.stringify(jobTypes))
  }, [jobTypes])

  useEffect(() => {
    localStorage.setItem("smartscheduler_skills", JSON.stringify(skills))
  }, [skills])

  const addJobType = (type: string) => {
    if (type.trim() && !jobTypes.includes(type.trim())) {
      setJobTypes([...jobTypes, type.trim()])
    }
  }

  const removeJobType = (type: string) => {
    setJobTypes(jobTypes.filter((t) => t !== type))
  }

  const updateJobType = (oldType: string, newType: string) => {
    setJobTypes(jobTypes.map((t) => (t === oldType ? newType.trim() : t)))
  }

  const addSkill = (skill: string) => {
    if (skill.trim() && !skills.includes(skill.trim())) {
      setSkills([...skills, skill.trim()])
    }
  }

  const removeSkill = (skill: string) => {
    setSkills(skills.filter((s) => s !== skill))
  }

  const updateSkill = (oldSkill: string, newSkill: string) => {
    setSkills(skills.map((s) => (s === oldSkill ? newSkill.trim() : s)))
  }

  return (
    <SettingsContext.Provider
      value={{
        jobTypes,
        skills,
        addJobType,
        removeJobType,
        updateJobType,
        addSkill,
        removeSkill,
        updateSkill,
      }}
    >
      {children}
    </SettingsContext.Provider>
  )
}

export function useSettings() {
  const context = useContext(SettingsContext)
  if (!context) {
    throw new Error("useSettings must be used within a SettingsProvider")
  }
  return context
}
