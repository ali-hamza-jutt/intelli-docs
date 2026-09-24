"use client";

import { useState } from "react";
import Link from "next/link";
import { cn } from "@/lib/cn";
import { Button } from "@/components/ui/Button";
import { SearchInput } from "@/components/ui/Field";
import { Icon } from "@/components/ui/Icon";
import { SkeletonRows } from "@/components/ui/Skeleton";
import type { DocumentResponse } from "@/lib/api/model";

/**
 * Left rail: which document the conversation is about.
 *
 * Answers are grounded in one document at a time, so the document has to be chosen before anything
 * can be asked. Only finished documents appear — one still being processed has no passages to
 * search. Module 9 turns this rail into the list of saved conversations.
 */
export function DocumentPicker({
  documents,
  loading,
  activeId,
  onSelect,
  onNewChat,
}: {
  /** Already filtered to the documents that can actually be asked about. */
  documents: DocumentResponse[];
  loading: boolean;
  activeId: string | null;
  onSelect: (id: string) => void;
  onNewChat: () => void;
}) {
  const [search, setSearch] = useState("");

  const shown = documents.filter((document) =>
    document.fileName.toLowerCase().includes(search.trim().toLowerCase()),
  );

  return (
    <div className="hidden w-[250px] flex-none flex-col border-r border-line bg-surface xl:flex">
      <div className="p-3.5">
        <Button icon="plus" fullWidth onClick={onNewChat}>
          New Chat
        </Button>
        <SearchInput
          aria-label="Search documents"
          placeholder="Search documents"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          className="mt-2.5"
        />
      </div>

      <div className="flex-1 overflow-auto px-2.5 pb-3.5">
        <p className="mb-1.5 ml-2 eyebrow">Your documents</p>

        {loading && <SkeletonRows count={4} />}

        {!loading && documents.length === 0 && (
          <p className="m-0 px-2 py-3 text-small leading-relaxed text-muted">
            No processed documents yet.{" "}
            <Link href="/documents" className="link-action">
              Upload one
            </Link>{" "}
            to start asking questions.
          </p>
        )}

        {shown.map((document) => (
          <button
            key={document.id}
            onClick={() => onSelect(document.id)}
            title={document.fileName}
            className={cn(
              "flex w-full cursor-pointer items-center gap-2 rounded-field px-2.5 py-2.5 text-left text-small font-medium",
              document.id === activeId
                ? "bg-brand-soft text-brand"
                : "text-ink-soft hover:bg-surface-alt",
            )}
          >
            <Icon name="fileText" className="flex-none text-base text-subtle" />
            <span className="min-w-0 flex-1 truncate">{document.fileName}</span>
          </button>
        ))}

        {documents.length > 0 && shown.length === 0 && (
          <p className="m-0 px-2 py-3 text-small text-muted">No document matches “{search}”.</p>
        )}
      </div>
    </div>
  );
}
