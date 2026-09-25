"use client";

import Link from "next/link";
import { Icon } from "@/components/ui/Icon";
import { IconButton } from "@/components/ui/IconButton";
import { pageLabel } from "@/components/chat/ChatMessage";
import type { MessageSourceResponse } from "@/lib/api/model";

/**
 * The passage the answer was written from, exactly as it was sent to the model.
 *
 * This is the whole point of sources: the user can read the source text themselves and judge
 * whether the answer represents it fairly.
 */
function SourceBody({ source }: { source: MessageSourceResponse }) {
  return (
    <p className="m-0 rounded-control border-l-[3px] border-warning bg-warning-soft px-3 py-2.5 text-body leading-[1.75] whitespace-pre-wrap text-ink">
      {source.text}
    </p>
  );
}

function SourceHeading({ source }: { source: MessageSourceResponse }) {
  return (
    <div className="min-w-0">
      <p className="m-0 truncate text-md font-semibold">{source.fileName}</p>
      <p className="mt-1 text-caption text-subtle">
        {pageLabel(source)} · cited as [{source.marker}] ·{" "}
        {Math.round(source.similarity * 100)}% match
      </p>
    </div>
  );
}

/** Wide screens: a docked right-hand rail. */
export function SourceRail({
  source,
  onClose,
}: {
  source: MessageSourceResponse;
  onClose: () => void;
}) {
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
        <SourceHeading source={source} />
        <div className="mt-4">
          <SourceBody source={source} />
        </div>
        <Link
          href={`/documents/${source.documentId}`}
          className="btn btn-secondary btn-md mt-[18px] w-full"
        >
          <Icon name="external" className="text-base" />
          Open document
        </Link>
      </div>
    </aside>
  );
}

/** Narrow screens: a bottom sheet with the same content. */
export function SourceSheet({
  source,
  onClose,
}: {
  source: MessageSourceResponse;
  onClose: () => void;
}) {
  return (
    <div className="fixed inset-x-0 bottom-0 z-70 max-h-[60vh] overflow-auto rounded-t-panel border-t border-line bg-surface px-[18px] pt-4 pb-6 shadow-sheet animate-sheet-up 2xl:hidden">
      <div className="mx-auto mb-3.5 h-1 w-9 rounded-pill bg-line" />
      <div className="flex items-start justify-between gap-3">
        <SourceHeading source={source} />
        <IconButton
          icon="x"
          label="Close sources"
          size="sm"
          className="flex-none bg-surface-alt"
          onClick={onClose}
        />
      </div>
      <div className="mt-4">
        <SourceBody source={source} />
      </div>
    </div>
  );
}
