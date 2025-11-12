import { DashboardShell } from "@/components/dashboard-shell"
import { JobQueue } from "@/components/jobs/job-queue"
import { StatsOverview } from "@/components/stats-overview"
import { ActivityFeed } from "@/components/activity-feed"

export default function DashboardPage() {
  return (
    <DashboardShell>
      <div className="flex flex-col gap-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-semibold tracking-tight text-balance">SmartScheduler</h1>
            <p className="text-muted-foreground text-pretty">Intelligent contractor scheduling and job management</p>
          </div>
        </div>

        <StatsOverview />

        <div className="grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-2">
            <JobQueue />
          </div>

          <div className="lg:col-span-1">
            <ActivityFeed />
          </div>
        </div>
      </div>
    </DashboardShell>
  )
}
