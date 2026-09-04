"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { Progress } from "@/components/ui/Progress";
import { Tile } from "@/components/ui/Tile";
import { cn } from "@/lib/cn";
import { UPLOAD_STEPS } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";

const STEP_MS = 620;
const SAMPLE = { name: "Employee Handbook.pdf", size: "2.4 MB", chunks: 124 };

type Phase = "idle" | "running" | "done";

/**
 * The flow itself. Mounted only while the dialog is open, so reopening starts
 * from `idle` without needing an effect to reset it.
 */
function UploadFlow({ onClose }: { onClose: () => void }) {
  const [phase, setPhase] = useState<Phase>("idle");
  const [step, setStep] = useState(0);
  const router = useRouter();
  const toast = useToast();

  useEffect(() => {
    if (phase !== "running") return;

    const timer = setTimeout(() => {
      if (step >= UPLOAD_STEPS.length - 1) {
        setPhase("done");
        toast("Document uploaded");
      } else {
        setStep((s) => s + 1);
      }
    }, STEP_MS);

    return () => clearTimeout(timer);
  }, [phase, step, toast]);

  const percent = Math.round(((step + 1) / UPLOAD_STEPS.length) * 100);

  if (phase === "idle") {
    return (
      <div className="rounded-card border-[1.5px] border-dashed border-faint bg-canvas px-6 py-10 text-center transition-colors hover:border-brand hover:bg-brand-soft">
        <span className="tile mx-auto size-11 rounded-xl border border-line bg-surface text-xl text-brand">
          <Icon name="upload" />
        </span>
        <p className="mt-4 mb-1 text-md font-semibold">Drop your documents here</p>
        <p className="mb-[18px] text-small text-muted">PDF, DOCX, TXT up to 20MB</p>
        <Button variant="secondary" onClick={() => setPhase("running")}>
          Browse Files
        </Button>
      </div>
    );
  }

  if (phase === "running") {
    return (
      <>
        <div className="rounded-xl border border-line p-4">
          <div className="flex items-center gap-3">
            <Tile icon="fileText" tone="brand" className="size-[34px] text-lg" />
            <div className="min-w-0 flex-1">
              <p className="m-0 truncate text-base font-semibold">{SAMPLE.name}</p>
              <p className="mt-0.5 text-tiny text-subtle">{SAMPLE.size}</p>
            </div>
            <span className="text-small font-semibold text-brand">{percent}%</span>
          </div>
          <Progress value={percent} className="mt-3.5" />
        </div>

        <ol className="mt-[18px] flex list-none flex-col gap-0.5 p-0">
          {UPLOAD_STEPS.map((label, i) => {
            const done = i < step;
            const active = i === step;
            return (
              <li
                key={label}
                className={cn(
                  "flex items-center gap-2.5 px-1 py-1.5 text-body transition-colors",
                  done ? "text-ink" : active ? "text-brand" : "text-subtle",
                )}
              >
                <span
                  className={cn(
                    "inline-flex size-4 flex-none items-center justify-center text-small",
                    done ? "text-success" : active ? "text-brand" : "text-faint",
                  )}
                >
                  <Icon name={done ? "check" : active ? "loader" : "clock"} spinning={active} />
                </span>
                {label}
              </li>
            );
          })}
        </ol>
      </>
    );
  }

  return (
    <div className="px-2 py-6 text-center animate-fade-up">
      <span className="inline-flex size-[46px] items-center justify-center rounded-full bg-success-soft text-[22px] text-success">
        <Icon name="check" />
      </span>
      <p className="mt-4 mb-1 text-lg font-semibold">{SAMPLE.name} is ready</p>
      <p className="m-0 text-body text-muted">
        {SAMPLE.chunks} knowledge chunks indexed. You can ask about it now.
      </p>
      <div className="mt-[22px] flex justify-center gap-2.5">
        <Button variant="secondary" onClick={onClose}>
          Done
        </Button>
        <Button
          onClick={() => {
            onClose();
            router.push("/chat");
          }}
        >
          Chat with document
        </Button>
      </div>
    </div>
  );
}

/** Upload flow: drop zone, then a stepped progress readout, then a success panel. */
export function UploadDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Upload documents"
      subtitle="Add knowledge for your assistant to use."
    >
      <UploadFlow onClose={onClose} />
    </Modal>
  );
}
