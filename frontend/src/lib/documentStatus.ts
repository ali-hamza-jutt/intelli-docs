import type { IconName } from "@/components/ui/Icon";

/** The status values the API returns, mirroring the DocumentStatus enum. */
export type DocumentStatusValue = "Uploaded" | "Processing" | "Completed" | "Failed";

type StatusPresentation = {
  label: string;
  badgeClass: string;
  icon: IconName;
  spinning: boolean;
  /** Whether the frontend should keep polling this document's status. */
  inProgress: boolean;
};

const PRESENTATION: Record<DocumentStatusValue, StatusPresentation> = {
  Uploaded: {
    label: "Queued",
    badgeClass: "badge-info",
    icon: "clock",
    spinning: false,
    inProgress: true,
  },
  Processing: {
    label: "Processing",
    badgeClass: "badge-info",
    icon: "loader",
    spinning: true,
    inProgress: true,
  },
  Completed: {
    label: "Ready",
    badgeClass: "badge-success",
    icon: "check",
    spinning: false,
    inProgress: false,
  },
  Failed: {
    label: "Failed",
    badgeClass: "badge-danger",
    icon: "alert",
    spinning: false,
    inProgress: false,
  },
};

/** Falls back to the Failed presentation for an unrecognised value rather than throwing. */
export function presentStatus(status: string): StatusPresentation {
  return PRESENTATION[status as DocumentStatusValue] ?? PRESENTATION.Failed;
}

/** True while a document is still moving through the pipeline. */
export function isInProgress(status: string): boolean {
  return presentStatus(status).inProgress;
}

/**
 * How long to keep polling a document that has not settled. A document still unfinished after
 * this long is stuck — the worker died, or nothing has picked it up — and continuing to poll
 * would hammer the API forever without ever changing the answer.
 */
const POLL_WINDOW_MS = 10 * 60 * 1000;

/** Whether a document is worth polling: still in progress, and recent enough to expect a change. */
export function shouldPoll(doc: { status: string; createdAt: string }): boolean {
  if (!isInProgress(doc.status)) return false;

  const age = Date.now() - new Date(doc.createdAt).getTime();
  return age < POLL_WINDOW_MS;
}

/** The filter chips on the documents screen, mapped to API values. */
export const STATUS_FILTERS = ["All", "Queued", "Processing", "Ready", "Failed"] as const;

export function matchesFilter(status: string, filter: string): boolean {
  return filter === "All" || presentStatus(status).label === filter;
}
