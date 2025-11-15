import Link from "next/link";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { demoGroups, demoStudents } from "@/data/admin-demo";

export default function AdminDashboardPage() {
  return (
    <div className="bg-background">
      <section className="mx-auto w-full max-w-6xl space-y-6 px-4 py-8 md:py-10">
        <div className="space-y-2">
          <h1 className="text-2xl md:text-3xl font-semibold tracking-tight text-foreground">
            Admin overview
          </h1>
          <p className="max-w-2xl text-sm text-muted-foreground">
            This is the owner view for DualPath. Later, these numbers will come
            from the database, showing how many students and groups are active
            across the platform.
          </p>
        </div>

        <div className="grid gap-4 md:grid-cols-3">
          <Card className="h-full w-full rounded-xl border border-[#0f172a]/15 bg-[#0f172a]/5 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Total students</CardTitle>
              <CardDescription>
                All enrolled students across groups.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-semibold text-foreground">
                {demoStudents.length}
              </p>
            </CardContent>
          </Card>

          <Card className="h-full w-full rounded-xl border border-[#0f172a]/15 bg-[#0f172a]/5 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Student groups</CardTitle>
              <CardDescription>Configured cohorts or classes.</CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-semibold text-foreground">
                {demoGroups.length}
              </p>
            </CardContent>
          </Card>

          <Card className="h-full w-full rounded-xl border border-[#0f172a]/15 bg-[#0f172a]/5 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Avg module completion</CardTitle>
              <CardDescription>
                Rough average across all students.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-semibold text-foreground">
                {Math.round(
                  demoStudents.reduce(
                    (sum, s) => sum + s.completionPercent,
                    0
                  ) / Math.max(demoStudents.length, 1)
                )}
                %
              </p>
            </CardContent>
          </Card>
        </div>

        <div className="space-y-3">
          <div className="flex items-center justify-between gap-2">
            <h2 className="text-lg font-semibold tracking-tight text-foreground">
              Student groups
            </h2>
            <p className="text-xs text-muted-foreground">
              Later, owners will be able to create groups and assign group
              admins here.
            </p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {demoGroups.map((group) => (
              <Card
                key={group.id}
                className="flex h-full w-full flex-col rounded-xl border border-[#0f172a]/15 bg-[#0f172a]/5 shadow-sm backdrop-blur-sm"
              >
                <CardHeader className="space-y-1">
                  <CardTitle className="text-base font-semibold">
                    {group.name}
                  </CardTitle>
                  <CardDescription className="text-xs">
                    Group admin: {group.adminName}
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-3 text-sm text-muted-foreground">
                  <div className="flex items-center justify-between text-xs">
                    <span>Students</span>
                    <span className="font-semibold text-foreground">
                      {group.studentCount}
                    </span>
                  </div>
                  <div className="flex items-center justify-between text-xs">
                    <span>Avg completion</span>
                    <span className="font-semibold text-foreground">
                      {group.activeModulesAverage}%
                    </span>
                  </div>
                  <div className="pt-2">
                    <Link
                      href={`/admin/groups/${group.id}`}
                      className="text-xs font-medium text-[#0f172a] hover:underline"
                    >
                      View group details
                    </Link>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
