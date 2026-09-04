"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui/Button";
import { SearchInput, Select } from "@/components/ui/Field";
import { SegmentedControl } from "@/components/ui/Tabs";
import { EmptyState } from "@/components/ui/EmptyState";
import { DocumentRow } from "@/components/documents/DocumentRow";
import { UploadDialog } from "@/components/documents/UploadDialog";
import { DOCUMENTS, STATUS_LABELS, type Doc } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";

const FILTERS = ["All", "Processing", "Ready", "Failed"] as const;

export default function DocumentsPage() {
  const [docs, setDocs] = useState<Doc[]>(DOCUMENTS);
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState<string>("All");
  const [sort, setSort] = useState("newest");
  const [uploadOpen, setUploadOpen] = useState(false);
  const toast = useToast();

  const visible = useMemo(() => {
    const q = query.trim().toLowerCase();
    const matched = docs.filter(
      (d) =>
        (filter === "All" || STATUS_LABELS[d.status] === filter) &&
        (!q || d.name.toLowerCase().includes(q)),
    );
    if (sort === "name") return [...matched].sort((a, b) => a.name.localeCompare(b.name));
    if (sort === "oldest") return [...matched].reverse();
    return matched;
  }, [docs, query, filter, sort]);

  const remove = (doc: Doc) => {
    setDocs((prev) => prev.filter((d) => d.id !== doc.id));
    toast("Document deleted");
  };

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
        <SegmentedControl options={FILTERS} active={filter} onChange={setFilter} />
        <Select label="Sort documents" value={sort} onChange={(e) => setSort(e.target.value)}>
          <option value="newest">Newest</option>
          <option value="oldest">Oldest</option>
          <option value="name">Name</option>
        </Select>
      </div>

      {visible.length === 0 ? (
        <div className="mt-4">
          <EmptyState
            icon="fileText"
            title="No documents yet"
            body="Upload your first document to start building your knowledge base."
            action={<Button onClick={() => setUploadOpen(true)}>Upload Document</Button>}
          />
        </div>
      ) : (
        <div className="card mt-4 overflow-hidden">
          <div className="flex items-center gap-3.5 border-b border-line bg-canvas px-[18px] py-3 eyebrow">
            <span className="flex-1">Document</span>
            <span className="hidden w-[110px] sm:block">Status</span>
            <span className="hidden w-[90px] lg:block">Chunks</span>
            <span className="hidden w-[110px] lg:block">Uploaded</span>
            <span className="w-[104px] text-right">Actions</span>
          </div>

          {visible.map((doc) => (
            <DocumentRow
              key={doc.id}
              doc={doc}
              onDelete={remove}
              onRetry={(d) => toast("Reprocessing " + d.name)}
            />
          ))}
        </div>
      )}

      <UploadDialog open={uploadOpen} onClose={() => setUploadOpen(false)} />
    </div>
  );
}
