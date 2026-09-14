"use client";

import { useState } from "react";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { SkeletonRows } from "@/components/ui/Skeleton";
import { cn } from "@/lib/cn";
import { useGetApiDocumentsIdText } from "@/lib/api/generated/documents/documents";
import { ApiError } from "@/lib/api/client";
import { isInProgress } from "@/lib/documentStatus";

/**
 * The extracted-text preview on the document detail screen.
 *
 * Pages are shown individually rather than as one blob: the page number is the thing that makes a
 * citation verifiable later, so it earns a place in the UI now.
 */
export function ExtractedTextCard({
  documentId,
  status,
}: {
  documentId: string;
  status: string;
}) {
  const [expanded, setExpanded] = useState(false);

  const text = useGetApiDocumentsIdText(documentId, {
    query: {
      // Nothing exists until processing finishes, and a 404 before then is expected rather
      // than an error worth retrying.
      enabled: status === "Completed",
      retry: false,
    },
  });

  const pending = isInProgress(status);

  return (
    <Card>
      <div className="mb-3 flex items-center justify-between gap-3">
        <h3 className="eyebrow">Extracted text</h3>

        {text.isSuccess && (
          <span className="text-tiny text-subtle tabular-nums">
            {text.data.pageCount} {text.data.pageCount === 1 ? "page" : "pages"} ·{" "}
            {text.data.wordCount.toLocaleString()} words
          </span>
        )}
      </div>

      {pending && (
        <div className="rounded-control bg-canvas px-4 py-6 text-center">
          <Icon name="loader" className="mx-auto text-xl text-brand" spinning />
          <p className="mt-2 mb-0 text-body text-muted">
            Reading this document. The text will appear here when it finishes.
          </p>
        </div>
      )}

      {status === "Failed" && (
        <div className="rounded-control bg-canvas px-4 py-6 text-center">
          <Icon name="alert" className="mx-auto text-xl text-danger" />
          <p className="mt-2 mb-0 text-body text-muted">
            No text was extracted, so there is nothing to preview.
          </p>
        </div>
      )}

      {status === "Completed" && text.isPending && <SkeletonRows count={5} />}

      {status === "Completed" && text.isError && (
        <div className="rounded-control bg-canvas px-4 py-6 text-center">
          <Icon name="alert" className="mx-auto text-xl text-subtle" />
          <p className="mt-2 mb-0 text-body text-muted">
            {text.error instanceof ApiError && text.error.status === 404
              ? "No extracted text was found for this document."
              : "Could not load the extracted text."}
          </p>
        </div>
      )}

      {text.isSuccess && (
        <>
          <div
            className={cn(
              "overflow-auto pr-1.5 text-body leading-[1.75] text-ink-soft",
              expanded ? "max-h-[640px]" : "max-h-[320px]",
            )}
          >
            {text.data.pages.map((page) => (
              <section key={page.pageNumber} className="mb-5 last:mb-0">
                <p className="mb-1.5 eyebrow">Page {page.pageNumber}</p>
                {/* whitespace-pre-wrap keeps the paragraph breaks the cleaner preserved. */}
                <p className="m-0 whitespace-pre-wrap">{page.text}</p>
              </section>
            ))}
          </div>

          {text.data.pages.length > 1 && (
            <Button
              variant="secondary"
              size="sm"
              className="mt-3"
              onClick={() => setExpanded((value) => !value)}
            >
              {expanded ? "Show less" : "Show more"}
            </Button>
          )}
        </>
      )}
    </Card>
  );
}
