import { cn } from "@/lib/cn";

const ROW_WIDTHS = ["80%", "62%", "90%", "54%", "72%"];

export function Skeleton({ className, style }: { className?: string; style?: React.CSSProperties }) {
  return <div className={cn("skeleton-bar", className)} style={style} />;
}

/** Staggered-width placeholder rows, used while lists load. */
export function SkeletonRows({ count = 5, className }: { count?: number; className?: string }) {
  return (
    <div className={cn("flex flex-col gap-3.5", className)}>
      {ROW_WIDTHS.slice(0, count).map((width, i) => (
        <Skeleton key={i} className="h-4" style={{ width }} />
      ))}
    </div>
  );
}
