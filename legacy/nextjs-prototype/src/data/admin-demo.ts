export type DemoGroup = {
  id: number;
  name: string;
  adminName: string;
  studentCount: number;
  activeModulesAverage: number;
};

export type DemoStudent = {
  id: number;
  name: string;
  email: string;
  groupId: number;
  completionPercent: number;
};

export const demoGroups: DemoGroup[] = [
  {
    id: 1,
    name: "Cohort A – 2025",
    adminName: "Alex Rivera",
    studentCount: 24,
    activeModulesAverage: 68,
  },
  {
    id: 2,
    name: "CS Freshmen – Fall",
    adminName: "Jordan Lee",
    studentCount: 32,
    activeModulesAverage: 54,
  },
  {
    id: 3,
    name: "Internship Bootcamp – Summer",
    adminName: "Taylor Morgan",
    studentCount: 18,
    activeModulesAverage: 73,
  },
  {
    id: 4,
    name: "PRODUCT Fellows",
    adminName: "Casey Owens",
    studentCount: 21,
    activeModulesAverage: 61,
  },
];

export const demoStudents: DemoStudent[] = [
  {
    id: 1,
    name: "Sam Patel",
    email: "sam.patel@example.com",
    groupId: 1,
    completionPercent: 72,
  },
  {
    id: 2,
    name: "Jamie Chen",
    email: "jamie.chen@example.com",
    groupId: 1,
    completionPercent: 40,
  },
  {
    id: 3,
    name: "Riley Johnson",
    email: "riley.j@example.com",
    groupId: 2,
    completionPercent: 55,
  },
  {
    id: 4,
    name: "Morgan Lee",
    email: "morgan.lee@example.com",
    groupId: 2,
    completionPercent: 30,
  },
  {
    id: 5,
    name: "Taylor Fox",
    email: "taylor.fox@example.com",
    groupId: 3,
    completionPercent: 80,
  },
  {
    id: 6,
    name: "Avery Brooks",
    email: "avery.b@example.com",
    groupId: 4,
    completionPercent: 64,
  },
];
