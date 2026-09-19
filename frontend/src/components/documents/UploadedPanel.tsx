"use client";

import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { useGetApiDocumentsIdStatus } from "@/lib/api/generated/documents/documents";
import { isInProgress, presentStatus } from "@/lib/documentStatus";
import { formatBytes } from "@/lib/format";
import type { DocumentResponse } from "@/lib/api/model";

const POLL_MS = 1500;

/**
 * Shown after the bytes have landed. The document is queued, not ready, so this polls until
 * processing settles rather than declaring success the moment the upload finishes.
 *
 * Polls faster than the documents list because the user is watching this one directly.
 */
export function UploadedPanel({
  document,
  onClose,
}: {
  document: DocumentResponse;
  onClose: () => void;
}) {
  const router = useRouter();

  const status = useGetApiDocumentsIdStatus(document.id, {
    query: {
      refetchInterval: (query) =>
        query.state.data && isInProgress(query.state.data.status) ? POLL_MS : false,
      // The dialog opens straight after a 202, so start from what the upload returned.
      initialData: {
        id: document.id,
        status: document.status,
        errorMessage: document.errorMessage,
        processedAt: document.processedAt,
      },
    },
  });

  const current = status.data?.status ?? document.status;
  const { label } = presentStatus(current);
  const working = isInProgress(current);
  const failed = current === "Failed";

  return (
    <div className="px-2 py-6 text-center animate-fade-up">
      <span
        className={`inline-flex size-[46px] items-center justify-center rounded-full text-[22px] ${
          failed
            ? "bg-danger-soft text-danger"
            : working
              ? "bg-brand-soft text-brand"
              : "bg-success-soft text-success"
        }`}
      >
        <Icon
          name={failed ? "alert" : working ? "loader" : "check"}
          spinning={working}
        />
      </span>

      <p className="mt-4 mb-1 text-lg font-semibold">{document.fileName}</p>

      <p className="m-0 text-body text-muted">
        {failed
          ? (status.data?.errorMessage ?? "Processing failed.")
          : working
            ? `${formatBytes(document.fileSize)} · ${label.toLowerCase()}…`
            : `${formatBytes(document.fileSize)} · ready to search.`}
      </p>

      {working && (
        <p className="mt-2 mb-0 text-tiny text-subtle">
          You can close this — processing continues in the background.
        </p>
      )}

      <div className="mt-[22px] flex justify-center gap-2.5">
        <Button variant="secondary" onClick={onClose}>
          Done
        </Button>
        <Button
          onClick={() => {
            onClose();
            router.push(`/documents/${document.id}`);
          }}
        >
          View document
        </Button>
      </div>
    </div>
  );
}
