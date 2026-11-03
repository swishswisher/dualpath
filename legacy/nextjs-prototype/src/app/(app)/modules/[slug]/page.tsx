import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, CheckCircle2 } from "lucide-react";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { demoModules } from "@/data/modules";

interface ModulePageProps {
  params: { slug: string };
}

export default function ModuleDetailPage({ params }: ModulePageProps) {
  const module = demoModules.find((m) => m.slug === params.slug);

  if (!module) {
    notFound();
  }

  const isAiModule = module.id === 6;

  return (
    <div className="bg-background">
      <section className="mx-auto w-full max-w-6xl space-y-8 px-4 py-8 md:py-10">
        <div className="flex items-center justify-between gap-3">
          <Button
            asChild
            variant="outline"
            size="sm"
            className="border-[#0f172a]/30 text-[#0f172a] hover:bg-[#0f172a]/10"
          >
            <Link href="/modules">
              <ArrowLeft className="mr-1 h-4 w-4" />
              All modules
            </Link>
          </Button>
          <span className="text-xs font-medium text-muted-foreground">
            {isAiModule ? "AI-personalized module" : "Core module"}
          </span>
        </div>

        <div className="space-y-3">
          <h1 className="text-2xl md:text-3xl font-semibold tracking-tight text-foreground">
            {module.title}
          </h1>
          <p className="max-w-2xl text-sm text-muted-foreground">
            {module.summary}
          </p>
          <div className="flex flex-wrap items-center gap-2 text-xs">
            <span className="inline-flex items-center rounded-full bg-[#0f172a]/5 px-3 py-1 font-semibold uppercase tracking-wide text-[#0f172a]">
              {isAiModule ? "AI Module" : `Module ${module.id}`}
            </span>
            <span className="inline-flex items-center rounded-full bg-white/70 px-3 py-1 text-[11px] font-medium text-[#0f172a]/80">
              Estimated time: 30–60 minutes
            </span>
          </div>
        </div>

        <div className="grid gap-6 md:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]">
          <Card className="w-full rounded-xl border border-[#0f172a]/15 bg-[#0f172a]/5 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>What you&apos;ll work on</CardTitle>
              <CardDescription>
                This module is part of your path to landing internships faster
                through practical, repeatable skills.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3 text-sm text-muted-foreground">
              <p>
                Later, this section will pull structured content from the
                database: lessons, examples, and practice tasks specific to this
                module.
              </p>
              <ul className="space-y-2">
                {module.focusAreas?.map((area) => (
                  <li key={area} className="flex items-start gap-2">
                    <CheckCircle2 className="mt-0.5 h-4 w-4 text-[#0f172a]" />
                    <span>{area}</span>
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>

          <Card className="w-full rounded-xl border border-[#0f172a]/15 bg-[#0f172a]/5 shadow-sm backdrop-blur-sm">
            <CardHeader>
              <CardTitle>Progress & reflections</CardTitle>
              <CardDescription>
                For now, this is a static scaffold. In the MVP, it will show
                your saved progress and reflection entries.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4 text-sm text-muted-foreground">
              <div>
                <p className="font-medium text-foreground">
                  Completion status
                </p>
                <p className="text-xs text-muted-foreground">
                  Later this will update based on your real activity (lessons
                  completed, exercises submitted).
                </p>
              </div>
              <div>
                <p className="font-medium text-foreground">
                  Reflection prompt
                </p>
                <p className="text-xs text-muted-foreground">
                  In the MVP, you&apos;ll be able to answer reflection prompts
                  here and the AI module will use them to personalize Module 6.
                </p>
              </div>
              <Button
                variant="default"
                size="sm"
                className="bg-[#0f172a] text-white hover:bg-[#0f172a]/90"
                disabled
              >
                Mark lesson complete (coming soon)
              </Button>
            </CardContent>
          </Card>
        </div>
      </section>
    </div>
  );
}
