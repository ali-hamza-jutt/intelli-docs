"use client";

import { AppShell } from "@/components/layout/AppShell";
import { useRequireAuth } from "@/lib/auth/useRequireAuth";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useRequireAuth();

  // While the session is being restored, hold the frame rather than flashing the login screen
  // at a user who is in fact signed in.
  if (isLoading || !isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-canvas">
        <span className="sr-only">Loading your workspace…</span>
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-line border-t-brand" />
      </div>
    );
  }

  return <AppShell>{children}</AppShell>;
}
