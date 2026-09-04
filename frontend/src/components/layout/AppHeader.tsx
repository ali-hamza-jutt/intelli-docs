"use client";

import Link from "next/link";
import { SearchInput } from "@/components/ui/Field";
import { IconButton } from "@/components/ui/IconButton";
import { Avatar } from "@/components/ui/Avatar";
import { LogoMark } from "./Logo";
import { CURRENT_USER } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";

export function AppHeader({
  title,
  /** Chat provides its own in-pane search, so the header search is hidden there. */
  showSearch = true,
}: {
  title: string;
  showSearch?: boolean;
}) {
  const toast = useToast();

  return (
    <header className="sticky top-0 z-30 flex h-16 flex-none items-center gap-3 border-b border-line bg-surface/85 px-5 backdrop-blur-md">
      <span className="md:hidden">
        <LogoMark size="sm" />
      </span>

      <h1 className="m-0 text-md font-semibold tracking-[-0.01em]">{title}</h1>

      <div className="flex-1" />

      {showSearch && (
        <SearchInput
          aria-label="Search"
          placeholder="Search documents…"
          className="hidden w-[280px] lg:flex"
        />
      )}

      <IconButton
        icon="bell"
        label="Notifications"
        tone="bordered"
        onClick={() => toast("No new notifications")}
      />

      <Link href="/settings" aria-label="Account" className="rounded-full">
        <Avatar initials={CURRENT_USER.initials} />
      </Link>
    </header>
  );
}
