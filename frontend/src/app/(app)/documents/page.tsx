"use client";

import { useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/Button";
import { SearchInput, Select } from "@/components/ui/Field";
import { SegmentedControl } from "@/components/ui/Tabs";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRows } from "@/components/ui/Skeleton";
import { Icon } from "@/components/ui/Icon";
import { DocumentRow } from "@/components/documents/DocumentRow";
import { UploadDialog } from "@/components/documents/UploadDialog";
import {
  useGetApiDocuments,
  useDeleteApiDocumentsId,
  getGetApiDocumentsQueryKey,
} from "@/lib/api/generated/documents/documents";
import { matchesFilter, shouldPoll, STATUS_FILTERS } from "@/lib/documentStatus";
import { ApiError } from "@/lib/api/client";
import type { DocumentResponse } from "@/lib/api/model";
import { useToast } from "@/components/ui/Toast";

/** How often to re-check while anything is still being processed. */
const POLL_MS = 3000;

export default function DocumentsPage() {
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState<string>("All");
  const [sort, setSort] = useState("newest");
  const [uploadOpen, setUploadOpen] = useState(false);

  const queryClient = useQueryClient();
  const toast = useToast();

  const documents = useGetApiDocuments({
    query: {
      // Polls only while something is actually in flight, then stops on its own.
      refetchInterval: (query) =>
        query.state.data?.some(shouldPoll) ? POLL_MS : false,
    },
  });

  const remove = useDeleteApiDocumentsId({
    mutation: {
      onSuccess: () => {
        toast("Document deleted");
        queryClient.invalidateQueries({ queryKey: getGetApiDocumentsQueryKey() });
      },
      onError: (error) =>
        toast(error instanceof ApiError ? error.message : "Could not delete the document", "warn"),
    },
  });

  const visible = useMemo(() => {
    const needle = query.trim().toLowerCase();

    const matched = (documents.data ?? []).filter(
      (doc) =>
        matchesFilter(doc.status, filter) &&
        (!needle || doc.fileName.toLowerCase().includes(needle)),
    );

    if (sort === "name") {
      return [...matched].sort((a, b) => a.fileName.localeCompare(b.fileName));
    }

    if (sort === "oldest") {
      return [...matched].sort((a, b) => a.createdAt.localeCompare(b.createdAt));
    }

    return [...matched].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  }, [documents.data, query, filter, sort]);

  const hasAny = (documents.data ?? []).length > 0;

  return (
    <div className="page">
      <div className="flex flex-wrap items-start justify-between gap-3.5">
        <div>
          <h2 className="page-title">Documents</h2>
          <p className="page-subtitle">Manage the knowledge available to your AI assistant.</p>
        </div>
        <Button icon="upload" onClick={() => setUploadOpen(true)}>
          Upload Document
        </Button>
      </div>

      <div className="mt-5 flex flex-wrap items-center gap-2.5">
        <SearchInput
          variant="outlined"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          aria-label="Search documents"
          placeholder="Search documents…"
          className="min-w-[200px] flex-1"
        />
        <SegmentedControl options={STATUS_FILTERS} active={filter} onChange={setFilter} />
        <Select label="Sort documents" value={sort} onChange={(e) => setSort(e.target.value)}>
          <option value="newest">Newest</option>
          <option value="oldest">Oldest</option>
          <option value="name">Name</option>
        </Select>
      </div>

      {documents.isPending && (
        <div className="card mt-4 p-[18px]">
          <SkeletonRows count={5} />
        </div>
      )}

      {documents.isError && (
        <div className="alert-danger mt-4" role="alert">
          <Icon name="alert" className="text-md text-danger" />
          <p className="alert-danger-text">
            {documents.error instanceof ApiError
              ? documents.error.message
              : "Could not load your documents."}
          </p>
          <Button variant="secondary" size="sm" onClick={() => documents.refetch()}>
            Try again
          </Button>
        </div>
      )}

      {documents.isSuccess && !hasAny && (
        <div className="mt-4">
          <EmptyState
            icon="fileText"
            title="No documents yet"
            body="Upload your first document to start building your knowledge base."
            action={<Button onClick={() => setUploadOpen(true)}>Upload Document</Button>}
          />
        </div>
      )}

      {documents.isSuccess && hasAny && visible.length === 0 && (
        <div className="mt-4">
          <EmptyState
            icon="search"
            title="No matches"
            body="No documents match your search and filters."
            action={
              <Button
                variant="secondary"
                onClick={() => {
                  setQuery("");
                  setFilter("All");
                }}
              >
                Clear filters
              </Button>
            }
          />
        </div>
      )}

      {documents.isSuccess && visible.length > 0 && (
        <div className="card mt-4 overflow-hidden">
          <div className="flex items-center gap-3.5 border-b border-line bg-canvas px-[18px] py-3 eyebrow">
            <span className="flex-1">Document</span>
            <span className="hidden w-[110px] sm:block">Status</span>
            <span className="hidden w-[110px] lg:block">Uploaded</span>
            <span className="w-[104px] text-right">Actions</span>
          </div>

          {visible.map((doc: DocumentResponse) => (
            <DocumentRow
              key={doc.id}
              doc={doc}
              isDeleting={remove.isPending && remove.variables?.id === doc.id}
              onDelete={(target) => remove.mutate({ id: target.id })}
            />
          ))}
        </div>
      )}

      <UploadDialog open={uploadOpen} onClose={() => setUploadOpen(false)} />
    </div>
  );
}
