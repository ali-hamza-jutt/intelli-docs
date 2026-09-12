import Link from "next/link";
import { StatusBadge } from "@/components/ui/Badge";
import { DocumentTile } from "./DocumentTile";
import { formatDate, fileTypeOf } from "@/lib/format";
import type { DocumentResponse } from "@/lib/api/model";

/** Compact document row used inside the dashboard's "Recent documents" card. */
export function DocumentListItem({ doc }: { doc: DocumentResponse }) {
  return (
    <Link
      href={`/documents/${doc.id}`}
      className="flex w-full items-center gap-3 border-b border-line-soft px-[18px] py-3.5 text-left transition-colors last:border-b-0 hover:bg-canvas"
    >
      <DocumentTile status={doc.status} className="size-8 text-md" />
      <span className="min-w-0 flex-1">
        <span className="block truncate text-body font-medium">{doc.fileName}</span>
        <span className="block text-tiny text-subtle">
          {fileTypeOf(doc.originalFileName)} · {formatDate(doc.createdAt)}
        </span>
      </span>
      <StatusBadge status={doc.status} />
    </Link>
  );
}
