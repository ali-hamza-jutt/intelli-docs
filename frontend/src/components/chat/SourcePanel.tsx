"use client";

import Link from "next/link";
import { Icon } from "@/components/ui/Icon";
import { IconButton } from "@/components/ui/IconButton";
import type { Source } from "@/lib/data";

/** The retrieved passage, with the cited sentence highlighted in context. */
function SourceBody({ source }: { source: Source }) {
  return (
    <div className="text-body leading-[1.75] text-muted">
      <p className="mb-2.5">{source.before}</p>
      <p className="mb-2.5 rounded-control border-l-[3px] border-warning bg-warning-soft px-3 py-2.5 text-ink">
        {source.quote}
      </p>
      <p className="m-0">{source.after}</p>
    </div>
  );
}

/** Wide screens: a docked right-hand rail. */
export function SourceRail({ source, onClose }: { source: Source; onClose: () => void }) {
  return (
    <aside
      aria-label="Sources"
      className="hidden w-[308px] flex-none overflow-auto border-l border-line bg-surface animate-slide-in 2xl:block"
    >
      <div className="sticky top-0 flex items-center justify-between border-b border-line bg-surface px-[18px] py-4">
        <h3 className="m-0 eyebrow">Source</h3>
        <IconButton icon="x" label="Close sources" size="sm" onClick={onClose} />
      </div>
      <div className="p-[18px]">
        <p className="m-0 text-md font-semibold">{source.doc}</p>
        <p className="mt-1 mb-4 text-caption text-subtle">{source.page}</p>
        <SourceBody source={source} />
        <Link href="/documents/handbook" className="btn btn-secondary btn-md mt-[18px] w-full">
          <Icon name="external" className="text-base" />
          Open document
        </Link>
      </div>
    </aside>
  );
}

/** Narrow screens: a bottom sheet with the same content. */
export function SourceSheet({ source, onClose }: { source: Source; onClose: () => void }) {
  return (
    <div className="fixed inset-x-0 bottom-0 z-70 max-h-[60vh] overflow-auto rounded-t-panel border-t border-line bg-surface px-[18px] pt-4 pb-6 shadow-sheet animate-sheet-up 2xl:hidden">
      <div className="mx-auto mb-3.5 h-1 w-9 rounded-pill bg-line" />
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="m-0 text-md font-semibold">{source.doc}</p>
          <p className="mt-1 text-caption text-subtle">{source.page}</p>
        </div>
        <IconButton icon="x" label="Close sources" size="sm" className="bg-surface-alt" onClick={onClose} />
      </div>
      <p className="mt-4 rounded-field border-l-[3px] border-warning bg-warning-soft p-3 text-body leading-[1.7] text-ink">
        {source.quote}
      </p>
    </div>
  );
}
