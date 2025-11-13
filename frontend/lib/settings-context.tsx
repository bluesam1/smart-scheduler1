"use client"

import type React from "react"
import { createContext, useContext, useState, useEffect, useCallback } from "react"
import { useAuth } from "@/lib/auth/auth-context"
import { createAuthenticatedFetch, API_BASE_URL } from "@/lib/api/api-client-config"
import { toast } from "sonner"

interface SettingsContextType {
  jobTypes: string[]
  skills: string[]
  isLoading: boolean
  error: string | null
  addJobType: (type: string) => Promise<void>
  removeJobType: (type: string) => Promise<void>
  updateJobType: (oldType: string, newType: string) => Promise<void>
  addSkill: (skill: string) => Promise<void>
  removeSkill: (skill: string) => Promise<void>
  updateSkill: (oldSkill: string, newSkill: string) => Promise<void>
  refreshData: () => Promise<void>
}

const SettingsContext = createContext<SettingsContextType | undefined>(undefined)

// API response types
interface JobTypesResponse {
  jobTypes: string[]
}

interface SkillsResponse {
  skills: string[]
}

interface AddJobTypeRequest {
  jobType: string
}

interface AddJobTypeResponse {
  jobType: string
}

interface UpdateJobTypeRequest {
  oldValue: string
  newValue: string
}

interface UpdateSkillRequest {
  oldValue: string
  newValue: string
}

interface AddSkillRequest {
  skill: string
}

export function SettingsProvider({ children }: { children: React.ReactNode }) {
  const { getTokenProvider } = useAuth()
  const [jobTypes, setJobTypes] = useState<string[]>([])
  const [skills, setSkills] = useState<string[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Create authenticated fetch function
  const getAuthenticatedFetch = useCallback(() => {
    const tokenProvider = getTokenProvider()
    if (!tokenProvider) {
      return null
    }
    return createAuthenticatedFetch(tokenProvider)
  }, [getTokenProvider])

  // Fetch job types from API
  const fetchJobTypes = useCallback(async () => {
    const authenticatedFetch = getAuthenticatedFetch()
    if (!authenticatedFetch) {
      setError("Authentication required")
      return
    }

    try {
      const response = await authenticatedFetch(`${API_BASE_URL}/api/settings/job-types`, {
        method: "GET",
      })

      if (!response.ok) {
        if (response.status === 401) {
          throw new Error("Authentication required. Please log in.")
        }
        throw new Error(`Failed to fetch job types: ${response.statusText}`)
      }

      const data: JobTypesResponse = await response.json()
      setJobTypes(data.jobTypes || [])
      setError(null)
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "Failed to fetch job types"
      setError(errorMessage)
      console.error("Error fetching job types:", err)
    }
  }, [getAuthenticatedFetch])

  // Fetch skills from API
  const fetchSkills = useCallback(async () => {
    const authenticatedFetch = getAuthenticatedFetch()
    if (!authenticatedFetch) {
      setError("Authentication required")
      return
    }

    try {
      const response = await authenticatedFetch(`${API_BASE_URL}/api/settings/skills`, {
        method: "GET",
      })

      if (!response.ok) {
        if (response.status === 401) {
          throw new Error("Authentication required. Please log in.")
        }
        throw new Error(`Failed to fetch skills: ${response.statusText}`)
      }

      const data: SkillsResponse = await response.json()
      setSkills(data.skills || [])
      setError(null)
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "Failed to fetch skills"
      setError(errorMessage)
      console.error("Error fetching skills:", err)
    }
  }, [getAuthenticatedFetch])

  // Load data on mount and when auth changes
  const refreshData = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    await Promise.all([fetchJobTypes(), fetchSkills()])
    setIsLoading(false)
  }, [fetchJobTypes, fetchSkills])

  useEffect(() => {
    refreshData()
  }, [refreshData])

  const addJobType = useCallback(
    async (type: string) => {
      if (!type.trim()) {
        toast.error("Job type cannot be empty")
        return
      }

      const authenticatedFetch = getAuthenticatedFetch()
      if (!authenticatedFetch) {
        toast.error("Authentication required")
        return
      }

      try {
        const request: AddJobTypeRequest = { jobType: type.trim() }
        const response = await authenticatedFetch(`${API_BASE_URL}/api/settings/job-types`, {
          method: "POST",
          body: JSON.stringify(request),
        })

        if (!response.ok) {
          if (response.status === 409) {
            const errorData = await response.json().catch(() => ({}))
            throw new Error(errorData.message || "Job type already exists")
          }
          if (response.status === 401) {
            throw new Error("Authentication required. Please log in.")
          }
          throw new Error(`Failed to add job type: ${response.statusText}`)
        }

        await fetchJobTypes()
        toast.success("Job type added successfully")
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to add job type"
        toast.error(errorMessage)
        console.error("Error adding job type:", err)
      }
    },
    [getAuthenticatedFetch, fetchJobTypes]
  )

  const removeJobType = useCallback(
    async (type: string) => {
      const authenticatedFetch = getAuthenticatedFetch()
      if (!authenticatedFetch) {
        toast.error("Authentication required")
        return
      }

      try {
        const response = await authenticatedFetch(
          `${API_BASE_URL}/api/settings/job-types?jobType=${encodeURIComponent(type)}`,
          {
            method: "DELETE",
          }
        )

        if (!response.ok) {
          if (response.status === 409) {
            const errorData = await response.json().catch(() => ({}))
            throw new Error(errorData.message || "Job type is in use and cannot be deleted")
          }
          if (response.status === 404) {
            throw new Error("Job type not found")
          }
          if (response.status === 401) {
            throw new Error("Authentication required. Please log in.")
          }
          throw new Error(`Failed to remove job type: ${response.statusText}`)
        }

        await fetchJobTypes()
        toast.success("Job type removed successfully")
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to remove job type"
        toast.error(errorMessage)
        console.error("Error removing job type:", err)
      }
    },
    [getAuthenticatedFetch, fetchJobTypes]
  )

  const updateJobType = useCallback(
    async (oldType: string, newType: string) => {
      if (!newType.trim()) {
        toast.error("Job type cannot be empty")
        return
      }

      const authenticatedFetch = getAuthenticatedFetch()
      if (!authenticatedFetch) {
        toast.error("Authentication required")
        return
      }

      try {
        const request: UpdateJobTypeRequest = {
          oldValue: oldType,
          newValue: newType.trim(),
        }
        const response = await authenticatedFetch(`${API_BASE_URL}/api/settings/job-types`, {
          method: "PUT",
          body: JSON.stringify(request),
        })

        if (!response.ok) {
          if (response.status === 404) {
            throw new Error("Job type not found")
          }
          if (response.status === 401) {
            throw new Error("Authentication required. Please log in.")
          }
          const errorData = await response.json().catch(() => ({}))
          throw new Error(errorData.message || `Failed to update job type: ${response.statusText}`)
        }

        await fetchJobTypes()
        toast.success("Job type updated successfully")
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to update job type"
        toast.error(errorMessage)
        console.error("Error updating job type:", err)
      }
    },
    [getAuthenticatedFetch, fetchJobTypes]
  )

  const addSkill = useCallback(
    async (skill: string) => {
      if (!skill.trim()) {
        toast.error("Skill cannot be empty")
        return
      }

      const authenticatedFetch = getAuthenticatedFetch()
      if (!authenticatedFetch) {
        toast.error("Authentication required")
        return
      }

      try {
        const request: AddSkillRequest = { skill: skill.trim() }
        const response = await authenticatedFetch(`${API_BASE_URL}/api/settings/skills`, {
          method: "POST",
          body: JSON.stringify(request),
        })

        if (!response.ok) {
          if (response.status === 409) {
            const errorData = await response.json().catch(() => ({}))
            throw new Error(errorData.message || "Skill already exists")
          }
          if (response.status === 401) {
            throw new Error("Authentication required. Please log in.")
          }
          throw new Error(`Failed to add skill: ${response.statusText}`)
        }

        await fetchSkills()
        toast.success("Skill added successfully")
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to add skill"
        toast.error(errorMessage)
        console.error("Error adding skill:", err)
      }
    },
    [getAuthenticatedFetch, fetchSkills]
  )

  const removeSkill = useCallback(
    async (skill: string) => {
      const authenticatedFetch = getAuthenticatedFetch()
      if (!authenticatedFetch) {
        toast.error("Authentication required")
        return
      }

      try {
        const response = await authenticatedFetch(
          `${API_BASE_URL}/api/settings/skills?skill=${encodeURIComponent(skill)}`,
          {
            method: "DELETE",
          }
        )

        if (!response.ok) {
          if (response.status === 409) {
            const errorData = await response.json().catch(() => ({}))
            throw new Error(errorData.message || "Skill is in use and cannot be deleted")
          }
          if (response.status === 404) {
            throw new Error("Skill not found")
          }
          if (response.status === 401) {
            throw new Error("Authentication required. Please log in.")
          }
          throw new Error(`Failed to remove skill: ${response.statusText}`)
        }

        await fetchSkills()
        toast.success("Skill removed successfully")
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to remove skill"
        toast.error(errorMessage)
        console.error("Error removing skill:", err)
      }
    },
    [getAuthenticatedFetch, fetchSkills]
  )

  const updateSkill = useCallback(
    async (oldSkill: string, newSkill: string) => {
      if (!newSkill.trim()) {
        toast.error("Skill cannot be empty")
        return
      }

      const authenticatedFetch = getAuthenticatedFetch()
      if (!authenticatedFetch) {
        toast.error("Authentication required")
        return
      }

      try {
        const request: UpdateSkillRequest = {
          oldValue: oldSkill,
          newValue: newSkill.trim(),
        }
        const response = await authenticatedFetch(`${API_BASE_URL}/api/settings/skills`, {
          method: "PUT",
          body: JSON.stringify(request),
        })

        if (!response.ok) {
          if (response.status === 404) {
            throw new Error("Skill not found")
          }
          if (response.status === 401) {
            throw new Error("Authentication required. Please log in.")
          }
          const errorData = await response.json().catch(() => ({}))
          throw new Error(errorData.message || `Failed to update skill: ${response.statusText}`)
        }

        await fetchSkills()
        toast.success("Skill updated successfully")
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to update skill"
        toast.error(errorMessage)
        console.error("Error updating skill:", err)
      }
    },
    [getAuthenticatedFetch, fetchSkills]
  )

  return (
    <SettingsContext.Provider
      value={{
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
        refreshData,
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
