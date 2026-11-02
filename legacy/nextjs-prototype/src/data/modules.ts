export type DemoModule = {
  id: number;
  index: number;
  title: string;
  slug: string;
  summary: string;
  focusAreas: string[];
};

export const demoModules: DemoModule[] = [
  {
    id: 1,
    index: 1,
    title: "Discipline & Ownership",
    slug: "discipline-ownership",
    summary: "Build habits, take responsibility, and reflect on your actions.",
    focusAreas: ["habits", "reflection", "responsibility"],
  },
  {
    id: 2,
    index: 2,
    title: "Cold Email Fundamentals",
    slug: "cold-email-fundamentals",
    summary:
      "Craft outreach messages that get replies and open doors with professionals.",
    focusAreas: ["copywriting", "email sequencing", "follow-ups"],
  },
  {
    id: 3,
    index: 3,
    title: "Resume & Portfolio Proof",
    slug: "resume-portfolio-proof",
    summary:
      "Translate projects into proof. Build resumes, case studies, and personal sites that convert.",
    focusAreas: ["resume", "portfolio", "case studies"],
  },
  {
    id: 4,
    index: 4,
    title: "Networking & Relationship Building",
    slug: "networking-relationship-building",
    summary:
      "Use events, LinkedIn, and warm introductions to create a steady pipeline of opportunities.",
    focusAreas: ["networking", "linkedin", "events", "mentors"],
  },
  {
    id: 5,
    index: 5,
    title: "Interviewing & Negotiation",
    slug: "interviewing-negotiation",
    summary:
      "Prep for behavioral + technical interviews and learn how to negotiate offers with confidence.",
    focusAreas: ["behavioral", "technical interviews", "negotiation"],
  },
  {
    id: 6,
    index: 6,
    title: "AI-Personalized Practice",
    slug: "ai-personalized-module",
    summary:
      "Dynamic, AI-generated training tailored to your weak spots with instant feedback.",
    focusAreas: ["adaptive learning", "personalized practice"],
  },
];
