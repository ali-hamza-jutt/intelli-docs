import { cn } from "@/lib/cn";
import { Icon } from "./Icon";
import { presentStatus } from "@/lib/documentStatus";

/** Status pill shared by the documents table, dashboard list and detail header. */
export function StatusBadge({ status, className }: { status: string; className?: string }) {
  const { label, badgeClass, icon, spinning } = presentStatus(status);

  return (
    <span className={cn("badge", badgeClass, className)}>
      <Icon name={icon} className="text-tiny" spinning={spinning} />
      {label}
    </span>
  );
}
