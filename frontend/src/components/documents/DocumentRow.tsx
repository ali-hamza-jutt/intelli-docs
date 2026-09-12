"use client";

import Link from "next/link";
import { StatusBadge } from "@/components/ui/Badge";
import { IconButton, IconButtonLink } from "@/components/ui/IconButton";
import { Icon } from "@/components/ui/Icon";
import { DocumentTile } from "./DocumentTile";
import { formatBytes, formatDate, fileTypeOf } from "@/lib/format";
import { API_BASE_URL } from "@/lib/api/client";
import type { DocumentResponse } from "@/lib/api/model";

/** One row of the documents table, including its error sub-row. */
export function DocumentRow({
  doc,
  onDelete,
  isDeleting,
}: {
  doc: DocumentResponse;
  onDelete: (doc: DocumentResponse) => void;
  isDeleting?: boolean;
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
              {doc.fileName}
            </Link>
            <span className="block text-tiny text-subtle">
              {fileTypeOf(doc.originalFileName)} · {formatBytes(doc.fileSize)}
            </span>
          </span>
        </span>

        <span className="hidden w-[110px] sm:block">
          <StatusBadge status={doc.status} />
        </span>
        <span className="hidden w-[110px] text-small text-muted lg:block">
          {formatDate(doc.createdAt)}
        </span>

        <span className="flex w-[104px] justify-end gap-1">
          <IconButtonLink href={`/documents/${doc.id}`} icon="external" label="Open document" size="sm" />
          <a
            href={`${API_BASE_URL}/api/Documents/${doc.id}/download`}
            className="icon-btn icon-btn-sm icon-btn-plain"
            aria-label="Download document"
            title="Download"
          >
            <Icon name="download" />
          </a>
          <IconButton
            icon="trash"
            label="Delete document"
            size="sm"
            tone="danger"
            disabled={isDeleting}
            onClick={() => onDelete(doc)}
          />
        </span>
      </div>

      {doc.status === "Failed" && (
        <div className="alert-danger mx-[18px] mb-3.5 ml-16">
          <Icon name="alert" className="text-md text-danger" />
          <p className="alert-danger-text">
            {doc.errorMessage ?? "Something went wrong while processing this document."}
          </p>
        </div>
      )}
    </div>
  );
}
