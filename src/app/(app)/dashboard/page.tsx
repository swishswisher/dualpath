import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { demoModules } from "@/data/modules";

const moduleTag = (index: number) => {
  if (index === 1) return "Core";
  if (index >= 2 && index <= 5) return "Skills";
  return "AI";
};

export default function StudentDashboardPage() {
  return (
    <div className="bg-background">
      <section className="mx-auto w-full max-w-6xl space-y-6 px-4 py-8">
        <div className="space-y-2">
          <h1 className="text-2xl md:text-3xl font-semibold tracking-tight text-foreground">
            Student Dashboard
          </h1>
          <p className="max-w-xl text-sm text-muted-foreground">
            Track your progress, identify weak spots, and see what you should do
            next to move closer to your internship.
          </p>
        </div>

        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          <Card className="md:col-span-2 h-full w-full border border-[#0f172a]/15 bg-[#0f172a]/3 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Your Modules</CardTitle>
              <CardDescription className="text-xs text-muted-foreground">
                Discipline, outreach skills, then adaptive AI practice
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {demoModules.map((module) => (
                <div
                  key={module.id}
                  className="flex flex-col gap-3 rounded-xl border border-[#0f172a]/25 p-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                      {module.index === 6
                        ? "AI Personalized Module"
                        : `Module ${module.index}`}
                    </p>
                    <p className="text-sm font-medium text-foreground">
                      {module.title}
                    </p>
                  </div>
                  <span
                    className={
                      moduleTag(module.index) === "AI"
                        ? "rounded-full bg-[#0f172a]/80 px-2 py-0.5 text-[11px] text-white"
                        : "rounded-full bg-white/60 px-2 py-0.5 text-[11px] text-[#0f172a]/80"
                    }
                  >
                    {moduleTag(module.index)}
                  </span>
                </div>
              ))}
            </CardContent>
          </Card>

          <Card className="h-full w-full border border-[#0f172a]/15 bg-[#0f172a]/3 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Weak Spots</CardTitle>
              <CardDescription className="text-xs text-muted-foreground">
                Soon, this will show live insights from quizzes and reflections.
              </CardDescription>
            </CardHeader>
            <CardContent className="text-sm text-muted-foreground">
              Coming soon: AI-detected weak areas based on your answers, habits,
              and reflections.
              {/* TODO: Hook to analytics + reflection data. */}
            </CardContent>
          </Card>

          <Card className="h-full w-full border border-[#0f172a]/15 bg-[#0f172a]/3 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Next Action</CardTitle>
              <CardDescription>
                Always know the single most important thing to complete next.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-2">
              <p className="font-medium text-foreground">
                Next up: Complete the reflection exercise in Module 1.
              </p>
              <p className="text-sm text-muted-foreground">
                Capture what you learned, note where you struggled, and log the
                habits you want to strengthen this week.
              </p>
              {/* TODO: Make this dynamic from reminders API. */}
            </CardContent>
          </Card>
        </div>
      </section>
    </div>
  );
}
