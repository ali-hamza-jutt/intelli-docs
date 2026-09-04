"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";
import { PRIMARY_NAV, isActive } from "./AppSidebar";

/** Bottom tab bar shown in place of the sidebar on small screens. */
export function MobileNav() {
  const pathname = usePathname();

  return (
    <nav
      aria-label="Primary"
      className="fixed inset-x-0 bottom-0 z-40 flex border-t border-line bg-surface/95 px-1.5 pt-2 pb-2.5 backdrop-blur-md md:hidden"
    >
      {PRIMARY_NAV.map((item) => (
        <Link
          key={item.href}
          href={item.href}
          className={cn(
            "flex flex-1 flex-col items-center gap-1 px-0.5 py-1.5 text-micro font-medium",
            isActive(pathname, item.href) ? "text-brand" : "text-subtle",
          )}
        >
          <Icon name={item.icon} className="text-[19px]" />
          {item.label}
        </Link>
      ))}
    </nav>
  );
}
