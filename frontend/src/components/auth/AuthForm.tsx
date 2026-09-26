"use client";

import { useState } from "react";
import Link from "next/link";
import { Button } from "@/components/ui/Button";
import { Checkbox, Field } from "@/components/ui/Field";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";

export type AuthField = {
  id: string;
  label: string;
  type: string;
  placeholder: string;
  autoComplete: string;
  /** Renders a strength meter beneath the input. */
  strength?: boolean;
};

export type AuthConfig = {
  title: string;
  subtitle: string;
  cta: string;
  fields: AuthField[];
  showRemember?: boolean;
  showTerms?: boolean;
  showDivider?: boolean;
  footNote: string;
  footLink: string;
  footHref: string;
};

const STRENGTH_LABELS = ["Too short", "Weak", "Good", "Strong"];
const STRENGTH_COLORS = ["bg-danger", "bg-warning", "bg-brand", "bg-success"];

function scorePassword(value: string) {
  if (!value) return 0;
  return Math.min(
    3,
    (value.length > 7 ? 1 : 0) + (/[A-Z]/.test(value) ? 1 : 0) + (/[0-9!@#$%^&*]/.test(value) ? 1 : 0),
  );
}

/**
 * Presentational shell for all four auth screens. The page supplies the submit handler, so this
 * component knows nothing about the API — it only reports values, pending state and errors.
 */
export function AuthForm({
  config,
  onSubmit,
  isPending = false,
  errorMessage,
}: {
  config: AuthConfig;
  onSubmit: (values: Record<string, string>) => void;
  isPending?: boolean;
  errorMessage?: string | null;
}) {
  const [password, setPassword] = useState("");
  const [mismatch, setMismatch] = useState(false);

  const score = scorePassword(password);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const form = new FormData(event.currentTarget);
    const values = Object.fromEntries(
      config.fields.map((field) => [field.id, String(form.get(field.id) ?? "")]),
    );

    // Confirm-password is a client-side concern; the API never sees it.
    if ("confirm" in values && values.confirm !== values.password) {
      setMismatch(true);
      return;
    }

    setMismatch(false);
    onSubmit(values);
  };

  const error = mismatch ? "The two passwords do not match." : errorMessage;

  return (
    <div className="panel p-8 shadow-[0_4px_20px_-14px_rgb(17_24_39/0.2)]">
      <h1 className="m-0 mb-1.5 text-2xl font-bold tracking-[-0.02em]">{config.title}</h1>
      <p className="m-0 mb-6 text-base text-muted">{config.subtitle}</p>

      {error && (
        <div className="alert-danger mb-4" role="alert">
          <Icon name="alert" className="text-md text-danger" />
          <p className="alert-danger-text">{error}</p>
        </div>
      )}

      <form className="flex flex-col gap-4" onSubmit={handleSubmit} noValidate>
        {config.fields.map((field) => (
          <Field
            key={field.id}
            id={field.id}
            name={field.id}
            label={field.label}
            type={field.type}
            placeholder={field.placeholder}
            autoComplete={field.autoComplete}
            required
            disabled={isPending}
            onChange={field.strength ? (e) => setPassword(e.target.value) : undefined}
            hint={
              field.strength ? (
                <div className="mt-2">
                  <div className="progress-track h-1">
                    <div
                      className={cn(
                        "h-full rounded-pill transition-[width,background-color] duration-300",
                        STRENGTH_COLORS[score],
                      )}
                      style={{ width: password ? `${((score + 1) / 4) * 100}%` : "0%" }}
                    />
                  </div>
                  <p className="mt-1.5 text-tiny text-muted">
                    Password strength: {password ? STRENGTH_LABELS[score] : STRENGTH_LABELS[0]}
                  </p>
                </div>
              ) : undefined
            }
          />
        ))}

        {/* No endpoint sends a reset email, so there is no "Forgot password?" link to offer. The
            screens for it stay in the repo, unlinked, until there is something behind them. */}
        {config.showRemember && (
          <Checkbox label="Remember me" defaultChecked disabled={isPending} />
        )}

        {config.showTerms && (
          <Checkbox
            label="I agree to the Terms and Privacy Policy."
            className="items-start leading-normal"
            required
            disabled={isPending}
          />
        )}

        <Button type="submit" size="lg" fullWidth className="py-3 text-lead" disabled={isPending}>
          {isPending ? "Please wait…" : config.cta}
        </Button>
      </form>

      {config.showDivider && (
        <>
          <div className="my-5 flex items-center gap-3">
            <span className="h-px flex-1 bg-line" />
            <span className="text-tiny text-subtle">OR</span>
            <span className="h-px flex-1 bg-line" />
          </div>
          <button disabled className="btn btn-disabled btn-md w-full font-medium">
            Single sign-on — coming soon
          </button>
        </>
      )}

      <p className="mt-5 mb-0 text-center text-body text-muted">
        {config.footNote}{" "}
        <Link href={config.footHref} className="link-action text-body font-semibold">
          {config.footLink}
        </Link>
      </p>
    </div>
  );
}
