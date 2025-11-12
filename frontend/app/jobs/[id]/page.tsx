import { DashboardShell } from "@/components/dashboard-shell"
import { JobDetailsView } from "@/components/jobs/job-details-view"

export default function JobDetailPage({ params }: { params: { id: string } }) {
  return (
    <DashboardShell>
      <JobDetailsView jobId={params.id} />
    </DashboardShell>
  )
}
