export type DemoModule = {
  id: string;
  index: number;
  title: string;
  slug: string;
  summary: string;
  focusAreas: string[];
};

export const demoModules: DemoModule[] = [
  {
    id: "module-1",
    index: 1,
    title: "Discipline & Ownership",
    slug: "discipline-ownership",
    summary: "Build habits, take responsibility, and reflect on your actions.",
    focusAreas: ["habits", "reflection", "responsibility"],
  },
  {
    id: "module-2-5",
    index: 2,
    title:
      "Internship Skills (Cold Email, Resume, Networking, Interviews, Negotiation)",
    slug: "internship-skills",
    summary:
      "Learn how to reach out, pitch yourself, and perform strongly in interviews.",
    focusAreas: [
      "cold emails",
      "resume",
      "networking",
      "interviews",
      "negotiation",
    ],
  },
  {
    id: "module-6",
    index: 6,
    title: "AI-Personalized Module",
    slug: "ai-personalized",
    summary:
      "Dynamic, AI-generated training tailored to your weak spots.",
    focusAreas: ["adaptive learning", "personalized practice"],
  },
];
