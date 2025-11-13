"use client"

import { Suspense } from "react"
import { useSearchParams } from "next/navigation"
import { DashboardShell } from "@/components/dashboard-shell"
import { JobsTable } from "@/components/jobs/jobs-table"
import { JobDetailsView } from "@/components/jobs/job-details-view"

function JobsPageContent() {
  const searchParams = useSearchParams()
  const jobId = searchParams.get("id")

  // If jobId is provided, show job details; otherwise show jobs list
  if (jobId) {
    return (
      <DashboardShell>
        <JobDetailsView jobId={jobId} />
      </DashboardShell>
    )
  }

  return (
    <DashboardShell>
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold">Jobs</h1>
          <p className="text-muted-foreground">Manage and track all jobs</p>
        </div>
        <JobsTable />
      </div>
    </DashboardShell>
  )
}

export default function JobsPage() {
  return (
    <Suspense fallback={
      <DashboardShell>
        <div>Loading...</div>
      </DashboardShell>
    }>
      <JobsPageContent />
    </Suspense>
  )
}
