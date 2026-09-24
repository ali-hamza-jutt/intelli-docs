"use client";

import { useState } from "react";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { SkeletonRows } from "@/components/ui/Skeleton";
import { useGetApiDocumentsIdChunks } from "@/lib/api/generated/documents/documents";
import type { DocumentChunkResponse } from "@/lib/api/model";

/** Chunks are up to ~1,000 characters each, so ten is a comfortable screenful. */
export const CHUNK_PAGE_SIZE = 10;

function pageLabel(chunk: DocumentChunkResponse) {
  return chunk.pageNumber === chunk.endPageNumber
    ? `Page ${chunk.pageNumber}`
    : `Pages ${chunk.pageNumber}–${chunk.endPageNumber}`;
}

/**
 * Shows how a document was split for retrieval.
 *
 * The highlighted opening of each chunk is the overlap — text repeated from the end of the chunk
 * before it. Seeing the boundaries is the quickest way to judge whether the chunk size suits a
 * document: passages cut mid-thought mean the size is too small, walls of loosely related text
 * mean it is too large.
 */
export function ChunksCard({ documentId }: { documentId: string }) {
  const [offset, setOffset] = useState(0);

  const chunks = useGetApiDocumentsIdChunks(
    documentId,
    { offset, limit: CHUNK_PAGE_SIZE },
    // Keep the current page on screen while the next one loads, rather than flashing a skeleton.
    { query: { placeholderData: (previous) => previous } },
  );

  const total = chunks.data?.totalCount ?? 0;
  const first = offset + 1;
  const last = Math.min(offset + CHUNK_PAGE_SIZE, total);

  return (
    <Card>
      <div className="mb-1 flex flex-wrap items-center justify-between gap-2">
        <h3 className="eyebrow">Chunks</h3>

        {chunks.data && (
          <span className="text-tiny text-subtle tabular-nums">
            {total} {total === 1 ? "chunk" : "chunks"} · {chunks.data.chunkSize.toLocaleString()}{" "}
            chars, {chunks.data.chunkOverlap} overlap
          </span>
        )}
      </div>

      <p className="mt-0 mb-4 text-small text-muted">
        Each chunk is searched on its own when you ask a question. Highlighted text repeats the end
        of the previous chunk.
      </p>

      {chunks.isPending && <SkeletonRows count={4} />}

      {chunks.isError && (
        <div className="rounded-control bg-canvas px-4 py-6 text-center">
          <Icon name="alert" className="mx-auto text-xl text-subtle" />
          <p className="mt-2 mb-0 text-body text-muted">Could not load the chunks.</p>
        </div>
      )}

      {chunks.data && total === 0 && (
        <p className="m-0 rounded-control bg-canvas px-4 py-6 text-center text-body text-muted">
          This document has no chunks.
        </p>
      )}

      {chunks.data && total > 0 && (
        <>
          <ol className="m-0 flex list-none flex-col gap-3 p-0">
            {chunks.data.chunks.map((chunk) => (
              <li key={chunk.index} className="rounded-control border border-line-soft">
                <div className="flex flex-wrap items-center gap-x-3 gap-y-1 border-b border-line-soft px-3 py-2 text-tiny text-subtle tabular-nums">
                  <span className="font-semibold text-ink-soft">#{chunk.index + 1}</span>
                  <span>{pageLabel(chunk)}</span>
                  <span>{chunk.characterCount.toLocaleString()} chars</span>
                  <span>~{chunk.tokenEstimate} tokens</span>
                  {chunk.overlapWithPrevious > 0 && (
                    <span>{chunk.overlapWithPrevious} overlap</span>
                  )}
                </div>

                {/* whitespace-pre-wrap keeps the paragraph breaks the chunker joined segments with. */}
                <p className="m-0 max-h-48 overflow-auto px-3 py-2.5 text-small leading-relaxed whitespace-pre-wrap text-ink-soft">
                  {chunk.overlapWithPrevious > 0 && (
                    <mark className="rounded-sm bg-warning-soft text-ink">
                      {chunk.text.slice(0, chunk.overlapWithPrevious)}
                    </mark>
                  )}
                  {chunk.text.slice(chunk.overlapWithPrevious)}
                </p>
              </li>
            ))}
          </ol>

          {total > CHUNK_PAGE_SIZE && (
            <div className="mt-4 flex items-center justify-between gap-3">
              <span className="text-tiny text-subtle tabular-nums">
                {first}–{last} of {total}
              </span>
              <div className="flex gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  disabled={offset === 0 || chunks.isFetching}
                  onClick={() => setOffset((value) => Math.max(0, value - CHUNK_PAGE_SIZE))}
                >
                  Previous
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  disabled={last >= total || chunks.isFetching}
                  onClick={() => setOffset((value) => value + CHUNK_PAGE_SIZE)}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </Card>
  );
}
