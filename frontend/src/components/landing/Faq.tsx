"use client";

import { useState } from "react";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";
import { FAQS } from "@/lib/landing-data";

/** Single-open accordion. */
export function Faq() {
  const [open, setOpen] = useState(0);

  return (
    <div className="mx-auto max-w-[760px] overflow-hidden rounded-card border border-line bg-surface">
      {FAQS.map(([question, answer], i) => (
        <div key={question} className="border-b border-line-soft last:border-b-0">
          <button
            onClick={() => setOpen(open === i ? -1 : i)}
            aria-expanded={open === i}
            className="flex w-full cursor-pointer items-center justify-between gap-4 px-5 py-4.5 text-left text-base font-semibold transition-colors hover:bg-canvas"
          >
            {question}
            <Icon
              name="chevronDown"
              className={cn("flex-none text-md text-subtle transition-transform", open === i && "rotate-180")}
            />
          </button>
          {open === i && (
            <p className="m-0 px-5 pb-4.5 text-body leading-relaxed text-muted animate-fade-in">
              {answer}
            </p>
          )}
        </div>
      ))}
    </div>
  );
}
