import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

export default function AdminDashboardPage() {
  return (
    <div className="bg-background">
      <section className="mx-auto w-full max-w-6xl space-y-6 px-4 py-8">
        <div className="space-y-2">
          <h1 className="text-2xl md:text-3xl font-semibold tracking-tight text-foreground">
            Admin Dashboard
          </h1>
          <p className="max-w-xl text-sm text-muted-foreground">
            Monitor student progress, AI usage, and content performance.
          </p>
        </div>

        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          <Card className="h-full w-full border border-[#0f172a]/15 bg-[#0f172a]/3 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Student Overview</CardTitle>
              <CardDescription>
                Snapshot of cohort engagement and completion.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <p>
                <span className="font-semibold text-foreground">Active students:</span>{" "}
                <span className="text-sm text-muted-foreground">42</span>
              </p>
              <p>
                <span className="font-semibold text-foreground">Avg module completion:</span>{" "}
                <span className="text-sm text-muted-foreground">68%</span>
              </p>
              <p>
                <span className="font-semibold text-foreground">Needs follow-up:</span>{" "}
                <span className="text-sm text-amber-600 font-semibold">7</span>
              </p>
              {/* TODO: Connect to real DB analytics. */}
            </CardContent>
          </Card>

          <Card className="h-full w-full border border-[#0f172a]/15 bg-[#0f172a]/3 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>AI Stats</CardTitle>
              <CardDescription>
                See how the personalized module is being used.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <p>
                <span className="font-semibold text-foreground">
                  Module 6 generations:
                </span>{" "}
                <span className="text-sm text-muted-foreground">89</span>
              </p>
              <p>
                <span className="font-semibold text-foreground">
                  Avg score lift after Module 6:
                </span>{" "}
                <span className="text-emerald-600 font-semibold">+12%</span>
              </p>
              <p>
                <span className="font-semibold text-foreground">
                  Avg reflections completed:
                </span>{" "}
                <span className="text-sm text-muted-foreground">3.2 per student</span>
              </p>
              {/* TODO: Connect to AI usage events. */}
            </CardContent>
          </Card>

          <Card className="h-full w-full border border-[#0f172a]/15 bg-[#0f172a]/3 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Content Management</CardTitle>
              <CardDescription>
                Keep modules and lessons up to date.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-2">
              <p className="text-sm text-muted-foreground">
                Batch updates to modules, lessons, and practice tasks without
                leaving this dashboard. Review feedback and push changes with
                confidence.
              </p>
              <p className="text-xs text-muted-foreground">
                Content tools will live here in a future phase.
              </p>
              {/* TODO: Link to admin content tooling once available. */}
            </CardContent>
          </Card>
        </div>
      </section>
    </div>
  );
}
