"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Avatar } from "@/components/ui/Avatar";
import { Field } from "@/components/ui/Field";
import { Icon } from "@/components/ui/Icon";
import { Tabs } from "@/components/ui/Tabs";
import {
  usePutApiAuthMe,
  usePostApiAuthChangePassword,
} from "@/lib/api/generated/auth/auth";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthProvider";
import { initialsFor } from "@/lib/auth/initials";
import { useToast } from "@/components/ui/Toast";

/*
 * Only what the API can actually do.
 *
 * The design originally carried Preferences and Data tabs — theme, language, response style, export,
 * delete account — and a list of active sessions. None of them has an endpoint behind it, and a
 * control that silently does nothing is worse than one that is not there: it teaches people the app
 * lies. They come back when there is something to save them to.
 */
const TABS = ["Profile", "Security"] as const;

export default function SettingsPage() {
  const [tab, setTab] = useState<string>("Profile");
  const { user, setUser, signOut } = useAuth();
  const toast = useToast();
  const router = useRouter();

  const [name, setName] = useState(user?.name ?? "");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");

  const updateProfile = usePutApiAuthMe();
  const changePassword = usePostApiAuthChangePassword();

  const nameChanged = name.trim().length > 0 && name.trim() !== user?.name;

  const saveProfile = () => {
    if (!nameChanged) return;

    updateProfile.mutate(
      { data: { name: name.trim() } },
      {
        onSuccess: (updated) => {
          // The header, the avatar and the greeting all read from here, so they change with it.
          setUser(updated);
          toast("Profile updated");
        },
        onError: (error) =>
          toast(error instanceof ApiError ? error.message : "Could not save your profile", "warn"),
      },
    );
  };

  const savePassword = () => {
    if (!currentPassword || !newPassword) return;

    changePassword.mutate(
      { data: { currentPassword, newPassword } },
      {
        onSuccess: async () => {
          setCurrentPassword("");
          setNewPassword("");

          // Changing the password revokes every refresh token, this browser's included, so the
          // session is already gone server-side. Signing out here makes the client agree with that
          // rather than waiting for the next call to fail.
          toast("Password updated — please sign in again");
          await signOut();
          router.push("/login");
        },
        onError: (error) =>
          toast(
            error instanceof ApiError ? error.message : "Could not change your password",
            "warn",
          ),
      },
    );
  };

  return (
    <div className="page-narrow">
      <h2 className="page-title">Settings</h2>
      <p className="page-subtitle mb-5.5">Manage your account.</p>

      <Tabs tabs={TABS} active={tab} onChange={setTab} />

      {tab === "Profile" && (
        <Card className="p-[22px] animate-fade-in">
          <div className="mb-5.5 flex items-center gap-4">
            <Avatar initials={initialsFor(user?.name ?? "")} size="lg" />
            <div>
              <p className="m-0 text-base font-semibold">{user?.name}</p>
              <p className="mt-0.5 text-small text-muted">{user?.email}</p>
            </div>
          </div>

          <div className="grid gap-4 [grid-template-columns:repeat(auto-fit,minmax(220px,1fr))]">
            <Field
              label="Name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              disabled={updateProfile.isPending}
            />
            {/* No endpoint changes an email address, and a field that cannot be saved should not
                look editable. */}
            <Field
              label="Email"
              type="email"
              value={user?.email ?? ""}
              hint="Your email cannot be changed."
              disabled
              readOnly
            />
          </div>

          <Button
            className="mt-5"
            disabled={!nameChanged || updateProfile.isPending}
            onClick={saveProfile}
          >
            {updateProfile.isPending ? "Saving…" : "Save changes"}
          </Button>
        </Card>
      )}

      {tab === "Security" && (
        <Card className="p-[22px] animate-fade-in">
          <h3 className="mb-1 card-title">Change password</h3>
          <p className="mt-0 mb-3.5 text-small text-muted">
            Use at least 8 characters, mixing letters with a number or symbol. You will be signed out
            everywhere.
          </p>

          <div className="grid gap-3.5 [grid-template-columns:repeat(auto-fit,minmax(200px,1fr))]">
            <input
              type="password"
              aria-label="Current password"
              placeholder="Current password"
              className="field"
              autoComplete="current-password"
              value={currentPassword}
              onChange={(event) => setCurrentPassword(event.target.value)}
              disabled={changePassword.isPending}
            />
            <input
              type="password"
              aria-label="New password"
              placeholder="New password"
              className="field"
              autoComplete="new-password"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              disabled={changePassword.isPending}
            />
          </div>

          <Button
            className="mt-4"
            disabled={!currentPassword || !newPassword || changePassword.isPending}
            onClick={savePassword}
          >
            {changePassword.isPending ? "Updating…" : "Update password"}
          </Button>

          <div className="divider my-5.5" />

          <div className="flex items-start gap-2.5 text-small text-muted">
            <Icon name="lock" className="mt-0.5 flex-none text-md text-subtle" />
            <p className="m-0">
              Signing in issues a short-lived token kept in memory only, alongside a refresh cookie
              this site can read but scripts cannot. Changing your password revokes both, everywhere.
            </p>
          </div>
        </Card>
      )}
    </div>
  );
}
