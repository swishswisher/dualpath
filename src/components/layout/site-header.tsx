"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import { Menu, X } from "lucide-react";
import { Button } from "@/components/ui/button";

const navLinks = [
  { label: "Home", href: "/" },
  { label: "Student Portal", href: "/dashboard" },
  { label: "Admin", href: "/admin" },
];

export function SiteHeader() {
  const pathname = usePathname();
  const [isOpen, setIsOpen] = useState(false);

  const isActive = (href: string) => {
    if (href === "/") return pathname === "/";
    return pathname.startsWith(href);
  };

  return (
    <>
      {/* Main header bar */}
      <header className="sticky top-0 z-40 w-full border-b border-white/10 bg-[#0f172a]/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3 md:py-4">
          {/* Brand */}
          <Link
            href="/"
            className="text-lg font-semibold tracking-tight text-white"
          >
            DualPath
          </Link>

          {/* Desktop nav */}
          <nav className="hidden items-center gap-6 md:flex">
            {navLinks.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className={
                  "text-sm font-medium transition-colors " +
                  (isActive(item.href)
                    ? "text-white border-b-2 border-white pb-1"
                    : "text-white/70 hover:text-white")
                }
              >
                {item.label}
              </Link>
            ))}
            <Button
              asChild
              size="sm"
              className="ml-2 bg-white text-[#0f172a] hover:bg-slate-100"
            >
              <Link href="/dashboard">Get started</Link>
            </Button>
          </nav>

          {/* Mobile menu button */}
          <button
            type="button"
            className="inline-flex h-9 w-9 items-center justify-center rounded-full border border-white/30 bg-white/10 text-white hover:bg-white/20 md:hidden"
            onClick={() => setIsOpen(true)}
            aria-label="Open navigation"
          >
            <Menu className="h-5 w-5" />
          </button>
        </div>
      </header>

      {isOpen && (
        <div className="fixed inset-0 z-50 flex justify-end bg-black/40 md:hidden">
          <div className="flex h-full w-[90%] max-w-xs flex-col bg-[#0f172a]/95 text-white">
            <div className="flex items-center justify-between border-b border-white/10 px-4 py-3">
              <span className="text-base font-semibold">Menu</span>
              <button
                type="button"
                className="inline-flex h-8 w-8 items-center justify-center rounded-full border border-white/30 bg-white/10 text-white hover:bg-white/20"
                onClick={() => setIsOpen(false)}
                aria-label="Close navigation"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <nav className="flex flex-1 flex-col items-start justify-center gap-6 px-6 text-lg font-medium">
              {navLinks.map((item) => (
                <Link
                  key={item.href + "-mobile"}
                  href={item.href}
                  className={
                    "transition-colors " +
                    (isActive(item.href)
                      ? "text-white"
                      : "text-white/80 hover:text-white")
                  }
                  onClick={() => setIsOpen(false)}
                >
                  {item.label}
                </Link>
              ))}
              <Button
                asChild
                size="lg"
                className="mt-8 bg-white text-[#0f172a] hover:bg-slate-100"
              >
                <Link href="/dashboard" onClick={() => setIsOpen(false)}>
                  Get started
                </Link>
              </Button>
            </nav>
          </div>
        </div>
      )}
    </>
  );
}
