"use client";

import { Icon } from "@/components/ui/Icon";
import { IconButton } from "@/components/ui/IconButton";
import { Select } from "@/components/ui/Field";
import type { DocumentResponse } from "@/lib/api/model";

/**
 * Multi-line input with attach, the document being asked about, and send.
 *
 * The document select is not a duplicate of the left rail: that rail is hidden on narrow screens,
 * so this is the only way to choose a document on a phone.
 */
export function ChatComposer({
  value,
  onChange,
  onSubmit,
  onAttach,
  pending,
  documents,
  documentId,
  documentName,
  locked,
  onDocumentChange,
}: {
  value: string;
  onChange: (value: string) => void;
  onSubmit: () => void;
  onAttach: () => void;
  pending: boolean;
  documents: DocumentResponse[];
  documentId: string | null;
  /** The open conversation's document, or null when it has been deleted. */
  documentName?: string | null;
  /** Inside a conversation the document is fixed, so it is shown rather than offered. */
  locked: boolean;
  onDocumentChange: (id: string) => void;
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
            aria-label="Ask a question about this document"
            placeholder={
              documentId ? "Ask about this document…" : "Choose a document, then ask…"
            }
            className="w-full resize-none border-0 bg-transparent px-1 pt-1 pb-2 text-lead leading-[1.55] outline-none placeholder:text-subtle"
          />

          <div className="flex items-center gap-2">
            <IconButton icon="paperclip" label="Attach document" onClick={onAttach} />

            {locked ? (
              <span
                title={documentName ?? undefined}
                className="flex min-w-0 items-center gap-1.5 rounded-field bg-canvas px-2.5 py-1.5 text-caption text-muted"
              >
                <Icon name="fileText" className="flex-none text-small text-subtle" />
                <span className="truncate">{documentName ?? "Document deleted"}</span>
              </span>
            ) : (
              <Select
                label="Document to ask about"
                value={documentId ?? ""}
                onChange={(e) => onDocumentChange(e.target.value)}
                className="max-w-[200px] px-2.5 py-1.5 text-caption text-muted"
              >
                <option value="" disabled>
                  Choose a document
                </option>
                {documents.map((document) => (
                  <option key={document.id} value={document.id}>
                    {document.fileName}
                  </option>
                ))}
              </Select>
            )}

            <span className="flex-1" />

            <span className="hidden text-[11.5px] text-faint lg:inline">
              Enter to send · Shift+Enter for newline
            </span>

            <button
              type="submit"
              aria-label={pending ? "Answering" : "Send"}
              disabled={pending || !documentId}
              className="btn btn-primary inline-flex size-9 items-center justify-center p-0 text-lg disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Icon name={pending ? "loader" : "send"} spinning={pending} />
            </button>
          </div>
        </div>
      </form>
    </div>
  );
}
