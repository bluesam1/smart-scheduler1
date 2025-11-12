import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Users, Briefcase, Clock, TrendingUp } from "lucide-react"

export function StatsOverview() {
  const stats = [
    {
      title: "Active Contractors",
      value: "24",
      change: "+2 today",
      icon: Users,
    },
    {
      title: "Pending Jobs",
      value: "8",
      change: "3 unassigned",
      icon: Briefcase,
    },
    {
      title: "Avg Assignment Time",
      value: "4.2m",
      change: "-20% this week",
      icon: Clock,
    },
    {
      title: "Utilization Rate",
      value: "76%",
      change: "+5% this week",
      icon: TrendingUp,
    },
  ]

  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
      {stats.map((stat) => (
        <Card key={stat.title}>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{stat.title}</CardTitle>
            <stat.icon className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stat.value}</div>
            <p className="text-xs text-muted-foreground">{stat.change}</p>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
