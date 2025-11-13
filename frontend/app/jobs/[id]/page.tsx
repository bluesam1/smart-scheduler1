import { DashboardShell } from "@/components/dashboard-shell"
import { JobDetailsView } from "@/components/jobs/job-details-view"

export default async function JobDetailPage({ 
  params 
}: { 
  params: Promise<{ id: string }> | { id: string } 
}) {
  // Handle both Promise and direct params (Next.js 15+ compatibility)
  const resolvedParams = params instanceof Promise ? await params : params
  
  return (
    <DashboardShell>
      <JobDetailsView jobId={resolvedParams.id} />
    </DashboardShell>
  )
}
