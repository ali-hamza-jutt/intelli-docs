"use client";

import { cn } from "@/lib/cn";
import { Button } from "@/components/ui/Button";
import { SearchInput } from "@/components/ui/Field";
import { IconButton } from "@/components/ui/IconButton";
import { CONVERSATION_GROUPS } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";

/** Left rail listing saved conversations, grouped by recency. */
export function ConversationPane({
  activeId,
  onSelect,
  onNewChat,
}: {
  activeId: string;
  onSelect: (id: string) => void;
  onNewChat: () => void;
}) {
  const toast = useToast();

  return (
    <div className="hidden w-[250px] flex-none flex-col border-r border-line bg-surface xl:flex">
      <div className="p-3.5">
        <Button icon="plus" fullWidth onClick={onNewChat}>
          New Chat
        </Button>
        <SearchInput aria-label="Search conversations" placeholder="Search" className="mt-2.5" />
      </div>

      <div className="flex-1 overflow-auto px-2.5 pb-3.5">
        {CONVERSATION_GROUPS.map((group) => (
          <div key={group.label} className="mb-3.5">
            <p className="mb-1.5 ml-2 eyebrow">{group.label}</p>
            {group.items.map((item) => (
              <div
                key={item.id}
                className={cn(
                  "group flex items-center gap-1 rounded-field",
                  item.id === activeId ? "bg-brand-soft" : "hover:bg-surface-alt",
                )}
              >
                <button
                  onClick={() => onSelect(item.id)}
                  className={cn(
                    "min-w-0 flex-1 cursor-pointer truncate px-2.5 py-2.5 text-left text-small font-medium",
                    item.id === activeId ? "text-brand" : "text-ink-soft",
                  )}
                >
                  {item.title}
                </button>
                <IconButton
                  icon="settings"
                  label="Rename conversation"
                  size="sm"
                  className="size-6.5 text-small opacity-0 group-hover:opacity-100"
                  onClick={() => toast("Conversation renamed")}
                />
                <IconButton
                  icon="trash"
                  label="Delete conversation"
                  size="sm"
                  tone="danger"
                  className="mr-1 size-6.5 text-small opacity-0 group-hover:opacity-100"
                  onClick={() => toast("Conversation deleted")}
                />
              </div>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}
