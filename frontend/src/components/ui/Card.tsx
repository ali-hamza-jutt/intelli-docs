import { cn } from "@/lib/cn";

export function Card({
  children,
  className,
  padded = true,
  hoverable = false,
}: {
  children: React.ReactNode;
  className?: string;
  padded?: boolean;
  hoverable?: boolean;
}) {
  return (
    <div className={cn("card", padded && "card-pad", hoverable && "card-hoverable", className)}>
      {children}
    </div>
  );
}

/** A card whose content is a list — header strip on top, rows flush to the edges. */
export function ListCard({
  title,
  action,
  children,
  className,
}: {
  title: string;
  action?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <section className={cn("card overflow-hidden", className)}>
      <div className="card-header">
        <h3 className="card-title">{title}</h3>
        {action}
      </div>
      {children}
    </section>
  );
}
