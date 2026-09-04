import { cn } from "@/lib/cn";

export function Progress({
  value,
  className,
  trackClassName,
}: {
  /** Percentage 0–100. */
  value: number;
  className?: string;
  trackClassName?: string;
}) {
  return (
    <div className={cn("progress-track h-1.5", trackClassName, className)}>
      <div className="progress-fill" style={{ width: `${Math.min(100, Math.max(0, value))}%` }} />
    </div>
  );
}
