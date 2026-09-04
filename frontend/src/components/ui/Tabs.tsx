"use client";

import { cn } from "@/lib/cn";

/** Underlined tab strip used by Settings. */
export function Tabs({
  tabs,
  active,
  onChange,
}: {
  tabs: readonly string[];
  active: string;
  onChange: (tab: string) => void;
}) {
  return (
    <div role="tablist" className="mb-[22px] flex gap-0.5 overflow-x-auto border-b border-line">
      {tabs.map((tab) => (
        <button
          key={tab}
          role="tab"
          aria-selected={tab === active}
          onClick={() => onChange(tab)}
          className={cn(
            "cursor-pointer whitespace-nowrap border-b-2 px-3.5 py-[11px] text-body font-semibold transition-colors",
            tab === active ? "border-brand text-ink" : "border-transparent text-muted hover:text-ink",
          )}
        >
          {tab}
        </button>
      ))}
    </div>
  );
}

/** Pill-style segmented filter used by the documents toolbar. */
export function SegmentedControl({
  options,
  active,
  onChange,
}: {
  options: readonly string[];
  active: string;
  onChange: (option: string) => void;
}) {
  return (
    <div className="flex gap-1 rounded-control bg-surface-alt p-1">
      {options.map((option) => (
        <button
          key={option}
          onClick={() => onChange(option)}
          className={cn(
            "cursor-pointer rounded-[7px] px-3.5 py-1.5 text-small font-medium transition-colors",
            option === active ? "bg-surface text-ink shadow-sm" : "text-muted hover:text-ink",
          )}
        >
          {option}
        </button>
      ))}
    </div>
  );
}
