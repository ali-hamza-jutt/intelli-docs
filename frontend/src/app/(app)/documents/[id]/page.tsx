"use client";

import { use } from "react";
import Link from "next/link";
import { notFound, useRouter } from "next/navigation";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { IconButton } from "@/components/ui/IconButton";
import { StatusBadge } from "@/components/ui/Badge";
import { Icon } from "@/components/ui/Icon";
import { Tile } from "@/components/ui/Tile";
import { DOCUMENTS, DOCUMENT_ACTIONS, DOCUMENT_PREVIEW, STATUS_LABELS } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";

export default function DocumentDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const doc = DOCUMENTS.find((d) => d.id === id);
  const router = useRouter();
  const toast = useToast();

  if (!doc) notFound();

  const meta: [string, string][] = [
    ["File type", doc.type],
    ["Size", doc.size],
    ["Uploaded", doc.date],
    ["Chunks", String(doc.chunks)],
    ["Status", STATUS_LABELS[doc.status]],
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
          <Tile icon="fileText" tone="brand" className="size-11 rounded-[11px] text-xl" />
          <div className="min-w-0">
            <h2 className="m-0 truncate text-xl font-bold tracking-[-0.02em]">{doc.name}</h2>
            <StatusBadge status={doc.status} className="mt-1.5" />
          </div>
        </div>

        <div className="flex gap-2">
          <Button icon="message" onClick={() => router.push("/chat")}>
            Chat with document
          </Button>
          <IconButton
            icon="download"
            label="Download"
            tone="bordered"
            onClick={() => toast("Download started")}
          />
          <IconButton
            icon="trash"
            label="Delete"
            tone="bordered"
            className="hover:border-danger-border hover:bg-danger-soft hover:text-danger"
            onClick={() => {
              toast("Document deleted");
              router.push("/documents");
            }}
          />
        </div>
      </div>

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
            <h3 className="mb-3 eyebrow">Preview</h3>
            <div className="max-h-[320px] overflow-auto pr-1.5 text-body leading-[1.75] text-ink-soft">
              {DOCUMENT_PREVIEW.map((block, i) => (
                <p
                  key={i}
                  className={block.heading ? "mb-3 font-semibold text-ink" : "mb-3 last:mb-0"}
                >
                  {block.text}
                </p>
              ))}
            </div>
          </Card>
        </div>

        <Card>
          <div className="flex items-center gap-2">
            <Icon name="sparkles" className="text-lg text-brand" />
            <h3 className="card-title">Ask about this document</h3>
          </div>
          <p className="mt-2 mb-4 text-small leading-[1.55] text-muted">
            Start a conversation scoped to this file.
          </p>
          <div className="flex flex-col gap-2">
            {DOCUMENT_ACTIONS.map((action) => (
              <button
                key={action.label}
                onClick={() => router.push("/chat")}
                className="flex cursor-pointer items-center gap-2.5 rounded-control border border-line bg-surface px-3.5 py-3 text-left text-body font-medium transition-colors hover:border-brand-border hover:bg-brand-soft hover:text-brand"
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
