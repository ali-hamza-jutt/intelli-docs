"use client";

import { useState } from "react";
import { cn } from "@/lib/cn";
import { Button } from "@/components/ui/Button";
import { SearchInput } from "@/components/ui/Field";
import { Icon } from "@/components/ui/Icon";
import { IconButton } from "@/components/ui/IconButton";
import { SkeletonRows } from "@/components/ui/Skeleton";
import type { ConversationSummaryResponse } from "@/lib/api/model";

/** Buckets by age, so the rail reads as a history rather than one long list. */
const GROUPS = [
  { label: "Today", within: 1 },
  { label: "This week", within: 7 },
  { label: "Older", within: Infinity },
] as const;

function daysAgo(iso: string) {
  return (Date.now() - new Date(iso).getTime()) / 86_400_000;
}

/** Left rail: saved conversations, most recently used first. */
export function ConversationPane({
  conversations,
  loading,
  activeId,
  onSelect,
  onDelete,
  onNewChat,
}: {
  conversations: ConversationSummaryResponse[];
  loading: boolean;
  activeId: string | null;
  onSelect: (id: string) => void;
  onDelete: (id: string) => void;
  onNewChat: () => void;
}) {
  const [search, setSearch] = useState("");

  const term = search.trim().toLowerCase();
  const shown = conversations.filter((conversation) =>
    conversation.title.toLowerCase().includes(term),
  );

  // Each bucket takes the ages between the previous cut-off and its own, so a conversation lands in
  // exactly one and none is counted twice.
  const groups = GROUPS.map(({ label, within }, index) => ({
    label,
    items: shown.filter((conversation) => {
      const age = daysAgo(conversation.updatedAt);
      const from = index === 0 ? -Infinity : GROUPS[index - 1].within;

      return age >= from && age < within;
    }),
  })).filter((group) => group.items.length > 0);

  return (
    <div className="hidden w-[250px] flex-none flex-col border-r border-line bg-surface xl:flex">
      <div className="p-3.5">
        <Button icon="plus" fullWidth onClick={onNewChat}>
          New Chat
        </Button>
        <SearchInput
          aria-label="Search conversations"
          placeholder="Search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          className="mt-2.5"
        />
      </div>

      <div className="flex-1 overflow-auto px-2.5 pb-3.5">
        {loading && <SkeletonRows count={4} />}

        {!loading && conversations.length === 0 && (
          <p className="m-0 px-2 py-3 text-small leading-relaxed text-muted">
            No conversations yet. Choose a document below and ask something.
          </p>
        )}

        {conversations.length > 0 && shown.length === 0 && (
          <p className="m-0 px-2 py-3 text-small text-muted">Nothing matches “{search}”.</p>
        )}

        {groups.map((group) => (
          <div key={group.label} className="mb-3.5">
            <p className="mb-1.5 ml-2 eyebrow">{group.label}</p>
            {group.items.map((conversation) => (
              <div
                key={conversation.id}
                className={cn(
                  "group flex items-center gap-1 rounded-field",
                  conversation.id === activeId ? "bg-brand-soft" : "hover:bg-surface-alt",
                )}
              >
                <button
                  onClick={() => onSelect(conversation.id)}
                  title={conversation.title}
                  className={cn(
                    "min-w-0 flex-1 cursor-pointer truncate px-2.5 py-2.5 text-left text-small font-medium",
                    conversation.id === activeId ? "text-brand" : "text-ink-soft",
                  )}
                >
                  {conversation.title}
                </button>
                <IconButton
                  icon="trash"
                  label={`Delete ${conversation.title}`}
                  size="sm"
                  tone="danger"
                  className="mr-1 size-6.5 flex-none text-small opacity-0 group-hover:opacity-100"
                  onClick={() => onDelete(conversation.id)}
                />
              </div>
            ))}
          </div>
        ))}

        {!loading && conversations.length > 0 && (
          <p className="m-0 flex items-center gap-1.5 px-2 pt-1 text-tiny text-subtle">
            <Icon name="message" className="text-small" />
            {conversations.length} {conversations.length === 1 ? "conversation" : "conversations"}
          </p>
        )}
      </div>
    </div>
  );
}
