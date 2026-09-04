import { cn } from "@/lib/cn";

const SIZES = {
  sm: "size-7 text-tiny",
  md: "size-9 text-tiny",
  lg: "size-14 text-lg",
};

export function Avatar({
  initials,
  size = "md",
  className,
}: {
  initials: string;
  size?: keyof typeof SIZES;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "inline-flex flex-none items-center justify-center rounded-full bg-brand-softer font-bold text-brand",
        SIZES[size],
        className,
      )}
    >
      {initials}
    </span>
  );
}
