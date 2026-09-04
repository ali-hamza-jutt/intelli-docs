"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/Button";
import { Checkbox, Field } from "@/components/ui/Field";
import { cn } from "@/lib/cn";
import { useToast } from "@/components/ui/Toast";

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
  /** Where a successful submit lands, plus the toast it raises. */
  redirectTo: string;
  successMessage: string;
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

/** One form driving all four auth screens; the differences live in AuthConfig. */
export function AuthForm({ config }: { config: AuthConfig }) {
  const [password, setPassword] = useState("");
  const router = useRouter();
  const toast = useToast();

  const score = scorePassword(password);

  return (
    <div className="panel p-8 shadow-[0_4px_20px_-14px_rgb(17_24_39/0.2)]">
      <h1 className="m-0 mb-1.5 text-2xl font-bold tracking-[-0.02em]">{config.title}</h1>
      <p className="m-0 mb-6.5 text-base text-muted">{config.subtitle}</p>

      <form
        className="flex flex-col gap-4"
        onSubmit={(e) => {
          e.preventDefault();
          toast(config.successMessage);
          router.push(config.redirectTo);
        }}
      >
        {config.fields.map((field) => (
          <Field
            key={field.id}
            id={field.id}
            label={field.label}
            type={field.type}
            placeholder={field.placeholder}
            autoComplete={field.autoComplete}
            value={field.strength ? password : undefined}
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

        {config.showRemember && (
          <div className="flex items-center justify-between gap-3">
            <Checkbox label="Remember me" />
            <Link href="/forgot-password" className="link-action text-body">
              Forgot password?
            </Link>
          </div>
        )}

        {config.showTerms && (
          <Checkbox
            label="I agree to the Terms and Privacy Policy."
            className="items-start leading-normal"
          />
        )}

        <Button type="submit" size="lg" fullWidth className="py-3 text-lead">
          {config.cta}
        </Button>
      </form>

      {config.showDivider && (
        <>
          <div className="my-5.5 flex items-center gap-3">
            <span className="h-px flex-1 bg-line" />
            <span className="text-tiny text-subtle">OR</span>
            <span className="h-px flex-1 bg-line" />
          </div>
          <button disabled className="btn btn-disabled btn-md w-full font-medium">
            Single sign-on — coming soon
          </button>
        </>
      )}

      <p className="mt-5.5 mb-0 text-center text-body text-muted">
        {config.footNote}{" "}
        <Link href={config.footHref} className="link-action text-body font-semibold">
          {config.footLink}
        </Link>
      </p>
    </div>
  );
}
