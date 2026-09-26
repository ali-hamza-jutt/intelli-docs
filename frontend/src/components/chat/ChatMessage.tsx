"use client";

import { Icon } from "@/components/ui/Icon";
import { IconButton } from "@/components/ui/IconButton";
import { useToast } from "@/components/ui/Toast";
import type { ChatMessageResponse, MessageSourceResponse } from "@/lib/api/model";

/** "Page 4" or "Pages 4–5", matching how the answer's citation reads. */
export function pageLabel(source: MessageSourceResponse) {
  return source.pageNumber === source.endPageNumber
    ? `Page ${source.pageNumber}`
    : `Pages ${source.pageNumber}–${source.endPageNumber}`;
}

export function UserMessage({ text }: { text: string }) {
  return (
    <div className="flex justify-end">
      <div className="max-w-[80%]">
        <p className="mb-1.5 text-right eyebrow">You</p>
        <div className="rounded-[14px] rounded-br-[4px] bg-inverse px-4 py-3 text-lead leading-relaxed text-white">
          {text}
        </div>
      </div>
    </div>
  );
}

export function AssistantMessage({
  message,
  onOpenSource,
  onRegenerate,
}: {
  message: ChatMessageResponse;
  onOpenSource: (source: MessageSourceResponse) => void;
  onRegenerate: () => void;
}) {
  const toast = useToast();
  const sources = message.sources;

  return (
    <div className="flex gap-3">
      <span className="tile tile-brand size-[30px] text-lg">
        <Icon name="sparkles" />
      </span>

      <div className="min-w-0 flex-1">
        <p className="mb-1.5 eyebrow">DocuMind AI</p>

        <div className="card px-[18px] py-4">
          {/* whitespace-pre-wrap keeps the paragraphs and lists the model writes. */}
          <p className="m-0 text-lead leading-[1.7] whitespace-pre-wrap text-ink">{message.content}</p>

          {message.stopped && (
            <p className="mt-3 mb-0 flex items-center gap-2 text-small text-subtle">
              <Icon name="alert" className="text-base" />
              You stopped this answer, so it is incomplete.
            </p>
          )}

          {message.grounded === false && (
            <p className="mt-3 mb-0 flex items-center gap-2 text-small text-subtle">
              <Icon name="alert" className="text-base" />
              Nothing in this document was close enough to the question, so no answer was written.
            </p>
          )}

          {sources.length > 0 && (
            <div className="mt-4 border-t border-line-soft pt-3.5">
              <p className="mb-2.5 eyebrow">Sources</p>
              <div className="flex flex-wrap gap-2">
                {sources.map((source) => (
                  <button
                    key={source.marker}
                    onClick={() => onOpenSource(source)}
                    className="inline-flex cursor-pointer items-center gap-2 rounded-field border border-line bg-canvas px-3 py-2 text-caption transition-colors hover:border-brand-border hover:bg-brand-soft"
                  >
                    {/* The marker matches the [n] in the answer above, so a claim can be traced. */}
                    <span className="font-semibold text-brand">[{source.marker}]</span>
                    <span className="font-semibold">{source.fileName}</span>
                    <span className="text-subtle">{pageLabel(source)}</span>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>

        <div className="mt-2 flex gap-0.5">
          <IconButton
            icon="copy"
            label="Copy"
            size="sm"
            onClick={() => {
              navigator.clipboard?.writeText(message.content);
              toast("Answer copied");
            }}
          />
          <IconButton icon="refresh" label="Ask again" size="sm" onClick={onRegenerate} />
        </div>
      </div>
    </div>
  );
}

/**
 * The answer as it is being written, before it has been stored.
 *
 * The caret is the honest signal that more is coming; sources are deliberately absent, because which
 * passages the answer cites is not known until it has finished.
 */
export function StreamingMessage({ text }: { text: string }) {
  return (
    <div className="flex gap-3">
      <span className="tile tile-brand size-[30px] text-lg">
        <Icon name="sparkles" />
      </span>

      <div className="min-w-0 flex-1">
        <p className="mb-1.5 eyebrow">DocuMind AI</p>

        <div className="card px-[18px] py-4">
          <p className="m-0 text-lead leading-[1.7] whitespace-pre-wrap text-ink">
            {text}
            <span className="font-semibold text-brand animate-caret">▍</span>
          </p>
        </div>
      </div>
    </div>
  );
}

/**
 * Shown between sending a question and the first word of the answer — the wait while passages are
 * retrieved and the model starts writing.
 */
export function ThinkingIndicator() {
  return (
    <div className="flex gap-3 animate-fade-in">
      <span className="tile tile-brand size-[30px] text-lg animate-pulse-dot">
        <Icon name="sparkles" />
      </span>
      <div className="card flex items-center gap-2.5 px-[18px] py-3.5 text-small text-muted">
        <Icon name="loader" spinning className="text-base text-brand" />
        Searching the document…
      </div>
    </div>
  );
}
