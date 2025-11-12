import { DashboardShell } from "@/components/dashboard-shell"
import { ContractorsTable } from "@/components/contractors/contractors-table"

export default function ContractorsPage() {
  return (
    <DashboardShell>
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold">Contractors</h1>
          <p className="text-muted-foreground">Manage contractor information and schedules</p>
        </div>
        <ContractorsTable />
      </div>
    </DashboardShell>
  )
}
