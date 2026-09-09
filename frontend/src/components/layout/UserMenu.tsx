"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Avatar } from "@/components/ui/Avatar";
import { Icon } from "@/components/ui/Icon";
import { useAuth } from "@/lib/auth/AuthProvider";
import { initialsFor } from "@/lib/auth/initials";

/** Avatar button that opens the account menu and handles sign-out. */
export function UserMenu() {
  const [open, setOpen] = useState(false);
  const { user, signOut } = useAuth();
  const router = useRouter();

  if (!user) return null;

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        aria-label="Account"
        aria-expanded={open}
        className="rounded-full"
      >
        <Avatar initials={initialsFor(user.name)} />
      </button>

      {open && (
        <>
          {/* Click-away layer, so the menu closes without a document listener. */}
          <div className="fixed inset-0 z-40" onClick={() => setOpen(false)} />

          <div
            role="menu"
            className="absolute right-0 top-11 z-50 w-56 rounded-xl border border-line bg-surface p-1.5 shadow-pop animate-pop-in"
          >
            <div className="border-b border-line-soft px-2.5 py-2">
              <p className="m-0 truncate text-body font-semibold">{user.name}</p>
              <p className="m-0 truncate text-tiny text-subtle">{user.email}</p>
            </div>

            <Link
              href="/settings"
              onClick={() => setOpen(false)}
              className="mt-1 flex w-full items-center gap-2.5 rounded-control px-2.5 py-2 text-body text-ink-soft hover:bg-canvas"
            >
              <Icon name="settings" className="text-md text-subtle" />
              Settings
            </Link>

            <button
              onClick={async () => {
                setOpen(false);
                await signOut();
                router.replace("/login");
              }}
              className="flex w-full cursor-pointer items-center gap-2.5 rounded-control px-2.5 py-2 text-left text-body text-danger hover:bg-danger-soft"
            >
              <Icon name="lock" className="text-md" />
              Sign out
            </button>
          </div>
        </>
      )}
    </div>
  );
}
