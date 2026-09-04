"use client";

import { Icon } from "@/components/ui/Icon";
import { IconButton } from "@/components/ui/IconButton";
import { Select } from "@/components/ui/Field";
import { Button } from "@/components/ui/Button";
import { COLLECTIONS } from "@/lib/data";

/** Multi-line input with attach, collection scope, and send/stop controls. */
export function ChatComposer({
  value,
  onChange,
  onSubmit,
  onAttach,
  onStop,
  streaming,
}: {
  value: string;
  onChange: (value: string) => void;
  onSubmit: () => void;
  onAttach: () => void;
  onStop: () => void;
  streaming: boolean;
}) {
  return (
    <div className="flex-none border-t border-line bg-surface px-5 pt-3.5 pb-4.5">
      <form
        className="mx-auto max-w-[720px]"
        onSubmit={(e) => {
          e.preventDefault();
          onSubmit();
        }}
      >
        <div className="rounded-card border border-line bg-surface px-3 py-2.5 transition-[border-color,box-shadow] focus-within:border-brand focus-within:shadow-focus">
          <textarea
            rows={2}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                onSubmit();
              }
            }}
            aria-label="Ask anything about your knowledge base"
            placeholder="Ask anything about your knowledge base…"
            className="w-full resize-none border-0 bg-transparent px-1 pt-1 pb-2 text-lead leading-[1.55] outline-none placeholder:text-subtle"
          />

          <div className="flex items-center gap-2">
            <IconButton icon="paperclip" label="Attach document" onClick={onAttach} />

            <Select label="Knowledge collection" defaultValue="all" className="px-2.5 py-1.5 text-caption text-muted">
              <option value="all">All documents</option>
              {COLLECTIONS.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </Select>

            <span className="flex-1" />

            <span className="hidden text-[11.5px] text-faint lg:inline">
              Enter to send · Shift+Enter for newline
            </span>

            {streaming ? (
              <Button type="button" variant="secondary" size="sm" onClick={onStop}>
                Stop generating
              </Button>
            ) : (
              <button
                type="submit"
                aria-label="Send"
                className="btn btn-primary inline-flex size-9 items-center justify-center p-0 text-lg"
              >
                <Icon name="send" />
              </button>
            )}
          </div>
        </div>
      </form>
    </div>
  );
}
