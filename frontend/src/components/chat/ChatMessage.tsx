"use client";

import { Icon } from "@/components/ui/Icon";
import { IconButton } from "@/components/ui/IconButton";
import { SOURCES } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";

export type ChatTurn = {
  id: string;
  role: "user" | "assistant";
  text: string;
  sources?: number[];
  done?: boolean;
};

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
  onOpenSource,
  onRegenerate,
}: {
  turn: ChatTurn;
  onOpenSource: (index: number) => void;
  onRegenerate: () => void;
}) {
  const toast = useToast();
  const streaming = !turn.done;

  return (
    <div className="flex gap-3">
      <span className="tile tile-brand size-[30px] text-lg">
        <Icon name="sparkles" />
      </span>

      <div className="min-w-0 flex-1">
        <p className="mb-1.5 eyebrow">DocuMind AI</p>

        <div className="card px-[18px] py-4">
          <p className="m-0 text-lead leading-[1.7] text-ink">
            {turn.text}
            {streaming && <span className="font-semibold text-brand animate-caret">▍</span>}
          </p>

          {turn.done && turn.sources && turn.sources.length > 0 && (
            <div className="mt-4 border-t border-line-soft pt-3.5">
              <p className="mb-2.5 eyebrow">Sources</p>
              <div className="flex flex-wrap gap-2">
                {turn.sources.map((index) => (
                  <button
                    key={index}
                    onClick={() => onOpenSource(index)}
                    className="inline-flex cursor-pointer items-center gap-2 rounded-field border border-line bg-canvas px-3 py-2 text-caption transition-colors hover:border-brand-border hover:bg-brand-soft"
                  >
                    <Icon name="quote" className="text-small text-brand" />
                    <span className="font-semibold">{SOURCES[index].doc}</span>
                    <span className="text-subtle">{SOURCES[index].page}</span>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>

        {turn.done && (
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
            <IconButton icon="refresh" label="Regenerate" size="sm" onClick={onRegenerate} />
            <IconButton icon="thumbUp" label="Helpful" size="sm" onClick={() => toast("Thanks for the feedback")} />
            <IconButton icon="thumbDown" label="Not helpful" size="sm" onClick={() => toast("Thanks for the feedback")} />
          </div>
        )}
      </div>
    </div>
  );
}

/** Step-by-step retrieval readout shown while an answer is being prepared. */
export function ThinkingIndicator({ step }: { step: number }) {
  return (
    <div className="flex gap-3 animate-fade-in">
      <span className="tile tile-brand size-[30px] text-lg animate-pulse-dot">
        <Icon name="sparkles" />
      </span>
      <div className="card flex flex-col gap-1.5 px-[18px] py-3.5">
        {["Searching your knowledge…", "Finding relevant documents…", "Reviewing sources…", "Generating answer…"].map(
          (label, i) => {
            const done = i < step;
            const active = i === step;
            return (
              <div
                key={label}
                className={`flex items-center gap-2.5 text-small ${
                  done ? "text-muted" : active ? "text-ink" : "text-faint"
                }`}
              >
                <span
                  className={`inline-flex size-3.5 items-center justify-center text-micro ${
                    done ? "text-success" : active ? "text-brand" : "text-line"
                  }`}
                >
                  <Icon name={done ? "check" : active ? "loader" : "clock"} spinning={active} />
                </span>
                {label}
              </div>
            );
          },
        )}
      </div>
    </div>
  );
}
