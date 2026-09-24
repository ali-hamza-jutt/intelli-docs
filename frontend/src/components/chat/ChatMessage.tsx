"use client";

import { Icon } from "@/components/ui/Icon";
import { IconButton } from "@/components/ui/IconButton";
import { useToast } from "@/components/ui/Toast";
import type { ChatCitationResponse } from "@/lib/api/model";

export type ChatTurn = {
  id: string;
  role: "user" | "assistant";
  text: string;
  citations?: ChatCitationResponse[];
  /** False when nothing in the document matched, so the answer is a refusal rather than a claim. */
  grounded?: boolean;
};

/** "Page 4" or "Pages 4–5", matching how the answer's citation reads. */
export function pageLabel(citation: ChatCitationResponse) {
  return citation.pageNumber === citation.endPageNumber
    ? `Page ${citation.pageNumber}`
    : `Pages ${citation.pageNumber}–${citation.endPageNumber}`;
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
  turn,
  onOpenCitation,
  onRegenerate,
}: {
  turn: ChatTurn;
  onOpenCitation: (citation: ChatCitationResponse) => void;
  onRegenerate: () => void;
}) {
  const toast = useToast();
  const citations = turn.citations ?? [];

  return (
    <div className="flex gap-3">
      <span className="tile tile-brand size-[30px] text-lg">
        <Icon name="sparkles" />
      </span>

      <div className="min-w-0 flex-1">
        <p className="mb-1.5 eyebrow">DocuMind AI</p>

        <div className="card px-[18px] py-4">
          {/* whitespace-pre-wrap keeps the paragraphs and lists the model writes. */}
          <p className="m-0 text-lead leading-[1.7] whitespace-pre-wrap text-ink">{turn.text}</p>

          {turn.grounded === false && (
            <p className="mt-3 mb-0 flex items-center gap-2 text-small text-subtle">
              <Icon name="alert" className="text-base" />
              Nothing in this document was close enough to the question, so no answer was written.
            </p>
          )}

          {citations.length > 0 && (
            <div className="mt-4 border-t border-line-soft pt-3.5">
              <p className="mb-2.5 eyebrow">Sources</p>
              <div className="flex flex-wrap gap-2">
                {citations.map((citation) => (
                  <button
                    key={citation.marker}
                    onClick={() => onOpenCitation(citation)}
                    className="inline-flex cursor-pointer items-center gap-2 rounded-field border border-line bg-canvas px-3 py-2 text-caption transition-colors hover:border-brand-border hover:bg-brand-soft"
                  >
                    {/* The marker matches the [n] in the answer above, so a claim can be traced. */}
                    <span className="font-semibold text-brand">[{citation.marker}]</span>
                    <span className="font-semibold">{citation.fileName}</span>
                    <span className="text-subtle">{pageLabel(citation)}</span>
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
              navigator.clipboard?.writeText(turn.text);
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
 * Shown while an answer is being prepared.
 *
 * One line rather than a checklist of stages: retrieval and generation happen in a single request,
 * so the client cannot honestly say which one is running.
 */
export function ThinkingIndicator() {
  return (
    <div className="flex gap-3 animate-fade-in">
      <span className="tile tile-brand size-[30px] text-lg animate-pulse-dot">
        <Icon name="sparkles" />
      </span>
      <div className="card flex items-center gap-2.5 px-[18px] py-3.5 text-small text-muted">
        <Icon name="loader" spinning className="text-base text-brand" />
        Searching the document and writing an answer…
      </div>
    </div>
  );
}
