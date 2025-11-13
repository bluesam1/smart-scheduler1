const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

export interface DemoDataResult {
  contractorsCreated: number;
  jobsCreated: number;
  assignmentsCreated: number;
  auditRecordsCreated: number;
  durationMs: number;
}

export interface CleanupResult {
  contractorsDeleted: number;
  jobsDeleted: number;
  assignmentsDeleted: number;
  auditRecordsDeleted: number;
  eventLogsDeleted: number;
  durationMs: number;
}

export interface DataCounts {
  contractors: number;
  jobs: number;
  assignments: number;
  auditRecords: number;
  eventLogs: number;
}

/**
 * Generates demo data by calling the admin API endpoint.
 * Requires admin role.
 */
export async function generateDemoData(accessToken: string): Promise<DemoDataResult> {
  const response = await fetch(`${API_BASE_URL}/api/admin/demo-data`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`,
    },
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Failed to generate demo data' }));
    throw new Error(error.message || `HTTP error ${response.status}`);
  }

  return response.json();
}

/**
 * Deletes all data from the database.
 * WARNING: This operation cannot be undone!
 * Requires admin role.
 */
export async function deleteAllData(accessToken: string): Promise<CleanupResult> {
  const response = await fetch(`${API_BASE_URL}/api/admin/demo-data`, {
    method: 'DELETE',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`,
    },
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Failed to delete data' }));
    throw new Error(error.message || `HTTP error ${response.status}`);
  }

  return response.json();
}

/**
 * Gets counts of all data in the database.
 * Requires admin role.
 */
export async function getDataCounts(accessToken: string): Promise<DataCounts> {
  const response = await fetch(`${API_BASE_URL}/api/admin/demo-data/counts`, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`,
    },
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Failed to get data counts' }));
    throw new Error(error.message || `HTTP error ${response.status}`);
  }

  return response.json();
}

