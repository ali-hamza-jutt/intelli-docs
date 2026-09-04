"use client";

import { useState } from "react";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Avatar } from "@/components/ui/Avatar";
import { Checkbox, Field, Select } from "@/components/ui/Field";
import { Icon } from "@/components/ui/Icon";
import { Tabs } from "@/components/ui/Tabs";
import { CURRENT_USER, PREFERENCE_TOGGLES, SESSIONS } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";

const TABS = ["Profile", "Preferences", "Security", "Data"] as const;

/** A label/description pair with a control on the right — the settings row pattern. */
function SettingRow({
  title,
  description,
  control,
  danger = false,
}: {
  title: string;
  description: string;
  control: React.ReactNode;
  danger?: boolean;
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div>
        <p className={`m-0 text-base font-semibold ${danger ? "text-danger" : ""}`}>{title}</p>
        <p className="mt-0.5 text-small text-muted">{description}</p>
      </div>
      {control}
    </div>
  );
}

export default function SettingsPage() {
  const [tab, setTab] = useState<string>("Profile");
  const toast = useToast();

  return (
    <div className="page-narrow">
      <h2 className="page-title">Settings</h2>
      <p className="page-subtitle mb-5.5">Manage your account, preferences, and data.</p>

      <Tabs tabs={TABS} active={tab} onChange={setTab} />

      {tab === "Profile" && (
        <Card className="p-[22px] animate-fade-in">
          <div className="mb-5.5 flex items-center gap-4">
            <Avatar initials={CURRENT_USER.initials} size="lg" />
            <Button variant="secondary" size="sm" onClick={() => toast("Avatar updated")}>
              Change avatar
            </Button>
          </div>

          <div className="grid gap-4 [grid-template-columns:repeat(auto-fit,minmax(220px,1fr))]">
            <Field label="Name" defaultValue={CURRENT_USER.name} />
            <Field label="Email" type="email" defaultValue={CURRENT_USER.email} />
          </div>

          <Button className="mt-5" onClick={() => toast("Settings updated")}>
            Save changes
          </Button>
        </Card>
      )}

      {tab === "Preferences" && (
        <Card className="flex flex-col gap-5 p-[22px] animate-fade-in">
          <SettingRow
            title="Theme"
            description="Light is the default workspace theme."
            control={
              <Select label="Theme" defaultValue="light">
                <option value="light">Light</option>
                <option value="system">Match system</option>
              </Select>
            }
          />
          <div className="divider" />
          <SettingRow
            title="Language"
            description="Used for the interface and answers."
            control={
              <Select label="Language" defaultValue="en">
                <option value="en">English</option>
                <option value="ar">Arabic</option>
                <option value="de">German</option>
              </Select>
            }
          />
          <div className="divider" />
          <div>
            <p className="mb-2.5 text-base font-semibold">Response preferences</p>
            <div className="flex flex-col gap-2.5">
              {PREFERENCE_TOGGLES.map((pref) => (
                <Checkbox
                  key={pref.label}
                  label={pref.label}
                  defaultChecked={pref.on}
                  className="text-ink-soft"
                />
              ))}
            </div>
          </div>
        </Card>
      )}

      {tab === "Security" && (
        <Card className="p-[22px] animate-fade-in">
          <h3 className="mb-3.5 card-title">Change password</h3>
          <div className="grid gap-3.5 [grid-template-columns:repeat(auto-fit,minmax(200px,1fr))]">
            <input
              type="password"
              aria-label="Current password"
              placeholder="Current password"
              className="field"
            />
            <input
              type="password"
              aria-label="New password"
              placeholder="New password"
              className="field"
            />
          </div>
          <Button className="mt-4" onClick={() => toast("Password updated")}>
            Update password
          </Button>

          <div className="divider my-5.5" />

          <h3 className="mb-3 card-title">Active sessions</h3>
          <div className="flex flex-col gap-2.5">
            {SESSIONS.map((session) => (
              <div
                key={session.device}
                className="flex flex-wrap items-center justify-between gap-2.5 rounded-control border border-line px-3.5 py-3"
              >
                <div className="flex items-center gap-3">
                  <Icon name="lock" className="text-lg text-muted" />
                  <div>
                    <p className="m-0 text-body font-semibold">{session.device}</p>
                    <p className="mt-0.5 text-tiny text-subtle">{session.meta}</p>
                  </div>
                </div>
                <Button
                  variant="secondary"
                  size="sm"
                  disabled={session.current}
                  className={session.current ? "text-subtle" : "text-danger"}
                  onClick={() => toast("Session revoked")}
                >
                  {session.action}
                </Button>
              </div>
            ))}
          </div>
        </Card>
      )}

      {tab === "Data" && (
        <Card className="p-[22px] animate-fade-in">
          <SettingRow
            title="Export data"
            description="Download your documents and conversation history."
            control={
              <Button variant="secondary" icon="download" onClick={() => toast("Export started")}>
                Export
              </Button>
            }
          />
          <div className="divider my-5" />
          <SettingRow
            danger
            title="Delete account"
            description="Permanently removes your workspace and all indexed documents."
            control={
              <Button
                variant="danger"
                onClick={() => toast("Account deletion requires confirmation", "warn")}
              >
                Delete account
              </Button>
            }
          />
        </Card>
      )}
    </div>
  );
}
