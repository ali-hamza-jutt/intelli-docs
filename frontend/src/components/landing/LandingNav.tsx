"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { cn } from "@/lib/cn";
import { Logo } from "@/components/layout/Logo";
import { ButtonLink } from "@/components/ui/Button";
import { LANDING_NAV } from "@/lib/landing-data";

/** Sticky marketing header that gains a border once the page scrolls. */
export function LandingNav() {
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 8);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <header
      className={cn(
        "sticky top-0 z-40 border-b backdrop-blur-lg transition-colors duration-200",
        scrolled ? "border-line bg-surface/85" : "border-transparent bg-surface",
      )}
    >
      <div className="mx-auto flex h-17 max-w-[1200px] items-center gap-8 px-6">
        <Logo />

        <nav aria-label="Main" className="ml-auto hidden gap-1 md:flex">
          {LANDING_NAV.map((item) => (
            <Link
              key={item.label}
              href={item.href}
              className="rounded-control px-3 py-2 text-base font-medium text-muted transition-colors hover:bg-surface-alt hover:text-ink"
            >
              {item.label}
            </Link>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-2 md:ml-0">
          <ButtonLink href="/login" variant="ghost" className="font-medium">
            Sign In
          </ButtonLink>
          <ButtonLink href="/register">Get Started</ButtonLink>
        </div>
      </div>
    </header>
  );
}
