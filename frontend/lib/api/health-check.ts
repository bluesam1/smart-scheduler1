/**
 * Health Check Utility
 * 
 * Utilities for checking API availability and health status.
 */

import { getApiBaseUrl } from './api-client-config';

export interface HealthCheckResult {
  isHealthy: boolean;
  status?: string;
  timestamp?: string;
  error?: string;
}

/**
 * Checks if the API is available and healthy
 * 
 * @returns Promise resolving to health check result
 */
export async function checkApiHealth(): Promise<HealthCheckResult> {
  try {
    const baseUrl = getApiBaseUrl();
    const response = await fetch(`${baseUrl}/health`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
      // Don't include credentials for health check (it's a public endpoint)
    });

    if (!response.ok) {
      return {
        isHealthy: false,
        status: response.statusText,
        error: `Health check failed with status ${response.status}`,
      };
    }

    // Health check endpoint returns plain text "Healthy" or JSON
    const text = await response.text();
    
    return {
      isHealthy: true,
      status: text || 'Healthy',
      timestamp: new Date().toISOString(),
    };
  } catch (error) {
    return {
      isHealthy: false,
      error: error instanceof Error ? error.message : 'Unknown error',
      timestamp: new Date().toISOString(),
    };
  }
}

/**
 * Verifies API availability on app load
 * 
 * This can be called during app initialization to ensure the API is reachable.
 * 
 * @returns Promise resolving to true if API is available, false otherwise
 */
export async function verifyApiAvailability(): Promise<boolean> {
  const result = await checkApiHealth();
  return result.isHealthy;
}

