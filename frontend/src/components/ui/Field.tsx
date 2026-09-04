"use client";

import { useId } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "./Icon";

/** Labelled text input used across auth, settings and dialogs. */
export function Field({
  label,
  id,
  className,
  hint,
  ...rest
}: {
  label: string;
  hint?: React.ReactNode;
} & React.InputHTMLAttributes<HTMLInputElement>) {
  const generatedId = useId();
  const inputId = id ?? generatedId;

  return (
    <div className={className}>
      <label htmlFor={inputId} className="field-label">
        {label}
      </label>
      <input id={inputId} className="field" {...rest} />
      {hint}
    </div>
  );
}

export function Select({
  label,
  children,
  className,
  ...rest
}: {
  label: string;
  children: React.ReactNode;
} & React.SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <select aria-label={label} className={cn("select", className)} {...rest}>
      {children}
    </select>
  );
}

export function Checkbox({
  label,
  className,
  ...rest
}: { label: React.ReactNode } & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <label className={cn("flex cursor-pointer items-center gap-2 text-body text-muted", className)}>
      <input type="checkbox" className="checkbox" {...rest} />
      {label}
    </label>
  );
}

/** Icon + borderless input inside a single bordered shell. */
export function SearchInput({
  variant = "solid",
  className,
  ...rest
}: {
  variant?: "solid" | "outlined";
} & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div
      className={cn(
        "search-shell",
        variant === "solid" ? "search-shell-solid" : "search-shell-outlined",
        className,
      )}
    >
      <Icon name="search" className="text-md text-subtle" />
      <input className="search-input" {...rest} />
    </div>
  );
}
