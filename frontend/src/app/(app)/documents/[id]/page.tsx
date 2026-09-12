"use client";

import { use } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { IconButton } from "@/components/ui/IconButton";
import { StatusBadge } from "@/components/ui/Badge";
import { Icon } from "@/components/ui/Icon";
import { Tile } from "@/components/ui/Tile";
import { Skeleton } from "@/components/ui/Skeleton";
import {
  useGetApiDocumentsId,
  useDeleteApiDocumentsId,
  getGetApiDocumentsQueryKey,
} from "@/lib/api/generated/documents/documents";
import { API_BASE_URL, ApiError } from "@/lib/api/client";
import { formatBytes, formatDate, fileTypeOf } from "@/lib/format";
import { isInProgress, presentStatus, shouldPoll } from "@/lib/documentStatus";
import { DOCUMENT_ACTIONS } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";

const POLL_MS = 3000;

export default function DocumentDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const queryClient = useQueryClient();
  const toast = useToast();

  const document = useGetApiDocumentsId(id, {
    query: {
      // Keeps the status chip live while the document is still being ingested.
      refetchInterval: (query) =>
        query.state.data && shouldPoll(query.state.data) ? POLL_MS : false,
    },
  });

  const remove = useDeleteApiDocumentsId({
    mutation: {
      onSuccess: () => {
        toast("Document deleted");
        queryClient.invalidateQueries({ queryKey: getGetApiDocumentsQueryKey() });
        router.push("/documents");
      },
    },
  });

  if (document.isPending) {
    return (
      <div className="page pt-7">
        <Skeleton className="h-4 w-32" />
        <Skeleton className="mt-6 h-8 w-80" />
        <div className="mt-6 grid gap-4 [grid-template-columns:repeat(auto-fit,minmax(280px,1fr))]">
          <Card>
            <Skeleton className="h-4 w-24" />
            <Skeleton className="mt-4 h-16 w-full" />
          </Card>
          <Card>
            <Skeleton className="h-4 w-40" />
            <Skeleton className="mt-4 h-24 w-full" />
          </Card>
        </div>
      </div>
    );
  }

  if (document.isError) {
    const notFound = document.error instanceof ApiError && document.error.status === 404;

    return (
      <div className="page pt-7">
        <Link href="/documents" className="link-action mb-6 inline-block">
          ← All documents
        </Link>
        <Card className="px-6 py-16 text-center">
          <span className="tile tile-neutral mx-auto size-12 rounded-[13px] text-[22px]">
            <Icon name={notFound ? "fileText" : "alert"} />
          </span>
          <p className="mt-[18px] mb-1.5 text-lg font-semibold">
            {notFound ? "Document not found" : "Could not load this document"}
          </p>
          <p className="mb-5 text-base text-muted">
            {notFound
              ? "It may have been deleted, or it belongs to another account."
              : "Something went wrong fetching this document."}
          </p>
          <Button onClick={() => router.push("/documents")}>Back to documents</Button>
        </Card>
      </div>
    );
  }

  const doc = document.data;
  const status = presentStatus(doc.status);

  const meta: [string, string][] = [
    ["File type", fileTypeOf(doc.originalFileName)],
    ["Size", formatBytes(doc.fileSize)],
    ["Uploaded", formatDate(doc.createdAt)],
    ["Status", status.label],
    ["Processed", doc.processedAt ? formatDate(doc.processedAt) : "—"],
  ];

  return (
    <div className="page pt-7">
      <Link
        href="/documents"
        className="mb-4 inline-flex items-center gap-2 text-small font-medium text-muted hover:text-ink"
      >
        <Icon name="arrowRight" className="rotate-180 text-base" />
        All documents
      </Link>

      <div className="flex flex-wrap items-center justify-between gap-3.5">
        <div className="flex min-w-0 items-center gap-3.5">
          <Tile
            icon="fileText"
            tone={doc.status === "Failed" ? "danger" : "brand"}
            className="size-11 rounded-[11px] text-xl"
          />
          <div className="min-w-0">
            <h2 className="m-0 truncate text-xl font-bold tracking-[-0.02em]">{doc.fileName}</h2>
            <StatusBadge status={doc.status} className="mt-1.5" />
          </div>
        </div>

        <div className="flex gap-2">
          <Button
            icon="message"
            disabled={doc.status !== "Completed"}
            title={
              doc.status === "Completed"
                ? undefined
                : "Available once processing finishes"
            }
            onClick={() => router.push("/chat")}
          >
            Chat with document
          </Button>
          <a
            href={`${API_BASE_URL}/api/Documents/${doc.id}/download`}
            className="icon-btn icon-btn-md icon-btn-bordered"
            aria-label="Download"
            title="Download"
          >
            <Icon name="download" />
          </a>
          <IconButton
            icon="trash"
            label="Delete"
            tone="bordered"
            disabled={remove.isPending}
            className="hover:border-danger-border hover:bg-danger-soft hover:text-danger"
            onClick={() => remove.mutate({ id: doc.id })}
          />
        </div>
      </div>

      {doc.status === "Failed" && (
        <div className="alert-danger mt-5" role="alert">
          <Icon name="alert" className="text-md text-danger" />
          <p className="alert-danger-text">
            {doc.errorMessage ?? "Processing failed for this document."}
          </p>
        </div>
      )}

      <div className="mt-6 grid items-start gap-4 [grid-template-columns:repeat(auto-fit,minmax(280px,1fr))]">
        <div className="flex flex-col gap-4">
          <Card>
            <h3 className="mb-3.5 eyebrow">Metadata</h3>
            <div className="grid gap-3.5 [grid-template-columns:repeat(auto-fit,minmax(120px,1fr))]">
              {meta.map(([label, value]) => (
                <div key={label}>
                  <p className="m-0 text-tiny text-subtle">{label}</p>
                  <p className="mt-0.5 text-body font-semibold">{value}</p>
                </div>
              ))}
            </div>
          </Card>

          <Card>
            <h3 className="mb-3 eyebrow">Extracted text</h3>
            <div className="rounded-control bg-canvas px-4 py-6 text-center">
              <Icon name="sparkles" className="mx-auto text-xl text-subtle" />
              <p className="mt-2 mb-0 text-body text-muted">
                {isInProgress(doc.status)
                  ? "Text will appear here once the document has been processed."
                  : "Text extraction arrives in the next module."}
              </p>
            </div>
          </Card>
        </div>

        <Card>
          <div className="flex items-center gap-2">
            <Icon name="sparkles" className="text-lg text-brand" />
            <h3 className="card-title">Ask about this document</h3>
          </div>
          <p className="mt-2 mb-4 text-small leading-[1.55] text-muted">
            {doc.status === "Completed"
              ? "Start a conversation scoped to this file."
              : "Available once this document has finished processing."}
          </p>
          <div className="flex flex-col gap-2">
            {DOCUMENT_ACTIONS.map((action) => (
              <button
                key={action.label}
                disabled={doc.status !== "Completed"}
                onClick={() => router.push("/chat")}
                className="flex cursor-pointer items-center gap-2.5 rounded-control border border-line bg-surface px-3.5 py-3 text-left text-body font-medium transition-colors hover:border-brand-border hover:bg-brand-soft hover:text-brand disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:border-line disabled:hover:bg-surface disabled:hover:text-ink"
              >
                <Icon name={action.icon} className="text-md text-subtle" />
                {action.label}
              </button>
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
}
