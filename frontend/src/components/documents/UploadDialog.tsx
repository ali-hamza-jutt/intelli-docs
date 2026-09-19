"use client";

import { useRef, useState } from "react";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { Progress } from "@/components/ui/Progress";
import { Tile } from "@/components/ui/Tile";
import { cn } from "@/lib/cn";
import { formatBytes } from "@/lib/format";
import { ApiError } from "@/lib/api/client";
import { useUploadDocument } from "@/lib/api/useUploadDocument";
import type { DocumentResponse } from "@/lib/api/model";
import { UploadedPanel } from "./UploadedPanel";
import { useToast } from "@/components/ui/Toast";

const MAX_BYTES = 20 * 1024 * 1024;
const ACCEPTED = ".pdf";

type Phase = "idle" | "uploading" | "done" | "error";

/**
 * The upload flow itself. Mounted only while the dialog is open, so reopening always starts
 * from `idle` without an effect to reset it.
 */
function UploadFlow({ onClose }: { onClose: () => void }) {
  const [phase, setPhase] = useState<Phase>("idle");
  const [file, setFile] = useState<File | null>(null);
  const [uploaded, setUploaded] = useState<DocumentResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [dragging, setDragging] = useState(false);

  const inputRef = useRef<HTMLInputElement>(null);
  const { upload, cancel, progress } = useUploadDocument();
  const toast = useToast();

  /** Cheap client-side checks. The API repeats them — this only saves a round trip. */
  const reject = (candidate: File): string | null => {
    if (!candidate.name.toLowerCase().endsWith(ACCEPTED)) {
      return "Only PDF files are supported.";
    }
    if (candidate.size === 0) {
      return "That file is empty.";
    }
    if (candidate.size > MAX_BYTES) {
      return `That file is ${formatBytes(candidate.size)}; the limit is 20 MB.`;
    }
    return null;
  };

  const start = async (candidate: File) => {
    const problem = reject(candidate);

    if (problem) {
      setFile(candidate);
      setError(problem);
      setPhase("error");
      return;
    }

    setFile(candidate);
    setError(null);
    setPhase("uploading");

    try {
      const document = await upload(candidate);
      setUploaded(document);
      setPhase("done");
      toast("Upload complete — processing started");
    } catch (cause) {
      if (cause instanceof ApiError && cause.errorCode === "UPLOAD_CANCELLED") {
        setPhase("idle");
        setFile(null);
        return;
      }

      setError(cause instanceof ApiError ? cause.message : "Upload failed. Please try again.");
      setPhase("error");
    }
  };

  const pickFrom = (list: FileList | null) => {
    const candidate = list?.[0];
    if (candidate) void start(candidate);
  };

  if (phase === "uploading" && file) {
    return (
      <>
        <div className="rounded-xl border border-line p-4">
          <div className="flex items-center gap-3">
            <Tile icon="fileText" tone="brand" className="size-[34px] text-lg" />
            <div className="min-w-0 flex-1">
              <p className="m-0 truncate text-base font-semibold">{file.name}</p>
              <p className="mt-0.5 text-tiny text-subtle">{formatBytes(file.size)}</p>
            </div>
            <span className="text-small font-semibold text-brand tabular-nums">{progress}%</span>
          </div>
          <Progress value={progress} className="mt-3.5" />
        </div>

        <p className="mt-4 text-center text-small text-muted">
          {progress < 100 ? "Uploading…" : "Finishing up…"}
        </p>

        <div className="mt-4 flex justify-center">
          <Button variant="secondary" size="sm" onClick={cancel}>
            Cancel upload
          </Button>
        </div>
      </>
    );
  }

  if (phase === "done" && uploaded) {
    return <UploadedPanel document={uploaded} onClose={onClose} />;
  }

  return (
    <>
      {phase === "error" && error && (
        <div className="alert-danger mb-4" role="alert">
          <Icon name="alert" className="text-md text-danger" />
          <p className="alert-danger-text">{error}</p>
        </div>
      )}

      <div
        onDragOver={(e) => {
          e.preventDefault();
          setDragging(true);
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={(e) => {
          e.preventDefault();
          setDragging(false);
          pickFrom(e.dataTransfer.files);
        }}
        className={cn(
          "rounded-card border-[1.5px] border-dashed px-6 py-10 text-center transition-colors",
          dragging ? "border-brand bg-brand-soft" : "border-faint bg-canvas hover:border-brand",
        )}
      >
        <span className="tile mx-auto size-11 rounded-xl border border-line bg-surface text-xl text-brand">
          <Icon name="upload" />
        </span>
        <p className="mt-4 mb-1 text-md font-semibold">
          {dragging ? "Drop to upload" : "Drop your document here"}
        </p>
        <p className="mb-[18px] text-small text-muted">PDF up to 20MB</p>

        <input
          ref={inputRef}
          type="file"
          accept={ACCEPTED}
          className="sr-only"
          onChange={(e) => {
            pickFrom(e.target.files);
            // Allows re-picking the same file after an error.
            e.target.value = "";
          }}
        />

        <Button variant="secondary" onClick={() => inputRef.current?.click()}>
          {phase === "error" ? "Choose another file" : "Browse Files"}
        </Button>
      </div>
    </>
  );
}

export function UploadDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Upload document"
      subtitle="Add knowledge for your assistant to use."
    >
      <UploadFlow onClose={onClose} />
    </Modal>
  );
}
