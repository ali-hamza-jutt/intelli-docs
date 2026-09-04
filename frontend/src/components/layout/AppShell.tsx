"use client";

import { useState } from "react";
import { usePathname } from "next/navigation";
import { AppSidebar } from "./AppSidebar";
import { AppHeader } from "./AppHeader";
import { MobileNav } from "./MobileNav";

/** Maps a route to the title shown in the header. */
const TITLES: Record<string, string> = {
  "/dashboard": "Dashboard",
  "/documents": "Documents",
  "/chat": "Chat",
  "/collections": "Collections",
  "/settings": "Settings",
};

function titleFor(pathname: string) {
  if (pathname.startsWith("/documents/")) return "Document";
  return TITLES[pathname] ?? "Dashboard";
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const pathname = usePathname();
  const isChat = pathname === "/chat";

  return (
    <div className="flex min-h-screen bg-canvas">
      <AppSidebar open={sidebarOpen} onToggle={() => setSidebarOpen((v) => !v)} />

      <div className="flex min-w-0 flex-1 flex-col pb-[76px] md:pb-0">
        <AppHeader title={titleFor(pathname)} showSearch={!isChat} />
        <main className="min-w-0 flex-1">{children}</main>
      </div>

      <MobileNav />
    </div>
  );
}
