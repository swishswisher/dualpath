export function SiteFooter() {
  const year = new Date().getFullYear();

  return (
    <footer className="border-t border-white/10 bg-[#0f172a]/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl flex-col gap-2 px-4 py-6 text-xs text-white/80 md:flex-row md:items-center md:justify-between">
        <p>
          &copy; {year} <span className="font-semibold text-white">DualPath</span>. All rights reserved.
        </p>
        <p className="text-[11px] text-white/70">
          AI-powered internship prep for students who take ownership.
        </p>
      </div>
    </footer>
  );
}
