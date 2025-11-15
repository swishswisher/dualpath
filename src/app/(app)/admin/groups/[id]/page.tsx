import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { demoGroups, demoStudents } from "@/data/admin-demo";

interface GroupPageProps {
  params: { id: string };
}

export default function AdminGroupDetailPage({ params }: GroupPageProps) {
  const groupId = Number(params.id);
  const group = demoGroups.find((g) => g.id === groupId);

  if (!group) {
    notFound();
  }

  const studentsInGroup = demoStudents.filter((s) => s.groupId === groupId);

  return (
    <div className="bg-background">
      <section className="mx-auto w-full max-w-6xl space-y-6 px-4 py-8 md:py-10">
        <div className="flex items-center justify-between gap-3">
          <Button
            asChild
            variant="outline"
            size="sm"
            className="border-[#0f172a]/30 text-[#0f172a] hover:bg-[#0f172a]/10"
          >
            <Link href="/admin">
              <ArrowLeft className="mr-1 h-4 w-4" />
              Back to admin overview
            </Link>
          </Button>
          <span className="text-xs text-muted-foreground">
            Group admin: {group.adminName}
          </span>
        </div>

        <div className="space-y-2">
          <h1 className="text-2xl md:text-3xl font-semibold tracking-tight text-foreground">
            {group.name}
          </h1>
          <p className="max-w-2xl text-sm text-muted-foreground">
            This is a scaffold for group admins. Later, this page will show real
            module progress, attendance, and AI recommendations for this
            specific student group.
          </p>
        </div>

        <div className="grid gap-6 md:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
          <Card className="w-full rounded-xl border border-[#0f172a]/15 bg-[#0f172a]/5 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Group summary</CardTitle>
              <CardDescription>
                High-level numbers for this group. In the MVP these will come
                from the database.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3 text-sm text-muted-foreground">
              <div className="flex items-center justify-between text-xs">
                <span>Students in group</span>
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
              <p className="text-xs text-muted-foreground">
                Later, this card will also surface things like “most common weak
                spot” or “modules where this group gets stuck”.
              </p>
            </CardContent>
          </Card>

          <Card className="w-full rounded-xl border border-[#0f172a]/15 bg-[#0f172a]/5 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Students in this group</CardTitle>
              <CardDescription>
                This table is hard-coded for now. In the MVP it will be powered
                by the database.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-2 text-sm text-muted-foreground">
              {studentsInGroup.length === 0 && (
                <p className="text-xs text-muted-foreground">
                  No students in this group yet.
                </p>
              )}

              {studentsInGroup.length > 0 && (
                <div className="space-y-1">
                  {studentsInGroup.map((student) => (
                    <div
                      key={student.id}
                      className="flex items-center justify-between rounded-lg border border-[#0f172a]/15 bg-background/80 px-3 py-2"
                    >
                      <div className="flex flex-col">
                        <span className="font-medium text-foreground">
                          {student.name}
                        </span>
                        <span className="text-xs text-muted-foreground">
                          {student.email}
                        </span>
                      </div>
                      <span className="text-xs font-semibold text-[#0f172a]/80">
                        {student.completionPercent}%
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </section>
    </div>
  );
}
