import { cn } from "@/lib/cn";
import { Icon, type IconName } from "./Icon";
import { STATUS_LABELS, type DocumentStatus } from "@/lib/data";

const STATUS_STYLES: Record<DocumentStatus, { className: string; icon: IconName }> = {
  ready: { className: "badge-success", icon: "check" },
  processing: { className: "badge-info", icon: "loader" },
  failed: { className: "badge-danger", icon: "alert" },
};

/** Status pill shared by the documents table, dashboard list and detail header. */
export function StatusBadge({ status, className }: { status: DocumentStatus; className?: string }) {
  const { className: tone, icon } = STATUS_STYLES[status];
  return (
    <span className={cn("badge", tone, className)}>
      <Icon name={icon} className="text-tiny" spinning={status === "processing"} />
      {STATUS_LABELS[status]}
    </span>
  );
}
