"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";
import { Icon, type IconName } from "@/components/ui/Icon";
import { Avatar } from "@/components/ui/Avatar";
import { LogoMark } from "./Logo";
import { CURRENT_USER } from "@/lib/data";

export const PRIMARY_NAV: { href: string; label: string; icon: IconName }[] = [
  { href: "/dashboard", label: "Dashboard", icon: "grid" },
  { href: "/documents", label: "Documents", icon: "fileText" },
  { href: "/chat", label: "Chat", icon: "message" },
  { href: "/collections", label: "Collections", icon: "folder" },
];

const SECONDARY_NAV: { href: string; label: string; icon: IconName }[] = [
  { href: "/settings", label: "Settings", icon: "settings" },
];

/** True for the section root and any nested route beneath it. */
export function isActive(pathname: string, href: string) {
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function AppSidebar({
  open,
  onToggle,
}: {
  open: boolean;
  onToggle: () => void;
}) {
  const pathname = usePathname();

  return (
    <aside
      aria-label="Sidebar"
      className={cn(
        "sticky top-0 hidden h-screen flex-none flex-col border-r border-line bg-surface transition-[width] duration-200 md:flex",
        open ? "w-[244px]" : "w-[68px]",
      )}
    >
      <div className="flex h-16 items-center gap-2.5 border-b border-line px-4">
        <LogoMark size="sm" />
        {open && <span className="whitespace-nowrap text-md font-bold">DocuMind AI</span>}
      </div>

      <nav className="flex flex-1 flex-col gap-[3px] p-2.5 pt-3">
        {PRIMARY_NAV.map((item) => (
          <Link
            key={item.href}
            href={item.href}
            title={item.label}
            aria-current={isActive(pathname, item.href) ? "page" : undefined}
            className={cn("nav-item", isActive(pathname, item.href) && "nav-item-active")}
          >
            <Icon name={item.icon} className="text-[17px]" />
            {open && <span className="whitespace-nowrap">{item.label}</span>}
          </Link>
        ))}
      </nav>

      <div className="flex flex-col gap-[3px] border-t border-line p-2.5">
        {SECONDARY_NAV.map((item) => (
          <Link key={item.href} href={item.href} title={item.label} className="nav-item">
            <Icon name={item.icon} className="text-[17px]" />
            {open && <span className="whitespace-nowrap">{item.label}</span>}
          </Link>
        ))}

        <button onClick={onToggle} aria-label="Toggle sidebar" className="nav-item text-subtle">
          <Icon name="panelLeft" className="text-[17px]" />
          {open && <span className="whitespace-nowrap">Collapse</span>}
        </button>

        <div className="mt-1 flex items-center gap-2.5 rounded-field bg-canvas px-[11px] py-2.5">
          <Avatar initials={CURRENT_USER.initials} size="sm" />
          {open && (
            <span className="min-w-0">
              <span className="block truncate text-small font-semibold">{CURRENT_USER.name}</span>
              <span className="block truncate text-[11.5px] text-subtle">{CURRENT_USER.email}</span>
            </span>
          )}
        </div>
      </div>
    </aside>
  );
}
