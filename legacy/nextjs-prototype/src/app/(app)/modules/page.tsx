import Link from "next/link";
import { ArrowRight } from "lucide-react";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { demoModules } from "@/data/modules";

export default function ModulesPage() {
  return (
    <div className="bg-background">
      <section className="mx-auto w-full max-w-6xl space-y-6 px-4 py-8 md:py-10">
        <div className="space-y-2 text-center md:text-left">
          <h1 className="text-2xl md:text-3xl font-semibold tracking-tight text-foreground">
            All Modules
          </h1>
          <p className="max-w-2xl text-sm text-muted-foreground">
            Modules 1–5 cover the core discipline and outreach skills every
            student needs. Module 6 is an AI-personalized sprint that targets
            your specific weak spots based on reflections and data.
          </p>
        </div>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {demoModules.map((module) => (
            <Link key={module.id} href={`/modules/${module.slug}`} className="block">
              <Card className="flex h-full w-full flex-col rounded-xl border border-[#0f172a]/15 bg-[#0f172a]/5 shadow-sm backdrop-blur-sm transition-shadow hover:shadow-md">
                <CardHeader>
                  <p className="text-[11px] font-semibold uppercase tracking-wide text-primary">
                    {module.id === 6 ? "AI Module" : `Module ${module.id}`}
                  </p>
                  <CardTitle>{module.title}</CardTitle>
                  <CardDescription>{module.summary}</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {module.focusAreas?.map((area) => (
                      <span
                        key={area}
                        className="rounded-full bg-white/60 px-2.5 py-1 text-[11px] font-medium text-[#0f172a]/80"
                      >
                        {area}
                      </span>
                    ))}
                  </div>
                  <div className="mt-4 inline-flex items-center gap-1 text-sm text-primary">
                    View module
                    <ArrowRight className="h-3.5 w-3.5" />
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
}
