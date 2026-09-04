"use client";

import Link from "next/link";
import { StatusBadge } from "@/components/ui/Badge";
import { IconButton, IconButtonLink } from "@/components/ui/IconButton";
import { Icon } from "@/components/ui/Icon";
import { Progress } from "@/components/ui/Progress";
import { DocumentTile } from "./DocumentTile";
import type { Doc } from "@/lib/data";

/** One row of the documents table, including its processing and error sub-rows. */
export function DocumentRow({
  doc,
  onDelete,
  onRetry,
}: {
  doc: Doc;
  onDelete: (doc: Doc) => void;
  onRetry: (doc: Doc) => void;
}) {
  return (
    <div className="border-b border-line-soft last:border-b-0">
      <div className="flex items-center gap-3.5 px-[18px] py-3.5 transition-colors hover:bg-canvas">
        <span className="flex min-w-0 flex-1 items-center gap-3">
          <DocumentTile status={doc.status} className="size-[34px] text-md" />
          <span className="min-w-0">
            <Link
              href={`/documents/${doc.id}`}
              className="block truncate text-body font-semibold hover:text-brand"
            >
              {doc.name}
            </Link>
            <span className="block text-tiny text-subtle">
              {doc.type} · {doc.size}
            </span>
          </span>
        </span>

        <span className="hidden w-[110px] sm:block">
          <StatusBadge status={doc.status} />
        </span>
        <span className="hidden w-[90px] text-small text-muted lg:block">
          {doc.chunks ? `${doc.chunks} chunks` : "—"}
        </span>
        <span className="hidden w-[110px] text-small text-muted lg:block">{doc.date}</span>

        <span className="flex w-[104px] justify-end gap-1">
          <IconButtonLink href={`/documents/${doc.id}`} icon="external" label="Open document" size="sm" />
          <IconButtonLink href="/chat" icon="message" label="Chat with document" size="sm" />
          <IconButton icon="trash" label="Delete document" size="sm" tone="danger" onClick={() => onDelete(doc)} />
        </span>
      </div>

      {doc.status === "processing" && (
        <div className="pr-[18px] pb-3.5 pl-16">
          <Progress value={68} className="max-w-[320px]" />
          <p className="mt-1.5 text-tiny text-muted">Generating embeddings — 68%</p>
        </div>
      )}

      {doc.status === "failed" && (
        <div className="alert-danger mx-[18px] mb-3.5 ml-16">
          <Icon name="alert" className="text-md text-danger" />
          <p className="alert-danger-text">Something went wrong while processing this document.</p>
          <button
            onClick={() => onRetry(doc)}
            className="btn btn-sm rounded-control border border-danger-border bg-surface text-danger hover:bg-danger-softer"
          >
            Retry
          </button>
          <button
            onClick={() => onDelete(doc)}
            className="btn btn-sm rounded-control text-danger-strong hover:bg-danger-softer"
          >
            Delete
          </button>
        </div>
      )}
    </div>
  );
}
