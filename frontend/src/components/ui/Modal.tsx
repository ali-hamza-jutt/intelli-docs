"use client";

import { useEffect } from "react";
import { IconButton } from "./IconButton";

/** Centred dialog with a header strip. Closes on Escape and on backdrop click. */
export function Modal({
  open,
  onClose,
  title,
  subtitle,
  children,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
}) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label={title}
      onClick={onClose}
      className="fixed inset-0 z-80 flex items-center justify-center bg-ink/40 p-5 backdrop-blur-[2px] animate-fade-in"
    >
      <div
        onClick={(e) => e.stopPropagation()}
        className="w-full max-w-[540px] overflow-hidden rounded-panel border border-line bg-surface shadow-modal animate-pop-in"
      >
        <div className="flex items-center justify-between gap-4 border-b border-line px-[22px] py-5">
          <div>
            <h2 className="m-0 text-lg font-semibold">{title}</h2>
            {subtitle && <p className="mt-1 text-small text-muted">{subtitle}</p>}
          </div>
          <IconButton icon="x" label="Close" onClick={onClose} />
        </div>
        <div className="p-[22px]">{children}</div>
      </div>
    </div>
  );
}
