/**
 * Recommendations API utilities
 * Functions for fetching and recalculating contractor recommendations
 */

import { formatErrorForDisplay } from "./error-handling"

export interface RecommendationRequest {
  jobId: string
  desiredDate: string
  maxResults?: number
}

export interface RecommendationResponse {
  requestId: string
  jobId: string
  recommendations: any[]
  configVersion: number
  generatedAt: string
}

/**
 * Fetch/recalculate recommendations for a job
 * This triggers a fresh calculation on the backend
 * 
 * @param request - The recommendation request parameters
 * @param getToken - Function to get the auth token
 * @returns Promise with the recommendation response
 */
export async function fetchRecommendations(
  request: RecommendationRequest,
  getToken: () => string | null
): Promise<RecommendationResponse> {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5004"
  const token = getToken()
  
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  }
  
  if (token) {
    headers["Authorization"] = `Bearer ${token}`
  }

  console.log("[Recommendations API] Requesting recommendations for job:", request.jobId, "date:", request.desiredDate)
  
  const response = await fetch(`${apiUrl}/api/recommendations`, {
    method: "POST",
    headers,
    body: JSON.stringify({
      jobId: request.jobId,
      desiredDate: request.desiredDate,
      maxResults: request.maxResults || 10,
    }),
  })

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({ 
      message: "Failed to fetch recommendations" 
    }))
    throw new Error(errorData.message || `HTTP ${response.status}`)
  }

  const data: RecommendationResponse = await response.json()
  console.log("[Recommendations API] Received", data.recommendations.length, "recommendations")
  
  return data
}

/**
 * Trigger a recommendation recalculation for a job
 * This is a convenience wrapper around fetchRecommendations
 * 
 * @param jobId - The job ID
 * @param desiredDate - The desired date (YYYY-MM-DD format)
 * @param getToken - Function to get the auth token
 * @returns Promise with the recommendation response
 */
export async function recalculateRecommendations(
  jobId: string,
  desiredDate: string,
  getToken: () => string | null
): Promise<RecommendationResponse> {
  console.log("[Recommendations API] Triggering recalculation for job:", jobId)
  
  return fetchRecommendations(
    {
      jobId,
      desiredDate,
      maxResults: 10,
    },
    getToken
  )
}

/**
 * Get recommendations with error handling and toast notifications
 * 
 * @param jobId - The job ID
 * @param desiredDate - The desired date (YYYY-MM-DD format)
 * @param getToken - Function to get the auth token
 * @param onSuccess - Callback on success
 * @param onError - Callback on error
 */
export async function getRecommendationsWithFeedback(
  jobId: string,
  desiredDate: string,
  getToken: () => string | null,
  onSuccess?: (data: RecommendationResponse) => void,
  onError?: (error: string) => void
): Promise<void> {
  try {
    const data = await recalculateRecommendations(jobId, desiredDate, getToken)
    onSuccess?.(data)
  } catch (err) {
    const errorMessage = formatErrorForDisplay(err)
    console.error("[Recommendations API] Error:", err)
    onError?.(errorMessage)
  }
}

