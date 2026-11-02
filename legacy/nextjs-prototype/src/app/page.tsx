import Link from "next/link";
import { ArrowRight } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { demoModules } from "@/data/modules";

const progressRows = [
  {
    title: "Discipline & Ownership",
    status: "On track",
    width: "w-4/5",
  },
  {
    title: "Internship Skills",
    status: "In progress",
    width: "w-2/3",
  },
  {
    title: "AI Personalized Module",
    status: "Queued",
    width: "w-1/3",
  },
];

export default function HomePage() {
  return (
    <div className="bg-background">
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-10 px-4 py-12 md:flex-row md:items-center md:py-16">
        <div className="flex-1 space-y-5 md:max-w-xl">
          <p className="inline-flex items-center gap-2 rounded-full bg-secondary px-3 py-1 text-xs font-semibold uppercase tracking-wide text-primary">
            Launch internships faster
          </p>
          <div className="space-y-4">
            <h1 className="mb-2 text-3xl font-semibold tracking-tight text-foreground md:text-5xl">
              DualPath: AI-Powered Internship Launchpad
            </h1>
            <p className="max-w-xl text-sm text-muted-foreground md:text-base">
              Course + accountability system + AI coach helping high school and
              college students build real-world skills, ship standout artifacts,
              and land internships with conviction.
            </p>
          </div>
          <div className="flex flex-col items-stretch gap-3 pt-2 sm:flex-row sm:items-center">
            <Button
              asChild
              size="lg"
              variant="default"
              className="w-full gap-2 border border-white/20 bg-[#0f172a] text-white shadow-sm backdrop-blur-sm hover:bg-[#0c1320] sm:w-auto"
            >
              <Link href="/dashboard">
                Go to Student Portal
                <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
            <Button
              asChild
              size="lg"
              variant="outline"
              className="w-full border-[#0f172a] bg-white/80 text-[#0f172a] hover:bg-white sm:w-auto"
            >
              <Link href="/admin">View Admin Dashboard</Link>
            </Button>
          </div>
        </div>

        <div className="mx-auto w-full flex-1 md:max-w-none">
          <Card className="w-full border border-[#0f172a]/20 bg-[#0f172a]/5 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardDescription className="text-[11px] font-semibold uppercase tracking-wide text-primary">
                Student preview
              </CardDescription>
              <CardTitle>See your progress at a glance</CardTitle>
              <p className="text-sm text-muted-foreground">
                Live progress, reflections, and the next AI module—centralized so
                students never wonder what&apos;s next.
              </p>
            </CardHeader>
            <CardContent className="space-y-4">
              {progressRows.map((row) => (
                <div key={row.title} className="space-y-2">
                  <div className="flex flex-col gap-1 rounded-2xl border border-primary/15 bg-background/80 px-3 py-2 text-sm font-medium text-foreground sm:flex-row sm:items-center sm:justify-between">
                    <span className="text-balance">{row.title}</span>
                    <span
                      className={`text-xs ${
                        row.status === "On track"
                          ? "text-emerald-600"
                          : row.status === "In progress"
                            ? "text-amber-600"
                            : "text-slate-500"
                      }`}
                    >
                      {row.status}
                    </span>
                  </div>
                  <div className="h-2 w-full rounded-full bg-secondary">
                    <div className={`h-2 rounded-full bg-primary ${row.width}`} />
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        </div>
      </section>

      <section className="mx-auto w-full max-w-6xl space-y-6 px-4 pb-16 pt-8">
        <div className="space-y-2">
          <p className="inline-block rounded-full bg-secondary px-3 py-1 text-xs font-semibold uppercase tracking-wide text-primary">
            Modules overview
          </p>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground md:text-3xl">
            Structured sprints + AI personalization
          </h2>
          <p className="max-w-2xl text-sm text-muted-foreground">
            Core discipline and outreach sprints build fundamentals, then our AI
            module layers personalized drills to close skill gaps.
          </p>
        </div>
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {demoModules.map((module) => (
            <Card
              key={module.id}
              className="flex h-full w-full flex-col border border-[#0f172a]/15 bg-[#0f172a]/3 shadow-sm transition-shadow hover:shadow-md"
            >
              <CardHeader>
                <p className="text-[11px] font-semibold uppercase tracking-wide text-primary">
                  {module.index === 6 ? "AI Module" : `Module ${module.index}`}
                </p>
                <CardTitle className="text-balance">{module.title}</CardTitle>
                <CardDescription className="text-balance">
                  {module.summary}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-2">
                  {module.focusAreas.map((area) => (
                    <span
                      key={area}
                      className="rounded-full bg-white/40 px-2.5 py-1 text-[11px] font-medium text-[#0f172a]"
                    >
                      {area}
                    </span>
                  ))}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </section>
    </div>
  );
}
